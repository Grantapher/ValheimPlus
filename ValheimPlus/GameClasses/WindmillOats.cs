using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ValheimPlus.Configurations;
using ValheimPlus.Configurations.Sections;

namespace ValheimPlus.GameClasses
{
    internal static class WindmillOats
    {
        // Item custom data travels with inventories through saves and ownership changes.
        // Count protected units rather than locking the whole stack: returned oats can merge
        // with fresh output without either losing their eligibility or unlocking that output.
        private const string ProtectedKey = "vplus.windmill.protectedOats";
        internal static bool IsOat(ItemDrop.ItemData item) => item?.m_shared?.m_name == "$item_oat";
        internal static bool StopAtOat => Configuration.Current.Windmill.IsEnabled &&
            Configuration.Current.Windmill.oatProcessing == OatProcessingMode.Oat;

        internal static int ProtectedCount(ItemDrop.ItemData item)
        {
            if (!IsOat(item) || !item.m_customData.TryGetValue(ProtectedKey, out var value) ||
                !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)) return 0;
            return Math.Max(0, Math.Min(item.m_stack, count));
        }

        internal static bool NeedsTracking(Inventory inventory, ItemDrop.ItemData item) => IsOat(item) &&
            (ProtectedCount(item) > 0 || inventory.GetAllItems().Any(existing => ProtectedCount(existing) > 0));

        internal static void Protect(ItemDrop.ItemData item, int count)
        {
            count = Math.Max(0, Math.Min(item.m_stack, count));
            if (count == 0) item.m_customData.Remove(ProtectedKey);
            else item.m_customData[ProtectedKey] = count.ToString(CultureInfo.InvariantCulture);
        }

        internal static int RemoveProcessableOats(Container chest, int amount, out bool cheated)
        {
            int removed = 0;
            cheated = false;
            var inventory = chest.GetInventory();
            foreach (var item in inventory.GetAllItems().ToArray())
            {
                if (!IsOat(item)) continue;
                int take = Math.Min(amount - removed, item.m_stack - ProtectedCount(item));
                if (take <= 0) continue;
                cheated |= item.m_cheated;
                inventory.RemoveItem(item, take);
                removed += take;
                if (removed == amount) break;
            }
            if (removed > 0) InventoryAssistant.ConveyContainerToNetwork(chest);
            return removed;
        }

        internal sealed class Addition
        {
            private readonly Inventory destination;
            private readonly ItemDrop.ItemData source;
            private readonly int count;
            private readonly int protection;
            private readonly Dictionary<ItemDrop.ItemData, (int Count, int Protection)> before;

            internal Addition(Inventory destination, ItemDrop.ItemData source)
            {
                this.destination = destination;
                this.source = source;
                count = source.m_stack;
                protection = ProtectedCount(source);
                before = destination.GetAllItems().Where(IsOat)
                    .ToDictionary(item => item, item => (item.m_stack, ProtectedCount(item)));
            }

            internal void Complete()
            {
                var additions = destination.GetAllItems().Where(IsOat)
                    .Select(item => (Item: item, Added: item.m_stack - (before.TryGetValue(item, out var old) ? old.Count : 0)))
                    .Where(entry => entry.Added > 0).ToArray();
                int moved = additions.Sum(entry => entry.Added);
                if (moved == 0) return;

                // Partial transfers retain protection in both inventories. Only a completed
                // whole-stack transfer to the player can release the transferred protection.
                int transferredProtection = Math.Min(protection, moved);
                if (count > moved) Protect(source, protection - transferredProtection);
                foreach (var entry in additions)
                {
                    int addedProtection = Math.Min(entry.Added, transferredProtection);
                    int oldProtection = before.TryGetValue(entry.Item, out var old) ? old.Protection : 0;
                    Protect(entry.Item, oldProtection + addedProtection);
                    Transfer.Current?.Record(destination, source, entry.Item, addedProtection);
                    transferredProtection -= addedProtection;
                }
                // Native AddItem has already notified the container; save the corrected metadata too.
                destination.Changed();
            }
        }

        internal sealed class Transfer : IDisposable
        {
            [ThreadStatic] internal static Transfer Current;
            private readonly Transfer previous;
            private readonly Inventory source;
            private readonly Inventory destination;
            private readonly Dictionary<ItemDrop.ItemData, int> originals;
            private readonly List<(ItemDrop.ItemData Source, ItemDrop.ItemData Destination, int Count)> additions = new();

            internal Transfer(Inventory source, Inventory destination)
            {
                this.source = source;
                this.destination = destination;
                originals = source.GetAllItems().Where(item => ProtectedCount(item) > 0)
                    .ToDictionary(item => item, ProtectedCount);
                previous = Current;
                Current = this;
            }

            internal void Record(Inventory target, ItemDrop.ItemData from, ItemDrop.ItemData to, int count)
            {
                if (target == destination && originals.ContainsKey(from) && count > 0)
                    additions.Add((from, to, count));
            }

            public void Dispose()
            {
                Current = previous;
                bool changed = false;
                foreach (var original in originals)
                {
                    if (source.ContainsItem(original.Key)) continue;
                    var moved = additions.Where(entry => entry.Source == original.Key).ToArray();
                    if (moved.Sum(entry => entry.Count) != original.Value) continue;
                    foreach (var entry in moved)
                    {
                        Protect(entry.Destination, ProtectedCount(entry.Destination) - entry.Count);
                        changed = true;
                    }
                }
                if (changed) destination.Changed();
            }
        }
    }

    // All three item-data overloads can merge stacks without copying their custom data.
    [HarmonyPatch]
    internal static class Inventory_AddItem_OatProtection_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods() => AccessTools.GetDeclaredMethods(typeof(Inventory))
            .Where(method => method.Name == nameof(Inventory.AddItem) &&
                method.GetParameters().FirstOrDefault()?.ParameterType == typeof(ItemDrop.ItemData));

        private static void Prefix(Inventory __instance, ItemDrop.ItemData item, out WindmillOats.Addition __state)
        {
            __state = WindmillOats.NeedsTracking(__instance, item) ? new WindmillOats.Addition(__instance, item) : null;
        }

        private static void Postfix(WindmillOats.Addition __state) => __state?.Complete();
    }

    [HarmonyPatch]
    internal static class Inventory_Transfer_OatProtection_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods() => AccessTools.GetDeclaredMethods(typeof(Inventory))
            .Where(method => method.Name == nameof(Inventory.MoveItemToThis) ||
                method.Name == nameof(Inventory.MoveAll) || method.Name == nameof(Inventory.StackAll));

        private static void Prefix(Inventory __instance, Inventory fromInventory, out WindmillOats.Transfer __state)
        {
            __state = Player.m_localPlayer && __instance == Player.m_localPlayer.GetInventory() &&
                fromInventory != __instance ? new WindmillOats.Transfer(fromInventory, __instance) : null;
        }

        private static void Finalizer(WindmillOats.Transfer __state) => __state?.Dispose();
    }
}

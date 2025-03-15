using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    using StackResponse = Container_RPC_StackResponse_Patch;

    /// <summary>
    /// Alters teleportation prevention
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.IsTeleportable))]
    // ReSharper disable once IdentifierTypo 
    public static class Inventory_IsTeleportable_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ref bool __result)
        {
            var config = Configuration.Current.Items;
            if (!config.IsEnabled || !config.noTeleportPrevention) return;
            __result = true;
        }
    }

    /// <summary>
    /// Makes all items fill inventories top to bottom instead of just tools and weapons
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.TopFirst))]
    public static class Inventory_TopFirst_Patch
    {
        [UsedImplicitly]
        public static void Postfix(ref bool __result)
        {
            var config = Configuration.Current.Inventory;
            if (!config.IsEnabled || !config.inventoryFillTopToBottom) return;
            __result = true;
        }
    }

    /// <summary>
    /// Configure player inventory size
    /// </summary>
    [HarmonyPatch(typeof(Inventory), MethodType.Constructor, typeof(string), typeof(Sprite), typeof(int), typeof(int))]
    public static class Inventory_Constructor_Patch
    {
        private const int PlayerInventoryMaxRows = 20;
        private const int PlayerInventoryMinRows = 4;

        [UsedImplicitly]
        public static void Prefix(string name, ref int w, ref int h)
        {
            if (!Configuration.Current.Inventory.IsEnabled) return;

            // Player inventory
            if (name is "Grave" or "Inventory")
            {
                h = Helper.Clamp(value: Configuration.Current.Inventory.playerInventoryRows,
                    min: PlayerInventoryMinRows,
                    max: PlayerInventoryMaxRows);
            }
        }
    }


    public static class Inventory_NearbyChests_Cache
    {
        public static List<Container> chests = new();
        public static readonly Stopwatch delta = new();
    }

    // TODO isn't this fully trumped by the stack all feature now?
    /// <summary>
    /// When merging another inventory, try to merge items with existing stacks.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), "MoveAll")]
    public static class Inventory_MoveAll_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ref Inventory __instance, ref Inventory fromInventory)
        {
            var config = Configuration.Current.Inventory;
            if (!config.IsEnabled || !config.mergeWithExistingStacks) return;

            var otherInventoryItems = new List<ItemDrop.ItemData>(fromInventory.GetAllItems());
            foreach (var otherItem in otherInventoryItems)
            {
                if (otherItem.m_shared.m_maxStackSize <= 1) continue;

                foreach (var myItem in __instance.m_inventory)
                {
                    if (myItem.m_shared.m_name != otherItem.m_shared.m_name || myItem.m_quality != otherItem.m_quality)
                        continue;

                    int itemsToMove = Math.Min(myItem.m_shared.m_maxStackSize - myItem.m_stack, otherItem.m_stack);
                    myItem.m_stack += itemsToMove;
                    if (otherItem.m_stack == itemsToMove)
                    {
                        fromInventory.RemoveItem(otherItem);
                        break;
                    }

                    otherItem.m_stack -= itemsToMove;
                }
            }
        }
    }

    /// <summary>
    /// StackAll will have one setup step when Inventory.StackAll() is called in Inventory_StackAll_Patch#Prefix.
    /// At the end of the prefix, we start the loop of dequeuing containers that we are stacking to.
    /// The loop will consist of:
    ///   Dequeue containers from the queue, skipping those that are the current inventory or all already in use.
    ///   Call StackAll on the container, which will fire off an RPC (RPC_RequestStack).
    ///   We eventually will receive an RPC_StackResponse result. If valid it will call
    ///     Inventory.StackAll(Inventory fromInventory, bool message) which actually does the stacking logic.
    ///   At the end of Container.RPC_StackResponse we apply a Postfix (Container_RPC_StackResponse_Patch#Postfix)
    ///     that will deque the next container. (now go back to the beginning of the loop)
    /// At the end of the loop, we will display a message of what we stacked and then reset the variables. 
    /// </summary>

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll))]
    public static class Inventory_StackAll_Patch
    {
        private static bool ShouldMessage = false;
        private static bool IsProcessing = false;
        private static int ItemsBefore = 0;

        private static async Task QueueStackAll(List<Container> chests, Inventory fromInventory, Inventory instance)
        {
            IsProcessing = true;
            var containerCount = 0;
            foreach (var container in chests)
            {
                if (container.IsInUse()) continue;

                var inventory = container.GetInventory();
                if (inventory == null || inventory == instance)
                    continue;

                StackResponse.ResponseReceived = new();

                // Will call Inventory.StackAll but force-exit because IsProcessing is true
                container.StackAll();
                containerCount += 1;

                // Container.StackAll requests ownership, wait for response
                await StackResponse.ResponseReceived.Task;
            }

            if (ShouldMessage)
            {
                // Show stack message
                var itemsAfter = fromInventory.CountItems(null);
                var count = ItemsBefore - itemsAfter;

                string message = count > 0
                    ? $"$msg_stackall {count} in {containerCount} Chests"
                    : $"$msg_stackall_none in {containerCount} Chests";
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, message);
            }

            IsProcessing = false;
        }

        private static void Prefix(Inventory fromInventory, ref bool message)
        {
            var config = Configuration.Current.AutoStack;
            if (!config.IsEnabled) return;

            if (!IsProcessing)
            {
                ShouldMessage = message;
                ItemsBefore = fromInventory.CountItems(null);
            }

            // disable message
            message = false;
        }

        /// <summary>
        /// Start the auto stack all loop and suppress stack feedback message
        /// </summary>
        [UsedImplicitly]
        private static void Postfix(Inventory fromInventory, Inventory __instance, ref int __result)
        {
            var config = Configuration.Current.AutoStack;
            if (!config.IsEnabled || IsProcessing) return;

            // get chests in range
            var nearbyChests = InventoryAssistant.GetNearbyChests(Player.m_localPlayer.gameObject,
                Mathf.Clamp(config.autoStackAllRange, 1, 50),
                !config.autoStackAllIgnorePrivateAreaCheck);

            QueueStackAll(nearbyChests, fromInventory, __instance);
        }

        private static readonly MethodInfo Method_Inventory_ContainsItemByName =
            AccessTools.Method(typeof(Inventory), nameof(Inventory.ContainsItemByName));

        private static readonly MethodInfo Method_ContainsItemByName =
            AccessTools.Method(typeof(Inventory_StackAll_Patch), nameof(ContainsItemByName));

        /// <summary>
        /// Replaces the game's Inventory.ContainsItemByName call with our own.
        /// Their method only checks for a match by name, where ours has an additional check for whether
        /// the item is equip-able.
        /// </summary>
        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var config = Configuration.Current.AutoStack;
            if (!config.IsEnabled) return instructions;
            if (!config.autoStackAllIgnoreEquipment && !config.ignoreFood && !config.ignoreAmmo && !config.ignoreMead)
                return instructions;

            var il = instructions.ToList();

            for (int i = 0; i < il.Count; ++i)
            {
                if (il[i].Calls(Method_Inventory_ContainsItemByName))
                {
                    il[i].operand = Method_ContainsItemByName;
                    return il.AsEnumerable();
                }
            }

            ValheimPlusPlugin.Logger.LogError("Could not transpile `Inventory.ContainsItemByName`!");
            return il.AsEnumerable();
        }

        public static bool ContainsItemByName(Inventory inventory, string name)
        {
            foreach (var item in inventory.m_inventory)
            {
                if (item.m_shared.m_name != name)
                    continue;

                if (Configuration.Current.AutoStack.ignoreAmmo && item.IsAmmo())
                    continue;

                if (Configuration.Current.AutoStack.ignoreFood && item.IsFood())
                    continue;

                if (Configuration.Current.AutoStack.ignoreMead && item.IsMead())
                    continue;

                if (Configuration.Current.AutoStack.autoStackAllIgnoreEquipment && item.IsEquipable())
                    continue;

                return true;
            }

            return false;
        }
    }
}
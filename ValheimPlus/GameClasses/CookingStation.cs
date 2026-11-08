using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;
using ValheimPlus.Utility;
using Object = UnityEngine.Object;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.FindCookableItem))]
    public static class CookingStation_FindCookableItem_Transpiler
    {
        private static List<Container> nearbyChests;

        private static readonly MethodInfo Method_PullCookableItemFromNearbyChests =
            AccessTools.Method(typeof(CookingStation_FindCookableItem_Transpiler),
                nameof(PullCookableItemFromNearbyChests));

        /// <summary>
        /// Patches out the code that looks for cookable items in player inventory.
        /// When not cookables items have been found in the player inventory, check inside nearby chests.
        /// If found, remove the item from the chests it was in,
        /// instantiate a game object and returns it so that it can be placed on the CookingStation.
        /// </summary>
        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var config = Configuration.Current.CraftFromChest;
            if (!config.IsEnabled || config.disableCookingStation) return instructions;

            var il = instructions.ToList();
            for (int i = 0; i < il.Count; i++)
            {
                if (il[i].opcode == OpCodes.Ldnull)
                {
                    il[i] = new CodeInstruction(OpCodes.Ldarg_0)
                    {
                        labels = il[i].labels
                    };
                    il.Insert(++i, new CodeInstruction(OpCodes.Call, Method_PullCookableItemFromNearbyChests));
                    il.Insert(++i, new CodeInstruction(OpCodes.Stloc_3));
                    il.Insert(++i, new CodeInstruction(OpCodes.Ldloc_3));
                    return il;
                }
            }

            PatchLog.Failed(
                nameof(CookingStation_FindCookableItem_Transpiler),
                "Cooking stations will not take food from nearby chests.");
            return il;
        }

        private static ItemDrop.ItemData PullCookableItemFromNearbyChests(CookingStation station)
        {
            if (station.GetFreeSlot() == -1) return null;

            var stopwatch = GameObjectAssistant.GetStopwatch(station.gameObject);
            int lookupInterval = Helper.Clamp(Configuration.Current.CraftFromChest.lookupInterval, 1, 10) * 1000;
            if (nearbyChests == null || !stopwatch.IsRunning || stopwatch.ElapsedMilliseconds > lookupInterval)
            {
                nearbyChests = InventoryAssistant.GetNearbyChests(station.gameObject,
                    Helper.Clamp(Configuration.Current.CraftFromChest.range, 1, 50),
                    !Configuration.Current.CraftFromChest.ignorePrivateAreaCheck);
                stopwatch.Restart();
            }

            foreach (var itemConversion in station.m_conversion)
            {
                var itemData = itemConversion.m_from.m_itemData;
                foreach (var container in nearbyChests)
                {
                    if (!container.GetInventory().HaveItem(itemData.m_shared.m_name)) continue;

                    // Remove one item from chest
                    InventoryAssistant.RemoveItemFromChest(container, itemData);
                    // Instantiate cookabled GameObject
                    var itemPrefab = ObjectDB.instance.GetItemPrefab(itemConversion.m_from.gameObject.name);

                    ZNetView.m_forceDisableInit = true;
                    var cookabledItem = Object.Instantiate(itemPrefab);
                    ZNetView.m_forceDisableInit = false;

                    return cookabledItem.GetComponent<ItemDrop>().m_itemData;
                }
            }

            return null;
        }
    }

    public static class CookingStationFuel
    {
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.Awake))]
        public static class CookingStation_Awake_Patch
        {
            /// <summary>
            /// Applies the Frost Foundry's speed and fuel settings before it starts.
            /// </summary>
            [UsedImplicitly]
            private static void Prefix(CookingStation __instance)
            {
                var config = Configuration.Current.FrostFoundry;
                if (!config.IsEnabled || !__instance.m_name.Equals(CookingStationDefinitions.FrostFoundryName)) return;

                __instance.m_maxFuel = config.maximumFrozenFuel;
                foreach (var conversion in __instance.m_conversion) conversion.m_cookTime = config.productionSpeed;

                // Fuel burns over time, so each one lasts its share of an item's cook time.
                int fuelPerProduct = Math.Max(1, config.frozenFuelUsedPerProduct);
                __instance.m_secPerFuel = Math.Max(1, (int)Math.Round(config.productionSpeed / fuelPerProduct));
            }

            /// <summary>
            /// When fire source is loaded in view, check for configurations and set its fuel to max fuel
            /// </summary>
            [UsedImplicitly]
            private static void Postfix(CookingStation __instance)
            {
                if (!__instance.m_useFuel || !InfiniteFuel(__instance.m_name) ||
                    __instance.m_nview?.IsValid() != true) return;
                __instance.SetFuel(__instance.m_maxFuel);
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UpdateFuel))]
        public static class CookingStation_UpdateFuel_Patch
        {
            // If stay-at-max-fuel mode, fuel is set to max in Awake,
            // so simply always pass a time delta of 0 so calculated fuel usage is also 0.
            [UsedImplicitly]
            private static void Prefix(CookingStation __instance, ref float dt)
            {
                if (!InfiniteFuel(__instance.m_name)) return;
                dt = 0f;
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UpdateCooking))]
        public static class CookingStation_UpdateCooking_Patch
        {
            // If the oven isn't lit, UpdateFuel will never be called,
            // so we prefix UpdateCooking to autoFuel if necessary.
            [UsedImplicitly]
            private static void Prefix(CookingStation __instance)
            {
                if (!__instance.m_useFuel || !AutoFuel(__instance.m_name) ||
                    __instance.m_nview?.IsValid() != true) return;

                // Only check every second:
                var stopwatch = GameObjectAssistant.GetStopwatch(__instance.gameObject);
                if (stopwatch.IsRunning && stopwatch.ElapsedMilliseconds < 1000) return;
                stopwatch.Restart();

                AddFuelFromNearbyChests(__instance);
            }
        }

        private static void AddFuelFromNearbyChests(CookingStation __instance)
        {
            // Find the integer of fuels to take us back to exactly m_maxFuel and no further.
            int toMaxFuel = __instance.m_maxFuel - (int)Math.Ceiling(__instance.GetFuel());
            if (toMaxFuel < 1) return;

            var fuelItemData = __instance.m_fuelItem.m_itemData;
            int addedFuel = InventoryAssistant.RemoveItemInAmountFromChests(
                InventoryAssistant.GetNearbyChestsForMachine(__instance.gameObject,
                    Helper.Clamp(AutoRange(__instance.m_name), 1, 50),
                    !IgnorePrivateAreaCheck(__instance.m_name)),
                fuelItemData, toMaxFuel);
            if (addedFuel < 1) return;

            for (int i = 0; i < addedFuel; i++) __instance.m_nview.InvokeRPC("RPC_AddFuel");
            ValheimPlusPlugin.Logger.LogDebug(
                $"Added {addedFuel} fuel({fuelItemData.m_shared.m_name}) in {__instance.m_name}");
        }

        private static bool InfiniteFuel(string name)
        {
            var config = Configuration.Current;
            if (name.Equals(CookingStationDefinitions.OvenName)) return config.Oven.IsEnabled && config.Oven.infiniteFuel;
            return false;
        }

        private static bool AutoFuel(string name)
        {
            var config = Configuration.Current;
            if (name.Equals(CookingStationDefinitions.OvenName)) return config.Oven.IsEnabled && config.Oven.autoFuel;
            if (name.Equals(CookingStationDefinitions.FrostFoundryName)) return config.FrostFoundry.IsEnabled && config.FrostFoundry.autoFuel;
            return false;
        }

        internal static float AutoRange(string name)
        {
            var config = Configuration.Current;
            if (name.Equals(CookingStationDefinitions.OvenName)) return config.Oven.autoRange;
            if (name.Equals(CookingStationDefinitions.FrostFoundryName)) return config.FrostFoundry.autoRange;
            return 0f;
        }

        internal static bool IgnorePrivateAreaCheck(string name)
        {
            var config = Configuration.Current;
            if (name.Equals(CookingStationDefinitions.OvenName)) return config.Oven.ignorePrivateAreaCheck;
            if (name.Equals(CookingStationDefinitions.FrostFoundryName)) return config.FrostFoundry.ignorePrivateAreaCheck;
            return false;
        }
    }

    /// <summary>
    /// autoDeposit and autoCook: cooking stations that empty finished food into nearby chests and refill
    /// themselves with raw food from them. Only the oven has settings for this so far; see AutoDeposit/AutoCook.
    /// </summary>
    public static class CookingStationAutomation
    {
        private const string UpdateCookingMethodName = nameof(CookingStation.UpdateCooking);
        private const int SingleItem = 1;

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.Awake))]
        public static class CookingStation_Awake_Transpiler
        {
            /// <summary>
            /// A station that finished cooking while unloaded is emptied on its first update, so hold that update
            /// until nearby chests have loaded. Otherwise the food would land on the ground.
            /// </summary>
            [UsedImplicitly]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var config = Configuration.Current.Oven;
                if (!config.IsEnabled || !config.autoDeposit) return instructions;

                return GameObjectAssistant.ReplaceInvokeRepeating(instructions, nameof(CookingStation_Awake_Transpiler),
                    UpdateCookingMethodName,
                    AccessTools.Method(typeof(CookingStation_Awake_Transpiler), nameof(InvokeRepeatingWhenChestsLoaded)));
            }

            private static void InvokeRepeatingWhenChestsLoaded(CookingStation station, string methodName, float delay,
                float rate)
            {
                float chestRange = AutoDeposit(station.m_name)
                    ? Helper.Clamp(CookingStationFuel.AutoRange(station.m_name), 1, 50)
                    : 0f;
                GameObjectAssistant.InvokeRepeatingWhenChestsLoaded(station, methodName, delay, rate, chestRange);
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UpdateCooking))]
        public static class CookingStation_UpdateCooking_Patch
        {
            /// <summary>
            /// After vanilla has advanced cooking, move finished food into nearby chests (autoDeposit),
            /// then fill free slots with raw food from them (autoCook).
            /// UpdateCooking already runs once a second, so this needs no throttle of its own, and it stays clear of
            /// the stopwatch that autoFuel and CraftFromChest use.
            /// Only the station's owner changes its slots, the same as vanilla, so each oven is handled by one peer.
            /// </summary>
            [UsedImplicitly]
            private static void Postfix(CookingStation __instance)
            {
                bool autoDeposit = AutoDeposit(__instance.m_name);
                bool autoCook = AutoCook(__instance.m_name);
                if (!autoDeposit && !autoCook) return;
                if (__instance.m_nview?.IsValid() != true || !__instance.m_nview.IsOwner()) return;

                bool hasDoneItem = autoDeposit && __instance.HaveDoneItem();
                // Like the smelter, raw food is loaded whenever there's room, lit or not; it can't burn while cold.
                bool canFillSlot = autoCook && __instance.GetFreeSlot() != -1;
                if (!hasDoneItem && !canFillSlot) return;

                List<Container> nearbyChests = InventoryAssistant.GetNearbyChestsForMachine(__instance.gameObject,
                    Helper.Clamp(CookingStationFuel.AutoRange(__instance.m_name), 1, 50),
                    !CookingStationFuel.IgnorePrivateAreaCheck(__instance.m_name));

                if (hasDoneItem) DepositDoneItems(__instance, nearbyChests);
                if (canFillSlot) AddCookableItemsFromChests(__instance, nearbyChests);
            }
        }

        /// <summary>
        /// Moves every done or burnt item into the nearest chest with room, or drops it at the station when none has.
        /// A slot is only cleared once its item is saved in a chest or spawned in the world, so nothing is lost.
        /// </summary>
        private static void DepositDoneItems(CookingStation station, List<Container> nearbyChests)
        {
            for (int slot = 0; slot < station.m_slots.Length; slot++)
            {
                station.GetSlot(slot, out string itemName, out _, out _, out bool slotCheated);
                // IsItemDone covers both cooked and burnt items, the same check vanilla uses when a player takes them.
                if (itemName == "" || !station.IsItemDone(itemName)) continue;

                // The same cheat rule the smelter and fermenter auto-deposit use.
                bool cheated = (slotCheated || station.m_nview.GetZDO().IsCheated()) &&
                               !PlayerProfile.s_bypassCheatChecks;

                GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
                if (!itemPrefab)
                {
                    // Leave the slot alone; vanilla handles unknown items itself.
                    ValheimPlusPlugin.Logger.LogWarning($"{station.m_name} could not find item prefab '{itemName}'.");
                    continue;
                }

                Vector3 effectPosition = station.m_slots[slot].position;
                if (!DepositInNearbyChest(station, itemPrefab, cheated, nearbyChests))
                {
                    DropAtStation(station, itemPrefab, slot, cheated);
                    effectPosition = DropPosition(station, slot);
                }

                station.SetSlot(slot, "", 0f, CookingStation.Status.NotDone, false);
                station.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", slot, "");
                station.m_pickEffector.Create(effectPosition, Quaternion.identity);
                ValheimPlusPlugin.Logger.LogDebug($"{station.m_name} auto-deposited {itemName}.");
            }
        }

        /// <summary>
        /// Puts one of itemPrefab into a nearby chest and saves it, preferring chests that already hold it.
        /// Returns false, having changed nothing, when no chest has room.
        /// </summary>
        private static bool DepositInNearbyChest(CookingStation station, GameObject itemPrefab, bool cheated,
            List<Container> nearbyChests)
        {
            if (nearbyChests.Count == 0) return false;

            ZNetView.m_forceDisableInit = true;
            GameObject itemObject = Object.Instantiate(itemPrefab);
            ZNetView.m_forceDisableInit = false;

            try
            {
                ItemDrop itemDrop = itemObject.GetComponent<ItemDrop>();
                // Stamp the world level and cheat flag as vanilla's SpawnItem does, so the chest copy matches what
                // the station would have dropped.
                ItemDrop.OnCreateNew(itemDrop, cheated);
                ItemDrop.ItemData item = itemDrop.m_itemData;
                if (station.m_spawnFullDurability) item.m_durability = item.m_shared.m_maxDurability;

                return AddToChest(item, nearbyChests, mustHaveItem: true) ||
                       AddToChest(item, nearbyChests, mustHaveItem: false);
            }
            finally
            {
                Object.Destroy(itemObject);
            }
        }

        private static bool AddToChest(ItemDrop.ItemData item, List<Container> chests, bool mustHaveItem)
        {
            foreach (Container chest in chests)
            {
                // Someone has this chest open; leave it to them rather than race their changes.
                if (chest.IsInUse()) continue;

                Inventory inventory = chest.GetInventory();
                if (mustHaveItem && !inventory.HaveItem(item.m_shared.m_name)) continue;
                if (!inventory.CanAddItem(item) || !inventory.AddItem(item)) continue;

                InventoryAssistant.ConveyContainerToNetwork(chest);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Spawns the item where vanilla drops taken food, for when no nearby chest has room.
        /// </summary>
        private static void DropAtStation(CookingStation station, GameObject itemPrefab, int slot, bool cheated)
        {
            Vector3 position = DropPosition(station, slot);
            Vector3 direction = station.m_spawnPoint != null ? station.m_spawnPoint.forward : station.transform.forward;
            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);

            GameObject itemObject = Object.Instantiate(itemPrefab, position, rotation);
            ItemDrop itemDrop = itemObject.GetComponent<ItemDrop>();
            ItemDrop.OnCreateNew(itemDrop, cheated);
            if (station.m_spawnFullDurability && itemDrop)
                itemDrop.m_itemData.m_durability = itemDrop.m_itemData.m_shared.m_maxDurability;

            Rigidbody body = itemObject.GetComponent<Rigidbody>();
            if (body) body.linearVelocity = direction * station.m_spawnForce;
        }

        private static Vector3 DropPosition(CookingStation station, int slot) =>
            station.m_spawnPoint != null
                ? station.m_spawnPoint.position
                : station.m_slots[slot].position + station.transform.forward * 0.5f;

        /// <summary>
        /// Fills free slots with raw food from nearby chests, through vanilla's RPC_AddItem as if a player added it.
        /// The item is taken out of the chest (and saved) first, and put back if the station didn't take it.
        /// </summary>
        private static void AddCookableItemsFromChests(CookingStation station, List<Container> nearbyChests)
        {
            while (station.GetFreeSlot() != -1)
            {
                if (!TryTakeCookableItem(station, nearbyChests, out Container chest, out ItemDrop.ItemData item,
                        out string prefabName))
                    return;

                int freeSlot = station.GetFreeSlot();
                // Called directly rather than through InvokeRPC: this peer owns the station, and a direct call fills
                // the slot now, so a queued call can't find the slot taken and quietly drop the item.
                station.RPC_AddItem(ZDOMan.GetSessionID(), prefabName, item.m_cheated);

                station.GetSlot(freeSlot, out string addedName, out _, out _, out _);
                if (addedName != prefabName)
                {
                    ReturnToChest(station, chest, item, nearbyChests);
                    return;
                }

                ValheimPlusPlugin.Logger.LogDebug($"{station.m_name} auto-cooked {item.m_shared.m_name}.");
            }
        }

        /// <summary>
        /// Removes one item the station can cook from the nearest chest holding one.
        /// item is a copy of the removed item, with a stack of one.
        /// </summary>
        private static bool TryTakeCookableItem(CookingStation station, List<Container> nearbyChests,
            out Container chest, out ItemDrop.ItemData item, out string prefabName)
        {
            // Nearest chest first, taking whatever it holds that the station can cook, like the smelter's autoFuel.
            foreach (Container candidate in nearbyChests)
            {
                if (candidate.IsInUse()) continue;

                foreach (CookingStation.ItemConversion conversion in station.m_conversion)
                {
                    string itemName = conversion.m_from.m_itemData.m_shared.m_name;
                    ItemDrop.ItemData found = candidate.GetInventory().GetItem(itemName);
                    if (found == null) continue;

                    ItemDrop.ItemData copy = found.Clone();
                    copy.m_stack = SingleItem;
                    if (InventoryAssistant.RemoveItemFromChest(candidate, found, SingleItem) != SingleItem) continue;

                    chest = candidate;
                    item = copy;
                    prefabName = conversion.m_from.gameObject.name;
                    return true;
                }
            }

            chest = null;
            item = null;
            prefabName = null;
            return false;
        }

        // Only reached if RPC_AddItem refused an item it was checked to accept; puts it back so it isn't lost.
        private static void ReturnToChest(CookingStation station, Container chest, ItemDrop.ItemData item,
            List<Container> nearbyChests)
        {
            ValheimPlusPlugin.Logger.LogWarning($"{station.m_name} did not take {item.m_shared.m_name}; returning it.");
            Inventory inventory = chest.GetInventory();
            if (inventory.CanAddItem(item) && inventory.AddItem(item))
            {
                InventoryAssistant.ConveyContainerToNetwork(chest);
                return;
            }

            if (AddToChest(item, nearbyChests, mustHaveItem: false)) return;

            ItemDrop.DropItem(item, SingleItem, DropPosition(station, 0), Quaternion.identity);
        }

        private static bool AutoDeposit(string name)
        {
            var config = Configuration.Current.Oven;
            return name.Equals(CookingStationDefinitions.OvenName) && config.IsEnabled && config.autoDeposit;
        }

        private static bool AutoCook(string name)
        {
            var config = Configuration.Current.Oven;
            return name.Equals(CookingStationDefinitions.OvenName) && config.IsEnabled && config.autoCook;
        }
    }

    public static class CookingStationDefinitions
    {
        public static readonly string OvenName = "$piece_oven";
        public static readonly string FrostFoundryName = "$piece_frostfoundry";
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;
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

            ValheimPlusPlugin.Logger.LogError("Failed to apply CookingStation_FindCookableItem_Transpiler");
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
            /// When fire source is loaded in view, check for configurations and set its fuel to max fuel
            /// </summary>
            [UsedImplicitly]
            private static void Postfix(CookingStation __instance)
            {
                var config = Configuration.Current.Oven;
                if (!__instance.m_useFuel || !config.IsEnabled || !config.infiniteFuel ||
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
                var config = Configuration.Current.Oven;
                if (!config.IsEnabled || !config.infiniteFuel) return;
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
                var config = Configuration.Current.Oven;
                if (!__instance.m_useFuel || !config.IsEnabled || !config.autoFuel ||
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
            int addedFuel = InventoryAssistant.RemoveItemInAmountFromAllNearbyChests(__instance.gameObject,
                Helper.Clamp(Configuration.Current.Oven.autoRange, 1, 50), fuelItemData, toMaxFuel,
                !Configuration.Current.Oven.ignorePrivateAreaCheck);
            if (addedFuel < 1) return;

            for (int i = 0; i < addedFuel; i++) __instance.m_nview.InvokeRPC("RPC_AddFuel");
            ValheimPlusPlugin.Logger.LogInfo(
                $"Added {addedFuel} fuel({fuelItemData.m_shared.m_name}) in {__instance.m_name}");
        }
    }
}
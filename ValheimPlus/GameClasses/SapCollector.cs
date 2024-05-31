using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    public static class SapCollectorDeposit
    {
        /// <summary>
        /// Apply SapCollector class changes
        /// </summary>
        [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.Awake))]
        public static class SapCollector_Awake_Patch
        {
            [UsedImplicitly]
            private static void Prefix(ref float ___m_secPerUnit, ref int ___m_maxLevel)
            {
                var config = Configuration.Current.SapCollector;
                if (!config.IsEnabled) return;
                ___m_secPerUnit = config.sapProductionSpeed;
                ___m_maxLevel = config.maximumSapPerCollector;
            }
        }

        /// <summary>
        /// Altering the hover text to display the time until the next sap is produced
        /// </summary>
        [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.GetHoverText))]
        public static class SapCollector_GetHoverText_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SapCollector __instance, ref string __result)
            {
                var config = Configuration.Current.SapCollector;
                if (!config.IsEnabled || !config.showDuration || __instance.GetLevel() == __instance.m_maxLevel) return;

                int duration = (int)(__instance.m_secPerUnit - __instance.m_nview.GetZDO().GetFloat(ZDOVars.s_product));
                var info = duration >= 120 ? $"{duration / 60} minutes" : $"{duration} seconds";
                __result = __result.Replace(" )", " )\n(" + info + ")");
            }
        }

        /// <summary>
        /// Auto Deposit for SapCollectors
        /// </summary>
        [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.RPC_Extract))]
        public static class SapCollector_RPC_Extract_Patch
        {
            [UsedImplicitly]
            private static void Prefix(SapCollector __instance)
            {
                var config = Configuration.Current.SapCollector;
                if (!config.IsEnabled || !config.autoDeposit || __instance.GetLevel() <= 0) return;
                Deposit(__instance);
            }
        }

        [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.UpdateTick))]
        public static class SapCollector_UpdateTick_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SapCollector __instance)
            {
                var config = Configuration.Current.SapCollector;
                if (!config.IsEnabled || !config.autoDeposit || __instance.GetLevel() != __instance.m_maxLevel) return;
                Deposit(__instance);
            }
        }

        private static void Deposit(SapCollector __instance)
        {
            if (__instance.m_nview?.IsOwner() != true) return;

            // find nearby chests
            var nearbyChests = InventoryAssistant.GetNearbyChests(__instance.gameObject,
                Helper.Clamp(Configuration.Current.SapCollector.autoDepositRange, 1, 50));
            
            if (nearbyChests.Count == 0) return;

            var initialLevel = __instance.GetLevel();
            while (__instance.GetLevel() > 0)
            {
                var itemPrefab = ObjectDB.instance.GetItemPrefab(__instance.m_spawnItem.gameObject.name);

                ZNetView.m_forceDisableInit = true;
                var obj = Object.Instantiate(itemPrefab);
                ZNetView.m_forceDisableInit = false;

                var comp = obj.GetComponent<ItemDrop>();

                bool result = SpawnNearbyChest(comp, mustHaveItem: true, __instance, nearbyChests);
                Object.Destroy(obj);

                if (!result)
                {
                    // Couldn't drop in chest, letting original code handle things
                    return;
                }
            }

            if (__instance.GetLevel() < initialLevel)
                __instance.m_spawnEffect.Create(__instance.m_spawnPoint.position, Quaternion.identity);
        }

        private static bool SpawnNearbyChest(ItemDrop item, bool mustHaveItem, SapCollector __instance,
            List<Container> nearbyChests)
        {
            foreach (var chest in nearbyChests)
            {
                var inventory = chest.GetInventory();
                if (mustHaveItem && !inventory.HaveItem(item.m_itemData.m_shared.m_name))
                    continue;

                if (!inventory.AddItem(item.m_itemData))
                {
                    // Chest full, move to the next
                    continue;
                }

                __instance.m_nview.GetZDO().Set("level", __instance.GetLevel() - 1);
                InventoryAssistant.ConveyContainerToNetwork(chest);
                return true;
            }

            return mustHaveItem && SpawnNearbyChest(item, mustHaveItem: false, __instance, nearbyChests);
        }
    }
}
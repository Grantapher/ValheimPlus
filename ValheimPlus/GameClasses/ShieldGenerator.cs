using System;
using HarmonyLib;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    public static class ShieldGeneratorFuel
    {
        [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.Start))]
        public static class ShieldGenerator_Start_Patch
        {
            private static void Postfix(ShieldGenerator __instance)
            {
                if (!Configuration.Current.ShieldGenerator.IsEnabled || !__instance.m_nview.IsValid()) return;
                if (Configuration.Current.ShieldGenerator.infiniteFuel) __instance.SetFuel(__instance.m_maxFuel);
            }
        }

        [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.OnProjectileHit))]
        public static class ShieldGenerator_OnProjectileHit_Patch
        {
            private static void Postfix(ShieldGenerator __instance) => UpdateFuel(__instance);
        }
        
        [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.RPC_Attack))]
        public static class ShieldGenerator_RPC_Attack_Patch
        {
            private static void Postfix(ShieldGenerator __instance) => UpdateFuel(__instance);
        }

        // Don't rate-limit because shield only uses fuel when hit.
        private static void UpdateFuel(ShieldGenerator __instance)
        {
             if (!Configuration.Current.ShieldGenerator.IsEnabled || !__instance.m_nview.IsValid()) return;
            if (Configuration.Current.ShieldGenerator.infiniteFuel) __instance.SetFuel(__instance.m_maxFuel);
            if (Configuration.Current.ShieldGenerator.autoFuel) AddFuelFromNearbyChests(__instance);
        }
        
        private static void AddFuelFromNearbyChests(ShieldGenerator __instance)
        {
            // Find the integer of fuels to take us back to exactly m_maxFuel and no further.
            int toMaxFuel = __instance.m_maxFuel - (int)Math.Ceiling(__instance.GetFuel());
            if (toMaxFuel < 1) return;

            foreach (var item in __instance.m_fuelItems)
            {
                ItemDrop.ItemData fuelItemData = item.m_itemData;
                int addedFuel = InventoryAssistant.RemoveItemInAmountFromAllNearbyChests(__instance.gameObject,
                    Helper.Clamp(Configuration.Current.ShieldGenerator.autoRange, 1, 50), fuelItemData, toMaxFuel,
                    !Configuration.Current.ShieldGenerator.ignorePrivateAreaCheck);
                if (addedFuel < 1) return;
                for (int i = 0; i < addedFuel; i++) __instance.m_nview.InvokeRPC("RPC_AddFuel");
                ValheimPlusPlugin.Logger.LogInfo("Added " + addedFuel + " fuel(" + fuelItemData.m_shared.m_name +
                    ") in " + __instance.m_name);
                toMaxFuel -= addedFuel;
                if (toMaxFuel < 1) return;
            }
        }
    }
}

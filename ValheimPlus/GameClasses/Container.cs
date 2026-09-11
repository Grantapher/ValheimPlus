using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Container), "Awake")]
    public static class Container_Awake_Patch
    {
        private const int woodChestInventoryMaxRows = 10;
        private const int woodChestInventoryMinRows = 2;
        private const int woodChestInventoryMaxCol = 8;
        private const int woodChestInventoryMinCol = 3;

        private const int personalChestInventoryMaxRows = 20;
        private const int personalChestInventoryMinRows = 2;
        private const int personalChestInventoryMaxCol = 8;
        private const int personalChestInventoryMinCol = 3;

        private const int ironChestInventoryMaxRows = 20;
        private const int ironChestInventoryMinRows = 3;
        private const int ironChestInventoryMaxCol = 8;
        private const int ironChestInventoryMinCol = 3;

        private const int blackmetalChestInventoryMaxRows = 20;
        private const int blackmetalChestInventoryMinRows = 3;
        private const int blackmetalChestInventoryMaxCol = 8;
        private const int blackmetalChestInventoryMinCol = 3;

        private const int karveChestInventoryMaxRows = 30;
        private const int karveChestInventoryMinRows = 2;
        private const int karveChestInventoryMaxCol = 8;
        private const int karveChestInventoryMinCol = 2;

        private const int longboatChestInventoryMaxRows = 30;
        private const int longboatChestInventoryMinRows = 3;
        private const int longboatChestInventoryMaxCol = 8;
        private const int longboatChestInventoryMinCol = 6;

        private const int cartChestInventoryMaxRows = 30;
        private const int cartChestInventoryMinRows = 3;
        private const int cartChestInventoryMaxCol = 8;
        private const int cartChestInventoryMinCol = 6;

        /// <summary>
        /// Configure inventory size for chests, boats, cart
        /// </summary>
        static void Postfix(Container __instance, ref Inventory ___m_inventory)
        {
            ApplyConfiguredSize(__instance, ref ___m_inventory);

            if (!Configuration.Current.Inventory.IsEnabled) return;
            if (__instance == null || ___m_inventory == null) return;

            // Container.UpdateRows recomputes inventory height from Container.m_height on every
            // load, so the Container's own dimensions have to agree with ours.
            __instance.m_width = ___m_inventory.m_width;
            __instance.m_height = ___m_inventory.m_height;
        }

        static void ApplyConfiguredSize(Container __instance, ref Inventory ___m_inventory)
        {

            if (!Configuration.Current.Inventory.IsEnabled) return;

            if (__instance == null || ___m_inventory == null || !__instance.transform.parent) {

                // is chest

                if (___m_inventory == null) return;

                string inventoryName = ___m_inventory.m_name;
                ref int inventoryColumns = ref ___m_inventory.m_width;
                ref int inventoryRows = ref ___m_inventory.m_height;

                // Personal chest
                if (inventoryName == "$piece_chestprivate")
                {
                    inventoryRows = Helper.Clamp(Configuration.Current.Inventory.personalChestRows, personalChestInventoryMinRows, personalChestInventoryMaxRows);
                    inventoryColumns = Helper.Clamp(Configuration.Current.Inventory.personalChestColumns, personalChestInventoryMinCol, personalChestInventoryMaxCol);
                }
                // Wood chest
                else if (inventoryName == "$piece_chestwood")
                {
                    inventoryRows = Helper.Clamp(Configuration.Current.Inventory.woodChestRows, woodChestInventoryMinRows, woodChestInventoryMaxRows);
                    inventoryColumns = Helper.Clamp(Configuration.Current.Inventory.woodChestColumns, woodChestInventoryMinCol, woodChestInventoryMaxCol);
                }
                // Iron chest
                else if (inventoryName == "$piece_chest")
                {
                    inventoryRows = Helper.Clamp(Configuration.Current.Inventory.ironChestRows, ironChestInventoryMinRows, ironChestInventoryMaxRows);
                    inventoryColumns = Helper.Clamp(Configuration.Current.Inventory.ironChestColumns, ironChestInventoryMinCol, ironChestInventoryMaxCol);
                }
                // Black Metal chest
                else if (inventoryName == "$piece_chestblackmetal")
                {
                    inventoryRows = Helper.Clamp(Configuration.Current.Inventory.blackmetalChestRows, blackmetalChestInventoryMinRows, blackmetalChestInventoryMaxRows);
                    inventoryColumns = Helper.Clamp(Configuration.Current.Inventory.blackmetalChestColumns, blackmetalChestInventoryMinCol, blackmetalChestInventoryMaxCol);
                }

                return;
            } else {

                // is not chest

                string containerName = __instance.transform.parent.name;
                string inventoryName = ___m_inventory.m_name;
                ref int inventoryColumns = ref ___m_inventory.m_width;
                ref int inventoryRows = ref ___m_inventory.m_height;

                // Karve (small boat)
                // Use Contains because the actual name is "Karve (Clone)"
                if (containerName.Contains("Karve"))
                {
                    inventoryRows = Helper.Clamp(Configuration.Current.Inventory.karveInventoryRows, karveChestInventoryMinRows, karveChestInventoryMaxRows);
                    inventoryColumns = Helper.Clamp(Configuration.Current.Inventory.karveInventoryColumns, karveChestInventoryMinCol, karveChestInventoryMaxCol);
                }
                // Longboat (Large boat)
                else if (containerName.Contains("VikingShip"))
                {
                    inventoryRows = Helper.Clamp(Configuration.Current.Inventory.longboatInventoryRows, longboatChestInventoryMinRows, longboatChestInventoryMaxRows);
                    inventoryColumns = Helper.Clamp(Configuration.Current.Inventory.longboatInventoryColumns, longboatChestInventoryMinCol, longboatChestInventoryMaxCol);
                }
                // Cart (Wagon)
                else if (containerName.Contains("Cart"))
                {
                    inventoryRows = Helper.Clamp(Configuration.Current.Inventory.cartInventoryRows, cartChestInventoryMinRows, cartChestInventoryMaxRows);
                    inventoryColumns = Helper.Clamp(Configuration.Current.Inventory.cartInventoryColumns, cartChestInventoryMinCol, cartChestInventoryMaxCol);
                }
            };


        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.RPC_StackResponse))]
    public static class Container_RPC_StackResponse_Patch
    {
        /// <summary>
        /// Auto Stack handles replies from its own chests, skipping the game's per-chest message and effect.
        /// </summary>
        [UsedImplicitly]
        private static bool Prefix(Container __instance, bool granted)
        {
            if (AutoStackSweep.HandleResponse(__instance, granted)) return false;

            // The game answers this one. Either the open chest, whose stack goes on to start a sweep, or a
            // chest no sweep asked about, which has the same ownership problem with no message when it bites.
            var isOpenChest = InventoryGui.instance && InventoryGui.instance.m_currentContainer == __instance;
            ValheimPlusPlugin.Logger.LogDebug(
                $"Stack All {(granted ? "granted" : "refused")} for the game to handle" +
                $"{(isOpenChest ? " on the open chest, which starts a sweep next" : ", outside any sweep")}: " +
                $"{AutoStackSweep.Describe(__instance)}");
            return true;
        }
    }
}

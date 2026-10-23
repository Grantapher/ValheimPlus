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

        private const int graustenChestInventoryMaxRows = 20;
        private const int graustenChestInventoryMinRows = 3;
        private const int graustenChestInventoryMaxCol = 8;
        private const int graustenChestInventoryMinCol = 3;

        private const int wardrobeInventoryMaxRows = 20;
        private const int wardrobeInventoryMinRows = 3;
        private const int wardrobeInventoryMaxCol = 8;
        private const int wardrobeInventoryMinCol = 3;

        private const int barrelInventoryMaxRows = 20;
        private const int barrelInventoryMinRows = 2;
        private const int barrelInventoryMaxCol = 8;
        private const int barrelInventoryMinCol = 3;

        private const int karveChestInventoryMaxRows = 30;
        private const int karveChestInventoryMinRows = 2;
        private const int karveChestInventoryMaxCol = 8;
        private const int karveChestInventoryMinCol = 2;

        private const int longboatChestInventoryMaxRows = 30;
        private const int longboatChestInventoryMinRows = 3;
        private const int longboatChestInventoryMaxCol = 8;
        private const int longboatChestInventoryMinCol = 6;

        private const int drakkarChestInventoryMaxRows = 30;
        private const int drakkarChestInventoryMinRows = 3;
        private const int drakkarChestInventoryMaxCol = 8;
        private const int drakkarChestInventoryMinCol = 6;

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

            PreserveOccupiedColumns(__instance, ___m_inventory);
        }

        static void ApplyConfiguredSize(Container __instance, ref Inventory ___m_inventory)
        {

            if (!Configuration.Current.Inventory.IsEnabled) return;

            // Check the new containers first: VikingShip_Ashlands would otherwise match
            // the longboat's VikingShip substring below.
            if (__instance != null && ___m_inventory != null &&
                TryGetAdditionalContainerSize(__instance, ___m_inventory, out int columns, out int rows))
            {
                ___m_inventory.m_width = columns;
                ___m_inventory.m_height = rows;
                return;
            }

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

        internal static bool TryGetAdditionalContainerSize(Container container, Inventory inventory, out int columns, out int rows)
        {
            columns = rows = 0;
            if (inventory.m_name == "$piece_chestgrausten")
            {
                columns = Helper.Clamp(Configuration.Current.Inventory.graustenChestColumns, graustenChestInventoryMinCol, graustenChestInventoryMaxCol);
                rows = Helper.Clamp(Configuration.Current.Inventory.graustenChestRows, graustenChestInventoryMinRows, graustenChestInventoryMaxRows);
                return true;
            }
            if (inventory.m_name == "$piece_chestwarderobe")
            {
                columns = Helper.Clamp(Configuration.Current.Inventory.wardrobeColumns, wardrobeInventoryMinCol, wardrobeInventoryMaxCol);
                rows = Helper.Clamp(Configuration.Current.Inventory.wardrobeRows, wardrobeInventoryMinRows, wardrobeInventoryMaxRows);
                return true;
            }
            if (inventory.m_name == "$piece_chestbarrel")
            {
                columns = Helper.Clamp(Configuration.Current.Inventory.barrelColumns, barrelInventoryMinCol, barrelInventoryMaxCol);
                rows = Helper.Clamp(Configuration.Current.Inventory.barrelRows, barrelInventoryMinRows, barrelInventoryMaxRows);
                return true;
            }

            var root = container.m_rootObjectOverride ? container.m_rootObjectOverride.transform : container.transform.parent;
            if (root && Utils.GetPrefabName(root.name) == "VikingShip_Ashlands")
            {
                columns = Helper.Clamp(Configuration.Current.Inventory.drakkarInventoryColumns, drakkarChestInventoryMinCol, drakkarChestInventoryMaxCol);
                rows = Helper.Clamp(Configuration.Current.Inventory.drakkarInventoryRows, drakkarChestInventoryMinRows, drakkarChestInventoryMaxRows);
                return true;
            }
            return false;
        }

        internal static void PreserveOccupiedColumns(Container container, Inventory inventory)
        {
            if (!Configuration.Current.Inventory.IsEnabled || container == null || inventory == null) return;
            if (!TryGetAdditionalContainerSize(container, inventory, out _, out _)) return;

            // Native Container.UpdateRows retains occupied rows after loading. Do the
            // same for columns so lowering these settings cannot hide saved items.
            int columns = container.m_width;
            foreach (var item in inventory.GetAllItems())
                columns = System.Math.Max(columns, item.m_gridPos.x + 1);
            inventory.m_width = columns;
        }
    }

    [HarmonyPatch(typeof(Container), "Load")]
    public static class Container_Load_Patch
    {
        private static void Prefix(Container __instance, Inventory ___m_inventory, ZNetView ___m_nview, uint ___m_lastRevision, bool ___m_inUse)
        {
            if (!Configuration.Current.Inventory.IsEnabled || __instance == null || ___m_inventory == null || !___m_nview) return;
            if (!Container_Awake_Patch.TryGetAdditionalContainerSize(__instance, ___m_inventory, out _, out _)) return;

            var zdo = ___m_nview.GetZDO();
            if (zdo == null || ___m_inUse || zdo.DataRevision == ___m_lastRevision) return;
            var savedItems = zdo.GetByteArray(ZDOVars.s_items);
            if (savedItems == null) return;

            // Native loading rejects saved columns beyond the current width. Older
            // save formats also reject rows beyond the height. Read positions without
            // bounds first so every saved item fits when the real inventory loads.
            var savedInventory = new Inventory(true);
            savedInventory.Load(new ZPackage(savedItems));
            int columns = __instance.m_width;
            int rows = __instance.m_height;
            foreach (var item in savedInventory.GetAllItems())
            {
                columns = System.Math.Max(columns, item.m_gridPos.x + 1);
                rows = System.Math.Max(rows, item.m_gridPos.y + 1);
            }
            ___m_inventory.m_width = columns;
            ___m_inventory.m_height = rows;
        }

        private static void Postfix(Container __instance, Inventory ___m_inventory, bool __result)
        {
            if (__result)
                Container_Awake_Patch.PreserveOccupiedColumns(__instance, ___m_inventory);
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

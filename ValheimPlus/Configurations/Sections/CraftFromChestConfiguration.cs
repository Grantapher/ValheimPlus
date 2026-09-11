using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class CraftFromChestConfiguration : BaseConfig
    {
        private const string Section = "CraftFromChest";

        private ConfigEntry<float> rangeEntry;
        private ConfigEntry<bool> disableCookingStationEntry;
        private ConfigEntry<bool> checkFromWorkbenchEntry;
        private ConfigEntry<bool> ignorePrivateAreaCheckEntry;
        private ConfigEntry<int> lookupIntervalEntry;
        private ConfigEntry<bool> allowCraftingFromCartsEntry;
        private ConfigEntry<bool> allowCraftingFromShipsEntry;

        public float range => rangeEntry.Value;
        public bool disableCookingStation => disableCookingStationEntry.Value;
        public bool checkFromWorkbench => checkFromWorkbenchEntry.Value;
        public bool ignorePrivateAreaCheck => ignorePrivateAreaCheckEntry.Value;
        public int lookupInterval => lookupIntervalEntry.Value;
        public bool allowCraftingFromCarts => allowCraftingFromCartsEntry.Value;
        public bool allowCraftingFromShips => allowCraftingFromShipsEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.\nThis feature allows you to craft from nearby chests when in range.");
            rangeEntry = Bind(config, Section, "range", 20f, 1f, 50f,
                "The range of the chest detection in meters.");
            disableCookingStationEntry = Bind(config, Section, "disableCookingStation", false,
                "Change false to true to disable this feature when using a Cooking Station.");
            checkFromWorkbenchEntry = Bind(config, Section, "checkFromWorkbench", true,
                "If in a workbench area, uses it as reference point when scanning for chests.");
            ignorePrivateAreaCheckEntry = Bind(config, Section, "ignorePrivateAreaCheck", false,
                "This option prevents crafting to pull items from warded areas if the player doesnt have access to it.");
            lookupIntervalEntry = Bind(config, Section, "lookupInterval", 3, 1, 10,
                "The interval in seconds that the feature scans your nearby chests.\nWe recommend not going below 3 seconds.");
            allowCraftingFromCartsEntry = Bind(config, Section, "allowCraftingFromCarts", false,
                "Allows the system to use and see contents of carts for crafting. Might also allow use of other modded containers or vehicles not accessible otherwise.");
            allowCraftingFromShipsEntry = Bind(config, Section, "allowCraftingFromShips", false,
                "Allows the system to use and see contents of ships for crafting. Might also allow use of other modded containers or vehicles not accessible otherwise.");
        }
    }
}

using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class FurnaceConfiguration : BaseConfig
    {
        private const string Section = "Furnace";

        private ConfigEntry<int> maximumOreEntry;
        private ConfigEntry<int> maximumCoalEntry;
        private ConfigEntry<int> coalUsedPerProductEntry;
        private ConfigEntry<float> productionSpeedEntry;
        private ConfigEntry<bool> autoDepositEntry;
        private ConfigEntry<bool> autoFuelEntry;
        private ConfigEntry<bool> ignorePrivateAreaCheckEntry;
        private ConfigEntry<float> autoRangeEntry;
        private ConfigEntry<bool> allowAllOresEntry;

        public int maximumOre => maximumOreEntry.Value;
        public int maximumCoal => maximumCoalEntry.Value;
        public int coalUsedPerProduct => coalUsedPerProductEntry.Value;
        public float productionSpeed => productionSpeedEntry.Value;
        public bool autoDeposit => autoDepositEntry.Value;
        public bool autoFuel => autoFuelEntry.Value;
        public bool ignorePrivateAreaCheck => ignorePrivateAreaCheckEntry.Value;
        public float autoRange => autoRangeEntry.Value;
        public bool allowAllOres => allowAllOresEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            maximumOreEntry = Bind(config, Section, "maximumOre", 10,
                "Maximum amount of ore in a Furnace.");
            maximumCoalEntry = Bind(config, Section, "maximumCoal", 20,
                "Maximum amount of coal in a Furnace.");
            coalUsedPerProductEntry = Bind(config, Section, "coalUsedPerProduct", 2,
                "The total amount of coal used to produce a single smelted ingot.");
            productionSpeedEntry = Bind(config, Section, "productionSpeed", 30f,
                "The time it takes for the Furnace to produce a single ingot in seconds.");
            autoDepositEntry = Bind(config, Section, "autoDeposit", false,
                "Instead of dropping the items, they will be placed inside the nearest nearby chests.");
            autoFuelEntry = Bind(config, Section, "autoFuel", false,
                "The Furnace will pull coal and raw materials from nearby chests to be automatically added to it when its empty.");
            ignorePrivateAreaCheckEntry = Bind(config, Section, "ignorePrivateAreaCheck", true,
                "This option prevents the Furnace to pull items from warded areas if it isn't placed inside of it.\nFor convenience, we recommend this to be set to true.");
            autoRangeEntry = Bind(config, Section, "autoRange", 10f, 1f, 50f,
                "The range of the chest detection for the auto deposit and auto fuel features.\nMaximum is 50");
            allowAllOresEntry = Bind(config, Section, "allowAllOres", false,
                "This option allows all ores inside the Furnace.");
        }
    }
}

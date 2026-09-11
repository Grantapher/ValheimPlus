using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class KilnConfiguration : BaseConfig
    {
        private const string Section = "Kiln";

        private ConfigEntry<float> productionSpeedEntry;
        private ConfigEntry<int> maximumWoodEntry;
        private ConfigEntry<bool> dontProcessFineWoodEntry;
        private ConfigEntry<bool> dontProcessRoundLogEntry;
        private ConfigEntry<bool> autoDepositEntry;
        private ConfigEntry<bool> autoFuelEntry;
        private ConfigEntry<int> stopAutoFuelThresholdEntry;
        private ConfigEntry<bool> ignorePrivateAreaCheckEntry;
        private ConfigEntry<float> autoRangeEntry;

        public float productionSpeed => productionSpeedEntry.Value;
        public int maximumWood => maximumWoodEntry.Value;
        public bool dontProcessFineWood => dontProcessFineWoodEntry.Value;
        public bool dontProcessRoundLog => dontProcessRoundLogEntry.Value;
        public bool autoDeposit => autoDepositEntry.Value;
        public bool autoFuel => autoFuelEntry.Value;
        public int stopAutoFuelThreshold => stopAutoFuelThresholdEntry.Value;
        public bool ignorePrivateAreaCheck => ignorePrivateAreaCheckEntry.Value;
        public float autoRange => autoRangeEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            productionSpeedEntry = Bind(config, Section, "productionSpeed", 15f,
                "The time it takes for the Kiln to produce a single piece of coal in seconds.");
            maximumWoodEntry = Bind(config, Section, "maximumWood", 25,
                "Maximum amount of wood in a Kiln.");
            dontProcessFineWoodEntry = Bind(config, Section, "dontProcessFineWood", false,
                "Change false to true to disable Fine Wood processing.");
            dontProcessRoundLogEntry = Bind(config, Section, "dontProcessRoundLog", false,
                "Change false to true to disabled Round Log processing.");
            autoDepositEntry = Bind(config, Section, "autoDeposit", false,
                "Instead of dropping the items, they will be placed inside the nearest nearby chests.");
            autoFuelEntry = Bind(config, Section, "autoFuel", false,
                "The Kiln will pull wood from nearby chests to be automatically added to it when its empty.\nThis option respects the dontProcessFineWood and dontProcessRoundLog settings.");
            stopAutoFuelThresholdEntry = Bind(config, Section, "stopAutoFuelThreshold", 0,
                "Stops autoFuel (looking for fuel) when there is at leasts this quantity of Coal in nearby chests\n(ignored if set to 0)");
            ignorePrivateAreaCheckEntry = Bind(config, Section, "ignorePrivateAreaCheck", true,
                "This option prevents the Kiln to pull items from warded areas if it isn't placed inside of it.\nFor convenience, we recommend this to be set to true.");
            autoRangeEntry = Bind(config, Section, "autoRange", 10f, 1f, 50f,
                "The range of the chest detection for the auto deposit and fuel features.\nMaximum is 50");
        }
    }
}

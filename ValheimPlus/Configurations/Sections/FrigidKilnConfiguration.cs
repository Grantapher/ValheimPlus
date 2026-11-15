using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class FrigidKilnConfiguration : BaseConfig
    {
        private const string Section = "FrigidKiln";

        private ConfigEntry<float> productionSpeedEntry;
        private ConfigEntry<int> maximumIceEntry;
        private ConfigEntry<int> iceUsedPerProductEntry;
        private ConfigEntry<bool> autoDepositEntry;
        private ConfigEntry<bool> autoFuelEntry;
        private ConfigEntry<int> stopAutoFuelThresholdEntry;
        private ConfigEntry<bool> ignorePrivateAreaCheckEntry;
        private ConfigEntry<float> autoRangeEntry;

        public float productionSpeed => productionSpeedEntry.Value;
        public int maximumIce => maximumIceEntry.Value;
        public int iceUsedPerProduct => iceUsedPerProductEntry.Value;
        public bool autoDeposit => autoDepositEntry.Value;
        public bool autoFuel => autoFuelEntry.Value;
        public int stopAutoFuelThreshold => stopAutoFuelThresholdEntry.Value;
        public bool ignorePrivateAreaCheck => ignorePrivateAreaCheckEntry.Value;
        public float autoRange => autoRangeEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            productionSpeedEntry = Bind(config, Section, "productionSpeed", 30f,
                "The time it takes for the Frigid Kiln to produce a single Frozen Fuel in seconds.");
            maximumIceEntry = Bind(config, Section, "maximumIce", 25,
                "Maximum amount of ice in a Frigid Kiln.");
            iceUsedPerProductEntry = Bind(config, Section, "iceUsedPerProduct", 5,
                "The total amount of ice used to produce a single Frozen Fuel.");
            autoDepositEntry = Bind(config, Section, "autoDeposit", false,
                "Instead of dropping the items, they will be placed inside the nearest nearby chests.");
            autoFuelEntry = Bind(config, Section, "autoFuel", false,
                "The Frigid Kiln will pull ice from nearby chests to be automatically added to it.");
            stopAutoFuelThresholdEntry = Bind(config, Section, "stopAutoFuelThreshold", 0,
                "Stops autoFuel (pulling ice) when there is at least this quantity of Frozen Fuel in nearby chests\n(ignored if set to 0)");
            ignorePrivateAreaCheckEntry = Bind(config, Section, "ignorePrivateAreaCheck", true,
                "This option prevents the Frigid Kiln to pull items from warded areas if it isn't placed inside of it.\nFor convenience, we recommend this to be set to true.");
            autoRangeEntry = Bind(config, Section, "autoRange", 10f, 1f, 50f,
                "The range of the chest detection for the auto deposit and auto fuel features.\nMaximum is 50");
        }
    }
}

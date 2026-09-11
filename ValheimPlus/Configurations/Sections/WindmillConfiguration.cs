using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class WindmillConfiguration : BaseConfig
    {
        private const string Section = "Windmill";

        private ConfigEntry<int> maximumBarleyEntry;
        private ConfigEntry<float> productionSpeedEntry;
        private ConfigEntry<bool> ignoreWindIntensityEntry;
        private ConfigEntry<bool> autoDepositEntry;
        private ConfigEntry<bool> autoFuelEntry;
        private ConfigEntry<bool> ignorePrivateAreaCheckEntry;
        private ConfigEntry<float> autoRangeEntry;

        public int maximumBarley => maximumBarleyEntry.Value;
        public float productionSpeed => productionSpeedEntry.Value;
        public bool ignoreWindIntensity => ignoreWindIntensityEntry.Value;
        public bool autoDeposit => autoDepositEntry.Value;
        public bool autoFuel => autoFuelEntry.Value;
        public bool ignorePrivateAreaCheck => ignorePrivateAreaCheckEntry.Value;
        public float autoRange => autoRangeEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            maximumBarleyEntry = Bind(config, Section, "maximumBarley", 50,
                "Maximum amount of barley in a windmill.");
            productionSpeedEntry = Bind(config, Section, "productionSpeed", 10f,
                "The time it takes for the windmill to produce a single ingot in seconds.");
            ignoreWindIntensityEntry = Bind(config, Section, "ignoreWindIntensity", false,
                "Ignore wind intensity so it always takes the production speed value to process one barley.");
            autoDepositEntry = Bind(config, Section, "autoDeposit", false,
                "Instead of dropping the items, they will be placed inside the nearest nearby chests.");
            autoFuelEntry = Bind(config, Section, "autoFuel", false,
                "The Windmill will pull barley from nearby chests to be automatically added to it when its empty.");
            ignorePrivateAreaCheckEntry = Bind(config, Section, "ignorePrivateAreaCheck", true,
                "This option prevents the Windmill to pull items from warded areas if it isn't placed inside of it.\nFor convenience, we recommend this to be set to true.");
            autoRangeEntry = Bind(config, Section, "autoRange", 10f, 1f, 50f,
                "The range of the chest detection for the auto deposit and auto fuel features.\nMaximum is 50");
        }
    }
}

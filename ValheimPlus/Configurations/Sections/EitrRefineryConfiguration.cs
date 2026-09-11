using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class EitrRefineryConfiguration : BaseConfig
    {
        private const string Section = "EitrRefinery";

        private ConfigEntry<int> maximumSapEntry;
        private ConfigEntry<int> maximumSoftTissueEntry;
        private ConfigEntry<float> productionSpeedEntry;
        private ConfigEntry<bool> autoDepositEntry;
        private ConfigEntry<bool> autoFuelEntry;
        private ConfigEntry<bool> ignorePrivateAreaCheckEntry;
        private ConfigEntry<float> autoRangeEntry;

        public int maximumSap => maximumSapEntry.Value;
        public int maximumSoftTissue => maximumSoftTissueEntry.Value;
        public float productionSpeed => productionSpeedEntry.Value;
        public bool autoDeposit => autoDepositEntry.Value;
        public bool autoFuel => autoFuelEntry.Value;
        public bool ignorePrivateAreaCheck => ignorePrivateAreaCheckEntry.Value;
        public float autoRange => autoRangeEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            maximumSapEntry = Bind(config, Section, "maximumSap", 20,
                "Maximum amount of sap in an Eitr Refinery.");
            maximumSoftTissueEntry = Bind(config, Section, "maximumSoftTissue", 20,
                "Maximum amount of soft tissue in an Eitr Refinery.");
            productionSpeedEntry = Bind(config, Section, "productionSpeed", 40f,
                "The time it takes for the Eitr Refinery to produce a single eitr in seconds.");
            autoDepositEntry = Bind(config, Section, "autoDeposit", true,
                "Instead of dropping the items, they will be placed inside the nearest nearby chests.");
            autoFuelEntry = Bind(config, Section, "autoFuel", true,
                "The Eitr Refinery will pull sap and soft tissue from nearby chests to be automatically added to it when it's empty.");
            ignorePrivateAreaCheckEntry = Bind(config, Section, "ignorePrivateAreaCheck", true,
                "This option prevents the Eitr Refinery to pull items from warded areas if it isn't placed inside of it.\nFor convenience, we recommend this to be set to true.");
            autoRangeEntry = Bind(config, Section, "autoRange", 10f, 1f, 50f,
                "The range of the chest detection for the auto deposit and auto fuel features.\nMaximum is 50");
        }
    }
}

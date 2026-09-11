using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class OvenConfiguration : BaseConfig
    {
        private const string Section = "Oven";

        private ConfigEntry<bool> infiniteFuelEntry;
        private ConfigEntry<bool> autoFuelEntry;
        private ConfigEntry<bool> ignorePrivateAreaCheckEntry;
        private ConfigEntry<float> autoRangeEntry;

        public bool infiniteFuel => infiniteFuelEntry.Value;
        public bool autoFuel => autoFuelEntry.Value;
        public bool ignorePrivateAreaCheck => ignorePrivateAreaCheckEntry.Value;
        public float autoRange => autoRangeEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            infiniteFuelEntry = Bind(config, Section, "infiniteFuel", false,
                "If set to true, the oven will stay at max fuel level, without consuming any fuel.");
            autoFuelEntry = Bind(config, Section, "autoFuel", false,
                "The oven will fuel itself from nearby chests.");
            ignorePrivateAreaCheckEntry = Bind(config, Section, "ignorePrivateAreaCheck", true,
                "This option allows the oven to fuel itself from chests that it doesn't share a warded area with.\nFor convenience, we recommend this to be set to true.");
            autoRangeEntry = Bind(config, Section, "autoRange", 10f, 1f, 50f,
                "The range of the chest detection for the auto fuel feature.\nMaximum is 50");
        }
    }
}

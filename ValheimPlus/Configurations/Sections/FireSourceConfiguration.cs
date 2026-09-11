using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class FireSourceConfiguration : BaseConfig
    {
        private const string Section = "FireSource";

        private ConfigEntry<bool> torchesEntry;
        private ConfigEntry<bool> firesEntry;
        private ConfigEntry<bool> autoFuelEntry;
        private ConfigEntry<bool> ignorePrivateAreaCheckEntry;
        private ConfigEntry<float> autoRangeEntry;

        public bool torches => torchesEntry.Value;
        public bool fires => firesEntry.Value;
        public bool autoFuel => autoFuelEntry.Value;
        public bool ignorePrivateAreaCheck => ignorePrivateAreaCheckEntry.Value;
        public float autoRange => autoRangeEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            torchesEntry = Bind(config, Section, "torches", false,
                "If set to true, torch-type fire sources will have infinite fuel.\nApplies to: wood torches, iron torches, green torches, sconces and brazier.");
            firesEntry = Bind(config, Section, "fires", false,
                "If set to true, non torch-type fire sources will have infinite fuel.");
            autoFuelEntry = Bind(config, Section, "autoFuel", false,
                "Automatically pull wood from nearby chests to be placed inside the Fire as soon as its empty.");
            ignorePrivateAreaCheckEntry = Bind(config, Section, "ignorePrivateAreaCheck", true,
                "This option prevents the Fire to pull items from warded areas if it isn't placed inside of it.\nFor convenience, we recommend this to be set to true.");
            autoRangeEntry = Bind(config, Section, "autoRange", 10f, 1f, 50f,
                "The range of the chest detection for the auto fuel features.\nMaximum is 50");
        }
    }
}

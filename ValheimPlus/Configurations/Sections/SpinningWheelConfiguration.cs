using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class SpinningWheelConfiguration : BaseConfig
    {
        private const string Section = "SpinningWheel";

        private ConfigEntry<int> maximumFlaxEntry;
        private ConfigEntry<float> productionSpeedEntry;
        private ConfigEntry<bool> autoDepositEntry;
        private ConfigEntry<bool> autoFuelEntry;
        private ConfigEntry<bool> ignorePrivateAreaCheckEntry;
        private ConfigEntry<float> autoRangeEntry;

        public int maximumFlax => maximumFlaxEntry.Value;
        public float productionSpeed => productionSpeedEntry.Value;
        public bool autoDeposit => autoDepositEntry.Value;
        public bool autoFuel => autoFuelEntry.Value;
        public bool ignorePrivateAreaCheck => ignorePrivateAreaCheckEntry.Value;
        public float autoRange => autoRangeEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            maximumFlaxEntry = Bind(config, Section, "maximumFlax", 50,
                "Maximum amount of flax in a spinning wheel.");
            productionSpeedEntry = Bind(config, Section, "productionSpeed", 30f,
                "The time it takes for the spinning wheel to produce linen thread.");
            autoDepositEntry = Bind(config, Section, "autoDeposit", false,
                "Instead of dropping the items, they will be placed inside the nearest nearby chests.");
            autoFuelEntry = Bind(config, Section, "autoFuel", false,
                "The Spinning Wheel will pull flax from nearby chests to be automatically added to it when its empty.");
            ignorePrivateAreaCheckEntry = Bind(config, Section, "ignorePrivateAreaCheck", true,
                "This option prevents the Spinning Wheel to pull items from warded areas if it isn't placed inside of it.\nFor convenience, we recommend this to be set to true.");
            autoRangeEntry = Bind(config, Section, "autoRange", 10f, 1f, 50f,
                "The range of the chest detection for the auto deposit and auto fuel features\nMaximum is 50");
        }
    }
}

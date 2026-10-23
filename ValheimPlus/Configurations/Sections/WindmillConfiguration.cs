using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public enum OatProcessingMode
    {
        Oat,
        OatFlour
    }

    public class WindmillConfiguration : BaseConfig
    {
        private const string Section = "Windmill";

        private ConfigEntry<int> maximumBarleyEntry;
        private ConfigEntry<float> productionSpeedEntry;
        private ConfigEntry<bool> ignoreWindIntensityEntry;
        private ConfigEntry<bool> autoDepositEntry;
        private ConfigEntry<bool> autoFuelEntry;
        private ConfigEntry<OatProcessingMode> oatProcessingEntry;
        private ConfigEntry<bool> ignorePrivateAreaCheckEntry;
        private ConfigEntry<float> autoRangeEntry;

        public int maximumBarley => maximumBarleyEntry.Value;
        public float productionSpeed => productionSpeedEntry.Value;
        public bool ignoreWindIntensity => ignoreWindIntensityEntry.Value;
        public bool autoDeposit => autoDepositEntry.Value;
        public bool autoFuel => autoFuelEntry.Value;
        public OatProcessingMode oatProcessing => oatProcessingEntry.Value;
        public bool ignorePrivateAreaCheck => ignorePrivateAreaCheckEntry.Value;
        public float autoRange => autoRangeEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            maximumBarleyEntry = Bind(config, Section, "maximumBarley", 50,
                "Maximum number of queued inputs, shared by barley, oat seeds and oats. Inputs are processed in insertion order.\nWith the default capacity, 5 oat seeds followed by 5 oats show 10/50 items. Each queued input keeps its recipe: oat seeds produce oats, and oats produce oat flour.");
            productionSpeedEntry = Bind(config, Section, "productionSpeed", 10f,
                "The time it takes for the windmill to process one input in seconds.");
            ignoreWindIntensityEntry = Bind(config, Section, "ignoreWindIntensity", false,
                "Ignore wind intensity so it always takes the production speed value to process one input.");
            autoDepositEntry = Bind(config, Section, "autoDeposit", false,
                "Automatically empty processed output into nearby chests, preferring existing stacks. Any remainder drops normally.");
            autoFuelEntry = Bind(config, Section, "autoFuel", false,
                "Fill free windmill slots from nearby chests using the game's conversions, including barley and oats.\nProcess intermediate ingredients such as oats before raw seeds, then prefer the nearest chests.");
            oatProcessingEntry = Bind(config, Section, "oatProcessing", OatProcessingMode.OatFlour,
                "Choose the final product of automatic oat processing: Oat or OatFlour.\nOat mode still processes oats already added to the windmill into oat flour. Oats produced from seeds are automatically deposited and stay in chests until you move an entire stack into your inventory and return it.\nReturning a whole stack allows it to enter the queue for milling into flour. It waits in the chest if the windmill is full. Partial transfers do not release oats for further processing.");
            ignorePrivateAreaCheckEntry = Bind(config, Section, "ignorePrivateAreaCheck", true,
                "This option prevents the Windmill to pull items from warded areas if it isn't placed inside of it.\nFor convenience, we recommend this to be set to true.");
            autoRangeEntry = Bind(config, Section, "autoRange", 10f, 1f, 50f,
                "The range of the chest detection for the auto deposit and auto fuel features.\nMaximum is 50");
        }
    }
}

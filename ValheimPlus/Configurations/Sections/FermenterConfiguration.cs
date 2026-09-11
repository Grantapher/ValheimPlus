using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class FermenterConfiguration : BaseConfig
    {
        private const string Section = "Fermenter";

        private ConfigEntry<float> fermenterDurationEntry;
        private ConfigEntry<int> fermenterItemsProducedEntry;
        private ConfigEntry<bool> showDurationEntry;
        private ConfigEntry<bool> autoDepositEntry;
        private ConfigEntry<bool> autoFuelEntry;
        private ConfigEntry<bool> ignorePrivateAreaCheckEntry;
        private ConfigEntry<float> autoRangeEntry;

        public float fermenterDuration => fermenterDurationEntry.Value;
        public int fermenterItemsProduced => fermenterItemsProducedEntry.Value;
        public bool showDuration => showDurationEntry.Value;
        public bool autoDeposit => autoDepositEntry.Value;
        public bool autoFuel => autoFuelEntry.Value;
        public bool ignorePrivateAreaCheck => ignorePrivateAreaCheckEntry.Value;
        public float autoRange => autoRangeEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            fermenterDurationEntry = Bind(config, Section, "fermenterDuration", 2400f,
                "Configure the time that the fermenter takes to produce its product, 2400 seconds are 48 ingame hours.");
            fermenterItemsProducedEntry = Bind(config, Section, "fermenterItemsProduced", 6,
                "Configure the total amount of produced items from a fermenter.");
            showDurationEntry = Bind(config, Section, "showDuration", false,
                "Display the minutes and seconds until the fermenter is done on crosshair hover.");
            autoDepositEntry = Bind(config, Section, "autoDeposit", false,
                "Instead of dropping the items, they will be placed inside the nearest nearby chests.");
            autoFuelEntry = Bind(config, Section, "autoFuel", false,
                "Automatically pull meads from nearby chests to be placed inside the Fermenter as soon as its empty.");
            ignorePrivateAreaCheckEntry = Bind(config, Section, "ignorePrivateAreaCheck", true,
                "This option prevents the fermenter to pull items from warded areas if it isn't placed inside of it.\nFor convenience, we recommend this to be set to true.");
            autoRangeEntry = Bind(config, Section, "autoRange", 10f, 1f, 50f,
                "The range of the chest detection for the auto deposit and auto fuel features\nMaximum is 50");
        }
    }
}

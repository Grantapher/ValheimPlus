using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class BeehiveConfiguration : BaseConfig
    {
        private const string Section = "Beehive";

        private ConfigEntry<float> honeyProductionSpeedEntry;
        private ConfigEntry<int> maximumHoneyPerBeehiveEntry;
        private ConfigEntry<bool> autoDepositEntry;
        private ConfigEntry<float> autoDepositRangeEntry;
        private ConfigEntry<bool> showDurationEntry;

        public float honeyProductionSpeed => honeyProductionSpeedEntry.Value;
        public int maximumHoneyPerBeehive => maximumHoneyPerBeehiveEntry.Value;
        public bool autoDeposit => autoDepositEntry.Value;
        public float autoDepositRange => autoDepositRangeEntry.Value;
        public bool showDuration => showDurationEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            honeyProductionSpeedEntry = Bind(config, Section, "honeyProductionSpeed", 1200f,
                "Configure the speed at which the bees produce honey in seconds, 1200 seconds are 24 ingame hours.");
            maximumHoneyPerBeehiveEntry = Bind(config, Section, "maximumHoneyPerBeehive", 4,
                "Configure the maximum amount of honey in beehives.");
            autoDepositEntry = Bind(config, Section, "autoDeposit", false,
                "Instead of dropping the items, they will be placed inside the nearest nearby chests.");
            autoDepositRangeEntry = Bind(config, Section, "autoDepositRange", 10f, 1f, 50f,
                "The range of the chest detection for the auto deposit feature.\nMaximum is 50");
            showDurationEntry = Bind(config, Section, "showDuration", false,
                "Display the minutes and seconds until the beehive produces honey on crosshair hover.");
        }
    }
}

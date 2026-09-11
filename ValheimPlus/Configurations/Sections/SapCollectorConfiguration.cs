using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class SapCollectorConfiguration : BaseConfig
    {
        private const string Section = "SapCollector";

        private ConfigEntry<float> sapProductionSpeedEntry;
        private ConfigEntry<int> maximumSapPerCollectorEntry;
        private ConfigEntry<bool> autoDepositEntry;
        private ConfigEntry<float> autoDepositRangeEntry;
        private ConfigEntry<bool> showDurationEntry;

        public float sapProductionSpeed => sapProductionSpeedEntry.Value;
        public int maximumSapPerCollector => maximumSapPerCollectorEntry.Value;
        public bool autoDeposit => autoDepositEntry.Value;
        public float autoDepositRange => autoDepositRangeEntry.Value;
        public bool showDuration => showDurationEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            sapProductionSpeedEntry = Bind(config, Section, "sapProductionSpeed", 60f,
                "Configure the speed at which the collector produces sap in seconds, 75 seconds is 1 in-game hour.");
            maximumSapPerCollectorEntry = Bind(config, Section, "maximumSapPerCollector", 10,
                "Configure the maximum amount of sap per collector");
            autoDepositEntry = Bind(config, Section, "autoDeposit", false,
                "Instead of dropping the items, they will be placed inside the nearest nearby chests.");
            autoDepositRangeEntry = Bind(config, Section, "autoDepositRange", 10f, 1f, 50f,
                "The range of the chest detection for the auto deposit feature.\nMaximum is 50");
            showDurationEntry = Bind(config, Section, "showDuration", false,
                "Display the time until the collector produces sap, on hover.");
        }
    }
}

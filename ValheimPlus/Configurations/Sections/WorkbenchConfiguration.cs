using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class WorkbenchConfiguration : BaseConfig
    {
        private const string Section = "Workbench";

        private ConfigEntry<float> workbenchRangeEntry;
        private ConfigEntry<float> workbenchEnemySpawnRangeEntry;
        private ConfigEntry<float> workbenchAttachmentRangeEntry;
        private ConfigEntry<bool> disableRoofCheckEntry;

        public float workbenchRange => workbenchRangeEntry.Value;
        public float workbenchEnemySpawnRange => workbenchEnemySpawnRangeEntry.Value;
        public float workbenchAttachmentRange => workbenchAttachmentRangeEntry.Value;
        public bool disableRoofCheck => disableRoofCheckEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            workbenchRangeEntry = Bind(config, Section, "workbenchRange", 20f,
                "Set the workbench radius in meters.");
            workbenchEnemySpawnRangeEntry = Bind(config, Section, "workbenchEnemySpawnRange", 0f,
                "Set the enemy spawn prevention radius around workbenches in meters.\nA value of 0 uses the configured workbenchRange.\nStation upgrades still increase the building range, but do not change this spawn prevention radius.");
            workbenchAttachmentRangeEntry = Bind(config, Section, "workbenchAttachmentRange", 5f,
                "Sets the workbench attachment (e.g. anvil) radius.");
            disableRoofCheckEntry = Bind(config, Section, "disableRoofCheck", false,
                "Disables the roof and exposure requirement to use a workbench.");
        }
    }
}

using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class ItemsConfiguration : BaseConfig
    {
        private const string Section = "Items";

        private ConfigEntry<bool> noTeleportPreventionEntry;
        private ConfigEntry<float> baseItemWeightReductionEntry;
        private ConfigEntry<float> itemStackMultiplierEntry;
        private ConfigEntry<float> droppedItemOnGroundDurationInSecondsEntry;
        private ConfigEntry<bool> itemsFloatInWaterEntry;

        public bool noTeleportPrevention => noTeleportPreventionEntry.Value;
        public float baseItemWeightReduction => baseItemWeightReductionEntry.Value;
        public float itemStackMultiplier => itemStackMultiplierEntry.Value;
        public float droppedItemOnGroundDurationInSeconds => droppedItemOnGroundDurationInSecondsEntry.Value;
        public bool itemsFloatInWater => itemsFloatInWaterEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            noTeleportPreventionEntry = Bind(config, Section, "noTeleportPrevention", false,
                "Enables you to teleport with ores and other usually teleport restricted objects.");
            baseItemWeightReductionEntry = Bind(config, Section, "baseItemWeightReduction", 0f,
                "Increase or reduce item weight by a modifier in percent.\nThe value -50 will reduce item weight of every object by 50%, 50 will increase the weight of every item by 50%.");
            itemStackMultiplierEntry = Bind(config, Section, "itemStackMultiplier", 0f,
                "Increase or reduce the size of all maximum item stacks by a modifier in percent.\nThe value 50 would set a usual item stack of 100 to be 150.\nThe value -50 would set a usual item stack of 100 to be 50.");
            droppedItemOnGroundDurationInSecondsEntry = Bind(config, Section, "droppedItemOnGroundDurationInSeconds", 3600f, 0f, 3600f,
                "Set duration that dropped items stay on the ground before they are despawning. Game default is 3600 seconds.");
            itemsFloatInWaterEntry = Bind(config, Section, "itemsFloatInWater", false,
                "Items dropped always float in water.");
        }
    }
}

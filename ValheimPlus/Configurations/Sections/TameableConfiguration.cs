using ValheimPlus.GameClasses;

namespace ValheimPlus.Configurations.Sections
{
    public class TameableConfiguration : ServerSyncConfig<TameableConfiguration>
    {
        public AnimalType animalTypes { get; internal set; } = AnimalType.All;
        public int mortality { get; internal set; } = 0;
        public bool ownerDamageOverride { get; internal set; } = false;
        public float stunRecoveryTime { get; internal set; } = 10f;
        public bool stunInformation { get; internal set; } = false;
        public float tameTimeMultiplier { get; internal set; } = 0f;
        public float tameBoostMultiplier { get; internal set; } = 0f;
        public float tameBoostRangeMultiplier { get; internal set; } = 0f;
        public bool ignoreHunger { get; internal set; } = false;
        public bool ignoreAlerted { get; internal set; } = false;
    }
}

namespace ValheimPlus.Configurations.Sections
{
	public class TameableConfiguration : ServerSyncConfig<TameableConfiguration>
    {
        public int mortality { get; internal set; } = 0;
        public bool ownerDamageOverride { get; internal set; } = false;
        public float stunRecoveryTime { get; internal set; } = 10f;
        public bool stunInformation { get; internal set; } = false;
        public float tameTime { get; internal set; } = 1800f;
        public float tameBoostRange { get; internal set; } = 60f;
        public float tameBoostMultiplier { get; internal set; } = 2f;
		public bool ignoreHunger { get; internal set; } = false;
		public bool ignoreAlerted { get; internal set; } = false;
	}
}

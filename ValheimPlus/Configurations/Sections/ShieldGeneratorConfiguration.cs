namespace ValheimPlus.Configurations.Sections
{
    public class ShieldGeneratorConfiguration : ServerSyncConfig<ShieldGeneratorConfiguration>
    {
        public bool infiniteFuel { get; internal set; } = false;
        public bool autoFuel { get; internal set; } = false;
        public bool ignorePrivateAreaCheck { get; internal set; } = true;
        public float autoRange { get; internal set; } = 10;
    }
}

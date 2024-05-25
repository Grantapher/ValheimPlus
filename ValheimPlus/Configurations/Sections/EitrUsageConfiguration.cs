namespace ValheimPlus.Configurations.Sections
{
    public class EitrUsageConfiguration : ServerSyncConfig<EitrUsageConfiguration>
    {
        public float bloodMagic { get; internal set; } = 0;
        public float elementalMagic { get; internal set; } = 0;
    }
}

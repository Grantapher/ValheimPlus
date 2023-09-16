namespace ValheimPlus.Configurations.Sections
{
    public class DemisterConfiguration : ServerSyncConfig<DemisterConfiguration>
    {
        public float wispLight { get; internal set; } = 0;
        public float wispTorch { get; internal set; } = 0;
        public float Mistwalker { get; internal set; } = 0;
    }
}

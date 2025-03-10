namespace ValheimPlus.Configurations.Sections
{
    public class EggConfiguration : ServerSyncConfig<EggConfiguration>
    {
        public bool showHatchTime { get; set; } = false;
        public float hatchTime { get; set; } = 300f;
        public float growTime { get; set; } = 3000f;
        public bool requireShelter { get; set; } = true;
        public bool canStack { get; set; } = false;
        public bool soldByDefault { get; set; } = false;
        public int sellPrice { get; set; } = 1500;
    }
}

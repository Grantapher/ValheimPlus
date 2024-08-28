namespace ValheimPlus.Configurations.Sections
{
    public class MapConfiguration : ServerSyncConfig<MapConfiguration>
    {
        public bool shareMapProgression { get; internal set; } = false;
        public float exploreRadius { get; internal set; } = 50;
        public bool preventPlayerFromTurningOffPublicPosition { get; internal set; } = false;
        public bool shareAllPins { get; internal set; } = false;
        public bool displayCartsAndBoats { get; internal set; } = false;
    }
}

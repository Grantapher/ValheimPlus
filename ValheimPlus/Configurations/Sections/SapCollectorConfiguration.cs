namespace ValheimPlus.Configurations.Sections
{
    public class SapCollectorConfiguration : ServerSyncConfig<SapCollectorConfiguration>
    {
        public float sapProductionSpeed { get; internal set; } = 60;
        public int maximumSapPerCollector { get; internal set; } = 10;
        public bool autoDeposit { get; internal set; } = false;
        public float autoDepositRange { get; internal set; } = 10;
        public bool showDuration { get; internal set; } = false;
    }
}
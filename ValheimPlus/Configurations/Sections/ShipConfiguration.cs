namespace ValheimPlus.Configurations.Sections
{
    public class ShipConfiguration : ServerSyncConfig<ShipConfiguration>
    {
        public float forwardSpeed { get; internal set; } = 0f;
        public float backwardSpeed { get; internal set; } = 0f;
        public float rudderSpeed { get; internal set; } = 0f;
        public float steerForce { get; internal set; } = 0f;
        public float waterImpactDamage { get; internal set; } = 0f;
    }
}

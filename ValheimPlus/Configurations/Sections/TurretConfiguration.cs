namespace ValheimPlus.Configurations.Sections
{
    public class TurretConfiguration : ServerSyncConfig<TurretConfiguration>
    {
        public bool enablePvP { get; internal set; } = false;
        public bool unlimitedAmmo { get; internal set; } = false;
        public float turnRate { get; internal set; } = 22.5f;
        public float attackCooldown { get; internal set; } = 2f;
        public float viewDistance { get; internal set; } = 45f;
        public bool targetTamed { get; internal set; } = false;
        public float horizontalAngle { get; internal set; } = 50f;
        public float verticalAngle { get; internal set; } = 50f;
        public bool fixProjectiles { get; internal set; } = false;
    }
}
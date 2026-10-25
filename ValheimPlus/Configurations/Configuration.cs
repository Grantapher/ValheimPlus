using ValheimPlus.Configurations.Sections;

namespace ValheimPlus.Configurations
{
    public class Configuration
    {
        public static Configuration Current { get; set; }
        public AdvancedBuildingModeConfiguration AdvancedBuildingMode { get; set; }
        public AdvancedEditingModeConfiguration AdvancedEditingMode { get; set; }
        public ArmorConfiguration Armor { get; set; }
        public AutoStackConfiguration AutoStack { get; set; }
        public BedConfiguration Bed { get; set; }
        public BeehiveConfiguration Beehive { get; set; }
        public BrightnessConfiguration Brightness { get; set; }
        public BuildingConfiguration Building { get; set; }
        public CameraConfiguration Camera { get; set; }
        public ChatConfiguration Chat { get; set; }
        public CraftFromChestConfiguration CraftFromChest { get; set; }
        public DemisterConfiguration Demister { get; set; }
        public DurabilityConfiguration Durability { get; set; }
        public EggConfiguration Egg { get; set; }
        public EitrRefineryConfiguration EitrRefinery { get; set; }
        public EitrUsageConfiguration EitrUsage { get; set; }
        public ExperienceConfiguration Experience { get; set; }
        public FermenterConfiguration Fermenter { get; set; }
        public FireSourceConfiguration FireSource { get; set; }
        public FirstPersonConfiguration FirstPerson { get; internal set; }
        public FoodConfiguration Food { get; set; }
        public FreePlacementRotationConfiguration FreePlacementRotation { get; set; }
        public FrigidKilnConfiguration FrigidKiln { get; set; }
        public FrostFoundryConfiguration FrostFoundry { get; set; }
        public FurnaceConfiguration Furnace { get; set; }
        public GameConfiguration Game { get; set; }
        public GameClockConfiguration GameClock { get; set; }
        public GatherConfiguration Gathering { get; set; }
        public GridAlignmentConfiguration GridAlignment { get; set; }
        public HealthUsageConfiguration HealthUsage { get; set; }
        public HotkeyConfiguration Hotkeys { get; set; }
        public HotTubConfiguration HotTub { get; set; }
        public HudConfiguration Hud { get; set; }
        public InventoryConfiguration Inventory { get; set; }
        public ItemsConfiguration Items { get; set; }
        public KilnConfiguration Kiln { get; set; }
        public LootDropConfiguration LootDrop { get; set; }
        public MapConfiguration Map { get; set; }
        public MonsterProjectileConfiguration MonsterProjectile { get; set; }
        public OvenConfiguration Oven { get; set; }
        public PickableConfiguration Pickable { get; set; }
        public PlayerConfiguration Player { get; set; }
        public PlayerProjectileConfiguration PlayerProjectile { get; set; }
        public ProcreationConfiguration Procreation { get; set; }
        public SapCollectorConfiguration SapCollector { get; set; }
        public ServerConfiguration Server { get; set; }
        public ShieldGeneratorConfiguration ShieldGenerator { get; set; }
        public ShieldConfiguration Shields { get; set; }
        public ShipConfiguration Ship { get; set; }
        public SmelterConfiguration Smelter { get; set; }
        public SpinningWheelConfiguration SpinningWheel { get; set; }
        public StaminaConfiguration Stamina { get; set; }
        public StaminaUsageConfiguration StaminaUsage { get; set; }
        public StructuralIntegrityConfiguration StructuralIntegrity { get; set; }
        public TameableConfiguration Tameable { get; set; }
        public TimeConfiguration Time { get; set; }
        public TurretConfiguration Turret { get; set; }
        public WagonConfiguration Wagon { get; set; }
        public WardConfiguration Ward { get; set; }
        public WindmillConfiguration Windmill { get; set; }
        public WispSpawnerConfiguration WispSpawner { get; set; }
        public WorkbenchConfiguration Workbench { get; set; }
    }
}

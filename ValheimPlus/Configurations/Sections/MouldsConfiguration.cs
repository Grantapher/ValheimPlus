using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class MouldsConfiguration : BaseConfig
    {
        private const string Section = "Moulds";

        private ConfigEntry<float> callerDropChanceMultiplierEntry;
        private ConfigEntry<float> vanguardDropChanceMultiplierEntry;
        private ConfigEntry<float> protectorDropChanceMultiplierEntry;

        public float callerDropChanceMultiplier => callerDropChanceMultiplierEntry.Value;
        public float vanguardDropChanceMultiplier => vanguardDropChanceMultiplierEntry.Value;
        public float protectorDropChanceMultiplier => protectorDropChanceMultiplierEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable armor mould drop chance modifiers. These settings affect helmet, chest, and leg moulds only and do not change drop quantities. Each modifier combines multiplicatively with [LootDrop] lootDropChanceMultiplier when that section is enabled.");
            callerDropChanceMultiplierEntry = Bind(config, Section, "callerDropChanceMultiplier", 0f,
                "Modify the drop chance of each Caller armor mould (helmet, chest, and legs) from Hexen by a percentage. In Valheim 1.0.20, each piece has a 5% base chance, unaffected by creature stars. 0 keeps the existing chance, 100 doubles it, 200 triples it, -50 halves it, and -100 disables these drops. Combines multiplicatively with [LootDrop] lootDropChanceMultiplier when that section is enabled.");
            vanguardDropChanceMultiplierEntry = Bind(config, Section, "vanguardDropChanceMultiplier", 0f,
                "Modify the drop chance of each Vanguard armor mould (helmet, chest, and legs) from either Krigen variant by a percentage. In Valheim 1.0.20, each piece has a 3% base chance: 6% with one star and 12% with two stars. 0 keeps the existing chance, 100 doubles it, 200 triples it, -50 halves it, and -100 disables these drops. Combines multiplicatively with [LootDrop] lootDropChanceMultiplier when that section is enabled.");
            protectorDropChanceMultiplierEntry = Bind(config, Section, "protectorDropChanceMultiplier", 0f,
                "Modify the drop chance of each Protector armor mould (helmet, chest, and legs) from either Krigen variant by a percentage. In Valheim 1.0.20, each piece has a 3% base chance: 6% with one star and 12% with two stars. 0 keeps the existing chance, 100 doubles it, 200 triples it, -50 halves it, and -100 disables these drops. Combines multiplicatively with [LootDrop] lootDropChanceMultiplier when that section is enabled.");
        }
    }
}

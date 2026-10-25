using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class MouldsConfiguration : BaseConfig
    {
        private const string Section = "Moulds";
        private const string ModifierHint =
            " 0 keeps the normal chance, 100 doubles it, and -100 disables these drops.";

        private ConfigEntry<float> callerDropChanceMultiplierEntry;
        private ConfigEntry<float> vanguardDropChanceMultiplierEntry;
        private ConfigEntry<float> protectorDropChanceMultiplierEntry;

        public float callerDropChanceMultiplier => callerDropChanceMultiplierEntry.Value;
        public float vanguardDropChanceMultiplier => vanguardDropChanceMultiplierEntry.Value;
        public float protectorDropChanceMultiplier => protectorDropChanceMultiplierEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section. Modifies the drop chance of armor moulds from creatures. Stacks with [LootDrop] lootDropChanceMultiplier.");
            callerDropChanceMultiplierEntry = Bind(config, Section, "callerDropChanceMultiplier", 0f,
                "Modify the drop chance of Caller armor moulds (helmet, chest and legs) by a percentage." + ModifierHint);
            vanguardDropChanceMultiplierEntry = Bind(config, Section, "vanguardDropChanceMultiplier", 0f,
                "Modify the drop chance of Vanguard armor moulds (helmet, chest and legs) by a percentage." + ModifierHint);
            protectorDropChanceMultiplierEntry = Bind(config, Section, "protectorDropChanceMultiplier", 0f,
                "Modify the drop chance of Protector armor moulds (helmet, chest and legs) by a percentage." + ModifierHint);
        }
    }
}

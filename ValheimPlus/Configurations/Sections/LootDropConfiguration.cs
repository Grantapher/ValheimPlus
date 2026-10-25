using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class LootDropConfiguration : BaseConfig
    {
        private const string Section = "LootDrop";
        private const string ItemGroupChanceHint =
            " Stacks with lootDropChanceMultiplier. 0 keeps the normal chance, 100 doubles it, and -100 disables these drops.";

        private ConfigEntry<float> lootDropAmountMultiplierEntry;
        private ConfigEntry<float> lootDropChanceMultiplierEntry;
        private ConfigEntry<float> callerMouldDropChanceMultiplierEntry;
        private ConfigEntry<float> vanguardMouldDropChanceMultiplierEntry;
        private ConfigEntry<float> protectorMouldDropChanceMultiplierEntry;

        public float lootDropAmountMultiplier => lootDropAmountMultiplierEntry.Value;
        public float lootDropChanceMultiplier => lootDropChanceMultiplierEntry.Value;
        public float callerMouldDropChanceMultiplier => callerMouldDropChanceMultiplierEntry.Value;
        public float vanguardMouldDropChanceMultiplier => vanguardMouldDropChanceMultiplierEntry.Value;
        public float protectorMouldDropChanceMultiplier => protectorMouldDropChanceMultiplierEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section, if you set this to false the mode will not be accesible");
            lootDropAmountMultiplierEntry = Bind(config, Section, "lootDropAmountMultiplier", 0f,
                "Change the amount of loot dropped when creatures or monsters are slain.\nA value of -100 will eliminate all drops, 0 will have no effect, 100 will double drops, 200 will triple and so on.");
            lootDropChanceMultiplierEntry = Bind(config, Section, "lootDropChanceMultiplier", 0f,
                "Change the chance of loot dropping when creatures or monsters are slain.\nA value of -100 will eliminate all drops, 0 will have no effect, 100 will double the percent of getting a drop, 200 will triple and so on.\nExample: If a drop has a 40% chance, setting this to 200 will make that chance 80%,\nand setting it to 300 will make it 100% (120% technically, but anything above 100% acts as 100%)");
            callerMouldDropChanceMultiplierEntry = Bind(config, Section, "callerMouldDropChanceMultiplier", 0f,
                "Modify the drop chance of Caller armor moulds (helmet, chest and legs) by a percentage." + ItemGroupChanceHint);
            vanguardMouldDropChanceMultiplierEntry = Bind(config, Section, "vanguardMouldDropChanceMultiplier", 0f,
                "Modify the drop chance of Vanguard armor moulds (helmet, chest and legs) by a percentage." + ItemGroupChanceHint);
            protectorMouldDropChanceMultiplierEntry = Bind(config, Section, "protectorMouldDropChanceMultiplier", 0f,
                "Modify the drop chance of Protector armor moulds (helmet, chest and legs) by a percentage." + ItemGroupChanceHint);
        }
    }
}

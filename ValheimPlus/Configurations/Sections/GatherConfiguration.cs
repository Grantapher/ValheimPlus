using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class GatherConfiguration : BaseConfig
    {
        private const string Section = "Gathering";
        private const string ModifierHint =
            " 0 keeps the normal amount, 100 doubles it, and -100 removes the drops.";

        private ConfigEntry<float> woodEntry;
        private ConfigEntry<float> fineWoodEntry;
        private ConfigEntry<float> coreWoodEntry;
        private ConfigEntry<float> elderBarkEntry;
        private ConfigEntry<float> yggdrasilWoodEntry;
        private ConfigEntry<float> timberwoodEntry;
        private ConfigEntry<float> stoneEntry;
        private ConfigEntry<float> blackMarbleEntry;
        private ConfigEntry<float> tinOreEntry;
        private ConfigEntry<float> copperOreEntry;
        private ConfigEntry<float> copperScrapEntry;
        private ConfigEntry<float> ironScrapEntry;
        private ConfigEntry<float> silverOreEntry;
        private ConfigEntry<float> petrifiedTissueEntry;
        private ConfigEntry<float> chitinEntry;
        private ConfigEntry<float> featherEntry;
        private ConfigEntry<float> dropChanceEntry;
        private ConfigEntry<float> graustenEntry;
        private ConfigEntry<float> blackwoodEntry;
        private ConfigEntry<float> flametalOreEntry;
        private ConfigEntry<float> proustitePowderEntry;
        private ConfigEntry<float> obsidianEntry;
        private ConfigEntry<float> iceEntry;
        private ConfigEntry<float> softTissueEntry;

        public float wood => woodEntry.Value;
        public float fineWood => fineWoodEntry.Value;
        public float coreWood => coreWoodEntry.Value;
        public float elderBark => elderBarkEntry.Value;
        public float yggdrasilWood => yggdrasilWoodEntry.Value;
        public float timberwood => timberwoodEntry.Value;
        public float stone => stoneEntry.Value;
        public float blackMarble => blackMarbleEntry.Value;
        public float tinOre => tinOreEntry.Value;
        public float copperOre => copperOreEntry.Value;
        public float copperScrap => copperScrapEntry.Value;
        public float ironScrap => ironScrapEntry.Value;
        public float silverOre => silverOreEntry.Value;
        public float petrifiedTissue => petrifiedTissueEntry.Value;
        public float chitin => chitinEntry.Value;
        public float feather => featherEntry.Value;
        public float dropChance => dropChanceEntry.Value;
        public float grausten => graustenEntry.Value;
        public float blackwood => blackwoodEntry.Value;
        public float flametalOre => flametalOreEntry.Value;
        public float proustitePowder => proustitePowderEntry.Value;
        public float obsidian => obsidianEntry.Value;
        public float ice => iceEntry.Value;
        public float softTissue => softTissueEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section. This section contains modifiers. Modifiers are increases and reduction in percent declared by 50, or -50.");
            woodEntry = Bind(config, Section, "wood", 0f,
                "Modify the amount of Wood dropped from trees and logs by a percentage." + ModifierHint);
            fineWoodEntry = Bind(config, Section, "fineWood", 0f,
                "Modify the amount of Finewood dropped from trees and logs by a percentage." + ModifierHint);
            coreWoodEntry = Bind(config, Section, "coreWood", 0f,
                "Modify the amount of Corewood dropped from trees and logs by a percentage." + ModifierHint);
            elderBarkEntry = Bind(config, Section, "elderBark", 0f,
                "Modify the amount of Ancient Bark dropped from trees and logs by a percentage." + ModifierHint);
            yggdrasilWoodEntry = Bind(config, Section, "yggdrasilWood", 0f,
                "Modify the amount of Yggdrasil Wood dropped from trees and logs by a percentage." + ModifierHint);
            timberwoodEntry = Bind(config, Section, "timberwood", 0f,
                "Modify the amount of Timberwood dropped from trees and logs by a percentage." + ModifierHint);
            stoneEntry = Bind(config, Section, "stone", 0f,
                "Modify the amount of Stone dropped from rocks and ore deposits by a percentage." + ModifierHint);
            blackMarbleEntry = Bind(config, Section, "blackMarble", 0f,
                "Modify the amount of Black Marble dropped from rocks and ore deposits by a percentage." + ModifierHint);
            tinOreEntry = Bind(config, Section, "tinOre", 0f,
                "Modify the amount of Tin Ore dropped from rocks and ore deposits by a percentage." + ModifierHint);
            copperOreEntry = Bind(config, Section, "copperOre", 0f,
                "Modify the amount of Copper Ore dropped from rocks and ore deposits by a percentage." + ModifierHint);
            copperScrapEntry = Bind(config, Section, "copperScrap", 0f,
                "Modify the amount of Copper Scrap dropped from destroyed objects by a percentage. Copper Scrap dropped by creatures is not affected." + ModifierHint);
            ironScrapEntry = Bind(config, Section, "ironScrap", 0f,
                "Modify the amount of Scrap Iron dropped from destroyed objects by a percentage." + ModifierHint);
            silverOreEntry = Bind(config, Section, "silverOre", 0f,
                "Modify the amount of Silver Ore dropped from rocks and ore deposits by a percentage." + ModifierHint);
            petrifiedTissueEntry = Bind(config, Section, "petrifiedTissue", 0f,
                "Modify the amount of Petrified Tissue dropped when petrified rotten trolls are destroyed by a percentage." + ModifierHint);
            chitinEntry = Bind(config, Section, "chitin", 0f,
                "Modify the amount of Chitin dropped from destroyed objects by a percentage." + ModifierHint);
            featherEntry = Bind(config, Section, "feather", 0f,
                "Modify the amount of Feathers dropped by a percentage. This includes drops from trees and from shooting gulls and crows." + ModifierHint);
            dropChanceEntry = Bind(config, Section, "dropChance", 0f,
                "Modify the chance to drop resources from resource nodes affected by this category. This only works on resource nodes that do not have guaranteed drops.\nAs example by default scrap piles in dungeons have a 20% chance to drop a item, if you set this option to 200, you will then have a 60% chance to drop iron.");
            graustenEntry = Bind(config, Section, "grausten", 0f,
                "Modify the amount of Grausten dropped from rocks and ore deposits by a percentage." + ModifierHint);
            blackwoodEntry = Bind(config, Section, "blackwood", 0f,
                "Modify the amount of Ashwood dropped from trees and logs by a percentage." + ModifierHint);
            flametalOreEntry = Bind(config, Section, "flametalOre", 0f,
                "Modify the amount of Flametal Ore dropped from rocks and ore deposits by a percentage." + ModifierHint);
            proustitePowderEntry = Bind(config, Section, "proustitePowder", 0f,
                "Modify the amount of Proustite Powder dropped from destroyed objects by a percentage." + ModifierHint);
            obsidianEntry = Bind(config, Section, "obsidian", 0f,
                "Modify the amount of Obsidian dropped from rocks and ore deposits by a percentage." + ModifierHint);
            iceEntry = Bind(config, Section, "ice", 0f,
                "Modify the amount of Ice dropped from ice formations and frozen objects by a percentage." + ModifierHint);
            softTissueEntry = Bind(config, Section, "softTissue", 0f,
                "Modify the amount of Soft Tissue dropped from destroyed objects by a percentage." + ModifierHint);
        }
    }
}

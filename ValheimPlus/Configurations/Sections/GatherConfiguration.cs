using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class GatherConfiguration : BaseConfig
    {
        private const string Section = "Gathering";

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

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section. This section contains modifiers. Modifiers are increases and reduction in percent declared by 50, or -50.");
            woodEntry = Bind(config, Section, "wood", 0f,
                "Each of these values increase or reduce the dropped items from destroyed objects with tools (Stones, Trees, Resource nodes, etc.) by %.\nThe value 50 will increase the dropped wood from trees from 10 to 15. The value -50 will reduce the amount of dropped wood from 10 to 5.");
            fineWoodEntry = Bind(config, Section, "fineWood", 0f,
                "Each of these values increase or reduce the dropped items from destroyed objects with tools (Stones, Trees, Resource nodes, etc.) by %.\nThe value 50 will increase the dropped wood from trees from 10 to 15. The value -50 will reduce the amount of dropped wood from 10 to 5.");
            coreWoodEntry = Bind(config, Section, "coreWood", 0f,
                "Each of these values increase or reduce the dropped items from destroyed objects with tools (Stones, Trees, Resource nodes, etc.) by %.\nThe value 50 will increase the dropped wood from trees from 10 to 15. The value -50 will reduce the amount of dropped wood from 10 to 5.");
            elderBarkEntry = Bind(config, Section, "elderBark", 0f,
                "Each of these values increase or reduce the dropped items from destroyed objects with tools (Stones, Trees, Resource nodes, etc.) by %.\nThe value 50 will increase the dropped wood from trees from 10 to 15. The value -50 will reduce the amount of dropped wood from 10 to 5.");
            yggdrasilWoodEntry = Bind(config, Section, "yggdrasilWood", 0f,
                "Each of these values increase or reduce the dropped items from destroyed objects with tools (Stones, Trees, Resource nodes, etc.) by %.\nThe value 50 will increase the dropped wood from trees from 10 to 15. The value -50 will reduce the amount of dropped wood from 10 to 5.");
            timberwoodEntry = Bind(config, Section, "timberwood", 0f,
                "Modify the amount of Timberwood dropped from trees and logs by a percentage. 0 keeps the normal amount, 100 doubles it, and -100 removes the drops.");
            stoneEntry = Bind(config, Section, "stone", 0f,
                "Each of these values increase or reduce the dropped items from destroyed objects with tools (Stones, Trees, Resource nodes, etc.) by %.\nThe value 50 will increase the dropped wood from trees from 10 to 15. The value -50 will reduce the amount of dropped wood from 10 to 5.");
            blackMarbleEntry = Bind(config, Section, "blackMarble", 0f,
                "Each of these values increase or reduce the dropped items from destroyed objects with tools (Stones, Trees, Resource nodes, etc.) by %.\nThe value 50 will increase the dropped wood from trees from 10 to 15. The value -50 will reduce the amount of dropped wood from 10 to 5.");
            tinOreEntry = Bind(config, Section, "tinOre", 0f,
                "Each of these values increase or reduce the dropped items from destroyed objects with tools (Stones, Trees, Resource nodes, etc.) by %.\nThe value 50 will increase the dropped wood from trees from 10 to 15. The value -50 will reduce the amount of dropped wood from 10 to 5.");
            copperOreEntry = Bind(config, Section, "copperOre", 0f,
                "Each of these values increase or reduce the dropped items from destroyed objects with tools (Stones, Trees, Resource nodes, etc.) by %.\nThe value 50 will increase the dropped wood from trees from 10 to 15. The value -50 will reduce the amount of dropped wood from 10 to 5.");
            copperScrapEntry = Bind(config, Section, "copperScrap", 0f,
                "copperScrap doesn't increase drop rate from looting, ex. killing Dvergrs.");
            ironScrapEntry = Bind(config, Section, "ironScrap", 0f,
                "copperScrap doesn't increase drop rate from looting, ex. killing Dvergrs.");
            silverOreEntry = Bind(config, Section, "silverOre", 0f,
                "copperScrap doesn't increase drop rate from looting, ex. killing Dvergrs.");
            petrifiedTissueEntry = Bind(config, Section, "petrifiedTissue", 0f,
                "Modify the amount of Petrified Tissue dropped when petrified rotten trolls are destroyed by a percentage. 0 keeps the normal amount, 100 doubles it, and -100 removes the drops.");
            chitinEntry = Bind(config, Section, "chitin", 0f,
                "copperScrap doesn't increase drop rate from looting, ex. killing Dvergrs.");
            featherEntry = Bind(config, Section, "feather", 0f,
                "feather will also affect the drops from shooting gulls/crows, as well as drops from trees.");
            dropChanceEntry = Bind(config, Section, "dropChance", 0f,
                "Modify the chance to drop resources from resource nodes affected by this category. This only works on resource nodes that do not have guaranteed drops.\nAs example by default scrap piles in dungeons have a 20% chance to drop a item, if you set this option to 200, you will then have a 60% chance to drop iron.");
            graustenEntry = Bind(config, Section, "grausten", 0f,
                "Each of these values increase or reduce the dropped items from destroyed objects with tools (Stones, Trees, Resource nodes, etc.) by %.\nThe value 50 will increase the dropped wood from trees from 10 to 15. The value -50 will reduce the amount of dropped wood from 10 to 5.");
            blackwoodEntry = Bind(config, Section, "blackwood", 0f,
                "Each of these values increase or reduce the dropped items from destroyed objects with tools (Stones, Trees, Resource nodes, etc.) by %.\nThe value 50 will increase the dropped wood from trees from 10 to 15. The value -50 will reduce the amount of dropped wood from 10 to 5.");
            flametalOreEntry = Bind(config, Section, "flametalOre", 0f,
                "Ashlands");
            proustitePowderEntry = Bind(config, Section, "proustitePowder", 0f,
                "Ashlands");
        }
    }
}

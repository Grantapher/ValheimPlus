using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class ExperienceConfiguration : BaseConfig
    {
        private const string Section = "Experience";

        private ConfigEntry<float> swordsEntry;
        private ConfigEntry<float> knivesEntry;
        private ConfigEntry<float> clubsEntry;
        private ConfigEntry<float> polearmsEntry;
        private ConfigEntry<float> spearsEntry;
        private ConfigEntry<float> blockingEntry;
        private ConfigEntry<float> axesEntry;
        private ConfigEntry<float> bowsEntry;
        private ConfigEntry<float> elementalMagicEntry;
        private ConfigEntry<float> bloodMagicEntry;
        private ConfigEntry<float> unarmedEntry;
        private ConfigEntry<float> pickaxesEntry;
        private ConfigEntry<float> woodCuttingEntry;
        private ConfigEntry<float> crossbowsEntry;
        private ConfigEntry<float> jumpEntry;
        private ConfigEntry<float> sneakEntry;
        private ConfigEntry<float> runEntry;
        private ConfigEntry<float> swimEntry;
        private ConfigEntry<float> fishingEntry;
        private ConfigEntry<float> cookingEntry;
        private ConfigEntry<float> farmingEntry;
        private ConfigEntry<float> craftingEntry;
        private ConfigEntry<float> dodgeEntry;
        private ConfigEntry<float> rideEntry;

        public float swords => swordsEntry.Value;
        public float knives => knivesEntry.Value;
        public float clubs => clubsEntry.Value;
        public float polearms => polearmsEntry.Value;
        public float spears => spearsEntry.Value;
        public float blocking => blockingEntry.Value;
        public float axes => axesEntry.Value;
        public float bows => bowsEntry.Value;
        public float elementalMagic => elementalMagicEntry.Value;
        public float bloodMagic => bloodMagicEntry.Value;
        public float unarmed => unarmedEntry.Value;
        public float pickaxes => pickaxesEntry.Value;
        public float woodCutting => woodCuttingEntry.Value;
        public float crossbows => crossbowsEntry.Value;
        public float jump => jumpEntry.Value;
        public float sneak => sneakEntry.Value;
        public float run => runEntry.Value;
        public float swim => swimEntry.Value;
        public float fishing => fishingEntry.Value;
        public float cooking => cookingEntry.Value;
        public float farming => farmingEntry.Value;
        public float crafting => craftingEntry.Value;
        public float dodge => dodgeEntry.Value;
        public float ride => rideEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section. This section contains modifiers.\nModifiers are increases and reduction in percent declared by 50, or -50. The value 50 will increase experience gained by 50%, -50 will reduce experience gained by 50%.");
            swordsEntry = Bind(config, Section, "swords", 0f,
                "The modifier value for the experience gained of swords.");
            knivesEntry = Bind(config, Section, "knives", 0f,
                "The modifier value for the experience gained of knives.");
            clubsEntry = Bind(config, Section, "clubs", 0f,
                "The modifier value for the experience gained of clubs.");
            polearmsEntry = Bind(config, Section, "polearms", 0f,
                "The modifier value for the experience gained of polearms.");
            spearsEntry = Bind(config, Section, "spears", 0f,
                "The modifier value for the experience gained of spears.");
            blockingEntry = Bind(config, Section, "blocking", 0f,
                "The modifier value for the experience gained of blocking.");
            axesEntry = Bind(config, Section, "axes", 0f,
                "The modifier value for the experience gained of axes.");
            bowsEntry = Bind(config, Section, "bows", 0f,
                "The modifier value for the experience gained of bows.");
            elementalMagicEntry = Bind(config, Section, "elementalMagic", 0f,
                "The modifier value for the experience gained of elemental magic.");
            bloodMagicEntry = Bind(config, Section, "bloodMagic", 0f,
                "The modifier value for the experience gained of blood magic.");
            unarmedEntry = Bind(config, Section, "unarmed", 0f,
                "The modifier value for the experience gained of unarmed.");
            pickaxesEntry = Bind(config, Section, "pickaxes", 0f,
                "The modifier value for the experience gained of mining.");
            woodCuttingEntry = Bind(config, Section, "woodCutting", 0f,
                "The modifier value for the experience gained of wood cutting.");
            crossbowsEntry = Bind(config, Section, "crossbows", 0f,
                "The modifier value for the experience gained of crossbows.");
            jumpEntry = Bind(config, Section, "jump", 0f,
                "The modifier value for the experience gained of jumping.");
            sneakEntry = Bind(config, Section, "sneak", 0f,
                "The modifier value for the experience gained of sneaking.");
            runEntry = Bind(config, Section, "run", 0f,
                "The modifier value for the experience gained of running.");
            swimEntry = Bind(config, Section, "swim", 0f,
                "The modifier value for the experience gained of swimming.");
            fishingEntry = Bind(config, Section, "fishing", 0f,
                "The modifier value for the experience gained of fishing.");
            cookingEntry = Bind(config, Section, "cooking", 0f,
                "The modifier value for the experience gained of cooking.");
            farmingEntry = Bind(config, Section, "farming", 0f,
                "The modifier value for the experience gained of farming.");
            craftingEntry = Bind(config, Section, "crafting", 0f,
                "The modifier value for the experience gained of crafting.");
            dodgeEntry = Bind(config, Section, "dodge", 0f,
                "The modifier value for the experience gained of dodging.");
            rideEntry = Bind(config, Section, "ride", 0f,
                "The modifier value for the experience gained of riding.");
        }
    }
}

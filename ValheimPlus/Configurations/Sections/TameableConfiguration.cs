using BepInEx.Configuration;
using ValheimPlus.GameClasses;

namespace ValheimPlus.Configurations.Sections
{
    public class TameableConfiguration : BaseConfig
    {
        private const string Section = "Tameable";

        private ConfigEntry<AnimalType> animalTypesEntry;
        private ConfigEntry<int> mortalityEntry;
        private ConfigEntry<bool> ownerDamageOverrideEntry;
        private ConfigEntry<float> stunRecoveryTimeEntry;
        private ConfigEntry<bool> stunInformationEntry;
        private ConfigEntry<float> tameTimeMultiplierEntry;
        private ConfigEntry<float> tameBoostMultiplierEntry;
        private ConfigEntry<float> tameBoostRangeMultiplierEntry;
        private ConfigEntry<bool> ignoreHungerEntry;
        private ConfigEntry<bool> ignoreAlertedEntry;

        public AnimalType animalTypes => animalTypesEntry.Value;
        public int mortality => mortalityEntry.Value;
        public bool ownerDamageOverride => ownerDamageOverrideEntry.Value;
        public float stunRecoveryTime => stunRecoveryTimeEntry.Value;
        public bool stunInformation => stunInformationEntry.Value;
        public float tameTimeMultiplier => tameTimeMultiplierEntry.Value;
        public float tameBoostMultiplier => tameBoostMultiplierEntry.Value;
        public float tameBoostRangeMultiplier => tameBoostRangeMultiplierEntry.Value;
        public bool ignoreHunger => ignoreHungerEntry.Value;
        public bool ignoreAlerted => ignoreAlertedEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            animalTypesEntry = Bind(config, Section, "animalTypes", AnimalType.All,
                "A comma-separated list of animals that can be tamed.\nValid types are: boar, hen, wolf, lox, asksvin, all, none");
            mortalityEntry = Bind(config, Section, "mortality", 0, 0, 2,
                "Modify what happens when a tamed creature is attacked.\n0 = normal, 1 = essential(deadly attacks stun instead of kill, tamed creatures can still die rarely), 2 = immortal.");
            ownerDamageOverrideEntry = Bind(config, Section, "ownerDamageOverride", true,
                "This will circumvent the mortality setting, so even if tamed creatures are immortal, players can still kill them with a butcher knife.\nFor this option to work you need to have mortality to set to either essential or immortal.");
            stunRecoveryTimeEntry = Bind(config, Section, "stunRecoveryTime", 10f,
                "How long it takes for a tamed creature to recover if mortality is set to 1(essential) and they are stunned.");
            stunInformationEntry = Bind(config, Section, "stunInformation", false,
                "If the tamed creature is recovering from a stun, then add Stunned to the hover text on mouse over.");
            tameTimeMultiplierEntry = Bind(config, Section, "tameTimeMultiplier", 0f,
                "A multiplier for the amount of time it takes to fully tame a creature in seconds.\nA value of 100 will double the time it takes to tame a creature, -100 will instantly tame the creature.");
            tameBoostMultiplierEntry = Bind(config, Section, "tameBoostMultiplier", 0f,
                "A multiplier for the taming bonus provided by the Brew of animal whispers\nA value of 100 will double the taming bonus, -100 will prevent a creature from taming while the buff is in effect.");
            tameBoostRangeMultiplierEntry = Bind(config, Section, "tameBoostRangeMultiplier", 0f,
                "A multiplier for the range that a taming boost can be applied to a creature.\nA value of 100 will double the range, -100 will prevent the buff from being applied.");
            ignoreHungerEntry = Bind(config, Section, "ignoreHunger", false,
                "Set to true to ignore hunger requirements while taming.\nStill requires you food to initiate the taming process.");
            ignoreAlertedEntry = Bind(config, Section, "ignoreAlerted", false,
                "Set to true to allow taming even when the creature is alerted.\nFor more information see https://valheim.fandom.com/wiki/Creature_senses");
        }
    }
}

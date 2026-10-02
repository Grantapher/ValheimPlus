using BepInEx.Configuration;
using ValheimPlus.GameClasses;

namespace ValheimPlus.Configurations.Sections
{
    public class ProcreationConfiguration : BaseConfig
    {
        private const string Section = "Procreation";

        private ConfigEntry<AnimalType> animalTypesEntry;
        private ConfigEntry<bool> loveInformationEntry;
        private ConfigEntry<bool> offspringInformationEntry;
        private ConfigEntry<float> requiredLovePointsMultiplierEntry;
        private ConfigEntry<float> pregnancyDurationMultiplierEntry;
        private ConfigEntry<float> pregnancyChanceMultiplierEntry;
        private ConfigEntry<float> partnerCheckRangeMultiplierEntry;
        private ConfigEntry<bool> ignoreHungerEntry;
        private ConfigEntry<bool> ignoreAlertedEntry;
        private ConfigEntry<float> creatureLimitMultiplierEntry;
        private ConfigEntry<float> maturityDurationMultiplierEntry;

        public AnimalType animalTypes => animalTypesEntry.Value;
        public bool loveInformation => loveInformationEntry.Value;
        public bool offspringInformation => offspringInformationEntry.Value;
        public float requiredLovePointsMultiplier => requiredLovePointsMultiplierEntry.Value;
        public float pregnancyDurationMultiplier => pregnancyDurationMultiplierEntry.Value;
        public float pregnancyChanceMultiplier => pregnancyChanceMultiplierEntry.Value;
        public float partnerCheckRangeMultiplier => partnerCheckRangeMultiplierEntry.Value;
        public bool ignoreHunger => ignoreHungerEntry.Value;
        public bool ignoreAlerted => ignoreAlertedEntry.Value;
        public float creatureLimitMultiplier => creatureLimitMultiplierEntry.Value;
        public float maturityDurationMultiplier => maturityDurationMultiplierEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            animalTypesEntry = Bind(config, Section, "animalTypes", AnimalType.All,
                "A comma-separated list of animals that can be tamed.\nValid types are: boar, hen, wolf, lox, asksvin, moose, all, none");
            loveInformationEntry = Bind(config, Section, "loveInformation", false,
                "Set to true to display the amount of love points a creature has.\nWhen they become pregnant it will display the amount of time until they give birth.");
            offspringInformationEntry = Bind(config, Section, "offspringInformation", false,
                "Set to true to display the amount of time a newborn creature will take to grow up.");
            requiredLovePointsMultiplierEntry = Bind(config, Section, "requiredLovePointsMultiplier", 0f,
                "A multiplier for the amount of successful checks required for a creature to become pregnant\nA value of 100 will double the amount of successful checks required\n-100 will remove the requirement and the creature will instantly become pregnant.");
            pregnancyDurationMultiplierEntry = Bind(config, Section, "pregnancyDurationMultiplier", 0f,
                "A multiplier for the time it takes for a creature to give birth after becoming pregnant.\nA value of 100 will double the pregnancy duration, -100 will cause the creature to give birth instantly.");
            pregnancyChanceMultiplierEntry = Bind(config, Section, "pregnancyChanceMultiplier", 0f,
                "A multiplier for the chance of a creature gaining a love point.\nA value of 100 will double the chance of gaining a love point, -100 will prevent the creature from gaining a love point.");
            partnerCheckRangeMultiplierEntry = Bind(config, Section, "partnerCheckRangeMultiplier", 0f,
                "A multiplier for the range that a creature can gain a love point from another creature in meters.\nA value of 100 will double the range, -100 will make them unable to procreate.");
            ignoreHungerEntry = Bind(config, Section, "ignoreHunger", false,
                "Set to true to ignore hunger requirements while breeding.\nAnimals will not require food to initiate the breeding process.");
            ignoreAlertedEntry = Bind(config, Section, "ignoreAlerted", false,
                "Set to true to allow animals to breed even when they are alerted\nFor more information see https://valheim.fandom.com/wiki/Creature_senses");
            creatureLimitMultiplierEntry = Bind(config, Section, "creatureLimitMultiplier", 0f,
                "A multiplier for the amount of offspring that can be nearby a creature before they will stop breeding.\nA value of 100 will double the amount of offspring that can be nearby, -100 will make them unable to breed.");
            maturityDurationMultiplierEntry = Bind(config, Section, "maturityDurationMultiplier", 0f,
                "A multiplier for the amount of time it takes for a creature to grow up after being born. Does not apply to eggs.\nA value of 100 will double the time it takes to grow into an adult, -100 will cause the offspring to immediately mature.");
        }
    }
}

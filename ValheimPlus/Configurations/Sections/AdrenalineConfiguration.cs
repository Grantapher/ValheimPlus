using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class AdrenalineConfiguration : BaseConfig
    {
        private const string Section = "Adrenaline";

        private ConfigEntry<float> gainMultiplierEntry;
        private ConfigEntry<float> decayMultiplierEntry;
        private ConfigEntry<float> trinketCostMultiplierEntry;
        private ConfigEntry<float> surgeDurationMultiplierEntry;

        public float gainMultiplier => gainMultiplierEntry.Value;
        public float decayMultiplier => decayMultiplierEntry.Value;
        public float trinketCostMultiplier => trinketCostMultiplierEntry.Value;
        public float surgeDurationMultiplier => surgeDurationMultiplierEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false, "Change false to true to enable this section. ");
            gainMultiplierEntry = Bind(config, Section, "gainMultiplier", 0f,
                "Modify how much adrenaline you gain by a percentage. " +
                "100 doubles it and -100 stops gaining it.");
            decayMultiplierEntry = Bind(config, Section, "decayMultiplier", 0f,
                "Modify how quickly adrenaline drains when you stop gaining it by a percentage. " +
                "100 drains twice as fast and -100 stops it draining.");
            trinketCostMultiplierEntry = Bind(config, Section, "trinketCostMultiplier", 0f,
                "Modify how much adrenaline your equipped trinkets need before they activate by a percentage. " +
                "100 doubles it and -50 halves it.\nValues below -99 are treated as -99.");
            surgeDurationMultiplierEntry = Bind(config, Section, "surgeDurationMultiplier", 0f,
                "Modify how long a trinket's effect lasts once activated by a percentage. " +
                "100 doubles it and -50 halves it.\nValues below -99 are treated as -99.");
        }
    }
}

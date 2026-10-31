using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class BrightnessConfiguration : BaseConfig
    {
        private const string Section = "Brightness";

        private ConfigEntry<float> nightBrightnessMultiplierEntry;

        /* changing brightness during a period of day had a coupling affect with other period, need further development
        public float morningBrightnessMultiplier { get; set; } = 0f;
        public float dayBrightnessMultiplier { get; set; } = 0f;
        public float eveningBrightnessMultiplier { get; set; } = 0f;
        */
        public float nightBrightnessMultiplier => nightBrightnessMultiplierEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            nightBrightnessMultiplierEntry = Bind(config, Section, "nightBrightnessMultiplier", 0f,
                "Changes how bright it looks at night. 5 is about double, 10 about 2.8x, 20 about 4.2x. Negative values darken.");
        }
    }
}

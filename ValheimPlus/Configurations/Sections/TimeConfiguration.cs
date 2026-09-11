using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class TimeConfiguration : BaseConfig
    {
        private const string Section = "Time";

        private ConfigEntry<bool> forcePartOfDayEntry;
        private ConfigEntry<float> forcePartOfDayTimeEntry;
        private ConfigEntry<float> totalDayTimeInSecondsEntry;
        private ConfigEntry<float> nightPercentEntry;

        public bool forcePartOfDay => forcePartOfDayEntry.Value;
        public float forcePartOfDayTime => forcePartOfDayTimeEntry.Value;
        public float totalDayTimeInSeconds => totalDayTimeInSecondsEntry.Value;
        public float nightPercent => nightPercentEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            forcePartOfDayEntry = Bind(config, Section, "forcePartOfDay", false,
                "Enables forcing a specific time of day. This option disables other day duration settings.");
            forcePartOfDayTimeEntry = Bind(config, Section, "forcePartOfDayTime", 0.5f, 0f, 1f,
                "The part of day the time should be frozen to. 0 would be middle of night, 0.5 will be middle of day");
            totalDayTimeInSecondsEntry = Bind(config, Section, "totalDayTimeInSeconds", 1800f,
                "Sets the duration of a whole day. This will affect the day count and may change time of day once after activation (new Worlds are not affected).");
            nightPercentEntry = Bind(config, Section, "nightPercent", 30f, 0f, 200f,
                "What percent of time is night. 0 is all daytime, 100 is all nighttime. Default is 30.");
        }
    }
}

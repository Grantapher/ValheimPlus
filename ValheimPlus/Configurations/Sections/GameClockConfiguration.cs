using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class GameClockConfiguration : BaseConfig
    {
        private const string Section = "GameClock";

        private ConfigEntry<bool> useAMPMEntry;
        private ConfigEntry<int> textFontSizeEntry;
        private ConfigEntry<int> textRedChannelEntry;
        private ConfigEntry<int> textGreenChannelEntry;
        private ConfigEntry<int> textBlueChannelEntry;
        private ConfigEntry<int> textTransparencyChannelEntry;

        public bool useAMPM => useAMPMEntry.Value;
        public int textFontSize => textFontSizeEntry.Value;
        public int textRedChannel => textRedChannelEntry.Value;
        public int textGreenChannel => textGreenChannelEntry.Value;
        public int textBlueChannel => textBlueChannelEntry.Value;
        public int textTransparencyChannel => textTransparencyChannelEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            useAMPMEntry = Bind(config, Section, "useAMPM", false,
                "Change time formatting from 24hr to AM-PM.");
            textFontSizeEntry = Bind(config, Section, "textFontSize", 34,
                "Change font size of time text.");
            textRedChannelEntry = Bind(config, Section, "textRedChannel", 248, 0, 255,
                "Change how red the time text is (51/255).");
            textGreenChannelEntry = Bind(config, Section, "textGreenChannel", 105, 0, 255,
                "Change how green the time text is (51/255).");
            textBlueChannelEntry = Bind(config, Section, "textBlueChannel", 0, 0, 255,
                "Change how blue the time text is (51/255).");
            textTransparencyChannelEntry = Bind(config, Section, "textTransparencyChannel", 255, 0, 255,
                "Change how transparent the time text is (255 is solid with no transparency).");
        }
    }
}

using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class MapConfiguration : BaseConfig
    {
        private const string Section = "Map";

        private ConfigEntry<bool> shareMapProgressionEntry;
        private ConfigEntry<float> exploreRadiusEntry;
        private ConfigEntry<bool> preventPlayerFromTurningOffPublicPositionEntry;
        private ConfigEntry<bool> shareAllPinsEntry;
        private ConfigEntry<bool> displayCartsAndBoatsEntry;

        public bool shareMapProgression => shareMapProgressionEntry.Value;
        public float exploreRadius => exploreRadiusEntry.Value;
        public bool preventPlayerFromTurningOffPublicPosition => preventPlayerFromTurningOffPublicPositionEntry.Value;
        public bool shareAllPins => shareAllPinsEntry.Value;
        public bool displayCartsAndBoats => displayCartsAndBoatsEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            shareMapProgressionEntry = Bind(config, Section, "shareMapProgression", false,
                "With this enabled you will receive the same exploration progression as other players on the server.\nThis will also enable the option for the server to sync everyones exploration progression on connecting to the server.");
            exploreRadiusEntry = Bind(config, Section, "exploreRadius", 100f,
                "The radius of the map that you explore when moving.");
            preventPlayerFromTurningOffPublicPositionEntry = Bind(config, Section, "preventPlayerFromTurningOffPublicPosition", false,
                "Prevents you and other people on the server to turn off their map sharing option.");
            shareAllPinsEntry = Bind(config, Section, "shareAllPins", false,
                "Shares map pins with everyone on the server.");
            displayCartsAndBoatsEntry = Bind(config, Section, "displayCartsAndBoats", false,
                "Display carts and boats on the map");
        }
    }
}

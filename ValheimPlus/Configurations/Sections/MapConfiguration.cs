using BepInEx.Configuration;

using UnityEngine;

namespace ValheimPlus.Configurations.Sections
{
    public class MapConfiguration : BaseConfig
    {
        private const string Section = "Map";

        private ConfigEntry<bool> shareMapProgressionEntry;
        private ConfigEntry<float> exploreRadiusEntry;
        private ConfigEntry<bool> preventPlayerFromTurningOffPublicPositionEntry;
        private ConfigEntry<bool> displayCartsAndBoatsEntry;
        private ConfigEntry<bool> sharePinsEntry;
        private ConfigEntry<KeyCode> privatePinKeyEntry;
        private ConfigEntry<KeyCode> publishPinKeyEntry;

        public bool shareMapProgression => shareMapProgressionEntry.Value;
        public float exploreRadius => exploreRadiusEntry.Value;
        public bool preventPlayerFromTurningOffPublicPosition => preventPlayerFromTurningOffPublicPositionEntry.Value;
        public bool displayCartsAndBoats => displayCartsAndBoatsEntry.Value;
        public bool sharePins => sharePinsEntry?.Value ?? false;
        public KeyCode privatePinKey => privatePinKeyEntry.Value;
        public KeyCode publishPinKey => publishPinKeyEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            sharePinsEntry = Bind(config, Section, "sharePins", false,
                "Share newly placed map pins through this server. Hold privatePinKey while placing a pin to keep it private. Public pins persist on the server; only their creator or an admin can delete them. Requires this V+ feature on server and clients. Disable TXC SharedMap before enabling.");
            privatePinKeyEntry = BindLocal(config, Section, "privatePinKey", KeyCode.LeftControl,
                "Hold while placing a new map pin to keep it private when sharePins is enabled.");
            publishPinKeyEntry = BindLocal(config, Section, "publishPinKey", KeyCode.LeftShift,
                "Hold and left-click a private pin to publish it to the server.");
            shareMapProgressionEntry = Bind(config, Section, "shareMapProgression", false,
                "With this enabled you will receive the same exploration progression as other players on the server.\nThis will also enable the option for the server to sync everyones exploration progression on connecting to the server.");
            exploreRadiusEntry = Bind(config, Section, "exploreRadius", 100f,
                "The radius of the map that you explore when moving.");
            preventPlayerFromTurningOffPublicPositionEntry = Bind(config, Section, "preventPlayerFromTurningOffPublicPosition", false,
                "Prevents you and other people on the server to turn off their map sharing option.");
            displayCartsAndBoatsEntry = Bind(config, Section, "displayCartsAndBoats", false,
                "Display carts and boats on the map");
        }
    }
}

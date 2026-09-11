using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class AutoStackConfiguration : BaseConfig
    {
        private const string Section = "AutoStack";

        private ConfigEntry<float> autoStackAllRangeEntry;
        private ConfigEntry<bool> autoStackAllIgnorePrivateAreaCheckEntry;
        private ConfigEntry<bool> autoStackAllIgnoreEquipmentEntry;
        private ConfigEntry<bool> ignoreAmmoEntry;
        private ConfigEntry<bool> ignoreFoodEntry;
        private ConfigEntry<bool> ignoreMeadEntry;
        private ConfigEntry<float> replyTimeoutEntry;

        public float autoStackAllRange => autoStackAllRangeEntry.Value;
        public bool autoStackAllIgnorePrivateAreaCheck => autoStackAllIgnorePrivateAreaCheckEntry.Value;
        public bool autoStackAllIgnoreEquipment => autoStackAllIgnoreEquipmentEntry.Value;
        public bool ignoreAmmo => ignoreAmmoEntry.Value;
        public bool ignoreFood => ignoreFoodEntry.Value;
        public bool ignoreMead => ignoreMeadEntry.Value;
        public float replyTimeout => replyTimeoutEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Set to true to automatically perform the \"Stack All\" action on all chests in range.");
            autoStackAllRangeEntry = Bind(config, Section, "autoStackAllRange", 10f, 1f, 50f,
                "Defines the range to search chests for the \"Stack All\" action.");
            autoStackAllIgnorePrivateAreaCheckEntry = Bind(config, Section, "autoStackAllIgnorePrivateAreaCheck", false,
                "This option prevents to \"Stack All\" into chests from warded areas if the player doesnt have access to it.");
            autoStackAllIgnoreEquipmentEntry = Bind(config, Section, "autoStackAllIgnoreEquipment", false,
                "Set to true to prevent equipable items to be stored automatically.");
            ignoreAmmoEntry = Bind(config, Section, "ignoreAmmo", false,
                "Set to true to prevent arrows and bolts to be stored automatically.");
            ignoreFoodEntry = Bind(config, Section, "ignoreFood", false,
                "Set to true to prevent food items to be stored automatically.");
            ignoreMeadEntry = Bind(config, Section, "ignoreMead", false,
                "Set to true to prevent mead to be stored automatically.");
            replyTimeoutEntry = Bind(config, Section, "replyTimeout", 1f, 0.25f, 5f,
                "Seconds to wait for nearby chests to answer a \"Stack All\", and then again for another player's chests to be handed over, before giving up on the ones that have not. Raise it on a high latency server.");
        }
    }
}

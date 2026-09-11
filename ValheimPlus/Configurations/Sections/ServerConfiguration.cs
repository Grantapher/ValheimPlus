using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class ServerConfiguration : BaseConfig
    {
        private const string Section = "Server";

        private ConfigEntry<int> maxPlayersEntry;
        private ConfigEntry<bool> disableServerPasswordEntry;
        private ConfigEntry<bool> enforceModEntry;
        private ConfigEntry<bool> serverSyncsConfigEntry;

        public int maxPlayers => maxPlayersEntry.Value;
        public bool disableServerPassword => disableServerPasswordEntry.Value; // todo supposedly not working correctly
        public bool enforceMod => enforceModEntry.Value;
        public bool serverSyncsConfig => serverSyncsConfigEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, true,
                "Change false to true to enable this section.");
            maxPlayersEntry = Bind(config, Section, "maxPlayers", 10, 1, 32,
                "Modify the maximum amount of players on your Server.");
            disableServerPasswordEntry = Bind(config, Section, "disableServerPassword", false,
                "Removes the requirement to have a server password.");
            enforceModEntry = Bind(config, Section, "enforceMod", true,
                "This settings add a version control check to make sure that people that try to join your game or the server you try to join has V+ installed\nWE HEAVILY RECOMMEND TO NEVER DISABLE THIS!");
            serverSyncsConfigEntry = Bind(config, Section, "serverSyncsConfig", true,
                "Changes whether the server will force it's config on clients that connect. Only affects servers.\nWE HEAVILY RECOMMEND TO NEVER DISABLE THIS!");
        }
    }
}

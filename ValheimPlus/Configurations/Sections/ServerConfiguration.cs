using System.Security.Policy;
using UnityEngine;

namespace ValheimPlus.Configurations.Sections
{
    public class ServerConfiguration : BaseConfig<ServerConfiguration>
    {
        public int maxPlayers { get; internal set; } = 10;
        public bool disableServerPassword { get; internal set; } = false;
        public bool enforceMod { get; internal set; } = true;
        /// <summary>
        /// Changes whether the server will force it's config on clients that connect. Only affects servers.
        /// WE HEAVILY RECOMMEND TO NEVER DISABLE THIS! 
        /// </summary>
        [LoadingOption(LoadingMode.RemoteOnly)]
        public bool serverSyncsConfig { get; internal set; } = true;
        /// <summary>
        /// If false allows you to keep your own defined hotkeys instead of synchronising the ones declared for the server.
        /// Sections need to be enabled in your local configuration to load hotkeys.
        /// This is a client side setting and not affected by server settings.
        /// </summary>
        [LoadingOption(LoadingMode.LocalOnly)]
        public bool serverSyncHotkeys { get; internal set; } = true;
    }

}

using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    /// <summary>
    /// Alters max player count
    /// </summary>
    // Now manually patched since steamworks isn't on the xbox game pass version of the game
    // [HarmonyPatch(typeof(SteamGameServer), "SetMaxPlayerCount")]
    // TODO is this used at all anymore?
    public static class ChangeSteamServerVariables
    {
        public static void Prefix(ref int cPlayersMax)
        {
            if (Configuration.Current.Server.IsEnabled)
            {
                int maxPlayers = Configuration.Current.Server.maxPlayers;
                if (maxPlayers >= 1)
                {
                    cPlayersMax = maxPlayers;
                }
            }
        }
    }
}

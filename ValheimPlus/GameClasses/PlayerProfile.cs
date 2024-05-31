using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    /// <summary>
    /// This skips the Intro from version 0.218.16 
    /// </summary>
    [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.LoadPlayerFromDisk))]
    public static class PlayerProfile_LoadPlayerFromDisk_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ref PlayerProfile __instance)
        {
            if (!Configuration.Current.Player.IsEnabled || !Configuration.Current.Player.skipIntro) return;
            __instance.m_firstSpawn = false;
        }
    }
}
using HarmonyLib;
using ValheimPlus.Configurations;
using static HarmonyLib.AccessTools;

namespace ValheimPlus.GameClasses
{
    class LuredWispModification
    {
        [HarmonyPatch(typeof(LuredWisp), "Awake")]
        public static class LuredWispPatch
        {
            static readonly FieldRef<LuredWisp, bool> m_despawnInDaylight = FieldRefAccess<LuredWisp, bool>("m_despawnInDaylight");

            [HarmonyPrefix]
            static void Prefix(LuredWisp __instance)
            {
                if (Configuration.Current.WispSpawner.IsEnabled)
                {
                    m_despawnInDaylight(__instance) = Configuration.Current.WispSpawner.onlySpawnAtNight;
                }
            }
        }
    }
}
using HarmonyLib;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.IsValidTarget))]
    public static class Projectile_IsValidTarget_Patch
    {
        private static void Postfix(IDestructible destr, Projectile __instance, ref bool __result)
        {
            var turretConfig = Configuration.Current.Turret;
            if (!turretConfig.IsEnabled || !turretConfig.fixProjectiles) return;

            if (__result && destr is Turret turret)
            {
                var turretOwner = TurretHelpers.GetPlayerCreator(turret);
                if (turretOwner == __instance.m_owner)
                {
                    ValheimPlusPlugin.Logger.LogInfo("Turret projectile hit itself");
                    __result = false;
                }
            }
        }
    }
}

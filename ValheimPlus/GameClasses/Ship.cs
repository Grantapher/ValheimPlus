using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
    public static class Ship_Awake_Patch
    {
        [UsedImplicitly]
        public static void Postfix(Ship __instance)
        {
            var shipConfig = Configuration.Current.Ship;
            if (!shipConfig.IsEnabled) return;

            Helper.applyModifierValueTo(ref __instance.m_force, shipConfig.forwardSpeed);
            Helper.applyModifierValueTo(ref __instance.m_stearForce, shipConfig.steerForce);
            Helper.applyModifierValueTo(ref __instance.m_backwardForce, shipConfig.backwardSpeed);
            Helper.applyModifierValueTo(ref __instance.m_waterImpactDamage, shipConfig.waterImpactDamage);
            Helper.applyModifierValueTo(ref __instance.m_rudderSpeed, shipConfig.rudderSpeed);
        }
    }
}

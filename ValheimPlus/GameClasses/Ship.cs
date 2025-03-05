using HarmonyLib;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    public class Ship_CustomFixedUpdate_Patch
    {
        private static float oldForce = 0f;
        private static float oldSteerForce = 0f;
        private static float oldBackwardForce = 0f;
        private static float oldWaterImpactDamage = 0f;
        private static float oldRudderSpeed = 0f;

        public static void Prefix(Ship __instance)
        {
            if (!Configuration.Current.Ship.IsEnabled)
                return;

            oldForce = __instance.m_force;
            oldSteerForce = __instance.m_stearForce;
            oldBackwardForce = __instance.m_backwardForce;
            oldWaterImpactDamage = __instance.m_waterImpactDamage;
            oldRudderSpeed = __instance.m_rudderSpeed;

            var shipConfig = Configuration.Current.Ship;
            var forceMultiplier = shipConfig.force / 100f + 1f;
            var steerForceMultiplier = shipConfig.steerForce / 100f + 1f;
            var backwardForceMultiplier = shipConfig.backwardForce / 100f + 1f;
            var waterImpactDamageMultiplier = shipConfig.waterImpactDamage / 100f + 1f;
            var rudderSpeedMultiplier = shipConfig.rudderSpeed / 100f + 1f;

            __instance.m_force = forceMultiplier * oldForce;
            __instance.m_stearForce = steerForceMultiplier * oldSteerForce;
            __instance.m_backwardForce = backwardForceMultiplier * oldBackwardForce;
            __instance.m_waterImpactDamage = waterImpactDamageMultiplier * oldWaterImpactDamage;
            __instance.m_rudderSpeed = rudderSpeedMultiplier * oldRudderSpeed;
        }

        public static void Postfix(Ship __instance)
        {
            if (!Configuration.Current.Ship.IsEnabled)
                return;

            __instance.m_force = oldForce;
            __instance.m_stearForce = oldSteerForce;
            __instance.m_backwardForce = oldBackwardForce;
            __instance.m_waterImpactDamage = oldWaterImpactDamage;
            __instance.m_rudderSpeed = oldRudderSpeed;
        }
    }
}

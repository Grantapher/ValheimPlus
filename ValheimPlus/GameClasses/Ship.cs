using HarmonyLib;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    public class Ship_CustomFixedUpdate_Patch
    {
        private static float oldSailForceFactor = 0f;
        private static float oldSailForceOffset = 0f;
        private static float oldSteerForce = 0f;
        private static float oldBackwardForce = 0f;
        private static float oldWaterImpactDamage = 0f;
        private static float oldRudderSpeed = 0f;

        // Unknown if this is actually needed
        private static float oldWaterLevelOffset = 0f;

        public static void Prefix(Ship __instance)
        {
            if (!Configuration.Current.Ship.IsEnabled)
                return;

            oldSailForceFactor = __instance.m_sailForceFactor;
            oldSailForceOffset = __instance.m_sailForceOffset;
            oldSteerForce = __instance.m_stearForce;
            oldBackwardForce = __instance.m_backwardForce;
            oldWaterImpactDamage = __instance.m_waterImpactDamage;
            oldRudderSpeed = __instance.m_rudderSpeed;
            oldWaterLevelOffset = __instance.m_waterLevelOffset;

            var shipConfig = Configuration.Current.Ship;
            var sailForceMultiplier = shipConfig.sailForce / 100f + 1f;
            var sailForceOffsetMultiplier = shipConfig.sailForceOffset / 100f + 1f;
            var steerForceMultiplier = shipConfig.steerForce / 100f + 1f;
            var backwardForceMultiplier = shipConfig.backwardForce / 100f + 1f;
            var waterImpactDamageMultiplier = shipConfig.waterImpactDamage / 100f + 1f;
            var rudderSpeedMultiplier = shipConfig.rudderSpeed / 100f + 1f;
            var waterLevelOffsetMultiplier = shipConfig.waterLevel / 100f + 1f;

            __instance.m_sailForceFactor = sailForceMultiplier * oldSailForceFactor;
            __instance.m_sailForceOffset = sailForceOffsetMultiplier * oldSailForceOffset;
            __instance.m_stearForce = steerForceMultiplier * oldSteerForce;
            __instance.m_backwardForce = backwardForceMultiplier * oldBackwardForce;
            __instance.m_waterImpactDamage = waterImpactDamageMultiplier * oldWaterImpactDamage;
            __instance.m_rudderSpeed = rudderSpeedMultiplier * oldRudderSpeed;
            __instance.m_waterLevelOffset = waterLevelOffsetMultiplier * oldWaterLevelOffset;
        }

        public static void Postfix(Ship __instance)
        {
            if (!Configuration.Current.Ship.IsEnabled)
                return;

            __instance.m_sailForceFactor = oldSailForceFactor;
            __instance.m_sailForceOffset = oldSailForceOffset;
            __instance.m_stearForce = oldSteerForce;
            __instance.m_backwardForce = oldBackwardForce;
            __instance.m_waterImpactDamage = oldWaterImpactDamage;
            __instance.m_rudderSpeed = oldRudderSpeed;
            __instance.m_waterLevelOffset = oldWaterLevelOffset;
        }
    }
}

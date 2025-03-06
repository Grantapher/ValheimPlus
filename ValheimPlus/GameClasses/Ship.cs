using HarmonyLib;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
    public class Ship_Awake_Patch
    {
        public static void Postfix(Ship __instance)
        {
            if (!Configuration.Current.Ship.IsEnabled)
                return;

            var shipConfig = Configuration.Current.Ship;
            __instance.m_force = Helper.applyModifierValue(__instance.m_force, shipConfig.forwardSpeed);
            __instance.m_stearForce = Helper.applyModifierValue(__instance.m_stearForce, shipConfig.steerForce);
            __instance.m_backwardForce = Helper.applyModifierValue(__instance.m_backwardForce, shipConfig.backwardSpeed);
            __instance.m_waterImpactDamage = Helper.applyModifierValue(__instance.m_waterImpactDamage, shipConfig.waterImpactDamage);
            __instance.m_rudderSpeed = Helper.applyModifierValue(__instance.m_rudderSpeed, shipConfig.rudderSpeed);
        }
    }
}

using HarmonyLib;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    class CraftingStationChanges
    {
        private static void UpdateSpawnSuppression(CraftingStation station)
        {
            var config = Configuration.Current.Workbench;
            if (!config.IsEnabled || !(config.workbenchRange > 0)) return;

            var collider = station.m_effectAreaCollider;
            if (!collider) return;

            var area = collider.GetComponent<EffectArea>();
            if (!area || (area.m_type & EffectArea.Type.PlayerBase) == 0) return;

            float radius = config.workbenchEnemySpawnRange > 0
                ? config.workbenchEnemySpawnRange
                : config.workbenchRange;
            if (collider is SphereCollider sphere && sphere.radius != radius)
                sphere.radius = radius;
            else if (collider is CapsuleCollider capsule && capsule.radius != radius)
                capsule.radius = radius;
        }

        /// <summary>
        /// Alter workbench range
        /// </summary>
        [HarmonyPatch(typeof(CraftingStation), "Start")]
        public static class WorkbenchRangeIncrease
        {
            private static void Prefix(CraftingStation __instance, ref float ___m_rangeBuild, GameObject ___m_areaMarker)
            {
                var config = Configuration.Current.Workbench;
                if (!config.IsEnabled || !(config.workbenchRange > 0)) return;

                ___m_rangeBuild = config.workbenchRange;
                UpdateSpawnSuppression(__instance);

                if (___m_areaMarker)
                {
                    var projector = ___m_areaMarker.GetComponent<CircleProjector>();
                    if (projector) projector.m_radius = ___m_rangeBuild;
                    float scaleIncrease = config.workbenchRange / 20f;
                    ___m_areaMarker.transform.localScale = new Vector3(scaleIncrease, 1f, scaleIncrease);
                }
            }
        }

        [HarmonyPatch(typeof(CraftingStation), "GetExtensions")]
        public static class WorkbenchSpawnRange
        {
            private static void Postfix(CraftingStation __instance)
            {
                // Native extension refreshes also resize the PlayerBase collider to the
                // building range. Keep the independently configured spawn radius intact.
                UpdateSpawnSuppression(__instance);
            }
        }

        /// <summary>
        /// Disable roof requirement on workbench
        /// </summary>
        [HarmonyPatch(typeof(CraftingStation), "CheckUsable")]
        public static class WorkbenchRemoveRestrictions
        {
            private static bool Prefix(ref CraftingStation __instance, ref Player player, ref bool showMessage, ref bool __result)
            {
                if(Configuration.Current.Workbench.disableRoofCheck && Configuration.Current.Workbench.IsEnabled)
                {
                    __instance.m_craftRequireRoof = false;
                }

                return true;
            }
        }
    }
}

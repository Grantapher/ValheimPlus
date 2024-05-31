using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Turret), nameof(Turret.Awake))]
    public static class Turret_Awake_Patch
    {
        /// <summary>
        /// Configure the turret on wakeup
        /// </summary>
        [UsedImplicitly]
        private static void Prefix(Turret __instance)
        {
            var config = Configuration.Current.Turret;
            if (!config.IsEnabled) return;
            if (config.ignorePlayers) __instance.m_targetPlayers = false;
            __instance.m_turnRate = Helper.applyModifierValue(__instance.m_turnRate, config.turnRate);
            __instance.m_attackCooldown = Helper.applyModifierValue(__instance.m_attackCooldown, config.attackCooldown);
            __instance.m_viewDistance = Helper.applyModifierValue(__instance.m_viewDistance, config.viewDistance);
        }
    }

    /// <summary>
    /// Remove ammo update on Turret.ShootProjectile resulting in unlimited ammo
    /// </summary>
    [HarmonyPatch(typeof(Turret), nameof(Turret.ShootProjectile))]
    public static class Turret_ShootProjectile_Patch
    {
        // ReSharper disable once InconsistentNaming wants ZDO to be Zdo
        private static readonly MethodInfo Method_ZDO_Set =
            AccessTools.Method(typeof(ZDO), nameof(ZDO.Set), new[] { typeof(int), typeof(int), typeof(bool) });

        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var config = Configuration.Current.Turret;
            if (!config.IsEnabled || !config.unlimitedAmmo) return instructions;

            var il = instructions.ToList();
            for (int i = 0; i < il.Count; ++i)
            {
                if (il[i].Calls(Method_ZDO_Set))
                {
                    // remove set ZDO code so ammo count is never updated!
                    il.RemoveRange(i - 8, 9);
                    return il.AsEnumerable();
                }
            }

            ValheimPlusPlugin.Logger.LogError("Couldn't transpile `Turret.ShootProjectile`!");
            return il.AsEnumerable();
        }
    }
}

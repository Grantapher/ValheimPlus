using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using ValheimPlus.Configurations;
using System.Linq;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Turret), nameof(Turret.Awake))]
    public static class Turret_Awake_Patch
    {
        /// <summary>
        /// Configure the turret on wakeup
        /// </summary>
        static void Prefix(Turret __instance)
        {
            if (!Configuration.Current.Turret.IsEnabled) return;

            if(Configuration.Current.Turret.ignorePlayers)
                __instance.m_targetPlayers = false;

            __instance.m_turnRate = Helper.applyModifierValue(__instance.m_turnRate, Configuration.Current.Turret.turnRate);

            __instance.m_attackCooldown = Helper.applyModifierValue(__instance.m_attackCooldown, Configuration.Current.Turret.attackCooldown);

            __instance.m_viewDistance = Helper.applyModifierValue(__instance.m_viewDistance, Configuration.Current.Turret.viewDistance);

        }
    }

    /// <summary>
    /// Remove ammo update on Turret.ShootProjectile resulting in unlimited ammo
    /// </summary>
    [HarmonyPatch(typeof(Turret), nameof(Turret.ShootProjectile))]
    public static class Turret_ShootProjectile_Patch
    {
        private static MethodInfo method_ZDO_Set = AccessTools.Method(typeof(ZDO), nameof(ZDO.Set), new System.Type[] { typeof(int), typeof(int), typeof(bool)});

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.Turret.IsEnabled || !Configuration.Current.Turret.unlimitedAmmo) return instructions;

            List<CodeInstruction> il = instructions.ToList();

            for (int i = 0; i < il.Count; ++i)
            {
                if (il[i].Calls(method_ZDO_Set))
                {
                    // remove set ZDO code so ammo count is never updated!
                    il.RemoveRange(i-8, 9);
                    break;
                }
            }

            return il.AsEnumerable();
        }
    }
}

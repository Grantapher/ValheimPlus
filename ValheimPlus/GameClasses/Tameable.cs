using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using ValheimPlus;
using ValheimPlus.Configurations;
using ValheimPlus.Utility;

namespace ValheimPlus.GameClasses
{
    public enum TameableMortalityTypes
    {
        Normal,
        Essential,
        Immortal
    }

    /// <summary>
    /// Adds a text indicator so player's know when an animal they've tamed has been stunned.
    /// </summary>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    public static class Tameable_GetHoverText_Patch
    {
        public static void Postfix(Tameable __instance, ref string __result)
        {
            if (Configuration.Current.Tameable.IsEnabled && Configuration.Current.Tameable.stunInformation)
            {
                // If tamed creature is recovering from a stun, then add Stunned to hover text.
                if (__instance.m_character.m_nview.GetZDO().GetBool("isRecoveringFromStun"))
                    __result = __result.Insert(__result.IndexOf(" )"), ", Stunned");
            }
        }
    }

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Awake))]
	public static class Tameable_Awake_Patch
    {
		public static void Postfix(Tameable __instance)
		{
            if (!Configuration.Current.Tameable.IsEnabled)
                return;

            __instance.m_tamingTime = Configuration.Current.Tameable.tameTime;
            __instance.m_tamingSpeedMultiplierRange = Configuration.Current.Tameable.tameBoostRange;
            __instance.m_tamingBoostMultiplier = Configuration.Current.Tameable.tameBoostMultiplier;

			if (__instance.m_nview.GetZDO().GetFloat(ZDOVars.s_tameTimeLeft, out float timeLeft))
                if (timeLeft > __instance.m_tamingTime)
					__instance.m_nview.GetZDO().Set(ZDOVars.s_tameTimeLeft, __instance.m_tamingTime);
		}
	}
}

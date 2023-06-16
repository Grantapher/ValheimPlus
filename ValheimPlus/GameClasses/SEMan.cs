﻿using HarmonyLib;
using System;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    // Modify length of in multiplayer and singleplayer casted guradian powers including around the player.
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), new Type[] { typeof(StatusEffect), typeof(bool), typeof(int), typeof(float) })]
    public static class SEMan_AddStatusEffect_Patch
    {
        private static void Postfix(ref SEMan __instance, ref StatusEffect statusEffect, bool resetTime = false, int itemLevel = 0, float skillLevel = 0)
        {

            if (!Configuration.Current.Player.IsEnabled)
                return;

            // Don't execute if the affected person is not the player
            if (!__instance.m_character.IsPlayer())
                return;

            // Every guardian power starts with GP_
            if (statusEffect.name.StartsWith("GP_"))
            {
                foreach (StatusEffect buff in __instance.m_statusEffects)
                {
                    var statusEffectPlayerInstance = __instance.GetStatusEffect(statusEffect.NameHash());
                    if (buff.m_name == statusEffectPlayerInstance.m_name)
                    {
                        statusEffectPlayerInstance.m_ttl = Configuration.Current.Player.guardianBuffDuration;
                    }
                }
            }
        }
    }
}

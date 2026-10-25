using HarmonyLib;
using System;
using JetBrains.Annotations;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    // Modify length of in multiplayer and singleplayer casted guradian powers including around the player.
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), new Type[] { typeof(StatusEffect), typeof(bool), typeof(int), typeof(float), typeof(short) })]
    public static class SEMan_AddStatusEffect_Patch
    {
        private static void Postfix(ref SEMan __instance, ref StatusEffect statusEffect, bool resetTime = false, int itemLevel = 0, float skillLevel = 0, short variant = -1)
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

    /// <summary>
    /// Scales a trinket surge on the player's own copy of the effect, so the shared item data is never changed.
    /// </summary>
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), typeof(StatusEffect), typeof(bool), typeof(int),
        typeof(float), typeof(short))]
    public static class SEMan_AddStatusEffect_AdrenalineSurge_Patch
    {
        [UsedImplicitly]
        private static void Postfix(SEMan __instance, StatusEffect statusEffect, StatusEffect __result)
        {
            var config = Configuration.Current.Adrenaline;
            if (!config.IsEnabled || config.surgeDurationMultiplier == 0f) return;
            if (__result == null) return;
            if (__instance.m_character is not Player player || !IsEquippedSurge(player, statusEffect)) return;

            // A ttl of 0 means the effect never ends, so leave those alone.
            if (statusEffect.m_ttl <= 0f) return;
            __result.m_ttl = Helper.applyModifierValue(statusEffect.m_ttl,
                Math.Max(config.surgeDurationMultiplier, -99f));
        }

        private static bool IsEquippedSurge(Player player, StatusEffect statusEffect)
        {
            foreach (var item in player.GetInventory().GetAllItems())
                if (item.m_equipped && item.m_shared.m_fullAdrenalineSE == statusEffect) return true;
            return false;
        }
    }
}

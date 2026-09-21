using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;
using static Skills;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
    public static class Skills_RaiseSkill_Patch
    {
        /// <summary>
        /// Apply experience modifications.
        /// </summary>
        [UsedImplicitly]
        private static void Prefix(ref SkillType skillType, ref float factor)
        {
            var config = Configuration.Current.Experience;
            if (!config.IsEnabled) return;

            var modifier = skillType switch
            {
                SkillType.Swords => config.swords,
                SkillType.Knives => config.knives,
                SkillType.Clubs => config.clubs,
                SkillType.Polearms => config.polearms,
                SkillType.Spears => config.spears,
                SkillType.Blocking => config.blocking,
                SkillType.Axes => config.axes,
                SkillType.Bows => config.bows,
                SkillType.ElementalMagic => config.elementalMagic,
                SkillType.BloodMagic => config.bloodMagic,
                SkillType.Unarmed => config.unarmed,
                SkillType.Pickaxes => config.pickaxes,
                SkillType.WoodCutting => config.woodCutting,
                SkillType.Crossbows => config.crossbows,
                SkillType.Jump => config.jump,
                SkillType.Sneak => config.sneak,
                SkillType.Run => config.run,
                SkillType.Swim => config.swim,
                SkillType.Fishing => config.fishing,
                SkillType.Cooking => config.cooking,
                SkillType.Farming => config.farming,
                SkillType.Crafting => config.crafting,
                SkillType.Dodge => config.dodge,
                SkillType.Ride => config.ride,
                _ => 0f
            };

            factor = Helper.applyModifierValue(factor, modifier);
        }

        /// <summary>
        /// Experience gained notifications
        /// </summary>
        [UsedImplicitly]
        private static void Postfix(Skills __instance, SkillType skillType, float factor = 1f)
        {
            var config = Configuration.Current.Hud;
            if (!config.IsEnabled || !config.experienceGainedNotifications || skillType == SkillType.None) return;

            var skill = __instance.GetSkill(skillType);
            float percent = skill.m_accumulator / (skill.GetNextLevelRequirement() / 100);
            var text =
                $"Level {skill.m_level.tFloat(0)} {skill.m_info.m_skill} " +
                $"[{skill.m_accumulator.tFloat(2)}/{skill.GetNextLevelRequirement().tFloat(2)}] ({percent.tFloat(0)}%)";
            __instance.m_player.Message(MessageHud.MessageType.TopLeft, text, 0, skill.m_info.m_icon);
        }
    }

    /// <summary>
    /// Apply our death penalty multiplier.
    /// </summary>
    [HarmonyPatch(typeof(Skills), nameof(Skills.LowerAllSkills))]
    public static class Skills_LowerAllSkills_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(ref float factor)
        {
            var config = Configuration.Current.Player;
            if (!config.IsEnabled) return true;

            // Skip any reduction and also skip the message that skills were lowered.
            if (config.deathPenaltyMultiplier <= -100f) return false;

            factor = Helper.applyModifierValue(factor, config.deathPenaltyMultiplier);
            return true;
        }
    }
}
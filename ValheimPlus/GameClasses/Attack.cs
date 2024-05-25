using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    public static class AttackExtensions
    {
        public static Skills.SkillType GetCharacterWeaponSkillType(this Attack __instance) =>
            __instance.m_character.GetCurrentWeapon()?.m_shared?.m_skillType ?? Skills.SkillType.Unarmed;

        public static bool IsAttackFromLocalPlayer(this Attack __instance) =>
            __instance.m_character == Player.m_localPlayer;
    }

    /// <summary>
    /// Alters stamina of weapons
    /// </summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
    public static class Attack_GetAttackStamina_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ref Attack __instance, ref float __result)
        {
            if (!Configuration.Current.StaminaUsage.IsEnabled || !__instance.IsAttackFromLocalPlayer()) return;

            var modifier = __instance.GetCharacterWeaponSkillType() switch
            {
                Skills.SkillType.Swords => Configuration.Current.StaminaUsage.swords,
                Skills.SkillType.Knives => Configuration.Current.StaminaUsage.knives,
                Skills.SkillType.Clubs => Configuration.Current.StaminaUsage.clubs,
                Skills.SkillType.Polearms => Configuration.Current.StaminaUsage.polearms,
                Skills.SkillType.Spears => Configuration.Current.StaminaUsage.spears,
                Skills.SkillType.Axes => Configuration.Current.StaminaUsage.axes,
                Skills.SkillType.Unarmed => Configuration.Current.StaminaUsage.unarmed,
                Skills.SkillType.Pickaxes => Configuration.Current.StaminaUsage.pickaxes,
                Skills.SkillType.Bows => Configuration.Current.StaminaUsage.bows,
                _ => 0f
            };

            if (modifier == 0f) return;
            __result = Helper.applyModifierValue(__result, modifier);
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr))]
    public static class Attack_GetAttackEitr_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ref Attack __instance, ref float __result)
        {
            if (!Configuration.Current.EitrUsage.IsEnabled || !__instance.IsAttackFromLocalPlayer()) return;

            var modifier = __instance.GetCharacterWeaponSkillType() switch
            {
                Skills.SkillType.BloodMagic => Configuration.Current.EitrUsage.bloodMagic,
                Skills.SkillType.ElementalMagic => Configuration.Current.EitrUsage.elementalMagic,
                _ => 0f
            };

            if (modifier == 0f) return;
            __result = Helper.applyModifierValue(__result, modifier);
        }
    }


    [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackHealth))]
    public static class Attack_GetAttackHealth_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ref Attack __instance, ref float __result)
        {
            if (!Configuration.Current.HealthUsage.IsEnabled || !__instance.IsAttackFromLocalPlayer()) return;

            var modifier = __instance.GetCharacterWeaponSkillType() switch
            {
                Skills.SkillType.BloodMagic => Configuration.Current.HealthUsage.bloodMagic,
                _ => 0f
            };

            if (modifier == 0f) return;
            __result = Helper.applyModifierValue(__result, modifier);
        }
    }

    /// <summary>
    /// Alter projectile velocity and accuracy without affecting damage
    /// </summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.ProjectileAttackTriggered))]
    public static class Attack_ProjectileAttackTriggered_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ref Attack __instance)
        {
            if (__instance == null) return;
            if (Configuration.Current.PlayerProjectile.IsEnabled && __instance.IsAttackFromLocalPlayer())
                AdjustPlayerProjectile(ref __instance);
            if (Configuration.Current.MonsterProjectile.IsEnabled && !__instance.m_character.IsPlayer())
                AdjustEnemyProjectile(ref __instance);
        }


        private static void AdjustPlayerProjectile(ref Attack __instance)
        {
            float playerProjectileVelocityMinMod = Helper.applyModifierValue(__instance.m_projectileVelMin,
                Configuration.Current.PlayerProjectile.playerMinChargeVelocityMultiplier);
            float playerProjectileVelocityMaxMod = Helper.applyModifierValue(__instance.m_projectileVel,
                Configuration.Current.PlayerProjectile.playerMaxChargeVelocityMultiplier);

            // negate value to handle increasing accuracy means decreasing variance
            float playerProjectileAccuracyMinMod = Helper.applyModifierValue(__instance.m_projectileAccuracyMin,
                -Configuration.Current.PlayerProjectile.playerMinChargeAccuracyMultiplier);
            float playerProjectileAccuracyMaxMod = Helper.applyModifierValue(__instance.m_projectileAccuracy,
                -Configuration.Current.PlayerProjectile.playerMaxChargeAccuracyMultiplier);

            float skillPercentage = 1f;
            if (Configuration.Current.PlayerProjectile.enableScaleWithSkillLevel)
            {
                var skillType = __instance.m_weapon.m_shared.m_skillType;
                if (skillType == Skills.SkillType.None) return; // https://github.com/valheimPlus/ValheimPlus/issues/758
                var player = (Player)__instance.m_character;
                skillPercentage = player.m_skills.GetSkill(skillType).m_level * 0.01f;
            }

            __instance.m_projectileVelMin =
                ShortcutLerp(__instance.m_projectileVelMin, playerProjectileVelocityMinMod, skillPercentage);
            __instance.m_projectileVel =
                ShortcutLerp(__instance.m_projectileVel, playerProjectileVelocityMaxMod, skillPercentage);

            __instance.m_projectileAccuracyMin =
                ShortcutLerp(__instance.m_projectileAccuracyMin, playerProjectileAccuracyMinMod, skillPercentage);
            __instance.m_projectileAccuracy =
                ShortcutLerp(__instance.m_projectileAccuracy, playerProjectileAccuracyMaxMod, skillPercentage);
        }

        private static void AdjustEnemyProjectile(ref Attack __instance)
        {
            __instance.m_projectileVelMin = ClampValue(__instance.m_projectileVelMin);
            __instance.m_projectileVel = ClampValue(Helper.applyModifierValue(__instance.m_projectileVel,
                Configuration.Current.MonsterProjectile.monsterMaxChargeVelocityMultiplier));

            __instance.m_projectileAccuracyMin = ClampValue(__instance.m_projectileAccuracyMin);
            __instance.m_projectileAccuracy = ClampValue(Helper.applyModifierValue(__instance.m_projectileAccuracy,
                // negate value to handle increasing accuracy means decreasing variance
                -Configuration.Current.MonsterProjectile.monsterMaxChargeAccuracyMultiplier));
        }
        
        // ReSharper disable once CompareOfFloatsByEqualityOperator : expected constant 1f which is specifiable in float
        private static float ShortcutLerp(float a, float b, float t) => t == 1f ? b : Mathf.Lerp(a, b, t);

        private const float MaxClampValue = 1E+6f;
        private static float ClampValue(float velocity) => Mathf.Clamp(velocity, 0f, MaxClampValue);
    }
}
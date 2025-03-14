using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
	public static class TurretHelpers
	{
		public static Player GetPlayerCreator(Turret turret)
		{
			var piece = turret.GetComponent<Piece>();
			if (piece == null) return null;
			return Player.GetPlayer(piece.GetCreator());
		}

		public static bool IsCreatorPvP(Turret turret)
			=> GetPlayerCreator(turret)?.IsPVPEnabled() ?? false;
	}

    [HarmonyPatch(typeof(Turret), nameof(Turret.Awake))]
    public static class Turret_Awake_Patch
    {
        /// <summary>
        /// Configure the turret on wakeup
        /// </summary>
        [UsedImplicitly]
		private static void Postfix(Turret __instance)
        {
            var config = Configuration.Current.Turret;
            if (!config.IsEnabled) return;
			__instance.m_targetPlayers = config.enablePvP && TurretHelpers.IsCreatorPvP(__instance);
			__instance.m_targetTamed = config.targetTamed;
			__instance.m_horizontalAngle = config.horizontalAngle;
			__instance.m_verticalAngle = config.verticalAngle;
			__instance.m_attackCooldown = config.attackCooldown;
			__instance.m_viewDistance = config.viewDistance;
			__instance.m_turnRate = config.turnRate;

			// Change de/acceleration proportional to turnRate
			// to maintain the degrees/second and normal game accuracy
			float accelCoeff = 1.2f / 22.5f;
			__instance.m_lookAcceleration = Mathf.Max(1.2f, accelCoeff * config.turnRate);

			float deaccelCoeff = 0.05f / 22.5f;
			__instance.m_lookDeacceleration = Mathf.Max(0.05f, deaccelCoeff * config.turnRate);
        }
    }

    [HarmonyPatch(typeof(Turret), nameof(Turret.ShootProjectile))]
    public static class Turret_ShootProjectile_Patch
    {
        private static readonly FieldInfo Field_Turret_M_MaxAmmo =
            AccessTools.Field(typeof(Turret), nameof(Turret.m_maxAmmo));

        private static readonly FieldInfo Field_Attack_M_ProjectileVel =
            AccessTools.Field(typeof(Attack), nameof(Attack.m_projectileVel));

        private static readonly FieldInfo Field_Attack_M_ProjectileAccuracy =
            AccessTools.Field(typeof(Attack), nameof(Attack.m_projectileAccuracy));

        [UsedImplicitly]
        [HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var config = Configuration.Current.Turret;
			if (!config.IsEnabled || !config.unlimitedAmmo) return instructions;

			return new CodeMatcher(instructions, ilGenerator)
				.MatchEndForward(
					OpCodes.Ldarg_0,
					new CodeMatch(inst => inst.LoadsField(Field_Turret_M_MaxAmmo)),
					OpCodes.Ldc_I4_0)
				.ThrowIfNotMatch("Couldn't transpile `Turret.ShootProjectile` for `Turret.unlimitedAmmo` config!")
				.Set(OpCodes.Ldc_I4, int.MaxValue)
				.InstructionEnumeration();
		}

            var il = instructions.ToList();
            int maxAmmoInstructionIndex = -1;
            int projectileVelocityInstructionIndex = -1;
            int projectileAccuracyInstructionIndex = -1;
            for (int i = 0; i < il.Count; ++i)
            {
                if (unlimitedAmmoEnabled &&
                    i + 2 < il.Count &&
                    il[i].LoadsField(Field_Turret_M_MaxAmmo) &&
                    il[i + 1].opcode == OpCodes.Ldc_I4_0 &&
                    il[i + 2].Branches(out _))
                {
                    // instead of if (maxAmmo > 0) then decrement ammo, we change the 0 to max int value so that the
                    // condition is never satisfied.
                    il[i + 1] = new CodeInstruction(OpCodes.Ldc_I4, int.MaxValue);
                    maxAmmoInstructionIndex = i + 1;
                }

	[HarmonyPatch(typeof(Turret), nameof(Turret.UpdateTarget))]
	public static class Turret_UpdateTarget_Patch
                {
		private static void Prefix(Turret __instance)
                        {
			var config = Configuration.Current.Turret;
			if (!config.IsEnabled) return;

			// Update targetPlayers when selecting a target because the creator might have toggled PvP
			__instance.m_targetPlayers = config.enablePvP && TurretHelpers.IsCreatorPvP(__instance);
                        }
                    );
                    projectileVelocityInstructionIndex = i;
                }

	[HarmonyPatch(typeof(Turret), nameof(Turret.GetAmmoItem))]
	public static class Turret_GetAmmoItem_Patch
                {
		private static void Postfix(Turret __instance, ref ItemDrop.ItemData __result)
                        {
			if (!Configuration.Current.Turret.IsEnabled || __result == null) return;

			// Under normal conditions the projectile velocity is set to the view distance.
			// We maintain the game's normal accuracy coefficient by resetting the projectile velocity to the view distance.
			// Patched here to maintain consistency in both UpdateTurretRotation and ShootProjectile
			__result.m_shared.m_attack.m_projectileVel = __instance.m_viewDistance;
        }
    }
}
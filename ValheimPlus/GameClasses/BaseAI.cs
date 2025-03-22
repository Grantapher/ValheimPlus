using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FindClosestCreature))]
    public static class BaseAI_FindClosestCreature_Patch
    {
        private static bool Turret_IsValidTarget(Transform me, Character target)
        {
            // Assume `targetPlayers` was true
            var turret = me.GetComponent<Turret>();
            if (turret == null || target is not Player p) return true;
            var creator = TurretHelpers.GetPlayerCreator(turret);
            return creator != p && p.IsPVPEnabled();
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var matcher = new CodeMatcher(instructions, ilGenerator);

            if (Configuration.Current.Turret.IsEnabled && !Configuration.Current.Turret.disablePvP)
            {
                var Turret_IsValidTargetMethod = AccessTools.Method(typeof(BaseAI_FindClosestCreature_Patch), nameof(Turret_IsValidTarget));
                var CharacterIsTamed = AccessTools.Method(typeof(Character), nameof(Character.IsTamed));

                var continueLabel = matcher.MatchStartForward(
                    OpCodes.Brtrue,
                    new(inst => inst.IsLdarg(11)),
                    OpCodes.Brfalse
                ).ThrowIfNotMatch("Could not find `onlyTargets` in BaseAI.FindClosestCreature transpiler")
                .Operand;

                matcher.Advance(1).InsertAndAdvance(
                    new(OpCodes.Ldarg_0),   // Transform
                    new(OpCodes.Ldloc, 5),  // target
                    new(OpCodes.Call, Turret_IsValidTargetMethod),
                    new(OpCodes.Brfalse, continueLabel)
                );
            }

            return matcher.InstructionEnumeration();
        }
    }
        
}

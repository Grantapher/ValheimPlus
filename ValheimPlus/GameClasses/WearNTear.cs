using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ValheimPlus.Configurations;
using ValheimPlus.Utility;
using System.Diagnostics;
using JetBrains.Annotations;

namespace ValheimPlus.GameClasses
{
    /// <summary>
    /// Disable weather damage
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "UpdateWear")]
    public static class WearNTear_UpdateWear_Patch
    {
        [UsedImplicitly]
        private static void Prefix(float time, ref float ___m_rainTimer)
        {
            if (Configuration.Current.Building.IsEnabled && Configuration.Current.Building.noWeatherDamage)
            {
                // if the rain timer is perpetually set to the current time in prefix,
                // then the timer will never become large enough to trigger the weather effect.
                ___m_rainTimer = time;
            }
        }

    }

    /// <summary>
    /// Disables selected environmental wear contributions while preserving vanilla state and cadence.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "UpdateWear")]
    public static class WearNTear_UpdateWear_Transpiler
    {
        private static readonly FieldInfo Field_GameSnowDamage =
            AccessTools.Field(typeof(Game), nameof(Game.m_snowDamage));
        private static readonly FieldInfo Field_GameAshDamage =
            AccessTools.Field(typeof(Game), nameof(Game.m_ashDamage));
        private static readonly FieldInfo Field_WearNTearLavaValue =
            AccessTools.Field(typeof(WearNTear), nameof(WearNTear.m_lavaValue));
        private static readonly FieldInfo Field_WearNTearAshDamageResist =
            AccessTools.Field(typeof(WearNTear), nameof(WearNTear.m_ashDamageResist));
        private static readonly MethodInfo Method_GameInstance =
            AccessTools.PropertyGetter(typeof(Game), nameof(Game.instance));
        private static readonly MethodInfo Method_AddHeavySnowWear =
            AccessTools.Method(typeof(WearNTear_UpdateWear_Transpiler), nameof(AddHeavySnowWear));
        private static readonly MethodInfo Method_AddAshWear =
            AccessTools.Method(typeof(WearNTear_UpdateWear_Transpiler), nameof(AddAshWear));
        private static readonly MethodInfo Method_AddLavaWear =
            AccessTools.Method(typeof(WearNTear_UpdateWear_Transpiler), nameof(AddLavaWear));

        /// <summary>
        /// Replaces only the validated environmental wear additions with configuration-aware helpers.
        /// </summary>
        [HarmonyTranspiler]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = instructions.ToList();

            try
            {
                var snowAnchors = FindContributionAnchors(original, Field_GameSnowDamage);
                var ashAnchors = FindContributionAnchors(original, Field_GameAshDamage);

                if (snowAnchors.Count != 1 || ashAnchors.Count != 1)
                {
                    return FailClosed(original, "Expected exactly one heavy-snow and one ash contribution.");
                }

                var accumulator = snowAnchors[0].AccumulatorLocal;
                if (accumulator != ashAnchors[0].AccumulatorLocal)
                {
                    return FailClosed(original, "Heavy-snow and ash contributions do not share the same wear accumulator.");
                }

                var lavaAnchors = FindLavaContributionAnchors(original, accumulator);
                if (lavaAnchors.Count != 1)
                {
                    return FailClosed(original, "Expected exactly one lava contribution in the validated wear accumulator.");
                }

                if (Method_GameInstance == null || Field_GameSnowDamage == null || Field_GameAshDamage == null ||
                    Field_WearNTearLavaValue == null || Field_WearNTearAshDamageResist == null ||
                    Method_AddHeavySnowWear == null || Method_AddAshWear == null || Method_AddLavaWear == null)
                {
                    return FailClosed(original, "Wear contribution members could not be resolved.");
                }

                if (!IsAddInstruction(original, snowAnchors[0]) || !IsAddInstruction(original, ashAnchors[0]) ||
                    !IsAddInstruction(original, lavaAnchors[0]))
                {
                    return FailClosed(original, "Validated wear anchors no longer point to add instructions.");
                }

                // Patch numeric contributions only; vanilla timers, state, gates, VFX, and ApplyDamage stay intact.
                var patched = original.Select(instruction => new CodeInstruction(instruction)).ToList();
                patched[snowAnchors[0].AddIndex] = ReplaceAdd(original[snowAnchors[0].AddIndex], Method_AddHeavySnowWear);
                patched[ashAnchors[0].AddIndex] = ReplaceAdd(original[ashAnchors[0].AddIndex], Method_AddAshWear);
                patched[lavaAnchors[0].AddIndex] = ReplaceAdd(original[lavaAnchors[0].AddIndex], Method_AddLavaWear);

                return patched;
            }
            catch (Exception exception)
            {
                return FailClosed(original, "Wear damage contributions will be unchanged.", exception);
            }
        }

        private static IEnumerable<CodeInstruction> FailClosed(
            List<CodeInstruction> original, string detail, Exception exception = null)
        {
            PatchLog.Failed(nameof(WearNTear_UpdateWear_Transpiler), detail, exception);
            return original;
        }

        private static bool IsAddInstruction(List<CodeInstruction> instructions, ContributionAnchor anchor)
        {
            return instructions[anchor.AddIndex].opcode == OpCodes.Add;
        }

        private static CodeInstruction ReplaceAdd(CodeInstruction original, MethodInfo helper)
        {
            return new CodeInstruction(OpCodes.Call, helper)
            {
                labels = original.labels,
                blocks = original.blocks,
            };
        }

        private static List<ContributionAnchor> FindContributionAnchors(
            List<CodeInstruction> il, FieldInfo contributionField)
        {
            var anchors = new List<ContributionAnchor>();

            for (var i = 0; i + 4 < il.Count; i++)
            {
                if (!TryGetLoadedLocalIndex(il[i], out var loadedLocal) ||
                    !il[i + 1].Calls(Method_GameInstance) ||
                    !il[i + 2].LoadsField(contributionField) ||
                    il[i + 3].opcode != OpCodes.Add ||
                    !TryGetStoredLocalIndex(il[i + 4], out var storedLocal) ||
                    loadedLocal != storedLocal)
                    continue;

                anchors.Add(new ContributionAnchor(i + 3, storedLocal));
            }

            return anchors;
        }

        private static List<ContributionAnchor> FindLavaContributionAnchors(
            List<CodeInstruction> il, int expectedAccumulatorLocal)
        {
            var anchors = new List<ContributionAnchor>();
            var sawLavaValue = false;
            var sawAshDamageResist = false;
            var sawMultiplication = false;

            for (var i = 0; i + 1 < il.Count; i++)
            {
                if (IsStoreToLocal(il[i], expectedAccumulatorLocal))
                {
                    sawLavaValue = false;
                    sawAshDamageResist = false;
                    sawMultiplication = false;
                }

                if (il[i].LoadsField(Field_WearNTearLavaValue))
                    sawLavaValue = true;
                if (il[i].LoadsField(Field_WearNTearAshDamageResist))
                    sawAshDamageResist = true;
                if (il[i].opcode == OpCodes.Mul)
                    sawMultiplication = true;

                if (il[i].opcode != OpCodes.Add || !TryGetStoredLocalIndex(il[i + 1], out var storedLocal) ||
                    storedLocal != expectedAccumulatorLocal || !sawLavaValue ||
                    !sawAshDamageResist || !sawMultiplication)
                    continue;

                anchors.Add(new ContributionAnchor(i, storedLocal));
            }

            return anchors;
        }

        private static bool IsStoreToLocal(CodeInstruction instruction, int expectedLocal)
        {
            return TryGetStoredLocalIndex(instruction, out var localIndex) && localIndex == expectedLocal;
        }

        private static bool TryGetLoadedLocalIndex(CodeInstruction instruction, out int localIndex)
        {
            if (instruction.opcode == OpCodes.Ldloc_0)
            {
                localIndex = 0;
                return true;
            }
            if (instruction.opcode == OpCodes.Ldloc_1)
            {
                localIndex = 1;
                return true;
            }
            if (instruction.opcode == OpCodes.Ldloc_2)
            {
                localIndex = 2;
                return true;
            }
            if (instruction.opcode == OpCodes.Ldloc_3)
            {
                localIndex = 3;
                return true;
            }

            if (instruction.opcode != OpCodes.Ldloc && instruction.opcode != OpCodes.Ldloc_S)
            {
                localIndex = 0;
                return false;
            }

            return TryGetLocalOperandIndex(instruction, out localIndex);
        }

        private static bool TryGetStoredLocalIndex(CodeInstruction instruction, out int localIndex)
        {
            if (instruction.opcode == OpCodes.Stloc_0)
            {
                localIndex = 0;
                return true;
            }
            if (instruction.opcode == OpCodes.Stloc_1)
            {
                localIndex = 1;
                return true;
            }
            if (instruction.opcode == OpCodes.Stloc_2)
            {
                localIndex = 2;
                return true;
            }
            if (instruction.opcode == OpCodes.Stloc_3)
            {
                localIndex = 3;
                return true;
            }

            if (instruction.opcode != OpCodes.Stloc && instruction.opcode != OpCodes.Stloc_S)
            {
                localIndex = 0;
                return false;
            }

            return TryGetLocalOperandIndex(instruction, out localIndex);
        }

        private static bool TryGetLocalOperandIndex(CodeInstruction instruction, out int localIndex)
        {
            switch (instruction.operand)
            {
                case int index:
                    localIndex = index;
                    return true;
                case byte index:
                    localIndex = index;
                    return true;
                case short index:
                    localIndex = index;
                    return true;
                case ushort index:
                    localIndex = index;
                    return true;
                case LocalBuilder local:
                    localIndex = local.LocalIndex;
                    return true;
                default:
                    localIndex = 0;
                    return false;
            }
        }

        // Keep m_snowDamageTimer untouched: it schedules snow VFX; this gate suppresses only numeric wear.
        private static float AddHeavySnowWear(float accumulated, float contribution)
        {
            var config = Configuration.Current.Building;
            return config.IsEnabled && config.noHeavySnowDamage ? accumulated : accumulated + contribution;
        }

        // Keep m_ashDamageImmune untouched: it is vanilla eligibility state, not a numeric wear setting.
        private static float AddAshWear(float accumulated, float contribution)
        {
            var config = Configuration.Current.Building;
            return config.IsEnabled && config.noAshDamage ? accumulated : accumulated + contribution;
        }

        // Keep m_ashDamageImmune untouched for lava as well; only the lava contribution is configuration-gated.
        private static float AddLavaWear(float accumulated, float contribution)
        {
            var config = Configuration.Current.Building;
            return config.IsEnabled && config.noLavaDamage ? accumulated : accumulated + contribution;
        }

        private readonly struct ContributionAnchor
        {
            internal ContributionAnchor(int addIndex, int accumulatorLocal)
            {
                AddIndex = addIndex;
                AccumulatorLocal = accumulatorLocal;
            }

            internal int AddIndex { get; }
            internal int AccumulatorLocal { get; }
        }
    }

    /// <summary>
    /// Removes the integrity check for having a connected piece to the ground.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "HaveSupport")]
    public static class WearNTear_HaveSupport_Patch
    {
        private static void Postfix(ref bool __result)
        {
            if (Configuration.Current.StructuralIntegrity.IsEnabled && Configuration.Current.StructuralIntegrity.disableStructuralIntegrity)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// Disable damage to player structures
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "ApplyDamage")]
    public static class WearNTear_ApplyDamage_Patch
    {
        private static readonly HashSet<string> UpdateWearMethodNames = new()
        {
            "UpdateWear",
            "DMD<WearNTear::UpdateWear>",
        };
        
        private static bool Prefix(ref WearNTear __instance, ref float damage)
        {
            // Gets the name of the method calling the ApplyDamage method
            StackTrace stackTrace = new StackTrace();
            string callingMethod = stackTrace.GetFrame(2).GetMethod().Name;

            if (!(Configuration.Current.StructuralIntegrity.IsEnabled && __instance.m_piece && __instance.m_piece.IsPlacedByPlayer() && !UpdateWearMethodNames.Contains(callingMethod)))
                return true;

            if (__instance.m_piece.m_name.StartsWith("$ship"))
            {
                if (Configuration.Current.StructuralIntegrity.disableDamageToPlayerBoats ||
                    (Configuration.Current.StructuralIntegrity.disableWaterDamageToPlayerBoats &&
                     stackTrace.GetFrame(15).GetMethod().Name == "UpdateWaterForce")) return false;

                return true;
            }
            if (__instance.m_piece.m_name.StartsWith("$tool_cart"))
            {
                if (Configuration.Current.StructuralIntegrity.disableDamageToPlayerCarts ||
                    (Configuration.Current.StructuralIntegrity.disableWaterDamageToPlayerCarts &&
                     stackTrace.GetFrame(15).GetMethod().Name == "UpdateWaterForce")) return false;

                return true;
            }
            return !Configuration.Current.StructuralIntegrity.disableDamageToPlayerStructures;
        }
    }

    /// <summary>
    /// Disable structural integrity
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.GetMaterialProperties))]
    public static class WearNTear_GetMaterialProperties_Patch
    {
        private static readonly Dictionary<WearNTear.MaterialType, Func<float>> Multipliers = new()
        {
            [WearNTear.MaterialType.Wood] = () => Configuration.Current.StructuralIntegrity.wood,
            [WearNTear.MaterialType.Stone] = () => Configuration.Current.StructuralIntegrity.stone,
            [WearNTear.MaterialType.Iron] = () => Configuration.Current.StructuralIntegrity.iron,
            [WearNTear.MaterialType.HardWood] = () => Configuration.Current.StructuralIntegrity.hardWood,
            [WearNTear.MaterialType.Marble] = () => Configuration.Current.StructuralIntegrity.marble,
            [WearNTear.MaterialType.Ashstone] = () => Configuration.Current.StructuralIntegrity.ashstone,
            [WearNTear.MaterialType.Ancient] = () => Configuration.Current.StructuralIntegrity.ancient,
            [WearNTear.MaterialType.Ice] = () => Configuration.Current.StructuralIntegrity.ice,
            [WearNTear.MaterialType.Timberwood] = () => Configuration.Current.StructuralIntegrity.timberwood,
        };

        [UsedImplicitly]
        private static void Postfix(ref WearNTear __instance, ref float horizontalLoss, ref float verticalLoss)
        {
            if (!Configuration.Current.StructuralIntegrity.IsEnabled) return;
            if (Configuration.Current.StructuralIntegrity.disableStructuralIntegrity)
            {
                verticalLoss = 0f;
                horizontalLoss = 0f;
                return;
            }

            // Unknown material type, don't modify.
            if (!Multipliers.TryGetValue(__instance.m_materialType, out var multiplier)) return;
            
            // scale the loss number between its current number and 0 based on the user config.
            float clampedMultiplier = Helper.Clamp(multiplier(), 0 , 100);
            verticalLoss -= verticalLoss / 100 * clampedMultiplier;
            horizontalLoss -= horizontalLoss / 100 * clampedMultiplier;
        }
    }
}

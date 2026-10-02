using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;
using ValheimPlus.Utility;

namespace ValheimPlus.GameClasses
{
    public enum TameableMortalityTypes
    {
        [UsedImplicitly] Normal,
        Essential,
        Immortal
    }

    [Flags]
    public enum AnimalType
    {
        [UsedImplicitly] None = 0,
        Boar = 1 << 0,
        Wolf = 1 << 1,
        Lox = 1 << 2,
        Hen = 1 << 3,
        Asksvin = 1 << 4,
        Moose = 1 << 5,
        All = (1 << 6) - 1
    }

    public static class TameableHelpers
    {
        public static readonly Dictionary<string, AnimalType> NamedTypes = new()
        {
            { "$enemy_moose", AnimalType.Moose },
            { "$enemy_moose_calf", AnimalType.Moose },
            { "$enemy_asksvin", AnimalType.Asksvin },
            { "$enemy_asksvin_hatchling", AnimalType.Asksvin },
            { "$enemy_boar", AnimalType.Boar },
            { "$enemy_boarpiggy", AnimalType.Boar },
            { "$enemy_wolf", AnimalType.Wolf },
            { "$enemy_wolfcub", AnimalType.Wolf },
            { "$enemy_lox", AnimalType.Lox },
            { "$enemy_loxcalf", AnimalType.Lox },
            { "$enemy_hen", AnimalType.Hen },
            { "$enemy_chicken", AnimalType.Hen }
        };

        public static bool IsValidAnimalType(string name)
        {
            if (!NamedTypes.TryGetValue(name, out var type)) return false;
            var config = Configuration.Current.Tameable;
            return config.animalTypes.HasFlag(type);
        }

        public static bool IsHungerIgnored(Tameable instance)
        {
            var config = Configuration.Current.Tameable;
            if (!config.IsEnabled || !config.ignoreHunger || !IsValidAnimalType(instance.m_character.m_name)) 
                return false;

            var timeLeft = instance.m_nview.GetZDO().GetFloat(ZDOVars.s_tameTimeLeft);
            return timeLeft > 0;
        }

        public static bool IsAlertedWithIgnore(Tameable tameable) =>
            !IsValidAnimalType(tameable.m_character.m_name) && tameable.m_monsterAI.IsAlerted();
    }

    /// <summary>
    /// Adds a text indicator so player's know when an animal they've tamed has been stunned.
    /// </summary>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    public static class Tameable_GetHoverText_Patch
    {
        [UsedImplicitly]
        public static void Postfix(Tameable __instance, ref string __result)
        {
            if (Configuration.Current.Tameable.IsEnabled && Configuration.Current.Tameable.stunInformation)
            {
                // If tamed creature is recovering from a stun, then add Stunned to hover text.
                if (__instance.m_character.m_nview.GetZDO().GetBool("isRecoveringFromStun"))
                    __result = __result.Insert(__result.IndexOf(" )", StringComparison.Ordinal), ", Stunned");
            }

            var procreation = __instance.GetComponent<Procreation>();
            if (procreation) ProcreationHelpers.AddLoveInformation(__instance, procreation, ref __result);
        }
    }

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.IsHungry))]
    public static class Tameable_IsHungry_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Tameable __instance, ref bool __result)
        {
            // We don't care if it's already false
            if (!__result) return;

            var isTamed = __instance.m_character.IsTamed();
            __result = !(isTamed
                ? ProcreationHelpers.IsHungerIgnored(__instance)
                : TameableHelpers.IsHungerIgnored(__instance));
        }
    }

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Awake))]
    public static class Tameable_Awake_Patch
    {
        [UsedImplicitly]
        public static void Postfix(Tameable __instance)
        {
            var config = Configuration.Current.Tameable;
            if (!config.IsEnabled) return;
            if (!TameableHelpers.IsValidAnimalType(__instance.m_character.m_name)) return;

            Helper.applyModifierValueTo(ref __instance.m_tamingTime, config.tameTimeMultiplier);
            Helper.applyModifierValueTo(ref __instance.m_tamingSpeedMultiplierRange, config.tameBoostRangeMultiplier);
            Helper.applyModifierValueTo(ref __instance.m_tamingBoostMultiplier, config.tameBoostMultiplier);

            if (!__instance.m_nview.GetZDO().GetFloat(ZDOVars.s_tameTimeLeft, out float timeLeft)) return;
            if (timeLeft > __instance.m_tamingTime)
                __instance.m_nview.GetZDO().Set(ZDOVars.s_tameTimeLeft, __instance.m_tamingTime);
        }
    }

    [HarmonyPatch(typeof(Tameable))]
    public static class Tameable_Alerted_Patches
    {
        [UsedImplicitly]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Tameable), nameof(Tameable.TamingUpdate));
            yield return AccessTools.Method(typeof(Tameable), nameof(Tameable.GetStatusString));
        }

        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator ilGenerator)
        {
            var config = Configuration.Current.Tameable;
            if (!config.IsEnabled || !config.ignoreAlerted) return instructions;

            var il = instructions.ToList();
            try
            {
                var originalIsAlertedMethod = AccessTools.Method(typeof(BaseAI), nameof(BaseAI.IsAlerted));
                var newIsAlertedMethod =
                    AccessTools.Method(typeof(TameableHelpers), nameof(TameableHelpers.IsAlertedWithIgnore));
                return new CodeMatcher(il, ilGenerator)
                    .MatchStartForward(new CodeMatch(OpCodes.Callvirt, originalIsAlertedMethod))
                    .ThrowIfNotMatch("Could not find BaseAI.IsAlerted call")
                    .Advance(-1)
                    .RemoveInstructions(2)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, newIsAlertedMethod))
                    .InstructionEnumeration();
            }
            catch (Exception ex)
            {
                PatchLog.Failed(nameof(Tameable_Alerted_Patches), "Tamed creature hover text will be unchanged.", ex);
            }

            return il;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    public static class ProcreationHelpers
    {
        public static bool IsValidAnimalType(string name)
        {
            if (!TameableHelpers.NamedTypes.TryGetValue(name, out var type)) return false;
            var config = Configuration.Current.Procreation;
            return config.animalTypes.HasFlag(type);
        }

        public static bool IsHungerIgnored(Tameable instance)
            => Configuration.Current.Procreation.IsEnabled
               && Configuration.Current.Procreation.ignoreHunger
               && IsValidAnimalType(instance.m_character.m_name);

        public static bool IsAlertedWithIgnore(Tameable tameable) =>
            !IsValidAnimalType(tameable.m_character.m_name) && tameable.m_monsterAI.IsAlerted();

        private static string GetPregnantStatus(Procreation procreation)
        {
            var ticks = procreation.m_nview.GetZDO().GetLong(ZDOVars.s_pregnant);
            var ticksNow = ZNet.instance.GetTime().Ticks;
            var elapsed = new TimeSpan(ticksNow - ticks).TotalSeconds;
            var timeLeft = (int)(procreation.m_pregnancyDuration - elapsed);

            var result = "\n<color=#FFAEC9>Pregnant";

            result += timeLeft switch
            {
                > 120 => " ( " + timeLeft / 60 + " minutes left )",
                > 0 => " ( " + timeLeft + " seconds left )",
                > -15 => " ( Due to give birth )",
                _ => " ( Overdue )"
            };

            result += "</color>";
            return result;
        }

        public static void AddLoveInformation(Tameable instance, Procreation procreation, ref string result)
        {
            var config = Configuration.Current.Procreation;
            if (!config.IsEnabled || !config.loveInformation)
                return;

            if (!IsValidAnimalType(instance.m_character.m_name))
                return;

            var lineBreak = result.IndexOf('\n');
            if (lineBreak <= 0)
                return;

            var lovePoints = procreation.m_nview.GetZDO().GetInt(ZDOVars.s_lovePoints);

            string extraText;
            if (procreation.IsPregnant())
                extraText = GetPregnantStatus(procreation);
            else if (lovePoints > 0)
                extraText = $"\nLoved ( {lovePoints} / {procreation.m_requiredLovePoints} )";
            else
                extraText = "\nNot loved";

            result = result.Insert(lineBreak, extraText);
        }

        public static void AddGrowupInformation(Character character, Growup growup, ref string result)
        {
            var config = Configuration.Current.Procreation;
            if (!config.IsEnabled || !config.offspringInformation)
                return;

            if (!IsValidAnimalType(character.m_name))
                return;

            result = ValheimPlus.LocalizationHelper.Localize(character.m_name);
            var timeLeft = GrowupHelpers.GetGrowTimeLeft(growup);

            result += timeLeft switch
            {
                > 120 => " ( Matures in " + timeLeft / 60 + " minutes )",
                > 0 => " ( Matures in " + timeLeft + " seconds )",
                _ => " ( Matured )"
            };
        }
    }

    [HarmonyPatch(typeof(Procreation), nameof(Procreation.Awake))]
    public static class Procreation_Awake_Patch
    {
        [UsedImplicitly]
        public static void Postfix(Procreation __instance)
        {
            var config = Configuration.Current.Procreation;
            if (!config.IsEnabled)
                return;

            if (!ProcreationHelpers.IsValidAnimalType(__instance.m_character.m_name))
                return;

            __instance.m_requiredLovePoints = (int)Helper.applyModifierValue(
                __instance.m_requiredLovePoints, config.requiredLovePointsMultiplier);

            __instance.m_maxCreatures = (int)Helper.applyModifierValue(
                __instance.m_maxCreatures, config.creatureLimitMultiplier);

            Helper.applyModifierValueTo(ref __instance.m_partnerCheckRange, config.partnerCheckRangeMultiplier);
            Helper.applyModifierValueTo(ref __instance.m_pregnancyDuration, config.pregnancyDurationMultiplier);
            Helper.applyModifierValueTo(ref __instance.m_pregnancyChance, config.pregnancyChanceMultiplier);
        }
    }

    [HarmonyPatch(typeof(Procreation), nameof(Procreation.Procreate))]
    public static class Procreation_Procreate_Patch
    {
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator ilGenerator)
        {
            var config = Configuration.Current.Procreation;
            if (!config.IsEnabled || !config.ignoreAlerted) return instructions;

            var il = instructions.ToList();
            try
            {
                var originalIsAlertedMethod = AccessTools.Method(typeof(BaseAI), nameof(BaseAI.IsAlerted));
                var newIsAlertedMethod =
                    AccessTools.Method(typeof(ProcreationHelpers), nameof(ProcreationHelpers.IsAlertedWithIgnore));
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
                ValheimPlusPlugin.Logger.LogError(ex);
            }

            return il;
        }
    }
}

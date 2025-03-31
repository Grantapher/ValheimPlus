using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{

    public static class ProcreationHelpers
    {
        public static bool IsValidAnimalType(string name) {
            if (!TameableHelpers.NamedTypes.TryGetValue(name, out AnimalType type))
                return false;

            var config = Configuration.Current.Procreation;
            return config.animalTypes.HasFlag(type);
        }

        public static bool IsAlertedIgnored(Procreation instance)
            => IsValidAnimalType(instance.m_character.m_name);

        public static bool IsHungerIgnored(Tameable instance)
            => Configuration.Current.Procreation.IsEnabled
            && Configuration.Current.Procreation.ignoreHunger
            && IsValidAnimalType(instance.m_character.m_name);

        private static string GetPregnantStatus(Procreation procreation)
        {
            var ticks = procreation.m_nview.GetZDO().GetLong(ZDOVars.s_pregnant);
            var ticksNow = ZNet.instance.GetTime().Ticks;
            var elapsed = new TimeSpan(ticksNow - ticks).TotalSeconds;
            var timeLeft = (int)(procreation.m_pregnancyDuration - elapsed);

            var result = "\n<color=#FFAEC9>Pregnant";

            if (timeLeft > 120)
                result += " ( " + (timeLeft / 60) + " minutes left )";

            else if (timeLeft > 0)
                result += " ( " + timeLeft + " seconds left )";

            else if (timeLeft > -15)
                result += " ( Due to give birth )";

            // Update interval is 30 seconds so we can just pretend it's overdue
            else
                result += " ( Overdue )";

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

            result = Localization.instance.Localize(character.m_name);
            var timeleft = GrowupHelpers.GetGrowTimeLeft(growup);

            if (timeleft > 120)
                result += " ( Matures in " + (timeleft / 60) + " minutes )";
            else if (timeleft > 0)
                result += " ( Matures in " + timeleft + " seconds )";
            else
                result += " ( Matured )";
        }
    }

    [HarmonyPatch(typeof(Procreation), nameof(Procreation.Awake))]
    public static class Procreation_Awake_Patch
    {
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
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var config = Configuration.Current.Procreation;
            if (!config.IsEnabled || !config.ignoreAlerted)
                return instructions;

            var baseAiIsAlertedMethod = AccessTools.Method(typeof(BaseAI), nameof(BaseAI.IsAlerted));
            var isAlertedIgnoredMethod = AccessTools.Method(typeof(ProcreationHelpers), nameof(ProcreationHelpers.IsAlertedIgnored));

            Label? passthrough = null;
            var matcher = new CodeMatcher(instructions, ilGenerator);
            matcher.MatchEndForward(
                new CodeMatch(inst => inst.Calls(baseAiIsAlertedMethod)),
                new CodeMatch(inst => inst.Branches(out passthrough))
            ).ThrowIfNotMatch("Could not find BaseAI.IsAlerted call");

            if (!passthrough.HasValue)
                throw new Exception("Could not find BaseAI.IsAlerted branch");

            matcher.Advance(1)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, isAlertedIgnoredMethod),
                new(OpCodes.Brtrue, passthrough)
            );

            return matcher.InstructionEnumeration();
        }
    }
}

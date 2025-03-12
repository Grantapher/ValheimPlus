using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
	[Flags]
	public enum AnimalType
	{
		None = 0,
		Boar = 1 << 0,
		Wolf = 1 << 1,
		Lox = 1 << 2,
		Hen = 1 << 3,
		Asksvin = 1 << 4,
		All = (1 << 5) - 1
	}

	public static class ProcreationHelper
	{
		private readonly static Dictionary<string, AnimalType> NamedTypes = new() {
			{ "$enemy_asksvin", AnimalType.Asksvin },
			{ "$enemy_asksvin_hatchling", AnimalType.Asksvin },
			{ "$enemy_boar", AnimalType.Boar },
			{ "$enemy_boarpiggy", AnimalType.Boar },
			{ "$enemy_wolf", AnimalType.Wolf },
			{ "$enemy_wolfcub", AnimalType.Wolf },
			{ "$enemy_lox", AnimalType.Lox },
			{ "$enemy_loxcalf", AnimalType.Lox }
		};

		public static bool IsValidAnimalType(string name) {
			if (!NamedTypes.TryGetValue(name, out AnimalType type))
				return false;

			var config = Configuration.Current.Procreation;
			return config.animalTypes.HasFlag(type);
		}

		public static bool IsTameValid(Character character)
		{
			// Call IsTamed first for compatibility with other mods
			return character.IsTamed() && IsValidAnimalType(character.m_name);
		}

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

		public static CodeMatcher IsTameValidTranspiler(CodeMatcher matcher)
		{
			var characterIsTamed = AccessTools.Method(typeof(Character), nameof(Character.IsTamed));
			var mIsTameValid = AccessTools.Method(typeof(ProcreationHelper), nameof(IsTameValid));
			return matcher
				.MatchEndForward(new CodeMatch(inst => inst.Calls(characterIsTamed)))
				.ThrowIfNotMatch("No match for IsTamed method call.")
				.Set(OpCodes.Call, mIsTameValid)
				.Start();
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

			if (!ProcreationHelper.IsValidAnimalType(__instance.m_character.m_name))
				return;

			__instance.m_requiredLovePoints = config.requiredLovePoints;
			__instance.m_partnerCheckRange = config.partnerCheckRange;
			__instance.m_maxCreatures = config.creatureLimit;

			__instance.m_pregnancyDuration = Helper.applyModifierValue(
				__instance.m_pregnancyDuration, config.pregnancyDurationMultiplier);

			__instance.m_pregnancyChance = 1f - Helper.applyModifierValue(
				__instance.m_pregnancyChance, config.pregnancyChanceMultiplier);
		}
	}

	[HarmonyPatch(typeof(Procreation), nameof(Procreation.Procreate))]
	public static class Procreation_Procreate_Transpiler
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
		{
			var config = Configuration.Current.Procreation;
			if (!config.IsEnabled || (!config.ignoreHunger && !config.ignoreAlerted))
				return instructions;

			try {
				var matcher = new CodeMatcher(instructions, ilGenerator);
				ProcreationHelper.IsTameValidTranspiler(matcher);

				if (config.ignoreHunger)
					TameableHelpers.IgnoreHungerTranspiler(matcher);

				if (config.ignoreAlerted)
					BaseAIHelpers.IgnoreAlertedTranspiler(matcher);

				return matcher.InstructionEnumeration();
			} catch (Exception ex)
			{
				ValheimPlusPlugin.Logger.LogError(ex);
				return instructions;
			}
		}
	}
}

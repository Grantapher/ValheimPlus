using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
	using static ProcreationHelper;

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
		private static AnimalType? animalTypes;

		public static AnimalType AnimalTypes
		{
			get => animalTypes ?? GetAnimalTypes();
		}

		public static AnimalType GetAnimalTypes()
		{
			var types = Configuration.Current.Procreation.animalTypes.ToLower();
			if (types.Contains("none"))
				return AnimalType.None;

			if (types.Contains("all"))
				return AnimalType.All;

			animalTypes = AnimalType.None;
			if (types.Contains("boar"))
				animalTypes |= AnimalType.Boar;
			if (types.Contains("wolf"))
				animalTypes |= AnimalType.Wolf;
			if (types.Contains("lox"))
				animalTypes |= AnimalType.Lox;
			if (types.Contains("hen"))
				animalTypes |= AnimalType.Hen;
			if (types.Contains("asksvin"))
				animalTypes |= AnimalType.Asksvin;

			foreach (var value in Enum.GetValues(typeof(AnimalType)))
				if (types.Contains(value.ToString().ToLower()))
					animalTypes |= (AnimalType)value;

			return animalTypes.Value;
		}

		public static bool IsValidAnimalType(string name) => name switch {
			"$enemy_asksvin" or "$enemy_asksvin_hatchling" => AnimalTypes.HasFlag(AnimalType.Asksvin),
			"$enemy_boar" or "$enemy_boarpiggy" => AnimalTypes.HasFlag(AnimalType.Boar),
			"$enemy_wolf" or "$enemy_wolfcub" => AnimalTypes.HasFlag(AnimalType.Wolf),
			"$enemy_lox" or "$enemy_loxcalf" => AnimalTypes.HasFlag(AnimalType.Lox),
			"$enemy_hen" or "$enemy_chicken" => AnimalTypes.HasFlag(AnimalType.Hen),
			_ => false,
		};

		public static bool IsTameValid(Character character)
		{
			// Call IsTamed first for compatibility with other mods
			var isValid = character.IsTamed() && IsValidAnimalType(character.m_name);
			ValheimPlusPlugin.Logger.LogInfo("Procreation.Procreate: instance " + character.m_name +
				(isValid ? " is valid" : " is invalid"));
			return isValid;
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
			if (!Configuration.Current.Procreation.IsEnabled || !Configuration.Current.Procreation.loveInformation)
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
			if (!Configuration.Current.Procreation.IsEnabled || !Configuration.Current.Procreation.offspringInformation)
				return;

			if (!IsValidAnimalType(character.m_name))
				return;

			result = Localization.instance.Localize(character.m_name);
			var timeleft = growup.GetGrowTimeLeft();

			if (timeleft > 120)
				result += " ( Matures in " + (timeleft / 60) + " minutes )";
			else if (timeleft > 0)
				result += " ( Matures in " + timeleft + " seconds )";
			else
				result += " ( Matured )";
		}

		public static CodeMatcher IsTameValidTranspiler(CodeMatcher matcher)
		{
			var CharacterField = AccessTools.Field(typeof(Procreation), nameof(Procreation.m_character));
			var characterIsTamed = AccessTools.Method(typeof(Character), nameof(Character.IsTamed));
			var mIsTameValid = AccessTools.Method(typeof(ProcreationHelper), nameof(IsTameValid));
			return matcher
				.MatchEndForward(
					OpCodes.Ldarg_0,
					new CodeMatch(inst => inst.LoadsField(CharacterField)),
					new CodeMatch(inst => inst.Calls(characterIsTamed)))
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
			if (!Configuration.Current.Procreation.IsEnabled)
				return;

			if (!IsValidAnimalType(__instance.m_character.m_name))
				return;

			var procreation = Configuration.Current.Procreation;
			__instance.m_requiredLovePoints = procreation.requiredLovePoints;
			__instance.m_pregnancyDuration = Helper.applyModifierValue(10f, procreation.pregnancyDurationMultiplier);
			__instance.m_pregnancyChance = Helper.applyModifierValue(0.5f, procreation.pregnancyChanceMultiplier);
			__instance.m_partnerCheckRange = procreation.partnerCheckRange;
			__instance.m_maxCreatures = procreation.creatureLimit;

			__instance.m_pregnancyDuration = Helper.applyModifierValue(
				__instance.m_pregnancyDuration, procreation.pregnancyDurationMultiplier);

			__instance.m_pregnancyChance = 1f - Helper.applyModifierValue(
				__instance.m_pregnancyChance, procreation.pregnancyChanceMultiplier);
		}
	}

	[HarmonyPatch(typeof(Procreation), nameof(Procreation.Procreate))]
	public static class Procreation_Procreate_Transpiler
		{
		private static FieldInfo BaseAIField = AccessTools.Field(typeof(Procreation), nameof(Procreation.m_baseAI));
		private static MethodInfo baseAiIsAlerted = AccessTools.Method(typeof(BaseAI), nameof(BaseAI.IsAlerted));

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
		{
			var config = Configuration.Current.Procreation;
			if (!config.IsEnabled || (!config.ignoreHunger && !config.ignoreAlerted))
				return instructions;

			try {
				var matcher = new CodeMatcher(instructions, ilGenerator);
				IsTameValidTranspiler(matcher);

				if (config.ignoreHunger)
					TameableHelpers.IgnoreHungerTranspiler(matcher);

				if (config.ignoreAlerted)
					BaseAIHelpers.IgnoreAlertedTranspiler(matcher);

				return matcher.InstructionEnumeration();
			} catch (Exception ex)
		{
				ValheimPlusPlugin.Logger.LogError(ex);
				return instructions;

			var TameableField = AccessTools.Field(typeof(Procreation), nameof(Procreation.m_tameable));
			var IsTamedMethod = AccessTools.Method(typeof(Character), nameof(Character.IsTamed));
			var mIsHungry = AccessTools.Method(typeof(ProcreationHelper), nameof(IsHungry));

			var codes = new List<CodeInstruction>(instructions);
			for (int i = 2; i < codes.Count; i++)
			{
				if (codes[i - 2].opcode == OpCodes.Ldarg_0
					&& codes[i - 1].LoadsField(TameableField)
					&& codes[i].Calls(IsTamedMethod))
				{
					codes[i] = new CodeInstruction(OpCodes.Call, mIsHungry);
					break;
				}
			}

			return codes.AsEnumerable();
		}
	}
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
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
		private static AnimalType animalTypes;

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

			return animalTypes;
		}

		public static bool IsValidAnimalType(string name) => name switch {
			"$enemy_boar" => animalTypes.HasFlag(AnimalType.Boar),
			"$enemy_wolf" => animalTypes.HasFlag(AnimalType.Wolf),
			"$enemy_lox" => animalTypes.HasFlag(AnimalType.Lox),
			"$enemy_hen" => animalTypes.HasFlag(AnimalType.Hen),
			"$enemy_asksvin" => animalTypes.HasFlag(AnimalType.Asksvin),
			_ => false,
		};

		public static bool IsValidAnimalType(Procreation instance)
			=> IsValidAnimalType(instance.GetComponent<Character>()?.m_name);

		public static bool IsHungry(Tameable tameable)
		{
			ValheimPlusPlugin.Logger.LogInfo("Procreation.Procreate: checking if instance " + tameable.m_character.m_name + " is hungry");
			return !Configuration.Current.Procreation.ignoreHunger && tameable.IsHungry();
		}

		public static void ApplyHoverInformation(Tameable instance, ref string result)
		{
			if (!instance.m_character.IsTamed()
				|| !Configuration.Current.Procreation.IsEnabled
				|| !Configuration.Current.Procreation.loveInformation)
				return;

			var procreation = instance.GetComponent<Procreation>();
			if (!procreation)
				return;

			var lineBreak = result.IndexOf('\n');
			if (lineBreak <= 0)
				return;

			var lovePoints = instance.m_nview.GetZDO().GetInt(ZDOVars.s_lovePoints);
			if (procreation.IsPregnant())
				result = result.Insert(lineBreak, "\n<color=#FFAEC9>Pregnant</color>");
			else if (lovePoints > 0)
				result = result.Insert(lineBreak, $"\nLoved ({lovePoints}/{procreation.m_requiredLovePoints})");
			else
				result = result.Insert(lineBreak, $"\nNot loved");
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
	public static class Procreation_Procreate_Patch
	{
		public static bool IsValidInstance(Character character)
		{
			ValheimPlusPlugin.Logger.LogInfo("Procreation.Procreate: checking if instance " + character.m_name + " is valid");
			return character.IsTamed() && IsValidAnimalType(character.m_name);
		}

		public static bool IsAlertedReplacement(BaseAI baseAI)
		{
			ValheimPlusPlugin.Logger.LogInfo("Procreation.Procreate: checking if instance " + baseAI.m_character.m_name + " is alerted");
			return !Configuration.Current.Procreation.ignoreAlerted && baseAI.IsAlerted();
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			if (!Configuration.Current.Procreation.IsEnabled)
				return instructions;

			var CharacterField = AccessTools.Field(typeof(Procreation), nameof(Procreation.m_character));
			var characterIsTamed = AccessTools.Method(typeof(Character), nameof(Character.IsTamed));

			var TameableField = AccessTools.Field(typeof(Procreation), nameof(Procreation.m_tameable));
			var tameableIsHungry = AccessTools.Method(typeof(Tameable), nameof(Tameable.IsHungry));

			var BaseAIField = AccessTools.Field(typeof(Procreation), nameof(Procreation.m_baseAI));
			var baseAiIsAlerted = AccessTools.Method(typeof(BaseAI), nameof(BaseAI.IsAlerted));

			var mIsValidInstance = AccessTools.Method(typeof(Procreation_Procreate_Patch), nameof(IsValidInstance));
			var mIsHungry = AccessTools.Method(typeof(ProcreationHelper), nameof(IsHungry));
			var mIsAlerted = AccessTools.Method(typeof(Procreation_Procreate_Patch), nameof(IsAlertedReplacement));

			var codes = new List<CodeInstruction>(instructions);
			for (int i = 2; i < codes.Count; i++)
			{
				if (codes[i - 2].opcode != OpCodes.Ldarg_0)
					continue;

				if (codes[i].Calls(characterIsTamed) && codes[i - 1].LoadsField(CharacterField))
					codes[i] = new CodeInstruction(OpCodes.Call, mIsValidInstance);

				else if (codes[i].Calls(tameableIsHungry) && codes[i - 1].LoadsField(TameableField))
					codes[i] = new CodeInstruction(OpCodes.Call, mIsHungry);

				else if (codes[i].Calls(baseAiIsAlerted) && codes[i - 1].LoadsField(BaseAIField))
					codes[i] = new CodeInstruction(OpCodes.Call, mIsAlerted);
			}

			return codes.AsEnumerable();
		}
	}

	[HarmonyPatch(typeof(Procreation), nameof(Procreation.ReadyForProcreation))]
	public static class Procreation_ReadyForProcreation_Patch
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			if (!Configuration.Current.Procreation.IsEnabled)
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

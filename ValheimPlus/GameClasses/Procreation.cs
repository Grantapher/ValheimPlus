using HarmonyLib;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
	[HarmonyPatch(typeof(Procreation), nameof(Procreation.Awake)]
	public static class Procreation_Awake_Patch
	{
		public static void Postfix(Procreation __instance)
		{
			if (!Configuration.Current.Procreation.IsEnabled)
				return;

			var procreation = Configuration.Current.Procreation;
			__instance.m_requiredLovePoints = procreation.requiredLovePoints;
			__instance.m_pregnancyDuration = Helper.applyModifierValue(10f, procreation.pregnancyDurationMultiplier);
			__instance.m_pregnancyChance = Helper.applyModifierValue(0.5f, procreation.pregnancyChanceMultiplier);
			__instance.m_partnerCheckRange = procreation.partnerCheckRange;
			// __instance.m_ = procreation.ignoreHunger;
			__instance.m_maxCreatures = procreation.creatureLimit;
		}
	}


}

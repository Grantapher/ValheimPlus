﻿using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
	public static class GrowupExtensions
	{
		public static int GetGrowTimeLeft(this Growup growup)
			=> (int)(growup.m_growTime - growup.m_baseAI.GetTimeSinceSpawned().TotalSeconds);
	}

    [HarmonyPatch(typeof(Growup), nameof(Growup.Start))]
    public static class Growup_Start_Patch
    {
        [UsedImplicitly]
        public static void Prefix(Growup __instance)
        {
            var config = Configuration.Current.Egg;
            if (!config.IsEnabled) return;

            var humanoid = __instance.m_grownPrefab.GetComponent<Humanoid>();
            if (!humanoid || humanoid.m_name != "$enemy_hen") return;

            __instance.m_growTime = config.growTime;
        }
    }
}

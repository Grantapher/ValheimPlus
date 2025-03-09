using HarmonyLib;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
	[HarmonyPatch(typeof(Growup), nameof(Growup.Start))]
	public class Growup_Start_Patch
	{
		public static void Prefix(Growup __instance)
		{
			if (!Configuration.Current.Egg.IsEnabled)
				return;

			var humanoid = __instance.m_grownPrefab.GetComponent<Humanoid>();
			if (humanoid && humanoid.m_name == "$enemy_hen")
				__instance.m_growTime = Configuration.Current.Egg.growTime;
		}
	}
}

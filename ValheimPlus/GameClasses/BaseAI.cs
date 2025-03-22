using HarmonyLib;
using System;
using System.Reflection.Emit;

namespace ValheimPlus.GameClasses
{
	public static class BaseAIHelpers
	{
		private static bool IsAlerted(BaseAI baseAi) => false;

		public static CodeMatcher IgnoreAlertedTranspiler(CodeMatcher matcher, Func<Tameable, bool> pred = null)
		{
			var ignoreAlertedMethod = AccessTools.Method(typeof(BaseAIHelpers), nameof(IsAlerted));
			var baseAiIsAlerted = AccessTools.Method(typeof(BaseAI), nameof(BaseAI.IsAlerted));
			return matcher
				.MatchStartForward(new CodeMatch(inst => inst.Calls(baseAiIsAlerted)))
				.ThrowIfNotMatch("No match for IsAlerted method call.")
				.Set(OpCodes.Call, pred?.Method ?? ignoreAlertedMethod)
				.Start();
		}
	}
}

using HarmonyLib;
using System;
using System.Reflection.Emit;

namespace ValheimPlus.GameClasses
{
    public static class BaseAIHelpers
    {
        /// <summary>
        /// Appends `BaseAI.m_character.m_name` to the first `BaseAI.IsAlerted` call
        /// Calls the provided predicate with the character name
        /// Then inverts the result and ANDs it with the original result
        /// This allows the original `BaseAI.IsAlerted` method to be called for compatibility
        /// And exceptions to be made for any `BaseAI.m_character.m_name` that matches the predicate
        /// </summary>
        /// <param name="matcher">The current transpiler matcher</param>
        /// <param name="pred">BaseAI.m_character.m_name filter</param>
        /// <returns>The current transpiler matcher</returns>
        public static CodeMatcher IsAlertedTranspiler(CodeMatcher matcher, Func<string, bool> pred)
        {
            var isAlertedMethod = AccessTools.Method(typeof(BaseAI), nameof(BaseAI.IsAlerted));
            var baseAICharacterField = AccessTools.Field(typeof(BaseAI), nameof(BaseAI.m_character));
            var characterNameField = AccessTools.Field(typeof(Character), nameof(Character.m_name));
            matcher.MatchStartForward(new CodeMatch(inst => inst.Calls(isAlertedMethod)))
                .ThrowIfNotMatch("Could not find BaseAI.IsAlerted call")
                .Advance(1)
                .InsertAndAdvance(
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, baseAICharacterField),
                    new(OpCodes.Ldfld, characterNameField),
                    new(OpCodes.Call, pred.Method),
                    new(OpCodes.Not),
                    new(OpCodes.And)
                );
            return matcher;
        }
    }
}

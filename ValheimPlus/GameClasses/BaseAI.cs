using HarmonyLib;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsAlerted))]
    public static class BaseAI_IsAlerted_Patch
    {
        public static void Postfix(BaseAI __instance, ref bool __result)
        {
            if (!__instance.m_character.m_tameable)
                return;

            var isTamed = __instance.m_character.IsTamed();
            __result = __result && !(isTamed ?
                ProcreationHelpers.ShouldIgnoreAlerted(__instance)
                : TameableHelpers.ShouldIgnoreAlerted(__instance));
        }
    }
}

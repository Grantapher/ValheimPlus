using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValheimPlus.Configurations;
using ValheimPlus.UI;

namespace ValheimPlus.GameClasses
{
    /// <summary>
    /// Set max player limit and disable server password 
    /// </summary>
    [HarmonyPatch(typeof(FejdStartup), "Awake")]
    public static class HookServerStart
    {
        private static void Postfix(ref FejdStartup __instance)
        {
            if (Configuration.Current.Server.IsEnabled && Configuration.Current.Server.disableServerPassword)
            {
                __instance.m_minimumPasswordLength = 0;
            }
        }
    }

    /// <summary>
    /// Adding V+ logo and version text
    /// </summary>
    [HarmonyPatch(typeof(FejdStartup), "SetupGui")]
    public static class FejdStartup_SetupGui_Patch
    {
        private static void Postfix(ref FejdStartup __instance)
        {
            __instance.m_moddedText.transform.localPosition += new Vector3(0, 50);

            // version text for bottom right of startup
            __instance.m_versionLabel.fontSize = 14;
            __instance.m_versionLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 30);
            __instance.m_versionLabel.text += "\nValheimPlus " + ValheimPlusPlugin.FullVersion + " (Grantapher)";

            ValheimPlusPlugin.Logger.LogInfo($"Version text: \"{__instance.m_versionLabel.text}\"".Replace("\n", ", "));
        }
    }

    /// <summary>
    /// Alters public password requirements
    /// </summary>
    [HarmonyPatch(typeof(FejdStartup), "IsPublicPasswordValid")]
    public static class ChangeServerPasswordBehavior
    {
        private static void Postfix(ref bool __result)
        {
            if (Configuration.Current.Server.IsEnabled && Configuration.Current.Server.disableServerPassword)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// Override password error
    /// </summary>
    [HarmonyPatch(typeof(FejdStartup), "GetPublicPasswordError")]
    public static class RemovePublicPasswordError
    {
        private static bool Prefix(ref string __result)
        {
            if (Configuration.Current.Server.IsEnabled && Configuration.Current.Server.disableServerPassword)
            {
                __result = "";
                return false;
            }

            return true;
        }
    }
}
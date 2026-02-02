using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimPlus.GameClasses
{
    public static class MuteGameInBackground
    {
        public static Toggle muteAudioToggle;

        public static bool CreateToggle()
        {
            foreach (var iSettingsTab in Settings.instance.SettingsTabs)
            {
                if (iSettingsTab.GetType() == typeof(Valheim.SettingsGui.AudioSettings))
                {
                    Toggle cmToggle = ((Valheim.SettingsGui.AudioSettings)iSettingsTab).m_continousMusic;
                    muteAudioToggle = GameObject.Instantiate(cmToggle, cmToggle.transform.parent, false);
                    muteAudioToggle.name = "MuteGameInBackground";
                    muteAudioToggle.GetComponentInChildren<TMP_Text>().text = "Mute game in background";

                    // scaleFactor is overwritten by GuiScaler::UpdateScale, which is called every frame, but impacted when pressing OK in the settings dialog
                    CanvasScaler canvasScalerComponent = muteAudioToggle.transform.root.GetComponentInChildren<CanvasScaler>();
                    muteAudioToggle.transform.Translate(new Vector2(0, -40 * canvasScalerComponent.scaleFactor));
                    return true;
                }
            }

            ValheimPlusPlugin.Logger.LogError("Failed to create MuteGameInBackground toggle");
            return false;
        }
    }

    /// <summary>
    /// Read in saved user preference for audio mute toggle
    /// </summary>
    [HarmonyPatch(typeof(Settings), nameof(Settings.Awake))]
    public static class Settings_LoadSettings_Patch
    {
        private static void Postfix()
        {
           if (MuteGameInBackground.muteAudioToggle == null && !MuteGameInBackground.CreateToggle())
                return;

           MuteGameInBackground.muteAudioToggle.isOn = (PlayerPrefs.GetInt("MuteGameInBackground", 0) == 1); ;
        }
    }

    /// <summary>
    /// Save out user preference for audio mute toggle
    /// </summary>
    [HarmonyPatch(typeof(Settings))]
    public static class Settings_SaveSettings_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var saveTabSettings = typeof(Settings).GetMethod("SaveTabSettings",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (saveTabSettings != null)
                return new[] { saveTabSettings };

            var tabSaved = typeof(Settings).GetMethod("TabSaved",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (tabSaved != null)
                return new[] { tabSaved };

            ValheimPlusPlugin.Logger?.LogWarning(
                "Settings save hook not found (SaveTabSettings/TabSaved). Skipping mute-in-background preference save.");
            return Array.Empty<MethodBase>();
        }

        private static void Postfix()
        {
            if (MuteGameInBackground.muteAudioToggle != null)
                PlayerPrefs.SetInt("MuteGameInBackground", MuteGameInBackground.muteAudioToggle.isOn ? 1 : 0);
        }
    }
}

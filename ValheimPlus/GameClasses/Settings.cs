using HarmonyLib;
using JetBrains.Annotations;
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
        [UsedImplicitly]
        private static void Postfix()
        {
           if (MuteGameInBackground.muteAudioToggle == null && !MuteGameInBackground.CreateToggle())
                return;

           MuteGameInBackground.muteAudioToggle.isOn = (PlayerPrefs.GetInt("MuteGameInBackground", 0) == 1);
        }
    }

    /// <summary>
    /// Save out user preference for audio mute toggle
    /// </summary>
    [HarmonyPatch(typeof(Settings), nameof(Settings.OnOk))]
    public static class Settings_OnOk_Patch
    {
        [UsedImplicitly]
        private static void Postfix()
        {
            if (MuteGameInBackground.muteAudioToggle != null)
                PlayerPrefs.SetInt("MuteGameInBackground", MuteGameInBackground.muteAudioToggle.isOn ? 1 : 0);
        }
    }
}

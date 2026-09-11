using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ValheimPlus.GameClasses
{
    /// <summary>
    /// Mute all sound when the application loses focus
    /// </summary>
    [HarmonyPatch(typeof(EventSystem), "OnApplicationFocus")]
    public static class EventSystem_OnApplicationFocus_Patch
    {
        [UsedImplicitly]
        private static void Postfix(bool hasFocus)
        {
            if (PlayerPrefs.GetInt("MuteGameInBackground", 0) == 1)
            {
                AudioListener.volume = hasFocus ? 1f : 0f;
            }
        }
    }
}

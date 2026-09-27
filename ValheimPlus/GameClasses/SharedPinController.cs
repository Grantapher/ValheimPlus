using HarmonyLib;
using TMPro;
using UnityEngine;
using ValheimPlus.RPC;

namespace ValheimPlus.GameClasses
{
    // D-pad up/down select an icon and right toggles its filter. Left is unused by UpdateMap.
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateMap))]
    internal static class SharedPinController
    {
        internal static bool PrivatePlacement;
        private static TMP_Text hint;
        private static Minimap hintMap;

        private static void Prefix(Minimap __instance, bool takeInput)
        {
            bool active = VPlusSharedPins.Enabled && __instance.m_mode == Minimap.MapMode.Large
                && ZInput.IsExclusiveGamepadActive() && takeInput && !Minimap.InTextInput()
                && __instance.inputDelay <= 0f;
            if (!active)
            {
                if (hint != null) hint.gameObject.SetActive(false);
                return;
            }

            // Controller actions target the map's centre crosshair, not the mouse cursor.
            var pos = __instance.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f));
            var pin = __instance.GetClosestPin(pos, __instance.PinInteractRadius);
            bool canShare = pin != null && !VPlusSharedPins.IsShared(pin)
                && SharedPinStore.AllowedType((int)pin.m_type);
            if (ZInput.GetButtonDown("JoyDPadLeft"))
            {
                if (canShare) VPlusSharedPins.Publish(pin);
                else PrivatePlacement = !PrivatePlacement;
            }
            ShowHint(__instance, canShare);
        }

        private static void ShowHint(Minimap map, bool canShare)
        {
            if (hint == null || hintMap != map)
            {
                if (hint != null) Object.Destroy(hint.gameObject);
                var label = new GameObject("VPlusSharedPinControllerHint", typeof(RectTransform));
                label.transform.SetParent(map.m_largeRoot.transform, false);
                hint = label.AddComponent<TextMeshProUGUI>();
                hintMap = map;
                hint.font = map.m_biomeNameLarge.font;
                hint.fontSize = 20;
                hint.alignment = TextAlignmentOptions.Center;
                hint.color = new Color(1f, 0.8f, 0.3f, 1f);
                hint.outlineWidth = 0.2f;
                hint.raycastTarget = false;
                var rect = hint.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 70f);
                rect.sizeDelta = new Vector2(700f, 60f);
            }
            hint.text = "New pins: " + (PrivatePlacement ? "Private" : "Public")
                + "\nD-pad Left: " + (canShare ? "Share pin" : "Switch Public / Private");
            hint.gameObject.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapMode))]
    internal static class SharedPinControllerMapMode
    {
        private static void Prefix(Minimap __instance, Minimap.MapMode mode)
        {
            if (mode == Minimap.MapMode.Large && __instance.m_mode != mode)
                SharedPinController.PrivatePlacement = false;
        }
    }
}

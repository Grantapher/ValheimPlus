using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using ValheimPlus.Configurations;
using ValheimPlus.RPC;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    internal static class SharedPinsStart
    {
        private static void Postfix() => VPlusSharedPins.Start();
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Update))]
    internal static class SharedPinsUpdate
    {
        private static void Postfix() => VPlusSharedPins.Update();
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
    internal static class SharedPinsShutdown
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            // Remove transient public pins before vanilla saves the player's map.
            VPlusSharedPins.Reset();
            SharedPinCreation.Pending = null;
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.ShowPinNameInput))]
    internal static class SharedPinCreation
    {
        internal static Minimap.PinData Pending;
        private static void Postfix(Minimap __instance)
        {
            Pending = VPlusSharedPins.Enabled && !ZInput.GetKey(Configuration.Current.Map.privatePinKey)
                ? __instance.m_namePin : null;
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.HidePinTextInput))]
    internal static class SharedPinFinishName
    {
        private static void Prefix(Minimap __instance, out Minimap.PinData __state)
        {
            __state = VPlusSharedPins.Enabled && SharedPinCreation.Pending == __instance.m_namePin
                ? SharedPinCreation.Pending : null;
            SharedPinCreation.Pending = null;
        }
        private static void Postfix(Minimap.PinData __state)
        {
            // OnPinTextEntered changes the name before HidePinTextInput; AddPin itself is too early.
            if (__state != null && Minimap.instance.m_pins.Contains(__state)) VPlusSharedPins.Publish(__state);
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    internal static class PublishPrivatePin
    {
        private static bool Prefix(Minimap __instance)
        {
            if (!VPlusSharedPins.Enabled || !ZInput.GetKey(Configuration.Current.Map.publishPinKey)) return true;
            var pin = __instance.GetClosestPinToCursor();
            if (pin == null || VPlusSharedPins.IsShared(pin)) return true;
            if (!SharedPinStore.AllowedType((int)pin.m_type)) return true;
            VPlusSharedPins.Publish(pin);
            return false;
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.RemovePin), new[] { typeof(Minimap.PinData) })]
    internal static class DeleteSharedPin
    {
        private static bool Prefix(Minimap.PinData pin) => VPlusSharedPins.Remove(pin);
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePins))]
    internal static class SharedPinColor
    {
        private static readonly Color Gold = new(1f, 0.8f, 0.3f, 1f);
        private static void Postfix()
        {
            if (!VPlusSharedPins.Enabled) return;
            foreach (var pin in VPlusSharedPins.Displayed.Values)
            {
                if (pin.m_iconElement != null) pin.m_iconElement.color = Gold;
                if (pin.m_NamePinData?.PinNameText != null) pin.m_NamePinData.PinNameText.color = Gold;
            }
        }
    }

    [HarmonyPatch]
    internal static class ExcludeSharedPinsFromCharacterSave
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(Minimap), nameof(Minimap.GetMapData));
            yield return AccessTools.DeclaredMethod(typeof(Minimap), nameof(Minimap.GetSharedMapData));
        }
        private static void Prefix(out List<Minimap.PinData> __state)
        {
            // m_save is also the vanilla hit-test flag; exclude public pins only during serialization.
            __state = VPlusSharedPins.Displayed.Values.Where(p => p.m_save).ToList();
            foreach (var pin in __state) pin.m_save = false;
        }
        private static void Postfix(List<Minimap.PinData> __state) => Restore(__state);
        private static void Finalizer(List<Minimap.PinData> __state) => Restore(__state);
        private static void Restore(List<Minimap.PinData> pins)
        {
            if (pins != null) foreach (var pin in pins) pin.m_save = true;
        }
    }
}

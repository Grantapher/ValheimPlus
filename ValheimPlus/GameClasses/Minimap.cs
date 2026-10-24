using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimPlus.Configurations;
using ValheimPlus.RPC;

namespace ValheimPlus.GameClasses
{
    /// <summary>
    /// Hooks base explore method
    /// </summary>
    [HarmonyPatch(typeof(Minimap))]
    public class HookExplore
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "Explore", new Type[] { typeof(Vector3), typeof(float) })]
        public static void call_Explore(object instance, Vector3 p, float radius) => throw new NotImplementedException();
    }

    /// <summary>
    /// Update exploration for all players
    /// </summary>
    [HarmonyPatch(typeof(Minimap), "UpdateExplore")]
    public static class ChangeMapBehavior
    {
        /// <summary>
        /// Past this the explore loop gets unreasonably expensive, so the configured value is capped.
        /// </summary>
        internal const float MaxExploreRadius = 10000f;

        private static void Prefix(ref float dt, ref Player player, ref Minimap __instance, ref float ___m_exploreTimer, ref float ___m_exploreInterval)
        {
            if (!Configuration.Current.Map.IsEnabled) return;

            if (Configuration.Current.Map.shareMapProgression)
            {
                float explorerTime = ___m_exploreTimer;
                explorerTime += Time.deltaTime;
                if (explorerTime > ___m_exploreInterval)
                {
                    if (ZNet.instance.m_players.Any())
                    {
                        foreach (ZNet.PlayerInfo m_Player in ZNet.instance.m_players)
                        {
                            HookExplore.call_Explore(__instance, m_Player.m_position, Mathf.Min(Configuration.Current.Map.exploreRadius, MaxExploreRadius));
                        }
                    }
                }
            }

            // Always reveal for your own, we do this non the less to apply the potentially bigger exploreRadius
            HookExplore.call_Explore(__instance, player.transform.position, Mathf.Min(Configuration.Current.Map.exploreRadius, MaxExploreRadius));
        }
    }

    [HarmonyPatch(typeof(Minimap), "Awake")]
    public static class MinimapAwake
    {
        private static void Postfix()
        {
            var map = Configuration.Current.Map;
            if (!ZNet.m_isServer || !map.IsEnabled) return;

            if (map.shareMapProgression)
            {
                //Init map array
                VPlusMapSync.ServerMapData = new BitArray(Minimap.instance.m_textureSize * Minimap.instance.m_textureSize);

                //Load map data from disk
                VPlusMapSync.LoadMapDataFromDisk();
            }

            //Start the save timer, which also saves shared map pins
            if (map.shareMapProgression || map.shareAllPins)
                ValheimPlusPlugin.MapSyncSaveTimer.Start();
        }
    }

    /// <summary>
    /// Show boats and carts on map
    /// </summary>
    public class displayCartsAndBoatsOnMap
    {
        static Dictionary<ZDO, Minimap.PinData> customPins = new Dictionary<ZDO, Minimap.PinData>();
        static Dictionary<int, Sprite> icons = new Dictionary<int, Sprite>();
        static int CartHashcode = "Cart".GetStableHashCode();
        static int RaftHashcode = "Raft".GetStableHashCode();
        static int KarveHashcode = "Karve".GetStableHashCode();
        static int LongshipHashcode = "VikingShip".GetStableHashCode();
        static int hammerHashCode = "Hammer".GetStableHashCode();
        static float updateInterval = 5.0f;

        // clear dictionary if the user logs out
        [HarmonyPatch(typeof(Minimap), "OnDestroy")]
        public static class Minimap_OnDestroy_Patch
        {
            private static void Postfix()
            {
                customPins.Clear();
                icons.Clear();
            }
        }

        [HarmonyPatch(typeof(Minimap), "UpdateMap")]
        public static class Minimap_UpdateMap_Patch
        {
            static float timeCounter = updateInterval;

            private static void FindIcons()
            {
                GameObject hammer = ObjectDB.instance.m_itemByHash[hammerHashCode];
                if (!hammer)
                    return;
                ItemDrop hammerDrop = hammer.GetComponent<ItemDrop>();
                if (!hammerDrop)
                    return;
                PieceTable hammerPieceTable = hammerDrop.m_itemData.m_shared.m_buildPieces;
                foreach (GameObject piece in hammerPieceTable.m_pieces)
                {
                    Piece p = piece.GetComponent<Piece>();
                    icons.Add(p.name.GetStableHashCode(), p.m_icon);
                }
            }

            private static bool CheckPin(Minimap __instance, Player player, ZDO zdo, int hashCode, string pinName)
            {
                if (zdo.m_prefab != hashCode)
                    return false;

                Minimap.PinData customPin;
                bool pinWasFound = customPins.TryGetValue(zdo, out customPin);

                // turn off associated pin if player controlled ship is in that position
                Ship controlledShip = player.GetControlledShip();
                if (controlledShip && Vector3.Distance(controlledShip.transform.position, zdo.m_position) < 0.01f)
                {
                    if (pinWasFound)
                    {
                        __instance.RemovePin(customPin);
                        customPins.Remove(zdo);
                    }
                    return true;
                }

                if (!pinWasFound)
                {
                    customPin = __instance.AddPin(zdo.m_position, Minimap.PinType.Death, pinName, false, false);

                    Sprite sprite;
                    if (icons.TryGetValue(hashCode, out sprite))
                        customPin.m_icon = sprite;

                    customPin.m_doubleSize = true;
                    customPins.Add(zdo, customPin);
                } 
                else
                    customPin.m_pos = zdo.m_position;

                return true;
            }

            public static void Postfix(ref Minimap __instance, Player player, float dt, bool takeInput)
            {
                timeCounter += dt;

                if (timeCounter < updateInterval || !Configuration.Current.Map.IsEnabled || !Configuration.Current.Map.displayCartsAndBoats)
                    return;

                timeCounter -= updateInterval;

                if (icons.Count == 0)
                    FindIcons();

                // search zones for ships and carts
                foreach (List<ZDO> zdoarray in ZDOMan.instance.m_objectsBySector)
                {
                    if (zdoarray != null)
                    {
                        foreach (ZDO zdo in zdoarray)
                        {
                            if (CheckPin(__instance, player, zdo, CartHashcode, "Cart"))
                                continue;
                            if (CheckPin(__instance, player, zdo, RaftHashcode, "Raft"))
                                continue;
                            if (CheckPin(__instance, player, zdo, KarveHashcode, "Karve"))
                                continue;
                            if (CheckPin(__instance, player, zdo, LongshipHashcode, "Longship"))
                                continue;
                        }
                    }
                }

                // clear pins for destroyed objects
                foreach (KeyValuePair<ZDO, Minimap.PinData> pin in customPins)
                {
                    if (!pin.Key.IsValid())
                    {
                        __instance.RemovePin(pin.Value);
                        customPins.Remove(pin.Key);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Sends pins this player places to the server, which owns the shared list.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.AddPin))]
    public static class Minimap_AddPin_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Minimap.PinData __result) => VPlusMapPinSync.SendAdd(__result);
    }

    /// <summary>
    /// Vanilla adds a map-click pin unnamed and names it when the dialog closes, so sharing waits for the name.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.ShowPinNameInput))]
    public static class Minimap_ShowPinNameInput_Patch
    {
        [UsedImplicitly]
        private static void Prefix() => VPlusMapPinSync.CreatingNamedPin = true;

        [UsedImplicitly]
        private static void Finalizer() => VPlusMapPinSync.CreatingNamedPin = false;
    }

    /// <summary>
    /// Reading a cartography table removes faded pins the table lacks. Keeps that local and restores shared pins.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.AddSharedMapData))]
    public static class Minimap_AddSharedMapData_Patch
    {
        [UsedImplicitly]
        private static void Prefix() => VPlusMapPinSync.BeginMapTableRead();

        [UsedImplicitly]
        private static void Finalizer() => VPlusMapPinSync.EndMapTableRead();
    }

    /// <summary>
    /// Shares the pin held for the name dialog once the dialog lets go of it, whichever way it closed.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    public static class Minimap_Update_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Minimap __instance, Minimap.PinData ___m_namePin) =>
            VPlusMapPinSync.FlushNamedPin(__instance, ___m_namePin);
    }

    /// <summary>
    /// Sends pin deletions to the server. A delete applies to every player.
    /// Both vanilla remove paths funnel through this overload, and ClearPins does not,
    /// so leaving a world never looks like a deletion.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.RemovePin), typeof(Minimap.PinData))]
    public static class Minimap_RemovePin_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Minimap.PinData pin) => VPlusMapPinSync.SendRemove(pin);
    }

    /// <summary>
    /// Drops shared pin state when leaving a world.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnDestroy))]
    public static class Minimap_OnDestroy_PinSync_Patch
    {
        [UsedImplicitly]
        private static void Postfix() => VPlusMapPinSync.Reset();
    }
}

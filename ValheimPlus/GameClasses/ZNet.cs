using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using ValheimPlus.Configurations;
using ValheimPlus.RPC;
using ValheimPlus.Utility;

// ToDo add packet system to convey map markers
namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(ZNet))]
    public class HookZNet
    {
        /// <summary>
        /// Hook base GetOtherPublicPlayer method
        /// </summary>
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ZNet), "GetOtherPublicPlayers", new Type[] { typeof(List<ZNet.PlayerInfo>) })]
        public static void GetOtherPublicPlayers(object instance, List<ZNet.PlayerInfo> playerList) => throw new NotImplementedException();
    }

    /// <summary>
    /// Send queued RPCs
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "SendPeriodicData")]
    public static class PeriodicDataHandler
    {
        private static void Postfix()
        {
            RpcQueue.SendNextRpc();
        }
    }

    /// <summary>
    /// Sync server client configuration
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    public static class ConfigServerSync
    {
        private static MethodInfo method_ZNet_GetNrOfPlayers = AccessTools.Method(typeof(ZNet), nameof(ZNet.GetNrOfPlayers));

        private static void Postfix(ref ZNet __instance)
        {
            if (!ZNet.m_isServer)
            {
                ValheimPlusPlugin.Logger.LogInfo("-------------------- SENDING VPLUGCONFIGSYNC REQUEST");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "VPlusConfigSync", new object[] { new ZPackage() });
            }
        }

        /// <summary>
        /// Alter server player limit
        /// </summary>
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> il = instructions.ToList();

            for (int i = 0; i < il.Count; i++)
            {
                if (il[i].Calls(method_ZNet_GetNrOfPlayers))
                {
                    il[i + 1].operand = Configuration.Current.Server.maxPlayers;
                    return il.AsEnumerable();
                }
            }

            ValheimPlusPlugin.Logger.LogError("Failed to alter server player limit (ZNet.RPC_PeerInfo.Transpiler)");

            return instructions;
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_CharacterID")]
    public static class ZNet_ID_Patch
    {
        // Triggers sending of map pins to players as they connect
        public static void Postfix(ZRpc rpc, ZDOID characterID)
        {
            ZNetPeer peer = ZNet.instance.GetPeer(rpc);
            if (peer != null)
            {
                peer.m_characterID = characterID;
                string playerName = peer.m_playerName;
                ZDOID zDOID = characterID;
                ZLog.Log("Got character ZDOID from " + playerName + " : " + zDOID.ToString());
                
                if (ZNet.m_isServer)
                {
                    // Send stored map pins to the player
                    SendPinsToPlayer(peer.m_uid);
                }
            }
        }

        // Sends stored pins from memory to players when they connect
        private static void SendPinsToPlayer(long playerId)
        {
            ValheimPlusPlugin.Logger.LogInfo("Sending stored map pins to player ID: " + playerId);

            foreach (var mapPinData in ValheimPlus.GameClasses.Game_Start_Patch.storedMapPins)
            {
                ZPackage packageToSend = new ZPackage();
                packageToSend.Write(mapPinData.SenderID);
                packageToSend.Write(mapPinData.SenderName);
                packageToSend.Write(mapPinData.Position);
                packageToSend.Write(mapPinData.PinType);
                packageToSend.Write(mapPinData.PinName);
                packageToSend.Write(mapPinData.KeepQuiet);

                ZRoutedRpc.instance.InvokeRoutedRPC(playerId, "VPlusMapAddPin", new object[] { packageToSend });
            }
        }
    }

    // Periodically saves map pins to disk
    [HarmonyPatch(typeof(ZNet), "SaveWorld")]
    public static class PinSave_patch
    {
        private static void Postfix(Game __instance)
        {
            if (ZNet.instance.IsServer() && !ZNet.instance.IsLocalInstance())
            {       
                List<MapPinData> mapData = ValheimPlus.GameClasses.Game_Start_Patch.storedMapPins;

                if (Configuration.Current.Map.shareAllPins)
                {
                    try
                    {
                        using (StreamWriter writer = new StreamWriter(ValheimPlus.GameClasses.Game_Start_Patch.PinDataFilePath, false, Encoding.UTF8))
                        {
                            foreach (var pin in mapData)
                            {
                                string newLine = $"{pin.SenderID},{pin.SenderName},{pin.Position.x},{pin.Position.y},{pin.Position.z},{pin.PinType},{pin.PinName},{pin.KeepQuiet}";
                                writer.WriteLine(newLine);
                            }

                            ValheimPlusPlugin.Logger.LogInfo("Saving Map Pins Completed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions (e.g., logging)
                        ValheimPlusPlugin.Logger.LogError("An error occurred while saving pins: " + ex.Message);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Load settngs from server instance
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "Shutdown")]
    public static class OnErrorLoadOwnIni
    {
        private static void Prefix(ref ZNet __instance)
        {
            if (!__instance.IsServer())
            {
                ValheimPlusPlugin.UnpatchSelf();

                // Load the client config file on server ZNet instance exit (server disconnect)
                if (ConfigurationExtra.LoadSettings() != true)
                {
                    ValheimPlusPlugin.Logger.LogError("Error while loading configuration file.");
                }

                ValheimPlusPlugin.PatchAll();

                //We left the server, so reset our map sync check.
                if (Configuration.Current.Map.IsEnabled && Configuration.Current.Map.shareMapProgression)
                    VPlusMapSync.ShouldSyncOnSpawn = true;
            }
            else
            {
                //Save map data to disk
                if (Configuration.Current.Map.IsEnabled && Configuration.Current.Map.shareMapProgression)
                    VPlusMapSync.SaveMapDataToDisk();
            }
        }
    }

    /// <summary>
    /// Force player public reference position on
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "SetPublicReferencePosition")]
    public static class PreventPublicPositionToggle
    {
        private static void Postfix(ref bool pub, ref bool ___m_publicReferencePosition)
        {
            if (Configuration.Current.Map.IsEnabled && Configuration.Current.Map.preventPlayerFromTurningOffPublicPosition)
            {
                ___m_publicReferencePosition = true;
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_ServerSyncedPlayerData")]
    public static class PlayerPositionWatcher
    {
        private static void Postfix(ref ZNet __instance, ZRpc rpc)
        {
            if (!__instance.IsServer()) return;

            if (Configuration.Current.Map.IsEnabled && Configuration.Current.Map.shareMapProgression)
            {
                ZNetPeer peer = __instance.GetPeer(rpc);
                if (peer == null) return;
                Vector3 pos = peer.m_refPos;
                Minimap.instance.WorldToPixel(pos, out int pixelX, out int pixelY);

                int radiusPixels =
                    (int)Mathf.Ceil(Configuration.Current.Map.exploreRadius / Minimap.instance.m_pixelSize);

                // todo this looks like it can be optimized better
                for (int y = pixelY - radiusPixels; y <= pixelY + radiusPixels; ++y)
                {
                    for (int x = pixelX - radiusPixels; x <= pixelX + radiusPixels; ++x)
                    {
                        if (x >= 0 && y >= 0 &&
                            (x < Minimap.instance.m_textureSize && y < Minimap.instance.m_textureSize) &&
                            ((double)new Vector2((float)(x - pixelX), (float)(y - pixelY)).magnitude <=
                             (double)radiusPixels))
                        {
                            VPlusMapSync.ServerMapData[y * Minimap.instance.m_textureSize + x] = true;
                        }
                    }
                }
            }
        }
    }
}

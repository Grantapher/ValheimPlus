using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.PeerToPeer.Collaboration;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using ValheimPlus.GameClasses;
using static Minimap;

namespace ValheimPlus.RPC
{
    public class VPlusMapPinSync
    {
        /// <summary>
		/// Sync Pin with clients via the server
        /// </summary>
        public static void RPC_VPlusMapAddPin(long sender, ZPackage mapPinPkg)
        {            
            if (ZNet.m_isServer) // Server
            {
                int count = ValheimPlus.GameClasses.Game_Start_Patch.storedMapPins.Count;                

                if (mapPinPkg == null)
                {
                    ValheimPlusPlugin.Logger.LogInfo("Map Package is null.");
                    return;
                }                
                                  
                // Should append to sharedMapPins
                List<MapPinData> pinList = new List<MapPinData>();                

                while (mapPinPkg.GetPos() < mapPinPkg.Size())
                {
                    long senderID = mapPinPkg.ReadLong();
                    string senderName = mapPinPkg.ReadString();
                    Vector3 pos = mapPinPkg.ReadVector3();
                    int pinType = mapPinPkg.ReadInt();
                    string pinName = mapPinPkg.ReadString();
                    bool keepQuiet = mapPinPkg.ReadBool();

                    MapPinData pinData = new MapPinData
                    {
                        SenderID = senderID,
                        SenderName = senderName,
                        Position = pos,
                        PinType = pinType,
                        PinName = pinName,
                        KeepQuiet = keepQuiet
                    };

                    // Generate unique ID for the pin based on coordinates
                    string uniqueID = pinData.GetUniqueID();
                        

                    bool exists = ValheimPlus.GameClasses.Game_Start_Patch.storedMapPins.Any(existingPin =>
                    {
                        // Compare the unique ID of the existing pin with the unique ID of the received pin
                        return existingPin.GetUniqueID() == uniqueID;
                    });

                    if (!exists)
                    {
                        // If the pin is not a duplicate, add it to the list
                        ValheimPlus.GameClasses.Game_Start_Patch.storedMapPins.Add(pinData);
                    }
                }

                try
                {
                    using (StreamWriter writer = new StreamWriter(ValheimPlus.GameClasses.Game_Start_Patch.PinDataFilePath, true, Encoding.UTF8))
                    {
                        foreach (var pin in pinList)
                        {
                            string newLine = $"{pin.SenderID},{pin.SenderName},{pin.Position.x},{pin.Position.y},{pin.Position.z},{pin.PinType},{pin.PinName},{pin.KeepQuiet}";
                            writer.WriteLine(newLine);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., logging)
                    ValheimPlusPlugin.Logger.LogInfo("An error occurred while saving pins: " + ex.Message);
                }

                foreach (ZNetPeer peer in ZRoutedRpc.instance.m_peers)
                {
                    if (peer.m_uid != sender)
                        ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "VPlusMapAddPin", new object[] { mapPinPkg });
                }

                // ValheimPlusPlugin.Logger.LogInfo("Sent map pin to all clients.");  // This also triggers on every pin sent even when players log in              

            }
            else // Client
            {
                if (sender != ZRoutedRpc.instance.GetServerPeerID()) return; // Only bother if it's from the server.

                if (mapPinPkg == null)
                {
                    ValheimPlusPlugin.Logger.LogWarning("Warning: Got empty map pin package from server.");
                    return;
                }                

                try
                {
                    // Reset the read position to the start of the package
                    mapPinPkg.SetPos(0);

                    long pinSender = mapPinPkg.ReadLong();
                    string senderName = mapPinPkg.ReadString();
                    Vector3 pinPos = mapPinPkg.ReadVector3();
                    int pinType = mapPinPkg.ReadInt();
                    string pinName = mapPinPkg.ReadString();
                    bool keepQuiet = mapPinPkg.ReadBool();
                    

                    if (senderName != Player.m_localPlayer.GetPlayerName() && pinSender != ZRoutedRpc.instance.m_id)
                    {      
                        if (!Minimap.instance.HaveSimilarPin(pinPos, (Minimap.PinType)pinType, pinName, true))
                        {
                            Minimap.PinData addedPin = Minimap.instance.AddPin(pinPos, (Minimap.PinType)pinType, pinName, true, false);
                            if (!keepQuiet)
                                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Received map pin {pinName} from {senderName}!",
                                0, Minimap.instance.GetSprite((Minimap.PinType)pinType));
                            ValheimPlusPlugin.Logger.LogInfo($"I got pin named {pinName} from {senderName}!");
                        }
                    }

                    // Send Ack
                    // VPlusAck.SendAck(sender);
                }
                catch (Exception ex)
                {
                    ValheimPlusPlugin.Logger.LogError($"Exception while reading map pin data: {ex.Message}");
                }
            }
        }

        /// <summary>
		/// Send the pin, attach client ID
        /// </summary>
        public static void SendMapPinToServer(Vector3 pos, Minimap.PinType type, string name, bool keepQuiet = false)
        {
            ValheimPlusPlugin.Logger.LogInfo("-------------------- SENDING VPLUS MapPin DATA");
            ZPackage pkg = new ZPackage();

            pkg.Write(ZRoutedRpc.instance.m_id); // Sender ID

            if (keepQuiet)
            {
                pkg.Write(""); // when true, loads blank name to prevent shouting
            }
            
            if (!keepQuiet)
            {
                pkg.Write(Player.m_localPlayer.GetPlayerName()); // Sender Name
            }

            pkg.Write(pos); // Pin position
            pkg.Write((int)type); // Pin type
            pkg.Write(name); // Pin name
            pkg.Write(keepQuiet); // Don't shout           

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "VPlusMapAddPin", new object[] { pkg });
        }
    }
}

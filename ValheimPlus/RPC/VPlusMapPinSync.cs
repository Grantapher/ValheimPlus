using UnityEngine;

namespace ValheimPlus.RPC
{
    public class VPlusMapPinSync
    {
        /// <summary>
		/// Sync Pin with clients via the server
        /// </summary>
        public static void RPC_VPlusMapAddPin(long sender, ZPackage mapPinPkg)
        {
            if (ZNet.m_isServer) //Server
            {
                if (sender == ZRoutedRpc.instance.GetServerPeerID()) return;

                if (mapPinPkg == null) return;

                foreach(ZNetPeer peer in ZRoutedRpc.instance.m_peers)
                {
                    if(peer.m_uid != sender)
                        ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "VPlusMapAddPin", new object[] { mapPinPkg });
                }

                ValheimPlusPlugin.Logger.LogInfo($"Sent map pin to all clients");
                //VPlusAck.SendAck(sender);
            }
            else //Client
            {
                if (sender != ZRoutedRpc.instance.GetServerPeerID()) return; //Only bother if it's from the server.

                if (mapPinPkg == null)
                {
                    ValheimPlusPlugin.Logger.LogWarning("Warning: Got empty map pin package from server.");
                    return;
                }
                long pinSender = mapPinPkg.ReadLong();
                string senderName = mapPinPkg.ReadString();
                if (senderName != Player.m_localPlayer.GetPlayerName() && pinSender != ZRoutedRpc.instance.m_id)
                {
                    ValheimPlusPlugin.Logger.LogInfo("Checking sent pin");
                    Vector3 pinPos = mapPinPkg.ReadVector3();
                    int pinType = mapPinPkg.ReadInt();
                    string pinName = mapPinPkg.ReadString();
                    bool keepQuiet = mapPinPkg.ReadBool();
                    if (!Minimap.instance.HaveSimilarPin(pinPos, (Minimap.PinType)pinType, pinName, true))
                    {
                        Minimap.PinData addedPin = Minimap.instance.AddPin(pinPos, (Minimap.PinType)pinType, pinName, true, false);
                        if(!keepQuiet)
                            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Received map pin {pinName} from {senderName}!",
                            0, Minimap.instance.GetSprite((Minimap.PinType)pinType));
                        ValheimPlusPlugin.Logger.LogInfo($"I got pin named {pinName} from {senderName}!");
                    }
                }
                //Send Ack
                //VPlusAck.SendAck(sender);
            }
        }

        /// <summary>
		/// Send the pin, attach client ID
        /// </summary>
        public static void SendMapPinToServer(Minimap.PinData pinData, bool keepQuiet = false)
        {
            ValheimPlusPlugin.Logger.LogInfo("-------------------- SENDING VPLUS MapPin DATA");
            ZPackage pkg = new ZPackage();

            pkg.Write(ZRoutedRpc.instance.m_id); // Sender ID
            if(keepQuiet)
                pkg.Write(""); // Loaded in
            else
                pkg.Write(Player.m_localPlayer.GetPlayerName()); // Sender Name
            pkg.Write(pinData.m_pos); // Pin position
            pkg.Write((int)pinData.m_type); // Pin type
            pkg.Write(pinData.m_name); // Pin name
            pkg.Write(keepQuiet); // Don't shout

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "VPlusMapAddPin", new object[] { pkg });

            ValheimPlusPlugin.Logger.LogInfo($"Sent map pin {pinData.m_name} to the server");

        }
    }
}

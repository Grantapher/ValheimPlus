using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Bootstrap;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.RPC
{
    // Original implementation. Public/private interaction follows the requested SharedMap workflow;
    // no TXC code, assets or wire format are used.
    internal static class VPlusSharedPins
    {
        private const string RequestRpc = "VPlusPinsRequestV1", ResponseRpc = "VPlusPinsResponseV1";
        private const int Protocol = 1, BatchSize = 16, MaxQueued = 512;
        private enum Request { Hello, Add, Delete, Check }
        private enum Response { Begin, Batch, End, Upsert, Delete, Rejected }
        private sealed class Subscriber
        {
            internal readonly Queue<ZPackage> Outgoing = new();
            internal float NextAction, NextHello;
        }
        private static readonly Dictionary<long, Subscriber> Subscribers = new();
        private static SharedPinStore store;
        private static string savePath;
        private static long world;
        private static bool registered, failed, conflictLogged;
        private static float nextPump, nextHello;
        private static long revision = -1, stagingRevision;
        private static int stagingCount;
        private static Dictionary<string, SharedPin> staging;
        private static readonly Dictionary<string, SharedPin> Records = new();
        internal static readonly Dictionary<string, Minimap.PinData> Displayed = new();
        private static readonly Dictionary<string, Minimap.PinData> Pending = new();
        internal static bool Applying;
        internal static bool Ready => revision >= 0 && Enabled;

        internal static bool Enabled
        {
            get
            {
                if (Configuration.Current?.Map?.IsEnabled != true || !Configuration.Current.Map.sharePins) return false;
                bool conflict = Chainloader.PluginInfos.Values.Any(p =>
                    string.Equals(p.Metadata.GUID, "txc.sharedmap", StringComparison.OrdinalIgnoreCase) ||
                    p.Instance?.GetType().Assembly.GetName().Name == "TXC.SharedMap");
                if (conflict && !conflictLogged)
                {
                    conflictLogged = true;
                    ValheimPlusPlugin.Logger.LogWarning("V+ shared pins disabled: TXC SharedMap is loaded. Use only one pin-sharing system.");
                }
                return !conflict;
            }
        }

        internal static void Start()
        {
            Reset();
            ZRoutedRpc.instance.Register<ZPackage>(RequestRpc, ReceiveRequest);
            ZRoutedRpc.instance.Register<ZPackage>(ResponseRpc, ReceiveResponse);
            registered = true;
        }

        internal static void Reset()
        {
            ClearDisplay();
            Pending.Clear(); Records.Clear(); Subscribers.Clear();
            store = null; staging = null; revision = -1; registered = false; failed = false;
            nextHello = nextPump = 0; conflictLogged = false;
        }

        private static bool LoadStore()
        {
            if (failed) return false;
            if (store != null) return true;
            try
            {
                world = ZNet.instance.GetWorldUID();
                savePath = Path.Combine(ValheimPlusPlugin.VPlusDataDirectoryPath, world + "_sharedPins.dat");
                store = SharedPinStore.Load(savePath, world);
                ValheimPlusPlugin.Logger.LogInfo($"Shared pins: loaded {store.Pins.Count} pins for world {world}.");
                return true;
            }
            catch (Exception e)
            {
                failed = true;
                ValheimPlusPlugin.Logger.LogError($"Shared pins disabled: could not read save; original file retained. {e}");
                return false;
            }
        }

        internal static void Update()
        {
            if (!registered || ZNet.instance == null || ZRoutedRpc.instance == null) return;
            if (!Enabled)
            {
                if (Displayed.Count > 0) ClearDisplay();
                Pending.Clear(); Records.Clear(); staging = null; revision = -1;
                Subscribers.Clear();
                return;
            }
            float now = Time.realtimeSinceStartup;
            if (Player.m_localPlayer != null && Minimap.instance != null && now >= nextHello)
            {
                nextHello = now + 5;
                ZPackage hello = Package((int)Request.Hello); hello.Write(revision);
                Send(hello);
            }
            if (now < nextPump) return;
            nextPump = now + 0.25f;
            if (ZNet.instance.IsServer())
            {
                foreach (long id in Subscribers.Keys.ToList())
                {
                    var peer = ZNet.instance.GetPeer(id);
                    bool host = id == ZRoutedRpc.instance.GetServerPeerID() && !ZNet.instance.IsDedicated();
                    if (!host && (peer == null || !peer.IsReady())) { Subscribers.Remove(id); continue; }
                    if (!host && peer.m_socket.GetSendQueueSize() > 5120) continue;
                    var queue = Subscribers[id].Outgoing;
                    for (int i = 0; i < 4 && queue.Count > 0; i++)
                    {
                        if (!host && peer.m_socket.GetSendQueueSize() > 5120) break;
                        ZRoutedRpc.instance.InvokeRoutedRPC(id, ResponseRpc, queue.Dequeue());
                    }
                }
            }
            if (Ready)
            {
                foreach (var entry in Displayed.ToList())
                {
                    if (!Records.TryGetValue(entry.Key, out var record) || entry.Value.m_checked == record.Checked) continue;
                    ZPackage request = Package((int)Request.Check); request.Write(entry.Key); request.Write(entry.Value.m_checked);
                    entry.Value.m_checked = record.Checked; // Render committed state until the server accepts the change.
                    Send(request);
                }
            }
        }

        private static ZPackage Package(int operation)
        {
            var pkg = new ZPackage(); pkg.Write(Protocol); pkg.Write(operation); return pkg;
        }
        private static void Send(ZPackage pkg) => ZRoutedRpc.instance.InvokeRoutedRPC(
            ZRoutedRpc.instance.GetServerPeerID(), RequestRpc, pkg);
        private static void WritePin(ZPackage pkg, SharedPin p)
        {
            pkg.Write(p.Id); pkg.Write(p.Owner ?? ""); pkg.Write(p.Name); pkg.Write(p.Type);
            pkg.Write(p.X); pkg.Write(p.Y); pkg.Write(p.Z); pkg.Write(p.Checked);
        }
        private static SharedPin ReadPin(ZPackage pkg) => new()
        {
            Id = pkg.ReadString(), Owner = pkg.ReadString(), Name = pkg.ReadString(), Type = pkg.ReadInt(),
            X = pkg.ReadSingle(), Y = pkg.ReadSingle(), Z = pkg.ReadSingle(), Checked = pkg.ReadBool()
        };

        private static void ReceiveRequest(long sender, ZPackage pkg)
        {
            if (!Enabled || !ZNet.instance.IsServer() || pkg == null || pkg.Size() > 2048) return;
            var peer = ZNet.instance.GetPeer(sender);
            bool host = sender == ZRoutedRpc.instance.GetServerPeerID() && !ZNet.instance.IsDedicated();
            if (!host && (peer == null || !peer.IsReady())) return;
            string account = host ? UserInfo.GetLocalUser().UserId.ToString() : peer.m_socket.GetHostName();
            if (string.IsNullOrEmpty(account) || !LoadStore()) return;
            try
            {
                if (pkg.ReadInt() != Protocol) return;
                var op = (Request)pkg.ReadInt();
                float now = Time.realtimeSinceStartup;
                if (op == Request.Hello)
                {
                    if (!Subscribers.TryGetValue(sender, out var sub)) Subscribers[sender] = sub = new Subscriber();
                    long known = pkg.ReadLong();
                    if (now < sub.NextHello) return;
                    sub.NextHello = now + 1;
                    if (known != store.Revision && sub.Outgoing.Count == 0) Snapshot(sub);
                    return;
                }
                if (!Subscribers.TryGetValue(sender, out var subscriber)) return;
                string id = op == Request.Add ? null : pkg.ReadString();
                SharedPin added = op == Request.Add ? ReadPin(pkg) : null;
                if (added != null) id = added.Id;
                if (!SharedPinStore.ValidId(id)) return;
                if (now < subscriber.NextAction) { Reject(subscriber, id, "Please wait before changing another shared pin."); return; }
                subscriber.NextAction = now + 0.1f;
                var next = store.Copy();
                bool accepted = op == Request.Add ? next.Add(added, account) :
                    op == Request.Delete ? next.Remove(id, account, host || ZNet.instance.IsAdmin(account)) :
                    op == Request.Check && next.Check(id, pkg.ReadBool());
                if (!accepted) { Reject(subscriber, id, "Shared pin change rejected (permission, limits or invalid data)."); return; }
                if (next.Revision == store.Revision)
                {
                    if (subscriber.Outgoing.Count == 0) Snapshot(subscriber);
                    return;
                }
                // Commit to disk before publishing/acknowledging any mutation.
                try { next.Save(savePath, world); }
                catch (Exception e)
                {
                    ValheimPlusPlugin.Logger.LogError($"Shared pin save failed; mutation not committed: {e}");
                    Reject(subscriber, id, "Could not save shared pins; change was not applied."); return;
                }
                store = next;
                var response = Package((int)(op == Request.Delete ? Response.Delete : Response.Upsert));
                response.Write(store.Revision);
                if (op == Request.Delete) response.Write(id); else WritePin(response, store.Pins[id]);
                foreach (var sub in Subscribers.Values)
                {
                    // Bound per-peer memory. A fresh snapshot supersedes any unsent backlog.
                    if (sub.Outgoing.Count >= MaxQueued) { sub.Outgoing.Clear(); Snapshot(sub); }
                    else sub.Outgoing.Enqueue(new ZPackage(response.GetArray()));
                }
                if (op == Request.Delete) ValheimPlusPlugin.Logger.LogInfo($"Shared pin {id} deleted by {account}.");
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogDebug($"Ignored invalid shared-pin request from {sender}: {e.Message}");
            }
        }

        private static void Reject(Subscriber sub, string id, string message)
        {
            if (sub.Outgoing.Count >= MaxQueued) return;
            var pkg = Package((int)Response.Rejected); pkg.Write(id); pkg.Write(message); sub.Outgoing.Enqueue(pkg);
        }
        private static void Snapshot(Subscriber sub)
        {
            var pins = store.Pins.Values.ToList();
            var begin = Package((int)Response.Begin); begin.Write(store.Revision); begin.Write(pins.Count); sub.Outgoing.Enqueue(begin);
            for (int offset = 0; offset < pins.Count; offset += BatchSize)
            {
                var part = Package((int)Response.Batch); part.Write(store.Revision); part.Write(offset);
                int count = Math.Min(BatchSize, pins.Count - offset); part.Write(count);
                for (int i = 0; i < count; i++) WritePin(part, pins[offset + i]);
                sub.Outgoing.Enqueue(part);
            }
            var end = Package((int)Response.End); end.Write(store.Revision); sub.Outgoing.Enqueue(end);
        }

        private static void ReceiveResponse(long sender, ZPackage pkg)
        {
            if (!Enabled || sender != ZRoutedRpc.instance.GetServerPeerID() || Player.m_localPlayer == null ||
                Minimap.instance == null || pkg == null || pkg.Size() > 32768) return;
            try
            {
                if (pkg.ReadInt() != Protocol) return;
                var op = (Response)pkg.ReadInt();
                if (op == Response.Rejected)
                {
                    Pending.Remove(pkg.ReadString()); Notice(pkg.ReadString()); return;
                }
                long incomingRevision = pkg.ReadLong();
                if (incomingRevision < 0) throw new InvalidDataException("Invalid revision");
                if (op == Response.Begin)
                {
                    stagingCount = pkg.ReadInt();
                    if (stagingCount < 0 || stagingCount > SharedPinStore.MaxPins) throw new InvalidDataException("Invalid count");
                    stagingRevision = incomingRevision; staging = new Dictionary<string, SharedPin>(); return;
                }
                if (op == Response.Batch)
                {
                    int offset = pkg.ReadInt(), count = pkg.ReadInt();
                    if (staging == null || stagingRevision != incomingRevision || offset != staging.Count || count < 1 ||
                        count > BatchSize || offset + count > stagingCount) throw new InvalidDataException("Invalid batch");
                    for (int i = 0; i < count; i++)
                    {
                        SharedPin p = ReadPin(pkg); Validate(p); staging.Add(p.Id, p);
                    }
                    return;
                }
                if (op == Response.End)
                {
                    if (staging == null || incomingRevision != stagingRevision || staging.Count != stagingCount)
                        throw new InvalidDataException("Incomplete snapshot");
                    foreach (string id in Displayed.Keys.Where(id => !staging.ContainsKey(id)).ToList()) RemoveDisplay(id);
                    Records.Clear();
                    foreach (SharedPin p in staging.Values) Upsert(p);
                    revision = incomingRevision; staging = null; return;
                }
                if (incomingRevision <= revision) return;
                if (staging != null || incomingRevision != revision + 1) throw new InvalidDataException("Revision gap");
                if (op == Response.Upsert) { SharedPin p = ReadPin(pkg); Validate(p); Upsert(p); }
                else if (op == Response.Delete) { string id = pkg.ReadString(); RemoveDisplay(id); Records.Remove(id); }
                else throw new InvalidDataException("Unknown operation");
                revision = incomingRevision;
            }
            catch (Exception e)
            {
                staging = null; revision = -1; nextHello = 0;
                ValheimPlusPlugin.Logger.LogWarning($"Shared pin sync will retry: {e.Message}");
            }
        }

        private static void Validate(SharedPin pin)
        {
            if (!SharedPinStore.Valid(pin) || string.IsNullOrEmpty(pin.Owner) || pin.Owner.Length > 256)
                throw new InvalidDataException("Invalid pin");
        }
        private static void Upsert(SharedPin pin)
        {
            Records[pin.Id] = pin;
            if (!Displayed.TryGetValue(pin.Id, out var display))
            {
                if (!Pending.TryGetValue(pin.Id, out display) || !Minimap.instance.m_pins.Contains(display))
                    display = Minimap.instance.AddPin(new Vector3(pin.X, pin.Y, pin.Z), (Minimap.PinType)pin.Type,
                        pin.Name, true, pin.Checked);
                Pending.Remove(pin.Id);
                Displayed[pin.Id] = display;
            }
            display.m_name = pin.Name; display.m_checked = pin.Checked; display.m_ownerID = 0;
            Minimap.instance.m_pinUpdateRequired = true;
        }
        private static void RemoveDisplay(string id)
        {
            if (!Displayed.TryGetValue(id, out var pin)) return;
            Applying = true;
            try { if (Minimap.instance != null) Minimap.instance.RemovePin(pin); }
            finally { Applying = false; Displayed.Remove(id); }
        }
        private static void ClearDisplay()
        {
            foreach (string id in Displayed.Keys.ToList()) RemoveDisplay(id);
        }
        internal static bool IsShared(Minimap.PinData pin) => Displayed.ContainsValue(pin);
        internal static void Publish(Minimap.PinData pin)
        {
            if (pin == null || IsShared(pin) || Pending.ContainsValue(pin) || !SharedPinStore.AllowedType((int)pin.m_type)) return;
            if (!Ready) { Notice("Shared pins are not ready on this server; this pin stays private."); return; }
            var data = new SharedPin { Id = Guid.NewGuid().ToString("N"), Name = pin.m_name ?? "", Type = (int)pin.m_type,
                X = pin.m_pos.x, Y = pin.m_pos.y, Z = pin.m_pos.z, Checked = pin.m_checked };
            if (!SharedPinStore.Valid(data)) { Notice("This pin cannot be shared (name or position is invalid)."); return; }
            Pending[data.Id] = pin;
            var pkg = Package((int)Request.Add); WritePin(pkg, data); Send(pkg);
        }
        internal static bool Remove(Minimap.PinData pin)
        {
            if (Applying || !IsShared(pin)) return true;
            if (!Ready) { Notice("Shared pins are reconnecting; deletion was not sent."); return false; }
            string id = Displayed.First(pair => pair.Value == pin).Key;
            var pkg = Package((int)Request.Delete); pkg.Write(id); Send(pkg);
            return false; // Only the server's committed delete removes the displayed pin.
        }
        private static void Notice(string text)
        {
            ValheimPlusPlugin.Logger.LogInfo(text);
            if (Player.m_localPlayer != null) Player.m_localPlayer.Message(MessageHud.MessageType.Center, text);
        }
    }
}

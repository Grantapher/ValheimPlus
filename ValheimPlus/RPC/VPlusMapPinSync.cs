using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.RPC
{
    /// <summary>
    /// Server-owned map pin sharing.
    ///
    /// The server holds the single pin list for the world and persists it, so pins survive
    /// restarts and reach players who were never online at the same time. Clients send their
    /// own adds and removes to the server and apply whatever the server broadcasts back.
    ///
    /// A pin that arrives from another player is added with a non-zero <c>m_ownerID</c>, which is
    /// how vanilla marks "someone else's pin": it draws faded, and a left click on it clears the
    /// owner and adopts it. None of that needs mod code.
    /// </summary>
    public static class VPlusMapPinSync
    {
        public const string AddRpc = "VPlusMapPinAdd";
        public const string RemoveRpc = "VPlusMapPinRemove";
        public const string SnapshotRpc = "VPlusMapPinSnapshot";
        public const string RequestRpc = "VPlusMapPinRequest";

        /// <summary>Pin types a player places deliberately. Death, spawn and pings are excluded.</summary>
        private static readonly HashSet<Minimap.PinType> ShareableTypes = new HashSet<Minimap.PinType>
        {
            Minimap.PinType.Icon0,
            Minimap.PinType.Icon1,
            Minimap.PinType.Icon2,
            Minimap.PinType.Icon3,
            Minimap.PinType.Icon4,
            Minimap.PinType.Boss,
            Minimap.PinType.Hildir1,
            Minimap.PinType.Hildir2,
            Minimap.PinType.Hildir3,
            Minimap.PinType.Memorial,
        };

        private static readonly Dictionary<string, SharedPin> serverPins = new Dictionary<string, SharedPin>();

        /// <summary>
        /// Ids deleted on the server, so a player who was offline drops them instead of re-sharing them.
        /// </summary>
        private static readonly HashSet<string> deletedPinIds = new HashSet<string>();
        private static bool serverPinsLoaded;
        private static bool serverPinsDirty;

        /// <summary>
        /// Guards the server store. The save timer runs on a worker thread; RPCs and shutdown run on the game thread.
        /// </summary>
        private static readonly object storeLock = new object();

        /// <summary>Serializes file writes between the save timer and shutdown.</summary>
        private static readonly object saveLock = new object();

        /// <summary>Set while network pins are being applied, so the Minimap hooks do not echo them back.</summary>
        private static bool applying;

        /// <summary>Set while vanilla merges a cartography table, which drops faded pins the table lacks.</summary>
        private static bool readingMapTable;

        /// <summary>Set while vanilla creates a map-click pin for the name dialog.</summary>
        public static bool CreatingNamedPin;

        /// <summary>The pin the name dialog is naming, shared by <see cref="FlushNamedPin"/>.</summary>
        private static Minimap.PinData namedPin;

        /// <summary>Cleared after the first spawn asks for the list, re-armed when leaving a server.</summary>
        public static bool ShouldSyncOnSpawn = true;

        private static bool Enabled =>
            Configuration.Current.Map.IsEnabled && Configuration.Current.Map.shareAllPins;

        private static bool IsServer => ZNet.instance != null && ZNet.instance.IsServer();

        private static bool IsShareable(Minimap.PinData pin) =>
            pin != null && pin.m_save && ShareableTypes.Contains(pin.m_type);

        /// <summary>
        /// Identifies a pin across clients. Vanilla pins carry no id, and a client must be able to
        /// name the pin it is deleting, so the id is derived from what every copy agrees on.
        /// Position is rounded to a tenth of a metre to absorb float drift over the wire.
        /// </summary>
        private static string PinId(Minimap.PinType type, Vector3 pos, string name) =>
            string.Format(CultureInfo.InvariantCulture, "{0}|{1:F1}|{2:F1}|{3}",
                (int)type, pos.x, pos.z, name ?? string.Empty);

        private static string PinId(Minimap.PinData pin) => PinId(pin.m_type, pin.m_pos, pin.m_name);

        private sealed class SharedPin
        {
            public string Id;
            public string Name;
            public Vector3 Pos;
            public Minimap.PinType Type;

            /// <summary>Player who first shared it, so every other client can draw it faded.</summary>
            public long OwnerId;
        }

        /// <summary>Asks the server for the full pin list and offers the pins this player already has.</summary>
        public static void RequestSnapshot()
        {
            if (!Enabled || ZRoutedRpc.instance == null) return;
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RequestRpc, new ZPackage());
        }

        /// <summary>Sends a pin this player just placed to the server.</summary>
        public static void SendAdd(Minimap.PinData pin)
        {
            if (!Enabled || applying || !IsShareable(pin) || ZRoutedRpc.instance == null) return;
            if (pin.m_ownerID != 0L) return; // not ours yet; adopting it is not a new pin

            // The pin id includes the name, which the name dialog has not set yet. FlushNamedPin sends it.
            if (CreatingNamedPin)
            {
                namedPin = pin;
                return;
            }

            // Vanilla re-adds the profile's pins before the player spawns. They have no owner yet;
            // the join snapshot offers the ones the server lacks once the player exists.
            var ownerId = LocalPlayerId();
            if (ownerId == 0L) return;

            var package = new ZPackage();
            WritePin(package, PinId(pin), pin.m_name, pin.m_pos, pin.m_type, ownerId);
            Send(AddRpc, package);
        }

        /// <summary>
        /// Vanilla treats every faded pin as table data and removes those the table lacks. Shared pins
        /// are faded too, so those removals must stay local.
        /// </summary>
        public static void BeginMapTableRead() => readingMapTable = true;

        /// <summary>Asks for the list again, so shared pins the table read dropped come back.</summary>
        public static void EndMapTableRead()
        {
            readingMapTable = false;
            RequestSnapshot();
        }

        /// <summary>Shares the pin held for the name dialog once the dialog no longer holds it.</summary>
        public static void FlushNamedPin(Minimap minimap, Minimap.PinData dialogPin)
        {
            if (namedPin == null || namedPin == dialogPin) return;

            var pin = namedPin;
            namedPin = null;
            if (minimap.m_pins.Contains(pin)) SendAdd(pin);
        }

        /// <summary>Sends a pin this player just deleted to the server. Deletes apply to everyone.</summary>
        public static void SendRemove(Minimap.PinData pin)
        {
            if (!Enabled || applying || readingMapTable || !IsShareable(pin) || ZRoutedRpc.instance == null) return;

            var package = new ZPackage();
            package.Write(PinId(pin));
            Send(RemoveRpc, package);
        }

        private static void Send(string rpc, ZPackage package)
        {
            if (IsServer)
            {
                // Hosting player: no round trip, handle it as the server would.
                package.SetPos(0);
                if (rpc == AddRpc) HandleAdd(package, broadcast: true);
                else HandleRemove(package, broadcast: true);
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), rpc, package);
        }

        private static long LocalPlayerId() =>
            Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;

        private static void ApplyAdd(SharedPin pin)
        {
            if (Minimap.instance == null) return;

            var existing = Find(pin.Id);
            if (existing != null) return;

            // A pin the local player shared comes back owned by them, so it stays fully coloured.
            var ownerId = pin.OwnerId == LocalPlayerId() ? 0L : pin.OwnerId;

            applying = true;
            try
            {
                Minimap.instance.AddPin(pin.Pos, pin.Type, pin.Name, save: true, isChecked: false, ownerID: ownerId);
            }
            finally
            {
                applying = false;
            }
        }

        private static void ApplyRemove(string id)
        {
            if (Minimap.instance == null) return;

            var existing = Find(id);
            if (existing == null) return;

            applying = true;
            try
            {
                Minimap.instance.RemovePin(existing);
            }
            finally
            {
                applying = false;
            }
        }

        private static Minimap.PinData Find(string id) =>
            Minimap.instance.m_pins.FirstOrDefault(pin => IsShareable(pin) && PinId(pin) == id);

        /// <summary>Every shareable pin this player already has, to seed a world that predates the setting.</summary>
        private static List<Minimap.PinData> LocalShareablePins() =>
            Minimap.instance == null
                ? new List<Minimap.PinData>()
                : Minimap.instance.m_pins.Where(IsShareable).ToList();

        /// <summary>Separates the store fields. Escaped out of names so it cannot appear inside one.</summary>
        private const string FieldSeparator = "\u001f";

        private static string StorePath =>
            ValheimPlusPlugin.VPlusDataDirectoryPath + Path.DirectorySeparatorChar +
            ZNet.instance.GetWorldName() + "_mapPins.dat";

        /// <summary>One escaped pin id per line. Separate file so the pin store format is unchanged.</summary>
        private static string DeletedStorePath =>
            ValheimPlusPlugin.VPlusDataDirectoryPath + Path.DirectorySeparatorChar +
            ZNet.instance.GetWorldName() + "_mapPinsDeleted.dat";

        /// <summary>
        /// Loads the persisted pin list on first use. Deferred rather than done at start-up because
        /// a dedicated server has no Minimap to hang the load off, and the world name is only known
        /// once ZNet is up.
        /// </summary>
        private static void EnsureLoaded()
        {
            lock (storeLock)
            {
                if (serverPinsLoaded) return;
                serverPinsLoaded = true;

                try
                {
                    if (File.Exists(StorePath))
                    {
                        foreach (var line in File.ReadAllLines(StorePath))
                        {
                            var pin = Deserialize(line);
                            if (pin != null) serverPins[pin.Id] = pin;
                        }
                    }

                    if (File.Exists(DeletedStorePath))
                    {
                        foreach (var line in File.ReadAllLines(DeletedStorePath))
                            if (line.Length > 0) deletedPinIds.Add(Unescape(line));
                    }

                    ValheimPlusPlugin.Logger.LogDebug($"Loaded {serverPins.Count} shared map pins and " +
                                                      $"{deletedPinIds.Count} deleted pin ids from disk.");
                }
                catch (Exception e)
                {
                    ValheimPlusPlugin.Logger.LogError(
                        $"Failed to read the shared map pin store. Pins shared before this restart will not " +
                        $"appear, and saving may overwrite them. Exception is:\n{e}");
                }
            }
        }

        /// <summary>
        /// Writes the pin list to disk. Called on the map sync timer's worker thread and on shutdown, so the
        /// lines are copied and the dirty flag cleared under the store lock; a later change marks it dirty again.
        /// </summary>
        public static void SavePinsToDisk()
        {
            if (!Enabled || !IsServer) return;

            lock (saveLock)
            {
                string[] pinLines;
                string[] deletedLines;
                lock (storeLock)
                {
                    if (!serverPinsLoaded || !serverPinsDirty) return;
                    pinLines = serverPins.Values.Select(Serialize).ToArray();
                    deletedLines = deletedPinIds.Select(Escape).ToArray();
                    serverPinsDirty = false;
                }

                try
                {
                    File.WriteAllLines(StorePath, pinLines);
                    File.WriteAllLines(DeletedStorePath, deletedLines);
                    ValheimPlusPlugin.Logger.LogDebug(
                        $"Saved {pinLines.Length} shared map pins and {deletedLines.Length} deleted pin ids to disk.");
                }
                catch (Exception e)
                {
                    lock (storeLock) serverPinsDirty = true;
                    ValheimPlusPlugin.Logger.LogError(
                        $"Failed to write the shared map pin store. Pins shared this session will be lost when " +
                        $"the server stops. Exception is:\n{e}");
                }
            }
        }

        /// <summary>
        /// Pin names arrive over the wire, so they can hold anything. The store is one pin per line
        /// with U+001F between fields; an unescaped name carrying either would split or shift the
        /// record and lose the pin. Escaping keeps the file line-oriented and recoverable per pin.
        /// </summary>
        private static string Escape(string value) => (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace(FieldSeparator, "\\u")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");

        private static string Unescape(string value)
        {
            var result = new StringBuilder(value.Length);

            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != '\\' || i + 1 >= value.Length)
                {
                    result.Append(value[i]);
                    continue;
                }

                i++;
                switch (value[i])
                {
                    case '\\': result.Append('\\'); break;
                    case 'u': result.Append(FieldSeparator); break;
                    case 'r': result.Append('\r'); break;
                    case 'n': result.Append('\n'); break;
                    default: result.Append('\\').Append(value[i]); break;
                }
            }

            return result.ToString();
        }

        private static string Serialize(SharedPin pin) =>
            string.Join(FieldSeparator, new[]
            {
                Escape(pin.Id),
                ((int)pin.Type).ToString(CultureInfo.InvariantCulture),
                pin.Pos.x.ToString("R", CultureInfo.InvariantCulture),
                pin.Pos.y.ToString("R", CultureInfo.InvariantCulture),
                pin.Pos.z.ToString("R", CultureInfo.InvariantCulture),
                pin.OwnerId.ToString(CultureInfo.InvariantCulture),
                Escape(pin.Name),
            });

        private static SharedPin Deserialize(string line)
        {
            var parts = line.Split(FieldSeparator[0]);
            if (parts.Length != 7) return null;

            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var type) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
                !float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ||
                !long.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var owner))
                return null;

            return new SharedPin
            {
                Id = Unescape(parts[0]),
                Type = (Minimap.PinType)type,
                Pos = new Vector3(x, y, z),
                OwnerId = owner,
                Name = Unescape(parts[6]),
            };
        }

        public static void RPC_MapPinAdd(long sender, ZPackage package)
        {
            if (!Enabled) return;

            if (IsServer) HandleAdd(package, broadcast: true);
            else if (sender == ZRoutedRpc.instance.GetServerPeerID()) ApplyAdd(ReadPin(package));
        }

        public static void RPC_MapPinRemove(long sender, ZPackage package)
        {
            if (!Enabled) return;

            if (IsServer) HandleRemove(package, broadcast: true);
            else if (sender == ZRoutedRpc.instance.GetServerPeerID())
            {
                var id = ReadPinId(package);
                if (id != null) ApplyRemove(id);
            }
        }

        /// <summary>Server: answers a joining client with the whole pin list.</summary>
        public static void RPC_MapPinRequest(long sender, ZPackage package)
        {
            if (!Enabled || !IsServer) return;

            EnsureLoaded();

            var snapshot = new ZPackage();
            snapshot.Write(serverPins.Count);
            foreach (var pin in serverPins.Values)
                WritePin(snapshot, pin.Id, pin.Name, pin.Pos, pin.Type, pin.OwnerId);

            snapshot.Write(deletedPinIds.Count);
            foreach (var id in deletedPinIds)
                snapshot.Write(id);

            ZRoutedRpc.instance.InvokeRoutedRPC(sender, SnapshotRpc, snapshot);
        }

        /// <summary>
        /// Client: applies the full pin list, drops local copies of pins deleted while it was away,
        /// and offers back any other pin the server does not have. The hosting player runs it too,
        /// since its profile pins are only offered from here.
        /// </summary>
        public static void RPC_MapPinSnapshot(long sender, ZPackage package)
        {
            if (!Enabled) return;
            if (sender != ZRoutedRpc.instance.GetServerPeerID()) return;

            var known = new HashSet<string>();
            int count;
            try
            {
                count = package.ReadInt();
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogWarning($"Discarded an empty shared map pin snapshot. {e.Message}");
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var pin = ReadPin(package);
                if (pin == null) continue;
                known.Add(pin.Id);
                ApplyAdd(pin);
            }

            var deleted = new HashSet<string>();
            try
            {
                var deletedCount = package.ReadInt();
                for (var i = 0; i < deletedCount; i++)
                    deleted.Add(package.ReadString());
            }
            catch (Exception e)
            {
                // Without the list a deleted pin may be shared again, which is the old behaviour.
                ValheimPlusPlugin.Logger.LogWarning(
                    $"Shared map pin snapshot has no readable delete list. {e.Message}");
            }

            foreach (var pin in LocalShareablePins())
            {
                var id = PinId(pin);
                if (known.Contains(id)) continue;
                if (deleted.Contains(id)) ApplyRemove(id);
                else SendAdd(pin);
            }
        }

        private static void HandleAdd(ZPackage package, bool broadcast)
        {
            EnsureLoaded();

            var pin = ReadPin(package);
            if (pin == null) return;

            // Senders always stamp their own player id; owner 0 would show the pin as everyone's own.
            if (pin.OwnerId == 0L) return;

            lock (storeLock)
            {
                if (serverPins.ContainsKey(pin.Id)) return;

                // Snapshot holders drop deleted pins instead of offering them, so an add here is a deliberate re-add.
                deletedPinIds.Remove(pin.Id);
                serverPins[pin.Id] = pin;
                serverPinsDirty = true;
            }

            if (!broadcast) return;

            var outgoing = new ZPackage();
            WritePin(outgoing, pin.Id, pin.Name, pin.Pos, pin.Type, pin.OwnerId);
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, AddRpc, outgoing);
            ApplyAdd(pin); // hosting player sees it too
        }

        private static void HandleRemove(ZPackage package, bool broadcast)
        {
            EnsureLoaded();

            var id = ReadPinId(package);
            if (id == null) return;

            lock (storeLock)
            {
                if (!serverPins.Remove(id)) return;

                deletedPinIds.Add(id);
                serverPinsDirty = true;
            }

            if (!broadcast) return;

            var outgoing = new ZPackage();
            outgoing.Write(id);
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RemoveRpc, outgoing);
            ApplyRemove(id);
        }

        private static void WritePin(
            ZPackage package, string id, string name, Vector3 pos, Minimap.PinType type, long ownerId)
        {
            package.Write(id);
            package.Write(name ?? string.Empty);
            package.Write(pos);
            package.Write((int)type);
            package.Write(ownerId);
        }

        private static SharedPin ReadPin(ZPackage package)
        {
            SharedPin pin;
            try
            {
                pin = new SharedPin
                {
                    Id = package.ReadString(),
                    Name = package.ReadString(),
                    Pos = package.ReadVector3(),
                    Type = (Minimap.PinType)package.ReadInt(),
                    OwnerId = package.ReadLong(),
                };
            }
            catch (Exception e)
            {
                // Anything on the wire can be short or corrupt; a bad packet must not kill the RPC.
                ValheimPlusPlugin.Logger.LogWarning(
                    $"Discarded a malformed shared map pin; that one pin will not appear. {e.Message}");
                return null;
            }

            if (string.IsNullOrEmpty(pin.Id)) return null;
            if (!ShareableTypes.Contains(pin.Type)) return null;
            if (!IsFinite(pin.Pos.x) || !IsFinite(pin.Pos.y) || !IsFinite(pin.Pos.z)) return null;

            // Every lookup recomputes the id from these fields, so a pin carrying any other id could never be removed.
            if (pin.Id != PinId(pin.Type, pin.Pos, pin.Name)) return null;
            return pin;
        }

        /// <summary>Reads the pin id of a remove packet, or null when the packet is short or corrupt.</summary>
        private static string ReadPinId(ZPackage package)
        {
            try
            {
                var id = package.ReadString();
                return string.IsNullOrEmpty(id) ? null : id;
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogWarning($"Discarded a malformed shared map pin removal. {e.Message}");
                return null;
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        /// <summary>Drops per-session state when leaving a world.</summary>
        public static void Reset()
        {
            lock (storeLock)
            {
                serverPins.Clear();
                deletedPinIds.Clear();
                serverPinsLoaded = false;
                serverPinsDirty = false;
            }
            applying = false;
            CreatingNamedPin = false;
            namedPin = null;
            readingMapTable = false;
            ShouldSyncOnSpawn = true;
        }
    }
}

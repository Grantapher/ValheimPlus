using System.Text;
using Splatform;

// Only game boundaries are replaced. Tests compile and execute the production RPC file.
namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
}

namespace Splatform
{
    public struct PlatformUserID
    {
        private string value;
        public PlatformUserID(string value) { this.value = value; }
        public bool IsValid => !string.IsNullOrEmpty(value);
        public override string ToString() => value ?? string.Empty;
    }
}

public sealed class Minimap
{
    public static Minimap instance;
    public List<PinData> m_pins = new();

    public enum PinType
    {
        Icon0, Icon1, Icon2, Icon3, Death, Bed, Icon4, Shout, None,
        Boss, Player, RandomEvent, Ping, EventArea, Hildir1, Hildir2, Hildir3, Memorial
    }

    public sealed class PinData
    {
        public UnityEngine.Vector3 m_pos;
        public PinType m_type;
        public string m_name;
        public bool m_save, m_checked;
        public long m_ownerID;
        public PlatformUserID m_author;
    }

    public PinData AddPin(UnityEngine.Vector3 pos, PinType type, string name, bool save, bool isChecked,
        long ownerID = 0L, PlatformUserID author = default)
    {
        var pin = new PinData
        {
            m_pos = pos, m_type = type, m_name = name,
            m_save = save, m_checked = isChecked, m_ownerID = ownerID, m_author = author,
        };
        m_pins.Add(pin);
        // Mirrors the Harmony AddPin postfix, so feedback loops are observable in tests.
        ValheimPlus.RPC.VPlusMapPinSync.SendAdd(pin);
        return pin;
    }

    public void RemovePin(PinData pin)
    {
        // Mirrors the Harmony RemovePin prefix: the send happens before the pin is gone.
        ValheimPlus.RPC.VPlusMapPinSync.SendRemove(pin);
        m_pins.Remove(pin);
    }
}

public sealed class Player
{
    public static Player m_localPlayer;
    public long PlayerId;
    public Player(long playerId) { PlayerId = playerId; }
    public long GetPlayerID() => PlayerId;
}

public sealed class ZNet
{
    public static ZNet instance;
    public bool Server;
    public string WorldName = "TestWorld";
    public ZNet(bool server) { Server = server; }
    public bool IsServer() => Server;
    public string GetWorldName() => WorldName;
}

public sealed class ZRoutedRpc
{
    public static ZRoutedRpc instance;
    public const long Everybody = 0L;

    public long m_id;
    public long ServerID = 1;

    /// <summary>Set by the harness so an invoke is delivered to the other node instead of vanishing.</summary>
    public Action<long, string, ZPackage> Router = (_, _, _) => { };

    public List<RpcCall> Calls = new();

    public long GetServerPeerID() => ServerID;

    public void InvokeRoutedRPC(long target, string method, ZPackage package)
    {
        Calls.Add(new RpcCall(target, method, new ZPackage(package.GetArray())));
        Router(target, method, new ZPackage(package.GetArray()));
    }
}

public sealed record RpcCall(long Target, string Method, ZPackage Package);

public sealed class ZPackage
{
    private readonly MemoryStream stream;
    private readonly BinaryReader reader;
    private readonly BinaryWriter writer;

    public ZPackage() : this(Array.Empty<byte>()) { }

    public ZPackage(byte[] bytes)
    {
        stream = new MemoryStream();
        stream.Write(bytes);
        stream.Position = 0;
        reader = new BinaryReader(stream, Encoding.UTF8, true);
        writer = new BinaryWriter(stream, Encoding.UTF8, true);
    }

    public byte[] GetArray() => stream.ToArray();
    public void SetPos(int pos) => stream.Position = pos;
    public void Write(int value) => writer.Write(value);
    public void Write(long value) => writer.Write(value);
    public void Write(float value) => writer.Write(value);
    public void Write(string value) => writer.Write(value);
    public void Write(UnityEngine.Vector3 value) { Write(value.x); Write(value.y); Write(value.z); }
    public int ReadInt() => reader.ReadInt32();
    public long ReadLong() => reader.ReadInt64();
    public string ReadString() => reader.ReadString();
    public UnityEngine.Vector3 ReadVector3() => new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
}

namespace ValheimPlus.Configurations
{
    public static class Configuration
    {
        public static Config Current = new();
        public sealed class Config { public MapConfig Map = new(); }
        public sealed class MapConfig { public bool IsEnabled = true; public bool shareAllPins = true; }
    }
}

namespace ValheimPlus
{
    public static class ValheimPlusPlugin
    {
        public static string VPlusDataDirectoryPath = Path.Combine(Path.GetTempPath(), "vplus-pin-tests");
        public static TestLogger Logger = new();

        public sealed class TestLogger
        {
            public void LogDebug(object message) { }
            public void LogWarning(object message) { }
            public void LogError(object message) { }
        }
    }
}

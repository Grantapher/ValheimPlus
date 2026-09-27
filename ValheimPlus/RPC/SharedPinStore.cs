using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ValheimPlus.RPC
{
    // No Unity dependency: validation, permissions and disk format are exercised in standalone tests.
    internal sealed class SharedPin
    {
        internal string Id, Owner, Name;
        internal int Type;
        internal float X, Y, Z;
        internal bool Checked;
        internal SharedPin Copy() => (SharedPin)MemberwiseClone();
    }

    internal sealed class SharedPinStore
    {
        internal const int MaxPins = 4096, MaxName = 128, MaxDeleted = 100000;
        internal readonly Dictionary<string, SharedPin> Pins = new(StringComparer.Ordinal);
        private readonly HashSet<string> deleted = new(StringComparer.Ordinal);
        internal long Revision { get; private set; }

        internal static bool ValidId(string id) => id != null && id.Length == 32 && Guid.TryParseExact(id, "N", out _);
        internal static bool AllowedType(int type) => type == 0 || type == 1 || type == 2 || type == 3 || type == 6 || type == 9;
        private static bool Coordinate(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && Math.Abs(n) <= 20000;
        internal static bool Valid(SharedPin pin) => pin != null && ValidId(pin.Id) &&
            AllowedType(pin.Type) && pin.Name != null && pin.Name.Length <= MaxName &&
            !pin.Name.Any(char.IsControl) && pin.Name.IndexOfAny(new[] { '<', '>', '$' }) < 0 &&
            Coordinate(pin.X) && Coordinate(pin.Y) && Coordinate(pin.Z);

        // The caller supplies an authenticated account, never an owner read from the request body.
        internal bool Add(SharedPin pin, string account)
        {
            if (!Valid(pin) || string.IsNullOrEmpty(account) || account.Length > 256 || deleted.Contains(pin.Id)) return false;
            if (Pins.TryGetValue(pin.Id, out var existing))
                return existing.Owner == account && existing.Name == pin.Name && existing.Type == pin.Type &&
                       existing.X == pin.X && existing.Y == pin.Y && existing.Z == pin.Z;
            if (Pins.Count >= MaxPins) return false;
            var saved = pin.Copy();
            saved.Owner = account;
            Pins.Add(saved.Id, saved);
            Revision++;
            return true;
        }

        internal bool Remove(string id, string account, bool admin)
        {
            if (!Pins.TryGetValue(id, out var pin) || (!admin && pin.Owner != account) || deleted.Count >= MaxDeleted)
                return false;
            Pins.Remove(id);
            deleted.Add(id); // Replayed creation packets cannot resurrect deleted pins.
            Revision++;
            return true;
        }

        internal bool Check(string id, bool value)
        {
            if (!Pins.TryGetValue(id, out var pin)) return false;
            if (pin.Checked != value) { pin.Checked = value; Revision++; }
            return true;
        }

        internal SharedPinStore Copy()
        {
            var copy = new SharedPinStore { Revision = Revision };
            foreach (var item in Pins) copy.Pins.Add(item.Key, item.Value.Copy());
            foreach (string id in deleted) copy.deleted.Add(id);
            return copy;
        }

        internal void Save(string path, long world)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(0x56505031); writer.Write(world); writer.Write(Revision);
                writer.Write(Pins.Count);
                foreach (SharedPin pin in Pins.Values.OrderBy(p => p.Id, StringComparer.Ordinal))
                {
                    writer.Write(pin.Id); writer.Write(pin.Owner); writer.Write(pin.Name); writer.Write(pin.Type);
                    writer.Write(pin.X); writer.Write(pin.Y); writer.Write(pin.Z); writer.Write(pin.Checked);
                }
                writer.Write(deleted.Count);
                foreach (string id in deleted.OrderBy(id => id, StringComparer.Ordinal)) writer.Write(id);
                writer.Flush(); stream.Flush(true);
            }
            // Never delete the previous save before the replacement has completed.
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
        }

        internal static SharedPinStore Load(string path, long world)
        {
            var store = new SharedPinStore();
            if (!File.Exists(path)) return store;
            if (new FileInfo(path).Length > 16 * 1024 * 1024) throw new InvalidDataException("Pin save is too large.");
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                if (reader.ReadInt32() != 0x56505031 || reader.ReadInt64() != world)
                    throw new InvalidDataException("Wrong pin format or world.");
                store.Revision = reader.ReadInt64();
                if (store.Revision < 0) throw new InvalidDataException("Invalid revision.");
                int count = ReadCount(reader, MaxPins);
                for (int i = 0; i < count; i++)
                {
                    var pin = new SharedPin { Id = reader.ReadString(), Owner = reader.ReadString(),
                        Name = reader.ReadString(), Type = reader.ReadInt32(), X = reader.ReadSingle(),
                        Y = reader.ReadSingle(), Z = reader.ReadSingle(), Checked = reader.ReadBoolean() };
                    if (!Valid(pin) || string.IsNullOrEmpty(pin.Owner) || pin.Owner.Length > 256 || store.Pins.ContainsKey(pin.Id))
                        throw new InvalidDataException("Invalid pin record.");
                    store.Pins.Add(pin.Id, pin);
                }
                count = ReadCount(reader, MaxDeleted);
                for (int i = 0; i < count; i++)
                {
                    string id = reader.ReadString();
                    if (!ValidId(id) || store.Pins.ContainsKey(id) || !store.deleted.Add(id))
                        throw new InvalidDataException("Invalid deleted pin ID.");
                }
                if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Trailing pin data.");
            }
            return store;
        }

        private static int ReadCount(BinaryReader reader, int maximum)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > maximum) throw new InvalidDataException("Invalid pin count.");
            return count;
        }
    }
}

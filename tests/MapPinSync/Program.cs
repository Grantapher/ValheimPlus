using UnityEngine;
using ValheimPlus;
using ValheimPlus.Configurations;
using ValheimPlus.RPC;

// Exercises the production VPlusMapPinSync against stub game boundaries.
// Two nodes live in one process; Node.Enter swaps the statics the production code reads.

internal static class Program
{
    private const long ServerPeer = 1;
    private const long ClientPeer = 2;
    private const long ServerPlayer = 1001;
    private const long ClientPlayer = 1002;

    private static int failures;

    private sealed class Node
    {
        public Minimap Map = new();
        public ZNet Net;
        public ZRoutedRpc Rpc = new();
        public Player LocalPlayer;

        public Node(bool server, long peerId, long playerId)
        {
            Net = new ZNet(server);
            Rpc.m_id = peerId;
            LocalPlayer = new Player(playerId);
        }

        public void Enter()
        {
            Minimap.instance = Map;
            ZNet.instance = Net;
            ZRoutedRpc.instance = Rpc;
            Player.m_localPlayer = LocalPlayer;
        }

        public List<string> PinNames() => Map.m_pins.Select(p => p.m_name).OrderBy(n => n).ToList();
        public Minimap.PinData Pin(string name) => Map.m_pins.FirstOrDefault(p => p.m_name == name);
    }

    private static Node server, client;

    private static string StoreFile =>
        Path.Combine(ValheimPlusPlugin.VPlusDataDirectoryPath, "TestWorld_mapPins.dat");

    private static string DeletedStoreFile =>
        Path.Combine(ValheimPlusPlugin.VPlusDataDirectoryPath, "TestWorld_mapPinsDeleted.dat");

    /// <summary>
    /// Routes an invoke to the other node and runs the matching production handler there,
    /// then restores the caller's context. This is what the real ZRoutedRpc does across peers.
    /// </summary>
    private static void Wire()
    {
        server.Rpc.Router = (target, method, package) =>
        {
            if (target == ZRoutedRpc.Everybody || target == ClientPeer) Deliver(client, ServerPeer, method, package);
        };
        client.Rpc.Router = (target, method, package) =>
        {
            if (target == ZRoutedRpc.Everybody || target == ServerPeer) Deliver(server, ClientPeer, method, package);
        };
    }

    private static void Deliver(Node destination, long sender, string method, ZPackage package)
    {
        var callerMap = Minimap.instance;
        var callerNet = ZNet.instance;
        var callerRpc = ZRoutedRpc.instance;
        var callerPlayer = Player.m_localPlayer;

        destination.Enter();
        package.SetPos(0);
        try
        {
            if (method == VPlusMapPinSync.AddRpc) VPlusMapPinSync.RPC_MapPinAdd(sender, package);
            else if (method == VPlusMapPinSync.RemoveRpc) VPlusMapPinSync.RPC_MapPinRemove(sender, package);
            else if (method == VPlusMapPinSync.RequestRpc) VPlusMapPinSync.RPC_MapPinRequest(sender, package);
            else if (method == VPlusMapPinSync.SnapshotRpc) VPlusMapPinSync.RPC_MapPinSnapshot(sender, package);
        }
        finally
        {
            Minimap.instance = callerMap;
            ZNet.instance = callerNet;
            ZRoutedRpc.instance = callerRpc;
            Player.m_localPlayer = callerPlayer;
        }
    }

    /// <summary>
    /// Fresh world: in-memory state and, unless <paramref name="keepDisk"/>, the persisted store too.
    /// Tests that exercise persistence keep the file and say so.
    /// </summary>
    private static void Setup(bool keepDisk = false)
    {
        VPlusMapPinSync.Reset();
        if (!keepDisk && File.Exists(StoreFile)) File.Delete(StoreFile);
        if (!keepDisk && File.Exists(DeletedStoreFile)) File.Delete(DeletedStoreFile);
        Configuration.Current.Map.IsEnabled = true;
        Configuration.Current.Map.shareAllPins = true;

        server = new Node(server: true, ServerPeer, ServerPlayer);
        client = new Node(server: false, ClientPeer, ClientPlayer);
        server.Rpc.ServerID = ServerPeer;
        client.Rpc.ServerID = ServerPeer;
        Wire();
        client.Enter();
    }

    private static int Main()
    {
        var store = ValheimPlusPlugin.VPlusDataDirectoryPath;
        if (Directory.Exists(store)) Directory.Delete(store, recursive: true);
        Directory.CreateDirectory(store);

        NonShareableTypesStayLocal();
        SaveFalsePinsStayLocal();
        ClientAddReachesServerAndBroadcasts();
        ReceivedPinFromOtherPlayerIsFaded();
        OwnPinComesBackUnfaded();
        ApplyingDoesNotEcho();
        DuplicateAddIsIgnored();
        RemovePropagatesToEveryone();
        RemoveOfFadedPinPropagates();
        RemoveOfUnknownPinIsSilent();
        SnapshotDeliversWholeList();
        SnapshotPullsUpClientOnlyPins();
        PersistenceRoundTrip();
        MalformedPinIsRejected();
        DisabledConfigSendsNothing();
        HostAuthoredPinReachesClients();
        TwoClientsConverge();
        SnapshotDoesNotDuplicateALocalPin();
        RemoveBeforeAddIsHarmless();
        NewlineInPinNameSurvivesTheStore();
        SeparatorInPinNameSurvivesTheStore();
        PipeInPinNameKeepsPinsDistinct();
        CorruptStoreLineIsSkipped();
        EmptyStoreLoadsCleanly();
        ManyPinsSurviveASnapshot();
        OwnPinComesBackUnfadedAfterRelog();
        NonShareableTypeIsRejectedOnTheWire();
        ReceiverDoesNotEchoAnAppliedAdd();
        ReceiverDoesNotEchoAnAppliedRemove();
        EmptyPinIdIsRejected();
        RemovedPinDoesNotComeBackAfterARestart();
        ProfilePinsLoadedBeforeSpawnAreNotSent();
        HostProfilePinsAreSharedAfterSpawn();
        DeleteWhileOfflineIsNotUndone();
        DeleteListSurvivesARestart();
        ReAddingADeletedPinWorks();
        MapClickPinIsSharedWithItsName();
        MapClickPinRemovedBeforeNamingIsNotShared();
        MapTableReadKeepsSharedPins();
        PinIdMustMatchItsFields();
        OwnerZeroIsRejected();
        MalformedRemoveIsDiscarded();

        Console.WriteLine(failures == 0 ? "\nALL TESTS PASS" : $"\n{failures} TEST FAILURE(S)");
        return failures;
    }

    // ------------------------------------------------------------------ tests

    private static void NonShareableTypesStayLocal()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Death, "death", save: true, isChecked: false);
        client.Map.AddPin(new Vector3(11, 0, 11), Minimap.PinType.Ping, "ping", save: true, isChecked: false);
        Check("non-shareable types stay local", client.Rpc.Calls.Count == 0);
    }

    private static void SaveFalsePinsStayLocal()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "transient", save: false, isChecked: false);
        Check("save:false pins stay local", client.Rpc.Calls.Count == 0);
    }

    private static void ClientAddReachesServerAndBroadcasts()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);

        Check("client sent one add", client.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 1);
        Check("server broadcast the add",
            server.Rpc.Calls.Any(c => c.Method == VPlusMapPinSync.AddRpc && c.Target == ZRoutedRpc.Everybody));
        Check("server node shows the pin", server.PinNames().Contains("copper"));
    }

    private static void ReceivedPinFromOtherPlayerIsFaded()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);

        var onServerNode = server.Pin("copper");
        Check("pin exists on the other node", onServerNode != null);
        Check("other player's pin is faded", onServerNode != null && onServerNode.m_ownerID == ClientPlayer);
    }

    private static void OwnPinComesBackUnfaded()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);

        var mine = client.Map.m_pins.Where(p => p.m_name == "copper").ToList();
        Check("own pin is not duplicated by the broadcast", mine.Count == 1);
        Check("own pin stays unfaded", mine.Count == 1 && mine[0].m_ownerID == 0L);
    }

    private static void ApplyingDoesNotEcho()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);

        // One add from the client, one broadcast from the server. Applying it must not start a loop.
        Check("no echo loop", client.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 1 &&
                              server.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 1);
    }

    private static void DuplicateAddIsIgnored()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);
        var broadcasts = server.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc);

        // Same position, type and name: the server already has it.
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);
        Check("duplicate add is not rebroadcast",
            server.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == broadcasts);
    }

    private static void RemovePropagatesToEveryone()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);
        Check("both nodes have it", client.Pin("copper") != null && server.Pin("copper") != null);

        client.Enter();
        client.Map.RemovePin(client.Pin("copper"));

        Check("remover no longer has it", client.Pin("copper") == null);
        Check("other node no longer has it", server.Pin("copper") == null);
    }

    private static void RemoveOfFadedPinPropagates()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);

        var faded = server.Pin("copper");
        Check("it is faded on the other node", faded != null && faded.m_ownerID != 0L);

        server.Enter();
        server.Map.RemovePin(faded);
        client.Enter();

        // Strict global delete: refusing someone else's pin removes it for everyone.
        Check("deleting a faded pin removes it everywhere",
            server.Pin("copper") == null && client.Pin("copper") == null);
    }

    private static void RemoveOfUnknownPinIsSilent()
    {
        Setup();
        var orphan = new Minimap.PinData
        {
            m_pos = new Vector3(500, 0, 500), m_type = Minimap.PinType.Icon0,
            m_name = "ghost", m_save = true,
        };
        client.Map.m_pins.Add(orphan);
        client.Map.RemovePin(orphan);

        Check("unknown pin is not rebroadcast",
            server.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.RemoveRpc) == 0);
    }

    private static void SnapshotDeliversWholeList()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);
        client.Map.AddPin(new Vector3(20, 0, 20), Minimap.PinType.Boss, "boss", save: true, isChecked: false);

        // A second client joins and asks for the list.
        var joiner = new Node(server: false, peerId: 3, playerId: 1003);
        joiner.Rpc.ServerID = ServerPeer;
        joiner.Rpc.Router = (target, method, package) => { if (target == ServerPeer) Deliver(server, 3, method, package); };
        server.Rpc.Router = (target, method, package) =>
        {
            if (target == ZRoutedRpc.Everybody || target == 3) Deliver(joiner, ServerPeer, method, package);
        };

        joiner.Enter();
        VPlusMapPinSync.RequestSnapshot();

        Check("joiner received both pins", joiner.PinNames().SequenceEqual(new[] { "boss", "copper" }));
        Check("joiner sees them faded", joiner.Map.m_pins.All(p => p.m_ownerID == ClientPlayer));
    }

    private static void SnapshotPullsUpClientOnlyPins()
    {
        Setup();
        // A pin placed before the setting was on: the server has never heard of it.
        Configuration.Current.Map.shareAllPins = false;
        client.Map.AddPin(new Vector3(30, 0, 30), Minimap.PinType.Icon1, "legacy", save: true, isChecked: false);
        Configuration.Current.Map.shareAllPins = true;

        client.Enter();
        VPlusMapPinSync.RequestSnapshot();

        Check("legacy pin was offered to the server", server.Pin("legacy") != null);
    }

    private static void PersistenceRoundTrip()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);

        server.Enter();
        VPlusMapPinSync.SavePinsToDisk();

        Check("store file written", File.Exists(StoreFile));

        // Restart: the server forgets everything and must reload from disk.
        Setup(keepDisk: true);
        var joiner = new Node(server: false, peerId: 3, playerId: 1003);
        joiner.Rpc.ServerID = ServerPeer;
        joiner.Rpc.Router = (target, method, package) => { if (target == ServerPeer) Deliver(server, 3, method, package); };
        server.Rpc.Router = (target, method, package) =>
        {
            if (target == ZRoutedRpc.Everybody || target == 3) Deliver(joiner, ServerPeer, method, package);
        };

        joiner.Enter();
        VPlusMapPinSync.RequestSnapshot();

        Check("pin survived a restart", joiner.Pin("copper") != null);
        Check("owner survived a restart", joiner.Pin("copper")?.m_ownerID == ClientPlayer);
    }

    private static void MalformedPinIsRejected()
    {
        Setup();
        var package = new ZPackage();
        package.Write("bogus-id");
        package.Write("nan");
        package.Write(new Vector3(float.NaN, 0, 0));
        package.Write((int)Minimap.PinType.Icon0);
        package.Write(ClientPlayer);

        Deliver(server, ClientPeer, VPlusMapPinSync.AddRpc, package);
        Check("NaN position is rejected", server.Map.m_pins.Count == 0 &&
                                          server.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 0);
    }

    private static void DisabledConfigSendsNothing()
    {
        Setup();
        Configuration.Current.Map.shareAllPins = false;
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);
        Check("nothing is sent when the setting is off", client.Rpc.Calls.Count == 0);
        Configuration.Current.Map.shareAllPins = true;
    }

    private static void HostAuthoredPinReachesClients()
    {
        Setup();
        server.Enter();
        server.Map.AddPin(new Vector3(40, 0, 40), Minimap.PinType.Icon2, "host-pin", save: true, isChecked: false);
        client.Enter();

        Check("host pin is not duplicated on the host", server.Map.m_pins.Count(p => p.m_name == "host-pin") == 1);
        Check("host pin reached the client", client.Pin("host-pin") != null);
        Check("host pin is faded on the client", client.Pin("host-pin")?.m_ownerID == ServerPlayer);
    }

    private static void TwoClientsConverge()
    {
        Setup();
        var second = new Node(server: false, peerId: 3, playerId: 1003);
        second.Rpc.ServerID = ServerPeer;
        second.Rpc.Router = (target, method, package) => { if (target == ServerPeer) Deliver(server, 3, method, package); };
        server.Rpc.Router = (target, method, package) =>
        {
            if (target == ZRoutedRpc.Everybody || target == ClientPeer) Deliver(client, ServerPeer, method, package);
            if (target == ZRoutedRpc.Everybody || target == 3) Deliver(second, ServerPeer, method, package);
        };

        client.Enter();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "from-a", save: true, isChecked: false);
        second.Enter();
        second.Map.AddPin(new Vector3(20, 0, 20), Minimap.PinType.Icon1, "from-b", save: true, isChecked: false);

        Check("client A has both", client.PinNames().SequenceEqual(new[] { "from-a", "from-b" }));
        Check("client B has both", second.PinNames().SequenceEqual(new[] { "from-a", "from-b" }));
        Check("each keeps its own unfaded",
            client.Pin("from-a").m_ownerID == 0L && second.Pin("from-b").m_ownerID == 0L);
        Check("each sees the other faded",
            client.Pin("from-b").m_ownerID == 1003 && second.Pin("from-a").m_ownerID == ClientPlayer);
    }

    private static void SnapshotDoesNotDuplicateALocalPin()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);

        client.Enter();
        VPlusMapPinSync.RequestSnapshot();

        Check("snapshot does not duplicate a pin the client already has",
            client.Map.m_pins.Count(p => p.m_name == "copper") == 1);
    }

    private static void RemoveBeforeAddIsHarmless()
    {
        Setup();
        var package = new ZPackage();
        package.Write("0|99.0|99.0|never-existed");
        Deliver(server, ClientPeer, VPlusMapPinSync.RemoveRpc, package);

        Check("removing an unknown id does not broadcast",
            server.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.RemoveRpc) == 0);

        client.Enter();
        client.Map.AddPin(new Vector3(99, 0, 99), Minimap.PinType.Icon0, "never-existed", save: true, isChecked: false);
        Check("a later add for that id still works", server.Pin("never-existed") != null);
    }

    private static void NewlineInPinNameSurvivesTheStore()
    {
        // A name reaching the store with a line break would split one pin into two records.
        var name = "line" + (char)10 + "break";

        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, name, save: true, isChecked: false);
        Check("newline pin reached the server", server.Pin(name) != null);

        RestartServerAndSnapshot(out var joiner);
        Check("newline in a pin name survives the store", joiner.Pin(name) != null);
        Check("newline pin did not multiply", joiner.Map.m_pins.Count == 1);
    }

    private static void SeparatorInPinNameSurvivesTheStore()
    {
        // The store joins fields with U+001F, so a name containing one would shift every field.
        var name = "unit" + (char)31 + "sep";

        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, name, save: true, isChecked: false);
        Check("separator pin reached the server", server.Pin(name) != null);

        RestartServerAndSnapshot(out var joiner);
        Check("record separator in a pin name survives the store", joiner.Pin(name) != null);
    }

    private static void PipeInPinNameKeepsPinsDistinct()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "a|b", save: true, isChecked: false);
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "a", save: true, isChecked: false);

        Check("a pipe in a name does not collide two pins", server.Map.m_pins.Count == 2);
    }

    private static void CorruptStoreLineIsSkipped()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);
        server.Enter();
        VPlusMapPinSync.SavePinsToDisk();

        File.WriteAllLines(StoreFile,
            new[] { "garbage", File.ReadAllLines(StoreFile)[0], "1" + (char)31 + "not-a-number" });

        RestartServerAndSnapshot(out var joiner, keepDisk: true);
        Check("corrupt lines are skipped, the good one loads",
            joiner.Map.m_pins.Count == 1 && joiner.Pin("copper") != null);
    }

    private static void EmptyStoreLoadsCleanly()
    {
        Setup();
        File.WriteAllText(StoreFile, string.Empty);

        RestartServerAndSnapshot(out var joiner, keepDisk: true);
        Check("an empty store loads cleanly", joiner.Map.m_pins.Count == 0);
    }

    private static void ManyPinsSurviveASnapshot()
    {
        Setup();
        for (var i = 0; i < 200; i++)
            client.Map.AddPin(new Vector3(i, 0, i), Minimap.PinType.Icon0, $"pin{i}", save: true, isChecked: false);

        RestartServerAndSnapshot(out var joiner);
        Check("200 pins survive the store and a snapshot", joiner.Map.m_pins.Count == 200);
    }

    /// <summary>Saves, wipes in-memory state, and joins a fresh client that asks for the list.</summary>
    private static void RestartServerAndSnapshot(out Node joiner, bool keepDisk = false)
    {
        if (!keepDisk)
        {
            server.Enter();
            VPlusMapPinSync.SavePinsToDisk();
        }

        Setup(keepDisk: true);
        var fresh = new Node(server: false, peerId: 3, playerId: 1003);
        fresh.Rpc.ServerID = ServerPeer;
        fresh.Rpc.Router = (target, method, package) => { if (target == ServerPeer) Deliver(server, 3, method, package); };
        server.Rpc.Router = (target, method, package) =>
        {
            if (target == ZRoutedRpc.Everybody || target == 3) Deliver(fresh, ServerPeer, method, package);
        };

        fresh.Enter();
        VPlusMapPinSync.RequestSnapshot();
        joiner = fresh;
    }

    private static void OwnPinComesBackUnfadedAfterRelog()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);

        // Same player, empty map: the snapshot is the only source, so ApplyAdd decides the fade.
        var relogged = new Node(server: false, peerId: 3, playerId: ClientPlayer);
        relogged.Rpc.ServerID = ServerPeer;
        relogged.Rpc.Router = (target, method, package) => { if (target == ServerPeer) Deliver(server, 3, method, package); };
        server.Rpc.Router = (target, method, package) =>
        {
            if (target == ZRoutedRpc.Everybody || target == 3) Deliver(relogged, ServerPeer, method, package);
        };

        relogged.Enter();
        VPlusMapPinSync.RequestSnapshot();

        Check("own pin returns after a relog", relogged.Pin("copper") != null);
        Check("own pin is not faded after a relog", relogged.Pin("copper")?.m_ownerID == 0L);

        // This pin is applied unfaded, so the m_ownerID guard cannot stop the postfix from
        // re-sending it. Only the "applying" flag does. The server would ignore the duplicate,
        // but the round trip is still wasted traffic on every relog.
        Check("re-applying an own pin sends nothing back",
            relogged.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 0);
    }

    private static void NonShareableTypeIsRejectedOnTheWire()
    {
        Setup();

        // A modified client could send any type; the server must not store or relay it.
        var package = new ZPackage();
        package.Write("4|10.0|10.0|death");
        package.Write("death");
        package.Write(new Vector3(10, 0, 10));
        package.Write((int)Minimap.PinType.Death);
        package.Write(ClientPlayer);
        Deliver(server, ClientPeer, VPlusMapPinSync.AddRpc, package);

        Check("a non-shareable type is rejected on the wire",
            server.Map.m_pins.Count == 0 &&
            server.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 0);
    }

    private static void ReceiverDoesNotEchoAnAppliedAdd()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);

        // The server node is the receiver here: applying the broadcast must not send anything back.
        Check("receiving a pin sends nothing back",
            server.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 1);
    }

    private static void ReceiverDoesNotEchoAnAppliedRemove()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);
        client.Enter();
        client.Map.RemovePin(client.Pin("copper"));

        // One broadcast from the server; applying it on the receiver must not send a second remove.
        Check("applying a remove sends nothing back",
            server.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.RemoveRpc) == 1 &&
            client.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.RemoveRpc) == 1);
    }

    private static void EmptyPinIdIsRejected()
    {
        Setup();
        var package = new ZPackage();
        package.Write(string.Empty);
        package.Write("no-id");
        package.Write(new Vector3(10, 0, 10));
        package.Write((int)Minimap.PinType.Icon0);
        package.Write(ClientPlayer);
        Deliver(server, ClientPeer, VPlusMapPinSync.AddRpc, package);

        Check("an empty pin id is rejected",
            server.Map.m_pins.Count == 0 &&
            server.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 0);
    }

    private static void RemovedPinDoesNotComeBackAfterARestart()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);
        client.Enter();
        client.Map.RemovePin(client.Pin("copper"));

        // The delete has to reach the persisted store, not just the connected clients.
        RestartServerAndSnapshot(out var joiner);
        Check("a deleted pin stays deleted after a restart", joiner.Pin("copper") == null);
    }

    /// <summary>A client node wired to the server. The server only reaches the nodes passed to <see cref="Online"/>.</summary>
    private static Node Joiner(long peerId, long playerId)
    {
        var node = new Node(server: false, peerId, playerId);
        node.Rpc.ServerID = ServerPeer;
        node.Rpc.Router = (target, method, package) => { if (target == ServerPeer) Deliver(server, peerId, method, package); };
        return node;
    }

    private static void Online(params (Node node, long peer)[] nodes)
    {
        server.Rpc.Router = (target, method, package) =>
        {
            if (target == ServerPeer) Deliver(server, ServerPeer, method, package);
            foreach (var (node, peer) in nodes)
                if (target == ZRoutedRpc.Everybody || target == peer) Deliver(node, ServerPeer, method, package);
        };
    }

    private static void ProfilePinsLoadedBeforeSpawnAreNotSent()
    {
        Setup();
        // Vanilla re-adds the profile's pins before the player exists; the postfix sees them all.
        client.LocalPlayer = null;
        client.Enter();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon1, "house", save: true, isChecked: false);
        Check("a profile pin is not sent before spawn",
            client.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 0);

        client.LocalPlayer = new Player(ClientPlayer);
        client.Enter();
        VPlusMapPinSync.RequestSnapshot();

        var other = Joiner(3, 1003);
        Online((client, ClientPeer), (other, 3));
        other.Enter();
        VPlusMapPinSync.RequestSnapshot();
        Check("the profile pin is offered once the player exists", other.Pin("house") != null);
        Check("it carries its owner, so others see it faded", other.Pin("house")?.m_ownerID == ClientPlayer);
    }

    private static void HostProfilePinsAreSharedAfterSpawn()
    {
        Setup();
        server.LocalPlayer = null;
        server.Enter();
        server.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon1, "host-house", save: true, isChecked: false);

        Online((client, ClientPeer));
        server.LocalPlayer = new Player(ServerPlayer);
        server.Enter();
        VPlusMapPinSync.RequestSnapshot();

        Check("a hosting player's profile pin reaches clients after spawn", client.Pin("host-house") != null);
        Check("and arrives faded with the host as owner", client.Pin("host-house")?.m_ownerID == ServerPlayer);
    }

    private static void DeleteWhileOfflineIsNotUndone()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);

        // The author leaves; another player deletes the pin.
        var other = Joiner(3, 1003);
        Online((other, 3));
        other.Enter();
        VPlusMapPinSync.RequestSnapshot();
        other.Map.RemovePin(other.Pin("copper"));

        // The author comes back still holding the pin in their profile.
        Online((client, ClientPeer), (other, 3));
        client.Enter();
        VPlusMapPinSync.RequestSnapshot();

        Check("a returning holder does not re-share a deleted pin", server.Pin("copper") == null);
        Check("a returning holder drops its own copy", client.Pin("copper") == null);
        Check("the deleter does not get it back", other.Pin("copper") == null);
    }

    private static void DeleteListSurvivesARestart()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);
        var other = Joiner(3, 1003);
        Online((client, ClientPeer), (other, 3));
        other.Enter();
        VPlusMapPinSync.RequestSnapshot();

        // The other player goes offline, the author deletes, the server restarts.
        Online((client, ClientPeer));
        client.Enter();
        client.Map.RemovePin(client.Pin("copper"));
        server.Enter();
        VPlusMapPinSync.SavePinsToDisk();

        var otherMap = other.Map;
        Setup(keepDisk: true);
        var returning = Joiner(3, 1003);
        returning.Map = otherMap;
        Online((returning, 3));
        returning.Enter();
        VPlusMapPinSync.RequestSnapshot();

        Check("a delete made before a restart still reaches an offline holder", returning.Pin("copper") == null);
        Check("and the pin is not re-shared", server.Pin("copper") == null);
    }

    /// <summary>Mirrors Minimap.ShowPinNameInput: vanilla adds the map-click pin unnamed.</summary>
    private static Minimap.PinData OpenNameDialog(Node node, Vector3 pos)
    {
        node.Enter();
        VPlusMapPinSync.CreatingNamedPin = true;
        try { return node.Map.AddPin(pos, Minimap.PinType.Icon3, "", save: true, isChecked: false); }
        finally { VPlusMapPinSync.CreatingNamedPin = false; }
    }

    private static void MapClickPinIsSharedWithItsName()
    {
        Setup();
        var other = Joiner(3, 1003);
        Online((client, ClientPeer), (other, 3));
        other.Enter();
        VPlusMapPinSync.RequestSnapshot();

        var pin = OpenNameDialog(client, new Vector3(10, 0, 10));
        VPlusMapPinSync.FlushNamedPin(client.Map, pin);
        Check("a map-click pin is not shared while its name dialog is open",
            client.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 0);

        pin.m_name = "copper"; // OnPinTextEntered
        VPlusMapPinSync.FlushNamedPin(client.Map, null);
        Check("it is shared with its name once the dialog closes", other.Pin("copper") != null);
        Check("no unnamed copy reaches the other player", other.Pin("") == null);

        client.Enter();
        client.Map.RemovePin(pin);
        Check("deleting the named pin removes it for the other player", other.Pin("copper") == null);
        Check("and from the server", server.Pin("copper") == null);
    }

    private static void MapClickPinRemovedBeforeNamingIsNotShared()
    {
        Setup();
        var pin = OpenNameDialog(client, new Vector3(10, 0, 10));
        client.Map.RemovePin(pin);
        VPlusMapPinSync.FlushNamedPin(client.Map, null);

        Check("a pin removed before its dialog closed is never shared",
            client.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 0 && server.Map.m_pins.Count == 0);
    }

    private static void MapTableReadKeepsSharedPins()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);
        var other = Joiner(3, 1003);
        Online((client, ClientPeer), (other, 3));
        other.Enter();
        VPlusMapPinSync.RequestSnapshot();

        // Mirrors Minimap.AddSharedMapData: a table without this pin removes the faded copy.
        VPlusMapPinSync.BeginMapTableRead();
        other.Map.RemovePin(other.Pin("copper"));
        VPlusMapPinSync.EndMapTableRead();

        Check("reading a map table does not delete a shared pin", server.Pin("copper") != null);
        Check("the author keeps it", client.Pin("copper") != null);
        Check("the reader gets it back, still faded", other.Pin("copper")?.m_ownerID == ClientPlayer);
    }

    /// <summary>An add packet whose fields are valid, with the id and owner left to the caller.</summary>
    private static ZPackage AddPacket(string id, long ownerId)
    {
        var package = new ZPackage();
        package.Write(id);
        package.Write("copper");
        package.Write(new Vector3(10, 0, 10));
        package.Write((int)Minimap.PinType.Icon0);
        package.Write(ownerId);
        return package;
    }

    private static void PinIdMustMatchItsFields()
    {
        Setup();
        Deliver(server, ClientPeer, VPlusMapPinSync.AddRpc, AddPacket("0|99.0|99.0|other", ClientPlayer));
        Check("a pin whose id does not match its fields is rejected",
            server.Map.m_pins.Count == 0 &&
            server.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 0);

        Deliver(server, ClientPeer, VPlusMapPinSync.AddRpc, AddPacket("0|10.0|10.0|copper", ClientPlayer));
        Check("the same pin with its canonical id is accepted", server.Pin("copper") != null);
    }

    private static void OwnerZeroIsRejected()
    {
        Setup();
        Deliver(server, ClientPeer, VPlusMapPinSync.AddRpc, AddPacket("0|10.0|10.0|copper", 0L));
        Check("a pin claiming no owner is rejected",
            server.Map.m_pins.Count == 0 &&
            server.Rpc.Calls.Count(c => c.Method == VPlusMapPinSync.AddRpc) == 0);
    }

    private static void MalformedRemoveIsDiscarded()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);

        var threw = false;
        try
        {
            Deliver(server, ClientPeer, VPlusMapPinSync.RemoveRpc, new ZPackage());
            Deliver(client, ServerPeer, VPlusMapPinSync.RemoveRpc, new ZPackage());
            Deliver(client, ServerPeer, VPlusMapPinSync.SnapshotRpc, new ZPackage());
        }
        catch (Exception)
        {
            threw = true;
        }

        Check("short remove and snapshot packets are discarded without throwing", !threw);
        Check("and change nothing", server.Pin("copper") != null && client.Pin("copper") != null);
    }

    private static void ReAddingADeletedPinWorks()
    {
        Setup();
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);
        client.Map.RemovePin(client.Pin("copper"));
        client.Map.AddPin(new Vector3(10, 0, 10), Minimap.PinType.Icon0, "copper", save: true, isChecked: false);
        Check("placing a deleted pin again shares it", server.Pin("copper") != null);

        RestartServerAndSnapshot(out var joiner);
        Check("the re-added pin survives a restart", joiner.Pin("copper") != null);
    }

    private static void Check(string name, bool ok)
    {
        Console.WriteLine($"  {(ok ? "PASS" : "FAIL")}  {name}");
        if (!ok) failures++;
    }
}

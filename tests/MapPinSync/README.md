# Map pin sync tests

Runs the production `ValheimPlus/RPC/VPlusMapPinSync.cs` against stub game types, with a server
node and a client node in one process. `Stubs.cs` replaces only the game boundary (`Minimap`,
`ZNet`, `ZRoutedRpc`, `ZPackage`, `Player`); no sync logic is duplicated here.

```
cd tests/MapPinSync
dotnet run
```

Exit code is the number of failed assertions.

## Design under test

The server owns the pin list and persists it to `<VPlusDataDirectory>/<World>_mapPins.dat`.
Clients send their own adds and removes to the server and apply what it broadcasts back.

A pin from another player is added with a non-zero `m_ownerID`, which is how vanilla marks
"someone else's pin": it draws faded, and a left click adopts it. That part is vanilla behaviour,
not mod code, so it is not covered here.

Deletes are global: removing a pin removes it for everyone, whether or not it was adopted first.
The server keeps deleted ids in `<World>_mapPinsDeleted.dat` and sends them in the join snapshot,
so a player who was offline drops their copy instead of sharing it again.

## Covered

Pin filtering, add propagation, fading and ownership, echo-loop suppression, duplicate
suppression, delete propagation (adopted and faded), join snapshots, seeding the server from
pins that predate the setting, profile pins loaded before spawn, deletes made while a holder was
offline (also across a restart), re-adding a deleted pin, map-click pins named after the dialog,
cartography table reads, ids that do not match their fields, adds claiming owner 0, short remove
and snapshot packets, the persistence round trip across a restart, malformed packets,
and the setting being off.

## Not covered — needs two clients in-game

| Scenario | Expected |
| --- | --- |
| Player-hosted session | Host add/delete applies locally with no round trip; clients converge |
| Dedicated server | Same, with the store loaded lazily on the first client request |
| Left-clicking a faded pin | Vanilla clears `m_ownerID`; the pin stays after a relog |
| Reconnect | Snapshot restores the list with no duplicates |
| Deleting a faded pin | Disappears for every connected player |
| Server restart | Pins reload from disk for a player who was never online with the author |
| Mixed versions | A client without this build neither sends nor receives; no errors either side |

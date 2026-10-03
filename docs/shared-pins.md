# Shared map pins (experimental, Valheim 1.0.16)

## Enable

Install the same new ValheimPlus.dll on the server and participating clients, and disable TXC SharedMap on all of them. In the existing `[Map]` section of `BepInEx/config/org.bepinex.plugins.valheim_plus.cfg`, set:

```ini
[Map]
enabled = true
sharePins = true
```

The server syncs sharePins to clients. Restart the server and clients after installation. This feature defaults off. It does not change shareMapProgression or existing exploration data. It uses a new RPC handshake; clients only receive pin packets after subscribing. An old server leaves newly created pins private, with an explanatory message.

## Use

- Place a pin normally and finish entering its name: it becomes public and gold after server confirmation.
- Hold Left Ctrl **when placing** a new pin: it stays private and white.
- Hold Left Shift and left-click an existing private pin: publish it.
- Left-click a public pin: check/uncheck it for everyone.
- Right-click a public pin: only its creator's account or a server admin may delete it. The server enforces this. Listen-server hosts count as admins.
- Existing personal pins are not automatically uploaded. Death, bed, player, event and temporary pins are not shared. The five normal user icons and boss pins are eligible.
- privatePinKey and publishPinKey are local [Map] settings, defaulting to LeftControl and LeftShift.

With a controller, open the large map and use **D-pad Left** over empty map space to toggle **New pins: Public / Private**. Press A to place the pin normally. The choice stays in effect until the map is reopened, which resets it to Public. When the centre crosshair targets an eligible private pin, D-pad Left instead **shares that pin** without changing the creation preference. A two-line hint above the normal map controls shows the current preference and available action. Text entry and blocked map input do not trigger this action. Keyboard Ctrl/Shift controls are unchanged; the controller preference applies only while the controller is the active input device. Existing public pins cannot be made private with this control.

The feature starts once the client completes a server handshake. A pin created before that stays private; Shift-click can publish it later. A rejected or failed upload also stays private. Public pins are kept out of character saves and cartography-table exports. They are received afresh when joining the server. Their visibility does not depend on vanilla's cartography checkbox.

TXC SharedMap 2.1.0 was used only as a reference for the requested interaction behavior. Its installed distribution provided no source URL or license file; no code, assets or file format were copied. This is an original implementation in V+. Do not enable both pin-sharing systems: V+ detects loaded TXC SharedMap and disables its own pin feature with a warning. Existing TXC server pin files are not imported or changed. Disable TXC rather than deleting its saved data. Previously shared TXC pins need a separate migration if you want to carry them over.

## Persistence and permissions

Pins are saved in V+'s existing data directory (`BepInEx/vplus-data`) as `<worldUID>_sharedPins.dat`; the previous successful save is retained as `.bak`. Include these files in world backups. Stop the server before restoring a backup. The UID prevents similarly named worlds from sharing a pin store.

The server derives ownership from the connected peer's socket identity, as used by Valheim's admin checks; an owner in a client packet is ignored. Character/display names are not authorization credentials. Checked-state changes are allowed for subscribed players; deletion is creator/admin only. Store mutations are written atomically before broadcasting. Invalid or unreadable saves disable the pin service without overwriting the file. Failed writes reject the change and preserve the previous committed state. Pin deletion IDs persist to reject replayed creation messages.

New RPC names and a protocol version keep this independent from the removed add-only V+ implementation. Initial snapshots are staged and applied after all pages arrive; later changes carry ordered revisions. A missing revision triggers resynchronization. Messages are batched (16 pins per snapshot page), and each peer gets at most four queued packets per 250 ms tick, pausing above 5 KiB of socket send backlog. Per-peer queues, request size, pin count, names and coordinates are bounded. Current limits: 4,096 public pins per world, 128 characters per name, 100,000 retained deleted IDs. There is no position-based auto-deletion/deduplication of intentionally overlapping pins; GUID identity prevents network echoes from duplicating a pin.

A connection lost after server commit but before acknowledgement can leave an unconfirmed private copy alongside the server's public pin on a later login. Keep the public pin and remove the redundant private copy if that occurs. The implementation favors preserving private data rather than automatically deleting similar pins.

## What was wrong with the removed feature

Before removal in upstream commit ab8e49f, shareablePins was an empty list. Its initialization lived inside commented-out retired UI code, so the active AddPin postfix never selected any pins. The code also sent AddPin before the final user-entered name, relayed only to players currently connected, lacked durable server state and deletion/check synchronization, relied on a local player while receiving/loading, and used player names in its self-filter. Restoring the old classes verbatim would not provide reliable shared pins.

This implementation hooks the end of vanilla's naming flow instead of every AddPin call. Receiving or loading a public pin cannot rebroadcast it. Private creation is captured at placement time. Serialization hooks temporarily clear the vanilla save flag only for public pins and restore it even if serialization throws; the flag stays true for normal vanilla hit testing.

## Validation

The source builds against the locally installed Valheim 1.0.16 game assemblies and BepInEx 5.4.23.5. The removed V+ code and current Minimap, ZNet, ZNetPeer and ZRoutedRpc methods were inspected, including current name-input finalization, pin selection, save/export filters and peer identity APIs.

The automated suite links the production sources and uses independent simulated peers with serialized messages. It covers persistence, private/default-public creation, late joins, final names, replay rejection, owner/admin authorization, checking, atomic-save failure, save/export exclusion, congestion, multi-page snapshots with intervening changes, server restart and TXC conflict detection. It is not a Unity runtime or Linux dedicated-server test.

Before relying on it in the main world, verify with two clients:

1. Place a named public pin; the other client sees exactly one gold pin with the final name.
2. Place a Ctrl-private pin; the other client never receives it. Shift-click it and confirm publication.
3. Check/uncheck a public pin from the other client. Attempt deletion as a non-owner (refused), then as the owner (removed for both). Test admin deletion if used.
4. Disconnect/reconnect, then restart the server. Public pins remain and deleted pins do not return. Try joining only after the creator has disconnected.
5. Save/reload characters and use a cartography table; shared pins should not turn into private or table copies.
6. Confirm ordinary private pins, exploration sharing, smelter automation and normal map controls still work.
7. On controller, toggle to Private over empty space, place a pin with A, and confirm the other player cannot see it. Target it with the centre crosshair and press D-pad Left to share it. Reopen the map and confirm the preference returns to Public. Check hint placement at your UI scale and ensure normal D-pad up/down/right, ping, check and delete controls still work.

This change contains shared-pin functionality only. Keep the last working DLL for rollback. Restoring it makes this feature unavailable but does not delete the shared-pin data file.

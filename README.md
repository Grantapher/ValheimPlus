<p align="center">
  <img src="https://raw.githubusercontent.com/grantapher/ValheimPlus/main/logo.png" alt="ValheimPlus Logo"/>
</p>

# ValheimPlus

A HarmonyX Mod aimed at improving the gameplay quality of Valheim. The mod includes several different main features that allow users to modify the stats of players, buildings and entities. V+ also offers players the ability to build and place objects with very high precision through a sophisticated system, as well as tweaking and modifying already placed objects with equal precision. The goal is to provide V+ as a base modification for Valheim to increase quality of life, tweak the game's difficulty, and in general, improve the player's experience. V+ also comes with a version and configuration control system for servers and users, enabling server owners to ensure that only players with the same configuration are able to join the server.

### All features can be enabled and tweaked through the V+ config file.

# Player

### Gameplay

- Modify stamina consumption and regeneration.
- Modify stamina consumption when using tools and weaponry.
- Modify eitr consumption of blood and elemental magic.
- Modify the health cost of blood magic.
- Modify food duration.
- Disable food degradation over time (maintain full benefit for the whole duration).
- Modify carry load, including the bonus from Megingjord's girdle.
- Disable the encumbered state entirely.
- Modify auto-pickup range.
- Modify unarmed damage.
- Modify each skill's experience gain separately by percent.
- Remove screen shakes.
- Tweak/disable death penalty.
- Modify fall damage by percent, and cap the maximum fall damage taken.
- Tweak Rested bonus duration per comfort level.
- Disable the use of portals.
- Shorten the portal teleport animation time to the minimum possible.
- Modify the Guardian buff duration, cooldown and animation.
- Modify adrenaline gain and decay, how much adrenaline trinkets need, and how long their effects last.
- Disable tutorials from Valkyrie.
- Skip the game's intro sequence.
- Disable the "I have arrived!" message on spawn.
- Modify velocity and accuracy of projectiles from bows and javelins including a option to scale it with skill level.
- Queue weapon switches requested mid-attack instead of dropping them.
- Option to sleep without setting your spawn point, optionally only in unclaimed beds.
- Allow auto pickup of items when encumbered (overweight).
- Option to disable the unequipping of items when entering the water, and to re-equip them on leaving it.
- Option to automatically unequip your shield when you're unequipping your main hand weapon.
- Option to automatically equip your shield when you're equipping your main hand weapon.
- Allow crafting stations to automatically repair all appropriate items in the player's inventory on interaction.
- Modify the mist-clearing radius of the Wisplight, Wisp Torch and Mistwalker.

### Player Hud

- Show skill experience gains and current skill level in the top left corner on gaining exp.
- Show amount of items in the player's inventory when crafting or building an object.
- Disable red screen flash on receiving damage.
- Display a warning message when attempting to place different crops too close to each other.
- Hotkey options for forward and backward roll.
- Display in-game clock in top center, with configurable size, color and 12/24 hour format.
- Disable Fog.
- Option to increase brightness at night.
- Show the amount of arrows when a bow is equipped.
- Display portal names in large text in the center of the screen.
- Force disable the in-game console.

### Map

- Force all players in the server to display their map position.
- Allow all players to share all map exploration with every other player in the server, as long as their map position is displayed.
- A system to automatically share all map progression with players connecting, even if they have never been on the server.
- Share map pins through the server. Other players' pins appear faded until clicked, and deleting a pin removes it for everyone.
- Modify the radius of the map that is explored as you move.
- Option to show boats and carts on the map with icons.

### Time

- Force a fixed time of day.
- Change day duration.
- Modify duration of night.

### Camera

- Change the player's Field of View.
- Change the maximum zoom-out distance.
- Change the maximum zoom-out distance when on a boat.
- Switch between first person and third person on button press.
- Hotkeys for changing FOV in first person.

### Sailing

- Modify forward and backward sailing speed.
- Modify how fast the rudder turns and how strongly the ship responds to steering.
- Modify the damage ships take from water impact while sailing.

### Gathering

- Modify the amount of resources dropped on destruction of objects (this includes chitin, stone, all types of wood, feathers, minerals, ice and soft tissue).
- Modify the amount of resources gathered from interactable objects.
  - Edibles: Carrots, Blueberries, Cloudberrys, Raspberrys, Mushrooms, Blue Mushrooms, Yellow Mushrooms, Magecap Mushrooms, Jotun Puffs, Smoke Puffs, Fiddleheads, Vineberrys, Onions, Lingonberries, Kale, Poteitr
  - Flowers and Ingredients : Barley, Carrot Seeds, Dandelions, Flax, Thistle, Turnip Seeds, Turnip, Onion Seeds, Kale Seeds, Oat Seeds, Royal Jelly, Volture Eggs, Entrails
  - Materials : Bone Fragments, Flint, Stone, Wood, Crystal, Tar, Wolf Hair, Wolf Claw, Obsidian, Grausten, ore and other minerals, Charred Skulls
  - Valuables : Ambers, Amber Pearls, Coins, Rubys, Gemstones
  - Surtling cores on item stands inside dungeons.
  - Black cores on item stands inside dungeons.
  - Molten cores 
  - Frostcores.
  - Quest items you can pick up: Dragon Egg, Withered Bone, Goblin Totem
- Modify the drop chance of resources from destroyed objects that have a drop chance like muddy scrap piles.

### Wagon

- Modify the weight contribution of items placed inside a wagon.
- Modify the base weight of all wagons.

### Fire sources

- Option to set fires to retain maximum fuel once the fuel is added.
- Option to set torches and braziers to retain maximum fuel once the fuel is added.
- A system to allow wood to be automatically pulled and inserted from nearby chests from fires.
- A system to automatically pull wood on interaction with a fireplace from nearby chests.

### Game Difficulty
- Modify the difficulty multipliers applied to health and damage of enemies based on the number of connected players.
- Modify the range at which the game considers other players to be nearby.
- Add a number of players to the player count for the purpose of difficulty calculation.
- Set the difficulty calculation to a specific player count.

### Turret
- Change the values and behavior of the in-game ballista / turret.
  - The ballista can ignore players
  - The ballista can have infinite ammo
  - The ballista can shoot faster
  - The ballista can turn faster
  - The ballista can see targets further away
  - The ballista can shoot faster projectiles
  - The ballista can shoot more accurately

## Inventory

- Modify the inventory behavior so that items will be placed in the first slot rather than the last available slot.
- Make items try to merge into an existing stack first, instead of returning to their original slot when recovering a tombstone.
- Add rows to the player inventory. The game's own row count is always respected, so this option only ever adds rows, never removes them. Columns are not configurable.
- Modify the number of rows and columns of every container type, well beyond the game's defaults:
  - Wood, iron, blackmetal and personal chests.
  - Cart/Wagon, Karve and Longboat cargo.
  - _Each container has its own minimum and maximum; see the comments in the config file._
  - _Containers taller than 4 rows get a scrollbar._
- Automatically perform the "Stack All" action into every chest in range when you open a container, with options to exclude equipment, ammo, food and mead.

**Note: Player inventory row configuration may conflict with the Equipment and Quick slots mod unless the Inventory section is disabled.**

## Items

- Modify the durability of each item type separately.
- Modify the amount of armor granted by armor pieces.
- Modify the amount of damage blocked by all shields.
- Remove teleport prevention from all items.
- Reduce the weight of all items by percent.
- Modify maximum item stack size by percent.
- Make all items float in water.
- Modify number of seconds it takes for items to despawn after being dropped on the ground. (default is 3600 seconds).
  - _Note: Items on ground will retain base game functionality which ensures that drops don't disappear if a player is nearby or there is a "player base" nearby_

# Crafting and Production

### Crafting

- Allow building using the content of nearby chests.
- Allow crafting from stations using the content of nearby chests.
- Optionally include carts and ships as crafting sources.
- Allow searching for items that fit inside nearby chests in addition to the inventory when interacting with objects.
  _as example when interacting with a kiln, it will search for wood in nearby chests in addition to your inventory._

### Workbench

- Modify Workbench radius.
- Modify the radius around a Workbench in which enemies are prevented from spawning, separately from the build radius.
- Disable Workbench requirements for roof and exposure.
- Modify the radius at which Workbench attachments can be placed.

### Charcoal Kiln, Smelter and Blast Furnace

_These are the `Kiln`, `Smelter` and `Furnace` config sections respectively._

- Modify processing speed.
- Modify maximum capacity, and the amount of fuel consumed per product.
- Disable Fine Wood and/or Round Log processing for the Charcoal Kiln.
- Allow fuel-type items to be automatically pulled from closest containers.
- Allow the Charcoal Kiln to stop pulling wood from closest containers when a specific threshold has been reached.
- Allow produced items to be automatically placed in the closest containers.
- Allow the Blast Furnace to process all ore types instead of just Black Metal Scrap and Flametal Ore.

### Frigid Kiln and Frost Foundry

- Modify production speed.
- Modify the maximum amount of ice / frozen fuel held, and the amount consumed per product.
- Allow fuel to be automatically pulled from closest containers.
- Allow items produced by the Frigid Kiln to be automatically placed in the closest containers.

### Eitr Refinery

- Modify refining speed.
- Modify the maximum amount of sap and soft tissue that can be placed inside.
- Allow materials to be automatically pulled from closest containers.
- Allow produced eitr to be automatically placed in the closest containers.

### Beehive

- Modify Beehive honey production speed.
- Modify Beehive capacity.
- Display time left until next production.
- Allow items produced by Beehive to be automatically placed in the closest containers.

### Sap Collector

- Modify sap production speed.
- Modify the maximum amount of sap held per collector.
- Display time left until next production.
- Allow collected sap to be automatically placed in the closest containers.

### Fermenter

- Modify Fermenter speed.
- Modify Fermenter output amount.
- Display time left until its finished its next production.
- Allow meads to be automatically pulled from closest containers.
- Allow items produced by Fermenter to be automatically placed in the closest containers.

### Windmill

- Modify Windmill speed production speed.
- Modify maximum amount of barley that can be placed inside.
- Allow ignoring wind intensity so it's always producing at max speed.
- Allow barley to be automatically pulled from closest containers.
- Allow the Windmill to stop pulling oats from closest containers when a specific amount of oat flour has been reached.
- Allow items produced by Windmill to be automatically placed in the closest containers.

### Spinning Wheel

- Modify Spinning Wheel production speed.
- Modify maximum amount of flax that can be placed inside.
- Allow flax to be automatically pulled from closest containers.
- Allow items produced by Spinning Wheel to be automatically placed in the closest containers.

### Oven, Hot Tub, and Shield Generator

- Option to keep them at maximum fuel without consuming any.
- Allow fuel to be automatically pulled from closest containers.

# Creatures and Monsters

### Tamed Pets

- Choose which animal types can be tamed at all.
- Added option for tamed creatures to be essential, or immortal.
- Essential tamed creatures are not fully invincble but get stunned when hit with a killing blow and healed to full life. They can still die, rarely.
- Immortal tamed creatures are fully invincble to every type of damage.
- Added option for modifying the time a tamed essential animal will spend stunned after being hit with what would normally be a killing blow.
- Added option for determining whether players can hurt tamed creatures or not when having essential/immortality option enabled.
- Added option to display if the pet is stunned when hover over it with the mouse.
- Modify how long taming takes, and the strength and range of the Brew of Animal Whispers.
- Option to tame creatures that are hungry or alerted.

### Breeding

- Choose which animal types can breed at all.
- Modify how much love is required to become pregnant, and the chance of gaining it.
- Modify pregnancy duration and the range at which partners find each other.
- Modify how many offspring can be nearby before breeding stops.
- Modify how long newborns take to mature.
- Option to breed creatures that are hungry or alerted.
- Display love points, time until birth, and time until a newborn grows up on hover.

### Eggs

- Modify the time an egg takes to hatch, and the time a chick takes to become an adult.
- Display the time until an egg hatches on hover.
- Option to remove the shelter (roof and fire) requirement.
- Option to let an entire dropped stack of eggs hatch at once.
- Option to have Haldor sell eggs unconditionally, at a configurable price.

### Wisp Spawner

- Modify the maximum number of wisps per spawner.
- Modify the spawn interval and spawn chance.
- Option to allow wisps to spawn during the day.

### Monsters

- Modify velocity and accuracy of projectiles from monsters.
- Modify damage and health scaling of monsters in multiplayer based on player count.

### Loot

- Modify the chance and amount of loot dropped.

# Server

- Remove password requirement for the server.
- Modify the maximum amount of players on a server.
- Automatically sync V+ configuration of players joining a server to match the server's configuration.

# Chat System

- Change default text visibility distances for all types of ingame messages.
- Disable forced uppercase and lowercase in shout and whisper messages.
- Options to limit shouting distance, with the option to still show out-of-range shouts in the chat window.
- Options to limit ping distance.

# Building

- Disable "Invalid Placement" restriction while building.
- Disable "Mystical forces" restriction while building and allows to destroying objects using the hammer in the area.
- Disable deterioration of placeables from rain and water erosion.
- Disable heavy snow damage to placeables.
- Disable lava damage to placeables.
- Free rotation mode for the default Building Mode.
- Advanced Building Mode.
- Advanced Editing Mode.
- Allow aligning buildings to a global grid.
- Modify the structural integrity of placeables.
- Modify the maximum distance you can place objects at.
- Added the ability for the hammer tool to repair all placeables in a configurable radius instead of just the targeted placeable.
- Added option for placeables destroyed/dismantled by players to always drop their full material cost, even if built by another player, optionally including pieces the developers marked as "do not drop".
- Modify effective radius of comfort placeables.
- Modify Ward structure protection radius, and the radius around a Ward in which enemies are prevented from spawning.

### Grid alignment

- When pressing the configured key (left alt is the default) new buildings will be aligned to a global grid.
  - The mode can also be toggled by pressing another key (F7 by default).
  - Building elements (from the third tab) are aligned to to their size (e.g. a wood wall will have an alignment of 2m in X and Y direction). The alignment of building elements in other direction can be configured (by default with the F6 key) to 0.5m, 1m, 2m or 4m.
  - Other buildings like furniture will always be aligned to 0.5m, but the Y position will not be aligned (to make sure they are always exactly on the floor).

### Structural Integrity

- Apply a modifier to the structural integrity of the following materials:
  - Wood
  - Stone
  - Iron
  - Hardwood
  - Marble
  - Ashstone
  - Ancient
  - Ice
  - Timberwood
- Disable structural integrity entirely (this will cause objects placed mid-air to not break and fall).
- Make anything built by players immune to all damage.
- Make boats and carts invincible to all damage, or only to water damage.
- Dismantle player-built boats and carts with the hammer.

### Free Rotation Mode for the default Building Mode

- **Video demo: https://imgur.com/xMH7STj.mp4**
- This modifies the default build mode. How it works (all mentioned hotkeys can be modified):
  - Players can rotate the object selected in any direction while in the usual building mode by pressing certain hotkeys. The location of the object can be manipulated with the mouse:
    - ScrollWheel + LeftAlt to rotate by 1 degree on the Y-axis.
    - ScrollWheel + C to rotate by 1 degree on the X-axis.
    - ScrollWheel + V to rotate by 1 degree on the Z-axis.
  - Use the copy rotation hotkeys to copy the current rotation or apply the same rotation to the next object that is being built.
  - Build the object by clicking.

### Advanced Building Mode

- **Video demo: https://i.imgur.com/ddQCzPy.mp4**
- How it works (all mentioned hotkeys can be modified):
  - Players can freeze the item by pressing the configured key (F1 by default).
  - Players can modify the item position and rotation with the following key combinations:
    - Arrow Up/Down/Left/Right to move the building object in the respective direction.
    - Arrow Up/Down + Control to move the building object up and down.
    - ScrollWheel to rotate the building object on the Y-axis.
    - ScrollWheel + Control to rotate the building object on the X-axis.
    - ScrollWheel + left Alt to rotate the building object on the Z-axis.
    - Numpad plus/minus to either increase or decrease speed, holding SHIFT to raise/lower by 10 instead of 1 (Pressing Shift at any moment in time increases the distance/rotation angle 3 times)
  - Build the object by clicking.

**NOTE:**

- _Objects built with this system are not exempt from the structure/support system. Dungeons and other no-build areas are still restricted._

### Advanced Editing Mode

- **Video demo: https://imgur.com/DMb4ZUv.mp4**
- You cannot be in Build mode (hammer, hoe or terrain tool). How it works:
  - Players can select the item with the configured key (Numpad0 is default).
  - Players can modify the item position and rotation with the following key combinations:
    - Arrow Up/Down/Left/Right to move the building object in the respective direction.
    - Arrow Up/Down + Control to move the building object up and down.
    - ScrollWheel = rotates the building object on the Y-axis.
    - ScrollWheel + Control to rotate the building object on the X-axis.
    - ScrollWheel + left Alt to rotate the building object on the Z-axis.
    - resetAdvancedEditingMode HotKey resets the position and rotation to the initial values.
    - Numpad plus/minus to either increase or decrease speed, holding SHIFT to raise/lower by 10 instead of 1 (Pressing Shift at any moment in time increases the distance/rotation angle 3 times)
  - Press the confirmPlacementOfAdvancedEditingMode Hotkey to confirm the changes. (press abortAndExitAdvancedEditingMode HotKey to abort editing mode and reset the object).

**NOTE:**

- _Other players will not be able to see the item being moved until the player building the item confirms the placement. Dungeons and other no-build areas are still restricted._

# Installation Instructions

**ATTENTION FOR MULTIPLAYER**:
Both the game and the server should have this mod installed to prevent all kinds of different issues.
If you have the mod installed and then have friends join over steam they should have the mod as well.

Installation instructions for every supported variant — mod manager, manual, client and dedicated server, on Windows and Unix — can be found in [INSTALL.md](https://github.com/Grantapher/ValheimPlus/blob/main/INSTALL.md).

**Please read the section about Server Config & Version Control (About Version Enforcement) below.**

# What if the game updates?

Game updates are unlikely to do more than partially break specific features of ValheimPlus at worst.
In case you encounter any issues, use Steam's verify integrity feature and wait for it to download/update all files.
This should resolve any issues related. If you continue to have issues, contact the help channel in [our discord server](https://discord.gg/WU69A2JTcn).

For which mod versions work with which game versions, see [COMPATIBILITY.md](https://github.com/Grantapher/ValheimPlus/blob/main/COMPATIBILITY.md).

# Server Config & Version Control (About Version Enforcement)

- If you have the Server section and enforceMod enabled in the mods config, only players with the same mod version can join your server and you can only join servers with the same mod version installed.
- If you have the Server section and serverSyncsConfig enabled in the mods config, every player joining your server will receive the servers configuration.

**This system is working reliably and is issue-free. Any issues encountered are likely derived from a faulty configuration set up, or the server/client not running v+.**

# Configuration File

The config file is `BepInEx\config\org.bepinex.plugins.valheim_plus.cfg`. It is created the first time you start your game or server, so there is nothing to download or copy in.

If you have [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) installed you can edit the settings in game instead, from the main menu. Settings are read-only while a world is loaded, because most of them only take effect when the game starts.

You can turn off and on every feature of V+ via the config file, by default all settings are turned off.

Upgrading keeps your settings: new options are added to your file with their defaults, and an older `valheim_plus.cfg` is imported once and then set aside as `valheim_plus.cfg.migrated`.

When hosting a server, players who join use the server's settings if you have the `Server` section and the `serverSyncsConfig` option enabled. Their own config file is not modified, and their settings come back when they disconnect. Keybinds, HUD options and field of view always stay the player's own.

Only the server configuration file (located in the server files) needs to be set up when hosting a server with V+ by default.

When hosting for other players over steam, every player will need v+ and they will receive the local settings from the host's game folder.

# Join the Discord

We have several different channels including a showcase channel and alpha testing system, allowing players to always get the newest versions available to test out. Click the logo below to join.

[![ValheimPlus Icon](https://raw.githubusercontent.com/grantapher/ValheimPlus/main/ico.png)](https://discord.gg/WU69A2JTcn)

# Contributing to ValheimPlus

Please see [CONTRIBUTING.md](https://github.com/Grantapher/ValheimPlus/blob/main/CONTRIBUTING.md) for details on compiling V+ for development and contributing to the project.

# Credits

- Kevin 'nx#8830' J.- https://github.com/nxPublic
- Miguel 'Mixone' T. - https://github.com/Mixone-FinallyHere
- Lilian 'healiha' C. - https://github.com/healiha
- Nathan 'NCJ' J. - https://github.com/ncjsvr
- Greg 'Zedle' G. - https://github.com/zedle
- Paige 'radmint' N. - https://github.com/radmint
- Chris 'Xenofell' S. - https://github.com/cstamford
- TheTerrasque - https://github.com/TheTerrasque
- Bruno Vasconcelos - https://github.com/Drakeny
- GaelicGamer - https://github.com/GaelicGamer
- Doudou 'xiaodoudou' - https://github.com/xiaodoudou
- MrPurple6411#0415 - BepInEx Valheim version, AssemblyPublicizer
- Mehdi 'AccretionCD' E. - https://github.com/AccretionCD
- Zogniton - https://github.com/Zogniton - Inventory Overhaul initial creator
- Jules - https://github.com/sirskunkalot
- Lilian Cahuzac - https://github.com/healiha
- Thomas 'Aeluwas#2855' B. - https://github.com/exscape
- Nick 'baconparticles' P. - https://github.com/baconparticles
- An 'Hachidan' N. - https://github.com/ahnguyen09
- Abra - https://github.com/Abrackadabra
- Increddibelly - https://github.com/increddibelly
- Radvo - https://github.com/Radvo
- Shawn - https://github.com/shawnwallace
- Bellian - https://github.com/Bellian
- JF10R - https://github.com/JF10R 

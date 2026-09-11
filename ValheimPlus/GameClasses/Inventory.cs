using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;
using ValheimPlus.Utility;

namespace ValheimPlus.GameClasses
{
    /// <summary>
    /// Alters teleportation prevention
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.IsTeleportable))]
    // ReSharper disable once IdentifierTypo 
    public static class Inventory_IsTeleportable_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ref bool __result)
        {
            var config = Configuration.Current.Items;
            if (!config.IsEnabled || !config.noTeleportPrevention) return;
            __result = true;
        }
    }

    /// <summary>
    /// Makes all items fill inventories top to bottom instead of just tools and weapons
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.TopFirst))]
    public static class Inventory_TopFirst_Patch
    {
        [UsedImplicitly]
        public static void Postfix(ref bool __result)
        {
            var config = Configuration.Current.Inventory;
            if (!config.IsEnabled || !config.inventoryFillTopToBottom) return;
            __result = true;
        }
    }

    /// <summary>
    /// Configure player inventory size as a minimum, since the game owns rows itself.
    /// Only the two calls that size the inventory and the GUI are raised, so the game
    /// still saves its own real row count.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetInventorySize))]
    public static class Player_SetInventorySize_Patch
    {
        private static readonly MethodInfo Method_Inventory_SetHeight =
            AccessTools.Method(typeof(Inventory), nameof(Inventory.SetHeight));

        private static readonly MethodInfo Method_InventoryGui_SetInventorySize =
            AccessTools.Method(typeof(InventoryGui), nameof(InventoryGui.SetInventorySize));

        private static readonly MethodInfo Method_AtLeastConfigured =
            AccessTools.Method(typeof(Player_SetInventorySize_Patch), nameof(AtLeastConfigured));

        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.Inventory.IsEnabled) return instructions;

            var il = instructions.ToList();
            try
            {
                // Raise the row count on the stack at each sizing call. The save between them,
                // AddUniqueKeyValue("invrows", rows.ToString()), is deliberately left alone.
                return new CodeMatcher(il)
                    .MatchStartForward(new CodeMatch(i => i.Calls(Method_Inventory_SetHeight)))
                    .ThrowIfNotMatch("No match for this.m_inventory.SetHeight(rows).")
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Method_AtLeastConfigured))
                    .MatchStartForward(new CodeMatch(i => i.Calls(Method_InventoryGui_SetInventorySize)))
                    .ThrowIfNotMatch("No match for InventoryGui.instance.SetInventorySize(rows).")
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Method_AtLeastConfigured))
                    .InstructionEnumeration();
            }
            catch (Exception e)
            {
                PatchLog.Failed(nameof(Player_SetInventorySize_Patch), "playerInventoryRows will have no effect.", e);
                return il;
            }
        }

        /// <summary>The configured rows, or the game's own count when that is larger.</summary>
        public static int AtLeastConfigured(int rows)
        {
            return Math.Max(rows, Configuration.Current.Inventory.playerInventoryRows);
        }
    }

    /// <summary>
    /// Size the inventory before its items load, since the game only applies rows on spawn.
    /// A character last saved before 1.0 stores its items in the old format, which drops
    /// anything below the current bottom row instead of loading it.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Load))]
    public static class Player_Load_InventorySize_Patch
    {
        [UsedImplicitly]
        public static void Prefix(Player __instance)
        {
            if (!Configuration.Current.Inventory.IsEnabled) return;
            if (__instance == null) return;

            // Height only, and no GUI, which is sized on spawn once InventoryGui exists.
            var inventory = __instance.GetInventory();
            int rows = Configuration.Current.Inventory.playerInventoryRows;
            if (inventory.GetHeight() < rows) inventory.SetHeight(rows);
        }
    }

    /// <summary>
    /// Size a fresh character, which has no saved rows for the game to size from, and match the
    /// GUI to whatever the inventory ended up at.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class Player_OnSpawned_InventorySize_Patch
    {
        [UsedImplicitly]
        public static void Postfix(Player __instance)
        {
            if (!Configuration.Current.Inventory.IsEnabled) return;
            if (__instance == null || __instance != Player.m_localPlayer) return;

            // Size directly, since SetInventorySize would save the config value as the character's own.
            // Basically call Player SetInventorySize but just what we need.
            var inventory = __instance.GetInventory();
            int rows = Math.Max(inventory.GetHeight(), Configuration.Current.Inventory.playerInventoryRows);
            inventory.SetHeight(rows);

            // Always, since the game leaves the GUI alone for a character it did not size itself.
            InventoryGui.instance.SetInventorySize(rows);
        }
    }

    public static class Inventory_NearbyChests_Cache
    {
        public static List<Container> chests = new();
        public static readonly Stopwatch delta = new();
    }

    // TODO isn't this fully trumped by the stack all feature now?
    /// <summary>
    /// When merging another inventory, try to merge items with existing stacks.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), "MoveAll")]
    public static class Inventory_MoveAll_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ref Inventory __instance, ref Inventory fromInventory)
        {
            var config = Configuration.Current.Inventory;
            if (!config.IsEnabled || !config.mergeWithExistingStacks) return;

            var otherInventoryItems = new List<ItemDrop.ItemData>(fromInventory.GetAllItems());
            foreach (var otherItem in otherInventoryItems)
            {
                if (otherItem.m_shared.m_maxStackSize <= 1) continue;

                foreach (var myItem in __instance.m_inventory)
                {
                    if (myItem.m_shared.m_name != otherItem.m_shared.m_name || myItem.m_quality != otherItem.m_quality)
                        continue;

                    int itemsToMove = Math.Min(myItem.m_shared.m_maxStackSize - myItem.m_stack, otherItem.m_stack);
                    myItem.m_stack += itemsToMove;
                    if (otherItem.m_stack == itemsToMove)
                    {
                        fromInventory.RemoveItem(otherItem);
                        break;
                    }

                    otherItem.m_stack -= itemsToMove;
                }
            }
        }
    }

    /// <summary>
    /// Auto Stack's sweep: asks every nearby chest holding a matching item at once, waits for the ones that
    /// answered to hand ownership over, then stacks into them nearest first. Chests that never answer or never
    /// arrive are dropped when the sweep cuts off.
    ///
    /// Ownership is the part that matters: Container only saves a chest it owns, so stacking into one we have
    /// been granted but do not own yet throws those items away.
    /// </summary>
    public static class AutoStackSweep
    {
        private static readonly List<Container> Candidates = new();
        private static readonly HashSet<Container> Pending = new();
        private static readonly HashSet<Container> Granted = new();
        private static readonly HashSet<Container> CutOff = new();
        private static int sweep;
        private static bool running;
        private static bool stacking;
        private static float deadline;
        private static float timeout;
        private static float startTime;
        private static int itemsMoved;
        private static int chestsStacked;
        private static int chestsMissed;
        private static bool effectPlayed;
        private static int skippedInUse;
        private static int skippedNoMatch;
        private static int skippedUnreadable;

        /// <summary>True while a sweep is still waiting on replies.</summary>
        public static bool IsRunning => running && Time.time < deadline;

        /// <summary>True while the sweep is stacking into its chests.</summary>
        public static bool IsStacking => stacking;

        /// <summary>Ask every candidate chest around the player for a stack.</summary>
        public static void Start(Player player, Container openChest, int movedIntoOpenChest)
        {
            var config = Configuration.Current.AutoStack;
            var id = ++sweep;
            running = true;
            timeout = config.replyTimeout;
            startTime = Time.time;
            deadline = Time.time + timeout;
            itemsMoved = movedIntoOpenChest;
            chestsStacked = movedIntoOpenChest > 0 ? 1 : 0;
            chestsMissed = 0;
            effectPlayed = false;
            skippedInUse = 0;
            skippedNoMatch = 0;
            skippedUnreadable = 0;
            Candidates.Clear();
            Pending.Clear();
            Granted.Clear();
            CutOff.Clear();

            var range = Mathf.Clamp(config.autoStackAllRange, 1, 50);

            // GetNearbyChests returns nearest first, which is the order we stack in.
            var nearby = InventoryAssistant.GetNearbyChests(player.gameObject, range,
                !config.autoStackAllIgnorePrivateAreaCheck, includeVehicles: false);
            Candidates.AddRange(nearby.Where(chest => chest != openChest && IsCandidate(chest, player)));

            // The open chest is counted separately, since it is filtered out before the candidate check.
            var openInRange = openChest && nearby.Contains(openChest) ? 1 : 0;
            Log(id, $"as session {ZDOMan.GetSessionID()}, range {range}, reply timeout {timeout}s: " +
                    $"{nearby.Count} chest(s) in range, {Candidates.Count} to ask, {openInRange} already open " +
                    $"holding {movedIntoOpenChest} item(s) of ours, {skippedInUse} in use, " +
                    $"{skippedNoMatch} with nothing to match, {skippedUnreadable} unreadable.");

            // Chests we own answer before StackAll returns, so every chest must be pending first.
            Pending.UnionWith(Candidates);
            foreach (var chest in Candidates)
            {
                try
                {
                    chest.StackAll();
                }
                catch (Exception e)
                {
                    Pending.Remove(chest);
                    chestsMissed++;
                    ValheimPlusPlugin.Logger.LogWarning($"Auto Stack could not ask '{NameOf(chest)}': {e}");
                }
            }

            // Runs through to the end here when every chest is ours, which is the single player case.
            player.StartCoroutine(Run(id, player));
        }

        /// <summary>Record a reply from one of the sweep's chests. False for anyone else's reply.</summary>
        public static bool HandleResponse(Container chest, bool granted)
        {
            // A reply after the cut-off changes nothing, but the game must not act on it either.
            if (CutOff.Remove(chest))
            {
                Log(sweep, $"ignored a late {(granted ? "grant" : "refusal")} from {Describe(chest)}.");
                return true;
            }

            if (!Pending.Remove(chest)) return false;

            if (granted)
            {
                Granted.Add(chest);
            }
            else
            {
                chestsMissed++;
                Log(sweep, $"was refused by {Describe(chest)}, which is in use or not ours to use.");
            }

            return true;
        }

        private static void StackInto(Container chest, Player player)
        {
            try
            {
                // The chest can change hands again between the grant and here, and a chest we do not own
                // discards what we put in it instead of saving it.
                if (!IsOwned(chest))
                {
                    chestsMissed++;
                    ValheimPlusPlugin.Logger.LogWarning(
                        $"Auto Stack put nothing into a chest it does not own: {Describe(chest)}");
                    return;
                }

                // Our copy can be a second old, and stacking saves the whole chest.
                chest.Load();

                var inventory = player.GetInventory();
                var revisionBefore = chest.m_nview.GetZDO().DataRevision;
                var beforeTotal = inventory.CountItems(null);

                chest.GetInventory().StackAll(inventory);

                var moved = beforeTotal - inventory.CountItems(null);
                var revisionAfter = chest.m_nview.GetZDO().DataRevision;
                Log(sweep, $"moved {moved} item(s) into {Describe(chest)}, rev {revisionBefore} -> {revisionAfter}.");

                // A chest only writes its contents when it is saved, and it only saves when we own it.
                // The ownership checks above should make this impossible, so say so loudly if it happens.
                if (moved > 0 && revisionAfter == revisionBefore)
                {
                    ValheimPlusPlugin.Logger.LogError(
                        $"Auto Stack moved {moved} item(s) into a chest that did not save them, so they are " +
                        $"lost: {Describe(chest)}");
                }

                if (moved <= 0) return;

                itemsMoved += moved;
                chestsStacked++;

                // One effect per sweep, instead of one per chest.
                if (effectPlayed || !InventoryGui.instance) return;
                InventoryGui.instance.m_moveItemEffects.Create(chest.transform.position, Quaternion.identity);
                effectPlayed = true;
            }
            catch (Exception e)
            {
                chestsMissed++;
                ValheimPlusPlugin.Logger.LogWarning($"Auto Stack failed on '{NameOf(chest)}': {e}");
            }
        }

        /// <summary>A loaded chest nobody has open, holding something the player would stack.</summary>
        private static bool IsCandidate(Container chest, Player player)
        {
            var view = chest.m_nview;
            if (!view || !view.IsValid())
            {
                skippedUnreadable++;
                return false;
            }

            // IsInUse is only set on the owner, so check what the owner shares with everyone else too.
            if (chest.IsInUse() || view.GetZDO().GetInt(ZDOVars.s_inUse) == 1)
            {
                skippedInUse++;
                return false;
            }

            // The game parses a chest's contents once a second, so refresh before matching against them.
            try
            {
                chest.Load();
            }
            catch (Exception e)
            {
                skippedUnreadable++;
                ValheimPlusPlugin.Logger.LogWarning($"Auto Stack could not read '{NameOf(chest)}': {e}");
                return false;
            }

            // Asking only matching chests avoids taking ownership of every chest in range.
            var chestInventory = chest.GetInventory();
            foreach (var item in player.GetInventory().GetAllItems())
            {
                if (!player.IsItemEquiped(item) &&
                    Inventory_StackAll_Patch.ContainsItemByName(chestInventory, item.m_shared.m_name))
                    return true;
            }

            skippedNoMatch++;
            return false;
        }

        /// <summary>True while the chest is ours to write to.</summary>
        private static bool IsOwned(Container chest) =>
            chest && chest.m_nview && chest.m_nview.IsValid() && chest.m_nview.IsOwner();

        /// <summary>
        /// Wait for the replies, then for the chests that granted one to actually become ours, then stack.
        /// Each wait gets its own replyTimeout.
        /// </summary>
        private static IEnumerator Run(int id, Player player)
        {
            var until = Time.time + timeout;
            while (Pending.Count > 0 && Time.time < until) yield return null;
            if (!running || id != sweep)
            {
                Log(id, "abandoned while waiting for replies, superseded by a newer sweep.");
                yield break;
            }

            if (Pending.Count > 0)
            {
                chestsMissed += Pending.Count;
                ValheimPlusPlugin.Logger.LogWarning(
                    $"Auto Stack gave up on {Pending.Count} chest(s) after {timeout}s: " +
                    string.Join(", ", Pending.Select(Describe).ToArray()));

                // Swallow their replies if they turn up, so nothing moves after the sweep has reported.
                CutOff.UnionWith(Pending);
                Pending.Clear();
            }
            else
            {
                Log(id, $"every reply in after {Ms}ms, {Granted.Count} granted.");
            }

            // A chest we already owned is granted on the spot, so this only waits on other players' chests.
            // Their owner hands the chest over in a separate ZDO update, which trails the reply it sent us.
            deadline = Time.time + timeout;
            while (Time.time < deadline && Granted.Any(chest => !IsOwned(chest))) yield return null;

            if (!running || id != sweep)
            {
                Log(id, "abandoned while waiting for ownership, superseded by a newer sweep.");
                yield break;
            }

            var stranded = Granted.Where(chest => !IsOwned(chest)).ToList();
            if (stranded.Count > 0)
            {
                ValheimPlusPlugin.Logger.LogWarning(
                    $"Auto Stack was granted {stranded.Count} chest(s) that never became ours within {timeout}s, " +
                    $"so nothing goes into them: {string.Join(", ", stranded.Select(Describe).ToArray())}");
            }
            else if (Granted.Count > 0)
            {
                Log(id, $"owns all {Granted.Count} granted chest(s) at {Ms}ms.");
            }

            Finish(id);
        }

        /// <summary>Stack into every chest the sweep owns, nearest first, then report.</summary>
        private static void Finish(int id)
        {
            if (!running || id != sweep) return;
            running = false;

            var player = Player.m_localPlayer;
            if (!player)
            {
                Granted.Clear();
                return;
            }

            stacking = true;
            try
            {
                foreach (var chest in Candidates)
                {
                    if (Granted.Contains(chest)) StackInto(chest, player);
                }
            }
            finally
            {
                stacking = false;
                Granted.Clear();
            }

            var message = itemsMoved > 0
                ? $"$msg_stackall {itemsMoved} in {chestsStacked} Chests"
                : "$msg_stackall_none";
            if (chestsMissed > 0) message += $", {chestsMissed} unavailable";
            player.Message(MessageHud.MessageType.Center, message);

            Log(id, $"done in {Ms}ms: {itemsMoved} item(s) into {chestsStacked} chest(s), " +
                    $"{chestsMissed} unavailable.");
        }

        private static string NameOf(Container chest) => chest ? chest.name : "a missing chest";

        /// <summary>Milliseconds since the sweep started.</summary>
        private static int Ms => Mathf.RoundToInt((Time.time - startTime) * 1000f);

        private static void Log(int id, string what) =>
            ValheimPlusPlugin.Logger.LogDebug($"Auto Stack sweep #{id} {what}");

        /// <summary>A chest with everything a sweep can go wrong over: who owns it, and how stale our copy is.</summary>
        public static string Describe(Container chest)
        {
            try
            {
                if (!chest) return "a missing chest";

                var view = chest.m_nview;
                if (!view || !view.IsValid()) return $"'{chest.name}' [no zdo]";

                var zdo = view.GetZDO();
                var owner = zdo.GetOwner();
                var whose = owner == 0L ? "nobody" : owner == ZDOMan.GetSessionID() ? "us" : "them";
                var inventory = chest.GetInventory();
                return $"'{chest.name}' [zdo {zdo.m_uid}, owner {owner} ({whose}), rev {zdo.DataRevision}, " +
                       $"inUse {zdo.GetInt(ZDOVars.s_inUse)}, " +
                       $"{(inventory == null ? "no" : inventory.NrOfItems().ToString())} items]";
            }
            catch (Exception e)
            {
                return $"a chest that could not be described: {e.Message}";
            }
        }
    }

    /// <summary>
    /// Starts an Auto Stack sweep from a Stack All into the open chest, and filters which items Stack All moves.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll))]
    public static class Inventory_StackAll_Patch
    {
        /// <summary>What the prefix saw, for a call that starts a sweep.</summary>
        public struct SweepStart
        {
            public bool Starts;
            public bool Message;
            public int ItemsBefore;
        }

        /// <summary>Hold back the game's message for a call that starts a sweep, which shows a summary instead.</summary>
        [UsedImplicitly]
        private static bool Prefix(Inventory __instance, Inventory fromInventory, ref bool message,
            out SweepStart __state)
        {
            __state = default;
            if (!IsStackAllIntoOpenChest(__instance, fromInventory)) return true;

            // The sweep stacking into a chest the player opened while it runs.
            if (AutoStackSweep.IsStacking) return true;

            // A sweep is still running, so do nothing rather than stack again.
            if (AutoStackSweep.IsRunning) return false;

            __state = new SweepStart
            {
                Starts = true,
                Message = message,
                ItemsBefore = fromInventory.CountItems(null)
            };
            message = false;
            return true;
        }

        /// <summary>Start the sweep over nearby chests.</summary>
        [UsedImplicitly]
        private static void Postfix(Inventory fromInventory, ref int __result, SweepStart __state)
        {
            if (!__state.Starts) return;

            var moved = __state.ItemsBefore - fromInventory.CountItems(null);

            // Without its message the game returns the chest's total, so give the caller what actually moved.
            if (__state.Message) __result = moved;

            AutoStackSweep.Start(Player.m_localPlayer, InventoryGui.instance.m_currentContainer, moved);
        }

        /// <summary>True for a Stack All from the player into the chest they have open.</summary>
        private static bool IsStackAllIntoOpenChest(Inventory chest, Inventory fromInventory)
        {
            if (!Configuration.Current.AutoStack.IsEnabled) return false;

            var player = Player.m_localPlayer;
            var gui = InventoryGui.instance;
            if (!player || !gui || !gui.m_currentContainer) return false;

            return fromInventory == player.GetInventory() && chest == gui.m_currentContainer.GetInventory();
        }

        private static readonly MethodInfo Method_Inventory_ContainsItemByName =
            AccessTools.Method(typeof(Inventory), nameof(Inventory.ContainsItemByName));

        private static readonly MethodInfo Method_ContainsItemByName =
            AccessTools.Method(typeof(Inventory_StackAll_Patch), nameof(ContainsItemByName));

        /// <summary>
        /// Replaces the game's Inventory.ContainsItemByName call with our own.
        /// Their method only checks for a match by name, where ours has an additional check for whether
        /// the item is equip-able.
        /// </summary>
        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var config = Configuration.Current.AutoStack;
            if (!config.IsEnabled) return instructions;
            if (!config.autoStackAllIgnoreEquipment && !config.ignoreFood && !config.ignoreAmmo && !config.ignoreMead)
                return instructions;

            var il = instructions.ToList();

            for (int i = 0; i < il.Count; ++i)
            {
                if (il[i].Calls(Method_Inventory_ContainsItemByName))
                {
                    il[i].operand = Method_ContainsItemByName;
                    return il.AsEnumerable();
                }
            }

            PatchLog.Failed(nameof(Inventory_StackAll_Patch), "Stack All will not match items by name.");
            return il.AsEnumerable();
        }

        public static bool ContainsItemByName(Inventory inventory, string name)
        {
            foreach (var item in inventory.m_inventory)
            {
                if (item.m_shared.m_name != name)
                    continue;

                if (Configuration.Current.AutoStack.ignoreAmmo && item.IsAmmo())
                    continue;

                if (Configuration.Current.AutoStack.ignoreFood && item.IsFood())
                    continue;

                if (Configuration.Current.AutoStack.ignoreMead && item.IsMead())
                    continue;

                if (Configuration.Current.AutoStack.autoStackAllIgnoreEquipment && item.IsEquipable())
                    continue;

                return true;
            }

            return false;
        }
    }
}
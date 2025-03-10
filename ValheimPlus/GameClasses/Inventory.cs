using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;

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
    /// Configure player inventory size
    /// </summary>
    [HarmonyPatch(typeof(Inventory), MethodType.Constructor, typeof(string), typeof(Sprite), typeof(int), typeof(int))]
    public static class Inventory_Constructor_Patch
    {
        private const int PlayerInventoryMaxRows = 20;
        private const int PlayerInventoryMinRows = 4;

        [UsedImplicitly]
        public static void Prefix(string name, ref int w, ref int h)
        {
            if (!Configuration.Current.Inventory.IsEnabled) return;

            // Player inventory
            if (name is "Grave" or "Inventory")
            {
                h = Helper.Clamp(value: Configuration.Current.Inventory.playerInventoryRows,
                    min: PlayerInventoryMinRows,
                    max: PlayerInventoryMaxRows);
            }
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
    /// StackAll will have one setup step when Inventory.StackAll() is called in Inventory_StackAll_Patch#Prefix.
    /// At the end of the prefix, we start the loop of dequeuing containers that we are stacking to.
    /// The loop will consist of:
    ///   Dequeue containers from the queue, skipping those that are the current inventory or all already in use.
    ///   Call StackAll on the container, which will fire off an RPC (RPC_RequestStack).
    ///   We eventually will receive an RPC_StackResponse result. If valid it will call
    ///     Inventory.StackAll(Inventory fromInventory, bool message) which actually does the stacking logic.
    ///   At the end of Container.RPC_StackResponse we apply a Postfix (Container_RPC_StackResponse_Patch#Postfix)
    ///     that will deque the next container. (now go back to the beginning of the loop)
    /// At the end of the loop, we will display a message of what we stacked and then reset the variables. 
    /// </summary>
    public static class StackAllQueueState
    {
        private static Inventory _currentInventory;
        private static int _lastPlayerItemCount;
        private static Queue<Container> _containerQueue;
        private static int _containerCount;

        public static bool isActive => _currentInventory != null;

        public static void Setup(Inventory fromInventory, List<Container> targetContainers)
        {
            if (isActive) return;
            
            _currentInventory = fromInventory;
            _lastPlayerItemCount = _currentInventory.CountItems(null);
            _containerQueue = new Queue<Container>(targetContainers);
            _containerCount = _containerQueue.Count;
        }

        /// <summary>
        /// Call StackAll on the next valid container.
        /// </summary>
        public static void StackAllNextContainer()
        {
            if (!isActive) return;

            while (_containerQueue.Count > 0)
            {
                var container = _containerQueue.Dequeue();
                if (container.m_inventory == _currentInventory || container.IsInUse()) continue;
                container.StackAll();
                break;
            }

            if (_containerQueue.Count == 0) FinishStacking();
        }

        private static void FinishStacking()
        {
            if (!isActive) return;
            
            // Show stack message
            int itemCount = _lastPlayerItemCount - _currentInventory.CountItems(null);
            string message = itemCount > 0
                ? $"$msg_stackall {itemCount} in {_containerCount} Chests"
                : $"$msg_stackall_none in {_containerCount} Chests";

            Player.m_localPlayer.Message(MessageHud.MessageType.Center, message);

            // disable stack recursion bypass
            _currentInventory = null;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll))]
    public static class Inventory_StackAll_Patch
    {
        /// <summary>
        /// Start the auto stack all loop and suppress stack feedback message
        /// </summary>
        [UsedImplicitly]
        private static void Prefix(Inventory fromInventory, ref bool message)
        {
            var config = Configuration.Current.AutoStack;
            if (!config.IsEnabled) return;

            // disable message
            message = false;

            // This method will be called every time we stack all to a different container,
            // so if we are already AutoStacking, then don't begin another AutoStacking.
            if (StackAllQueueState.isActive) return;

            // get chests in range
            var nearbyChests = InventoryAssistant.GetNearbyChests(Player.m_localPlayer.gameObject,
                Helper.Clamp(config.autoStackAllRange, 1, 50),
                !config.autoStackAllIgnorePrivateAreaCheck);

            StackAllQueueState.Setup(fromInventory: fromInventory, targetContainers: nearbyChests);

            // start the StackAll loop
            StackAllQueueState.StackAllNextContainer();
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

            ValheimPlusPlugin.Logger.LogError("Could not transpile `Inventory.ContainsItemByName`!");
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
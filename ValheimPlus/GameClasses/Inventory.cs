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

    public static class StackAllQueueState {
        public static Inventory _currentInventory;
        public static List<Container> _containerQueue;
        public static int _containerCount;
        public static int _lastPlayerItemCount;
        public static void DequeueContainer()
        {
            // finish up if all chests done.
            if (_containerQueue.Count <= 0)
            {
                FinishStacking();
                return;
            }
            // pop from queue
            var container = _containerQueue.First();
            _containerQueue.RemoveAt(0);

            // check for active inventory or in use before calling RPC and skip
            if (container.m_inventory == _currentInventory || container.IsInUse())
            {
                DequeueContainer();
                return;
            }

            // call StackAll on container
            container.StackAll();
        }

        public static void FinishStacking()
        {
            if (_containerQueue.Count > 0)
            {
                return;
            }

            // Show stack message
            int itemCount = _lastPlayerItemCount - Player.m_localPlayer.m_inventory.CountItems(null);
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
        private static void Prefix(Inventory __instance, ref bool message)
        {
            if (!Configuration.Current.AutoStack.IsEnabled) return;

            // disable message
            message = false;
            if (StackAllQueueState._currentInventory != null) return;

            // enable stack recursion bypass and reset count
            StackAllQueueState._lastPlayerItemCount = Player.m_localPlayer.m_inventory.CountItems(null);
            StackAllQueueState._currentInventory = __instance;

            // get chests in range
            var gameObj = Player.m_localPlayer.gameObject;
            StackAllQueueState._containerQueue = InventoryAssistant.GetNearbyChests(gameObj,
                Helper.Clamp(Configuration.Current.AutoStack.autoStackAllRange, 1, 50),
                !Configuration.Current.AutoStack.autoStackAllIgnorePrivateAreaCheck);
            StackAllQueueState._containerCount = StackAllQueueState._containerQueue.Count;

            StackAllQueueState.DequeueContainer();
        }

        private static readonly MethodInfo Method_Inventory_ContainsItemByName =
            AccessTools.Method(typeof(Inventory), nameof(Inventory.ContainsItemByName));

        private static readonly MethodInfo Method_ContainsItemByName = 
            AccessTools.Method(typeof(Inventory_StackAll_Patch), nameof(ContainsItemByName));

        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var config = Configuration.Current.AutoStack;
            if (!config.IsEnabled || !config.autoStackAllIgnoreEquipment) return instructions;

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

        public static bool ContainsItemByName(Inventory inventory, string name) =>
            inventory.m_inventory.Any(item => !item.IsEquipable() && item.m_shared.m_name == name);
    }
}
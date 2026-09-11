using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus
{
    static class InventoryAssistant
    {

        /// <summary>
        /// Get all valid nearby chests
        /// </summary>
        public static List<Container> GetNearbyChests(GameObject target, float range, bool checkWard = true)
        {
            // On clients, container/ward permissions are checked against the local player.
            // Dedicated servers never have Player.m_localPlayer, but can own production pieces.
            // In that case use the creator of the machine/piece as the acting player so
            // server-owned auto-deposit and auto-fuel can still find permitted containers.
            Player localPlayer = Player.m_localPlayer;
            long accessPlayerId;
            if (localPlayer)
            {
                accessPlayerId = localPlayer.GetPlayerID();
            }
            else
            {
                Piece sourcePiece = target ? target.GetComponentInParent<Piece>() : null;
                if (!sourcePiece)
                    return new List<Container>();

                accessPlayerId = sourcePiece.GetCreator();
                if (accessPlayerId == 0L)
                    return new List<Container>();
            }

            // item == cart layermask
            // vehicle == cart&ship layermask

            string[] layerMask = { "piece" };

            if (Configuration.Current.CraftFromChest.allowCraftingFromCarts || Configuration.Current.CraftFromChest.allowCraftingFromShips)
                layerMask = new string[] { "piece", "item", "vehicle" };

            Collider[] hitColliders = Physics.OverlapSphere(target.transform.position, range, LayerMask.GetMask(layerMask));

            // Order the found objects to select the nearest first instead of the farthest inventory.
            IOrderedEnumerable<Collider> orderedColliders = hitColliders.OrderBy(x => Vector3.Distance(x.gameObject.transform.position, target.transform.position));

            List<Container> validContainers = new List<Container>();
            foreach (var hitCollider in orderedColliders)
            {
                try
                {
                    Container foundContainer = hitCollider.GetComponentInParent<Container>();

                    if (!foundContainer)
                        continue;
                    
                    // Verify the container has loaded its inventory from the ZDO at least once.
                    // Without this, we could perform operations on a container that has not yet loaded its inventory,
                    // which could result in overwriting the container's saved contents with an empty inventory.
                    // Value starts at uint.MaxValue, the first revision is set to 0 and monotonically increases by 1.
                    if (foundContainer.m_lastRevision == uint.MaxValue)
                        continue;
                    
                    if (validContainers.Contains(foundContainer))
                        continue;

                    bool hasAccess = foundContainer.CheckAccess(accessPlayerId);
                    if (checkWard)
                    {
                        hasAccess = hasAccess && (localPlayer
                            ? PrivateArea.CheckAccess(hitCollider.gameObject.transform.position, 0f, false, true)
                            : CheckWardAccessForPlayer(hitCollider.gameObject.transform.position, 0f, accessPlayerId));
                    }
                    var piece = foundContainer.GetComponentInParent<Piece>();
                    var isVagon = foundContainer.GetComponentInParent<Vagon>() != null;
                    var isShip = foundContainer.GetComponentInParent<Ship>() != null;

                    if (piece != null
                        /*&& piece.IsPlacedByPlayer() Prevents detection of ship storage */
                        && hasAccess
                        && foundContainer.GetInventory() != null)
                    {

                        if (isVagon && !Configuration.Current.CraftFromChest.allowCraftingFromCarts)
                            continue;
                        if (isShip && !Configuration.Current.CraftFromChest.allowCraftingFromShips)
                            continue;

                        if (piece.IsPlacedByPlayer() || (isShip && Configuration.Current.CraftFromChest.allowCraftingFromShips))
                            validContainers.Add(foundContainer);
                    }
                }
                catch (Exception e)
                {
                    // Skip the container rather than abandon the search, but say what went wrong.
                    ValheimPlusPlugin.Logger.LogDebug(
                        $"GetNearbyChests skipped '{hitCollider.gameObject.name}': {e}");
                }
            }

            return validContainers;
        }

        private static bool CheckWardAccessForPlayer(Vector3 position, float radius, long playerId)
        {
            // Mirrors PrivateArea.CheckAccess(..., wardCheck: true), but does not depend on
            // Player.m_localPlayer. Every enabled ward covering the container must permit
            // the creator of the machine performing the automatic transfer.
            foreach (PrivateArea area in PrivateArea.m_allAreas)
            {
                if (!area || !area.IsEnabled() || !area.IsInside(position, radius))
                    continue;

                if (!area.IsPermitted(playerId))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get a chests that contain the specified itemInfo.m_shared.m_name (item name)
        /// </summary>
        public static List<Container> GetNearbyChestsWithItem(GameObject target, float range, ItemDrop.ItemData itemInfo, bool checkWard = true)
        {
            List<Container> nearbyChests = GetNearbyChests(target, range, checkWard);

            List<Container> validChests = new List<Container>();
            foreach (Container chest in nearbyChests)
            {
                if (ChestContainsItem(chest, itemInfo))
                {
                    validChests.Add(chest);
                }
            }

            return validChests;
        }

        /// <summary>
        /// Check if the container contains the itemInfo.m_shared.name (item name)
        /// </summary>
        public static bool ChestContainsItem(Container chest, ItemDrop.ItemData needle)
        {
            List<ItemDrop.ItemData> items = chest.GetInventory().GetAllItems();

            foreach (ItemDrop.ItemData item in items)
            {
                if (item.m_shared.m_name == needle.m_shared.m_name)
                    return true;
            }

            return false;
        }

        public static bool ChestContainsItem(Container chest, string needle)
        {
            List<ItemDrop.ItemData> items = chest.GetInventory().GetAllItems();

            foreach (ItemDrop.ItemData item in items)
            {
                if (item.m_shared.m_name == needle)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// function to get all items in nearby chests by range
        /// </summary>
        public static List<ItemDrop.ItemData> GetNearbyChestItems(GameObject target, float range = 10, bool checkWard = true)
        {
            List<ItemDrop.ItemData> itemList = new List<ItemDrop.ItemData>();
            List<Container> nearbyChests = GetNearbyChests(target, range, checkWard);

            foreach (Container chest in nearbyChests)
            {
                List<ItemDrop.ItemData> chestItemList = chest.GetInventory().GetAllItems();
                foreach (ItemDrop.ItemData item in chestItemList)
                    itemList.Add(item);
            }

            return itemList;
        }

        /// <summary>
        /// function to get all items in chest list
        /// </summary>
        public static List<ItemDrop.ItemData> GetNearbyChestItemsByContainerList(List<Container> nearbyChests)
        {
            List<ItemDrop.ItemData> itemList = new List<ItemDrop.ItemData>();

            foreach (Container chest in nearbyChests)
            {
                List<ItemDrop.ItemData> chestItemList = chest.GetInventory().GetAllItems();
                foreach (ItemDrop.ItemData item in chestItemList)
                    itemList.Add(item);
            }

            return itemList;
        }

        /// <summary>
        /// function to get the amount of a specific item in a list of ItemDrop.ItemData
        /// </summary>
        public static int GetItemAmountInItemList(List<ItemDrop.ItemData> itemList, ItemDrop.ItemData item,
            int quality = -1, bool matchWorldLevel = true) => itemList
            .Where(current => current.m_shared.m_name == item.m_shared.m_name &&
                              (quality < 0 || quality == current.m_quality) &&
                              (!matchWorldLevel || current.m_worldLevel >= Game.m_worldLevel))
            .Sum(current => current.m_stack);

        public static int GetItemAmountInItemList(List<ItemDrop.ItemData> itemList, string name,
            int quality = -1, bool matchWorldLevel = true) => itemList
            .Where(current => current.m_shared.m_name == name &&
                              (quality < 0 || quality == current.m_quality) &&
                              (!matchWorldLevel || current.m_worldLevel >= Game.m_worldLevel))
            .Sum(current => current.m_stack);

        // function to remove items in the amount from all nearby chests
        public static int RemoveItemInAmountFromAllNearbyChests(GameObject target, float range, ItemDrop.ItemData needle, int amount, bool checkWard = true)
        {
            List<Container> nearbyChests = GetNearbyChests(target, range, checkWard);

            // check if there are enough items nearby
            List<ItemDrop.ItemData> allItems = GetNearbyChestItemsByContainerList(nearbyChests);

            // get amount of item
            int availableAmount = GetItemAmountInItemList(allItems, needle);

            // check if there are enough items
            if (amount == 0)
                return 0;

            // iterate all chests and remove as many items as possible for the respective chest
            int itemsRemovedTotal = 0;
            foreach (Container chest in nearbyChests)
            {
                if (itemsRemovedTotal != amount)
                {
                    int removedItems = RemoveItemFromChest(chest, needle, amount);
                    itemsRemovedTotal += removedItems;
                    amount -= removedItems;
                }
            }

            return itemsRemovedTotal;
        }

        public static int RemoveItemInAmountFromAllNearbyChests(GameObject target, float range, string needle, int amount, bool checkWard = true)
        {
            List<Container> nearbyChests = GetNearbyChests(target, range, checkWard);

            // check if there are enough items nearby
            List<ItemDrop.ItemData> allItems = GetNearbyChestItemsByContainerList(nearbyChests);

            // get amount of item
            int availableAmount = GetItemAmountInItemList(allItems, needle);

            // check if there are enough items
            if (amount == 0)
                return 0;

            // iterate all chests and remove as many items as possible for the respective chest
            int itemsRemovedTotal = 0;
            foreach (Container chest in nearbyChests)
            {
                if (itemsRemovedTotal != amount)
                {
                    int removedItems = RemoveItemFromChest(chest, needle, amount);
                    itemsRemovedTotal += removedItems;
                    amount -= removedItems;
                }
            }

            return itemsRemovedTotal;
        }

        // function to add a item by name/ItemDrop.ItemData to a specified chest

        /// <summary>
        /// Removes the specified amount of a item found by m_shared.m_name by the declared amount
        /// </summary>
        public static int RemoveItemFromChest(Container chest, ItemDrop.ItemData needle, int amount = 1)
        {
            if (!ChestContainsItem(chest, needle))
            {
                return 0;
            }

            int totalRemoved = 0;
            // find item
            List<ItemDrop.ItemData> allItems = chest.GetInventory().GetAllItems();
            foreach (ItemDrop.ItemData itemData in allItems)
            {
                if (itemData.m_shared.m_name == needle.m_shared.m_name)
                {
                    int num = Mathf.Min(itemData.m_stack, amount);
                    itemData.m_stack -= num;
                    amount -= num;
                    totalRemoved += num;
                    if (amount <= 0)
                    {
                        break;
                    }
                }
            }

            // We don't want to send chest content through network
            if (totalRemoved == 0) return 0;

            allItems.RemoveAll((ItemDrop.ItemData x) => x.m_stack <= 0);
            chest.m_inventory.m_inventory = allItems;

            ConveyContainerToNetwork(chest);

            return totalRemoved;
        }

        public static int RemoveItemFromChest(Container chest, string needle, int amount = 1)
        {
            if (!ChestContainsItem(chest, needle))
            {
                return 0;
            }

            int totalRemoved = 0;
            // find item
            List<ItemDrop.ItemData> allItems = chest.GetInventory().GetAllItems();
            foreach (ItemDrop.ItemData itemData in allItems)
            {
                if (itemData.m_shared.m_name == needle)
                {
                    int num = Mathf.Min(itemData.m_stack, amount);
                    itemData.m_stack -= num;
                    amount -= num;
                    totalRemoved += num;
                    if (amount <= 0)
                    {
                        break;
                    }
                }
            }

            // We don't want to send chest content through network
            if (totalRemoved == 0) return 0;

            allItems.RemoveAll((ItemDrop.ItemData x) => x.m_stack <= 0);
            chest.m_inventory.m_inventory = allItems;

            ConveyContainerToNetwork(chest);

            return totalRemoved;
        }

        /// <summary>
        /// Function to convey the changes of a container to the network
        /// </summary>
        public static void ConveyContainerToNetwork(Container c)
        {
            c.Save();
            c.GetInventory().Changed();
        }
    }
}

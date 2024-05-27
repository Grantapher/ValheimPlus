using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Recipe), nameof(Recipe.GetAmount))]
    public static class Recipe_GetAmount_Transpiler
    {
        
        private static List<Container> nearbyChests = null;
        private static MethodInfo method_Player_GetFirstRequiredItem = AccessTools.Method(typeof(Player), nameof(Player.GetFirstRequiredItem));
        private static MethodInfo method_GetFirstRequiredItemFromNearbyChests = AccessTools.Method(typeof(Recipe_GetAmount_Transpiler), nameof(Recipe_GetAmount_Transpiler.GetFirstRequiredItem));
        

        /// <summary>
        /// A fix for the fishy partial recipe bug. https://github.com/Grantapher/ValheimPlus/issues/40
        /// </summary>
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.CraftFromChest.IsEnabled) return instructions;
            List<CodeInstruction> il = instructions.ToList();
            int endIdx = -1;
            for (int i = 0; i < il.Count; i++)
            {
                if (il[i].Calls(method_Player_GetFirstRequiredItem))
                {
                    il[i] = new CodeInstruction(OpCodes.Call, method_GetFirstRequiredItemFromNearbyChests);
                    endIdx = i;
                    break;
                }
            }
            if (endIdx == -1)
            {
                return instructions;
            }

            return il.AsEnumerable();
        }

        private static ItemDrop.ItemData GetFirstRequiredItem(Player player, Inventory inventory, Recipe recipe, int qualityLevel, out int amount, out int extraAmount)
        {
            // call the old method first
            ItemDrop.ItemData result = player.GetFirstRequiredItem(inventory, recipe, qualityLevel, out amount, out extraAmount);

            if(result != null) {
                // we found items on the player
                return result;
            } else {
                // need a game object here. Do not know if the player is a good choice for this. But i have no refference to the crafting station.
                Stopwatch delta = GameObjectAssistant.GetStopwatch(player.gameObject);
                int lookupInterval = Helper.Clamp(Configuration.Current.CraftFromChest.lookupInterval, 1, 10) * 1000;
                if (!delta.IsRunning || delta.ElapsedMilliseconds > lookupInterval)
                {
                    nearbyChests = InventoryAssistant.GetNearbyChests(player.gameObject, Helper.Clamp(Configuration.Current.CraftFromChest.range, 1, 50), !Configuration.Current.CraftFromChest.ignorePrivateAreaCheck);
                    delta.Restart();
                }

                // try to find them inside chests.
                Piece.Requirement[] resources = recipe.m_resources;
                foreach (Container c in nearbyChests)
                {
                    if(!c) continue;
                    foreach (Piece.Requirement requirement in resources)
                    {
                        if (!requirement.m_resItem)
                        {
                            continue;
                        }

                        int amount2 = requirement.GetAmount(qualityLevel);
                        for (int j = 0; j <= requirement.m_resItem.m_itemData.m_shared.m_maxQuality; j++)
                        {
                            if (c.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name, j) >= amount2)
                            {
                                amount = amount2;
                                extraAmount = requirement.m_extraAmountOnlyOneIngredient;
                                return c.m_inventory.GetItem(requirement.m_resItem.m_itemData.m_shared.m_name, j);
                            }
                        }
                    }
                }
            }

            amount = 0;
            extraAmount = 0;
            return null;
        }
    }
}

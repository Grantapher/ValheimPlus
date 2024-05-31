using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Recipe), nameof(Recipe.GetAmount))]
    public static class Recipe_GetAmount_Transpiler
    {
        private static List<Container> nearbyChests;

        private static readonly MethodInfo Method_Player_GetFirstRequiredItem =
            AccessTools.Method(typeof(Player), nameof(Player.GetFirstRequiredItem));

        private static readonly MethodInfo Method_GetFirstRequiredItemFromNearbyChests =
            AccessTools.Method(typeof(Recipe_GetAmount_Transpiler), nameof(GetFirstRequiredItem));


        /// <summary>
        /// A fix for the fishy partial recipe bug. https://github.com/Grantapher/ValheimPlus/issues/40
        /// Adds support for recipes with multiple sets of required items.
        /// </summary>
        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.CraftFromChest.IsEnabled) return instructions;
            var il = instructions.ToList();
            for (int i = 0; i < il.Count; i++)
            {
                if (il[i].Calls(Method_Player_GetFirstRequiredItem))
                {
                    il[i] = new CodeInstruction(OpCodes.Call, Method_GetFirstRequiredItemFromNearbyChests);
                    return il.AsEnumerable();
                }
            }

            ValheimPlusPlugin.Logger.LogError("Couldn't transpile `Recipe.GetAmount`!");
            return il.AsEnumerable();
        }

        private static ItemDrop.ItemData GetFirstRequiredItem(Player player, Inventory inventory, Recipe recipe,
            int qualityLevel, out int amount, out int extraAmount)
        {
            // call the old method first
            var result = player.GetFirstRequiredItem(inventory, recipe, qualityLevel, out amount, out extraAmount);
            if (result != null)
            {
                // we found items on the player
                return result;
            }

            var craftingStationGameObj = recipe.m_craftingStation.gameObject;
            var stopwatch = GameObjectAssistant.GetStopwatch(craftingStationGameObj);
            int lookupInterval = Helper.Clamp(Configuration.Current.CraftFromChest.lookupInterval, 1, 10) * 1000;
            if (!stopwatch.IsRunning || stopwatch.ElapsedMilliseconds > lookupInterval)
            {
                nearbyChests = InventoryAssistant.GetNearbyChests(craftingStationGameObj,
                    Helper.Clamp(Configuration.Current.CraftFromChest.range, 1, 50),
                    !Configuration.Current.CraftFromChest.ignorePrivateAreaCheck);
                stopwatch.Restart();
            }

            // try to find them inside chests.
            var requirements = recipe.m_resources;
            foreach (var chest in nearbyChests)
            {
                if (!chest) continue;
                foreach (var requirement in requirements)
                {
                    if (!requirement.m_resItem) continue;

                    int requiredAmount = requirement.GetAmount(qualityLevel);
                    var requirementSharedItemData = requirement.m_resItem.m_itemData.m_shared;
                    for (int i = 0; i <= requirementSharedItemData.m_maxQuality; i++)
                    {
                        var requirementName = requirementSharedItemData.m_name;
                        if (chest.m_inventory.CountItems(requirementName, i) < requiredAmount) continue;

                        amount = requiredAmount;
                        extraAmount = requirement.m_extraAmountOnlyOneIngredient;
                        return chest.m_inventory.GetItem(requirementName, i);
                    }
                }
            }

            amount = 0;
            extraAmount = 0;
            return null;
        }
    }
}
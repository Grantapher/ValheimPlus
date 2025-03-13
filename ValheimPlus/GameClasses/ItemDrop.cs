using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    public static class ItemDataExtensions
    {
        public static bool IsAmmo(this ItemDrop.ItemData itemData) =>
            !string.IsNullOrEmpty(itemData.m_shared.m_ammoType)
            && !itemData.m_shared.m_ammoType.EndsWith("turretbolt");

        public static bool IsFood(this ItemDrop.ItemData itemData) =>
            itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable
            && (itemData.m_shared.m_food > 0
            || itemData.m_shared.m_foodEitr > 0
            || itemData.m_shared.m_foodStamina > 0);

        public static bool IsMead(this ItemDrop.ItemData itemData) => itemData.m_shared.m_isDrink;
    }

    /// <summary>
    /// Item weight reduction and teleport prevention changes
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
    public static class ItemDrop_Awake_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ref ItemDrop __instance)
        {
            var config = Configuration.Current.Items;
            if (!config.IsEnabled) return;

            var sharedItemData = __instance.m_itemData.m_shared;
            sharedItemData.m_weight =
                Helper.applyModifierValue(sharedItemData.m_weight, config.baseItemWeightReduction);

            if (config.noTeleportPrevention) sharedItemData.m_teleportable = true;

            if (sharedItemData.m_maxStackSize > 1 && config.itemStackMultiplier >= 1)
            {
                sharedItemData.m_maxStackSize = 
                    (int)Helper.applyModifierValue(sharedItemData.m_maxStackSize, config.itemStackMultiplier);
            }

            // Add floating property to all dropped items.
            var gameObject = __instance.gameObject;
            if (config.itemsFloatInWater && gameObject.GetComponent<ZNetView>() && !gameObject.GetComponent<Floating>())
            {
                gameObject.AddComponent<Floating>().m_waterLevelOffset = 0.5f;
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.TimedDestruction))]
    public static class ItemDrop_TimedDestruction_Patch
    {
        private const int defaultSpawnTimeSeconds = 3600;
        private static MethodInfo method_SetDroppedItemDestroyDuration = AccessTools.Method(typeof(ItemDrop_TimedDestruction_Patch), nameof(ItemDrop_TimedDestruction_Patch.SetDroppedItemDestroyDuration));

        /// <summary>
        /// Patches the function that checks if an ItemDrop should be destroyed by changing the value being compared.
        /// </summary>
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.Items.IsEnabled) return instructions;

            List<CodeInstruction> il = instructions.ToList();
            for (int i = 0; i < il.Count; i++)
            {
                if (il[i].opcode == OpCodes.Ldc_R8)
                {
                    il[i] = new CodeInstruction(OpCodes.Call, method_SetDroppedItemDestroyDuration);
                    return il.AsEnumerable();
                }
            }

            ValheimPlusPlugin.Logger.LogError("Failed to apply ItemDrop_TimedDestruction_Patch");

            return instructions;
        }

        private static float SetDroppedItemDestroyDuration()
        {
            if (!Player.m_localPlayer)
                return defaultSpawnTimeSeconds;
            return Helper.Clamp(Configuration.Current.Items.droppedItemOnGroundDurationInSeconds, 0, defaultSpawnTimeSeconds);
        }
    }


    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetMaxDurability", new System.Type[] { typeof(int) })]
    public static class ItemDrop_GetMaxDurability_Patch
    {
        private static bool Prefix(ref ItemDrop.ItemData __instance, ref int quality, ref float __result)
        {
            if (!Configuration.Current.Durability.IsEnabled)
                return true;

            string itemName = __instance.m_shared.m_name.Replace("$item_", "");
            string itemType = itemName.Split(new char[] { '_' })[0];

            float maxDurability = (__instance.m_shared.m_maxDurability + (float)Mathf.Max(0, quality - 1) * __instance.m_shared.m_durabilityPerLevel);
            __result = maxDurability;
            float multiplierForItem = maxDurability;


            bool modified = false;
            switch (itemType)
            {

                // pickaxes
                case "pickaxe":
                    modified = true;
                    multiplierForItem = Helper.applyModifierValue(maxDurability, Configuration.Current.Durability.pickaxes);
                    break;

                // axes
                case "axe":
                    modified = true;
                    multiplierForItem = Helper.applyModifierValue(maxDurability, Configuration.Current.Durability.axes);
                    break;

                // hammer
                case "hammer":
                    modified = true;
                    multiplierForItem = Helper.applyModifierValue(maxDurability, Configuration.Current.Durability.hammer);
                    break;

                // cultivator
                case "cultivator":
                    modified = true;
                    multiplierForItem = Helper.applyModifierValue(maxDurability, Configuration.Current.Durability.cultivator);
                    break;

                // hoe
                case "hoe":
                    modified = true;
                    multiplierForItem = Helper.applyModifierValue(maxDurability, Configuration.Current.Durability.hoe);
                    break;

                case "torch":
                    modified = true;
                    multiplierForItem = Helper.applyModifierValue(maxDurability, Configuration.Current.Durability.torch);
                    break;

                default:
                    break;
            }

            switch (__instance.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                    // WEAPONS
                    if (!modified) // Some tools are considered to be OneHandedWeapons
                        multiplierForItem = Helper.applyModifierValue(maxDurability, Configuration.Current.Durability.weapons);
                    break;
                case ItemDrop.ItemData.ItemType.Bow:
                    // BOW
                    if (!modified)
                        multiplierForItem = Helper.applyModifierValue(maxDurability, Configuration.Current.Durability.bows);
                    break;
                case ItemDrop.ItemData.ItemType.Shield:
                    // Shields
                    if (!modified)
                        multiplierForItem = Helper.applyModifierValue(maxDurability, Configuration.Current.Durability.shields);
                    break;
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                    // ARMOR
                    if (!modified && __instance.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield)
                        multiplierForItem = Helper.applyModifierValue(maxDurability, Configuration.Current.Durability.armor);
                    break;
                default:
                    break;
            }



            if (multiplierForItem != maxDurability)
                __result = multiplierForItem;

            return false;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetArmor", new System.Type[] { typeof(int), typeof(float) })]
    public static class ItemDrop_GetArmor_Patch
    {
        private static void Postfix(ref ItemDrop.ItemData __instance, ref float __result)
        {
            if (!Configuration.Current.Armor.IsEnabled) return;

            switch (__instance.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Helmet:
                    __result = Helper.applyModifierValue(__result, Configuration.Current.Armor.helmets);
                    break;
                case ItemDrop.ItemData.ItemType.Chest:
                    __result = Helper.applyModifierValue(__result, Configuration.Current.Armor.chests);
                    break;
                case ItemDrop.ItemData.ItemType.Legs:
                    __result = Helper.applyModifierValue(__result, Configuration.Current.Armor.legs);
                    break;
                case ItemDrop.ItemData.ItemType.Shoulder:
                    __result = Helper.applyModifierValue(__result, Configuration.Current.Armor.capes);
                    break;
                default:
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetBaseBlockPower", new System.Type[] { typeof(int) })]
    public static class ItemDrop_GetBaseBlockPower_Patch
    {
        private static bool Prefix(ref ItemDrop.ItemData __instance, ref int quality, ref float __result)
        {
            if (!Configuration.Current.Shields.IsEnabled)
                return true;

            float blockValue = __instance.m_shared.m_blockPower + (float)Mathf.Max(0, quality - 1) * __instance.m_shared.m_blockPowerPerLevel;
            __result = Helper.applyModifierValue(blockValue, Configuration.Current.Shields.blockRating);
            return false;
        }
    }
}

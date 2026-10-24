using HarmonyLib;
using System;
using System.Collections.Generic;
using ValheimPlus.Configurations;
using ValheimPlus.Configurations.Sections;

namespace ValheimPlus
{
    /// <summary>
    /// Modify drops to increase their amount and chances.
    /// </summary>
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    public static class CharacterDrop_GenerateDropList_Patch
    {
        private static void Prefix(ref List<CharacterDrop.Drop> ___m_drops, out List<CharacterDrop.Drop> __state)
        {
            __state = null;
            var loot = Configuration.Current.LootDrop;
            var moulds = Configuration.Current.Moulds;
            bool lootEnabled = loot.IsEnabled;
            bool mouldsEnabled = moulds.IsEnabled;
            if (!lootEnabled && !mouldsEnabled) return;

            var modifiedDrops = new List<CharacterDrop.Drop>(___m_drops.Count);
            bool changed = lootEnabled;
            foreach (var originalDrop in ___m_drops)
            {
                // Non-item drops include spawned creatures and corpses; leave those intact.
                if (originalDrop.m_prefab == null || !originalDrop.m_prefab.TryGetComponent<ItemDrop>(out var unused))
                {
                    modifiedDrops.Add(originalDrop);
                    continue;
                }

                float mouldModifier = mouldsEnabled ? GetMouldModifier(originalDrop.m_prefab.name, moulds) : 0f;
                bool modifyMould = mouldModifier != 0f && !float.IsNaN(mouldModifier);
                if (!lootEnabled && !modifyMould)
                {
                    modifiedDrops.Add(originalDrop);
                    continue;
                }

                float chance = lootEnabled
                    ? Helper.applyModifierValue(originalDrop.m_chance, loot.lootDropChanceMultiplier)
                    : originalDrop.m_chance;
                if (modifyMould)
                {
                    changed = true;
                    chance = Helper.applyModifierValue(chance, mouldModifier);
                    // The game's pseudo-drop path can award a zero-chance entry. Omit
                    // disabled moulds entirely, including invalid nonpositive results.
                    if (!(chance > 0f)) continue;
                    // Above 100% is already guaranteed; bound extreme values before native math.
                    chance = Math.Min(1f, chance);
                }

                modifiedDrops.Add(new CharacterDrop.Drop
                {
                    m_prefab = originalDrop.m_prefab,
                    m_amountMin = lootEnabled ? (int)Helper.applyModifierValue(originalDrop.m_amountMin, loot.lootDropAmountMultiplier) : originalDrop.m_amountMin,
                    m_amountMax = lootEnabled ? (int)Helper.applyModifierValue(originalDrop.m_amountMax, loot.lootDropAmountMultiplier) : originalDrop.m_amountMax,
                    m_chance = chance,
                    m_onePerPlayer = originalDrop.m_onePerPlayer,
                    m_levelMultiplier = originalDrop.m_levelMultiplier,
                    m_levelMultiplierNotAmount = originalDrop.m_levelMultiplierNotAmount,
                    m_dontScale = originalDrop.m_dontScale
                });
            }

            if (!changed) return;
            __state = ___m_drops;
            ___m_drops = modifiedDrops;
        }

        private static float GetMouldModifier(string prefabName, MouldsConfiguration config)
        {
            switch (prefabName)
            {
                case "MoldArmorMageChest":
                case "MoldArmorMageHelmet":
                case "MoldArmorMageLegs":
                    return config.callerDropChanceMultiplier;
                case "MoldArmormediumChest":
                case "MoldArmorMediumHelmet":
                case "MoldArmorMediumLegs":
                    return config.vanguardDropChanceMultiplier;
                case "MoldArmorGoldChest":
                case "MoldArmorGoldHelmet":
                case "MoldArmorGoldLegs":
                    return config.protectorDropChanceMultiplier;
                default:
                    return 0f;
            }
        }

        private static void Postfix(ref List<CharacterDrop.Drop> ___m_drops, ref List<CharacterDrop.Drop> __state)
        {
            RestoreDrops(ref ___m_drops, ref __state);
        }

        private static void Finalizer(ref List<CharacterDrop.Drop> ___m_drops, ref List<CharacterDrop.Drop> __state)
        {
            RestoreDrops(ref ___m_drops, ref __state);
        }

        private static void RestoreDrops(ref List<CharacterDrop.Drop> drops, ref List<CharacterDrop.Drop> state)
        {
            if (state == null) return;
            drops = state;
            state = null;
        }
    }
}

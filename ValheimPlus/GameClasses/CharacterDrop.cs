using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
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
        [UsedImplicitly]
        private static void Prefix(ref List<CharacterDrop.Drop> ___m_drops, out List<CharacterDrop.Drop> __state)
        {
            __state = null;
            var config = Configuration.Current.LootDrop;
            if (!config.IsEnabled) return;

            __state = ___m_drops;
            var modifiedDrops = new List<CharacterDrop.Drop>(___m_drops.Count);
            foreach (var originalDrop in ___m_drops)
            {
                // only multiply items, else an oozer will drop more/less blobs as well as its usual item drops.
                if (originalDrop.m_prefab == null || !originalDrop.m_prefab.TryGetComponent<ItemDrop>(out _))
                {
                    modifiedDrops.Add(originalDrop);
                    continue;
                }

                float chance = Helper.applyModifierValue(originalDrop.m_chance, config.lootDropChanceMultiplier);
                float groupModifier =
                    ItemGroupChanceModifiers.TryGetValue(originalDrop.m_prefab.name, out var getModifier)
                        ? getModifier(config)
                        : 0f;
                if (groupModifier != 0f)
                    chance = Helper.applyModifierValue(chance, groupModifier);

                // Vanilla's pseudo-drop counter can still award a zero-chance drop, so leave it out.
                if (chance <= 0f) continue;

                modifiedDrops.Add(new CharacterDrop.Drop
                {
                    m_prefab = originalDrop.m_prefab,
                    m_amountMin =
                        (int)Helper.applyModifierValue(originalDrop.m_amountMin, config.lootDropAmountMultiplier),
                    m_amountMax =
                        (int)Helper.applyModifierValue(originalDrop.m_amountMax, config.lootDropAmountMultiplier),
                    m_chance = chance,
                    m_onePerPlayer = originalDrop.m_onePerPlayer,
                    m_levelMultiplier = originalDrop.m_levelMultiplier,
                    m_levelMultiplierNotAmount = originalDrop.m_levelMultiplierNotAmount,
                    m_dontScale = originalDrop.m_dontScale
                });
            }

            ___m_drops = modifiedDrops;
        }

        [UsedImplicitly]
        private static void Postfix(ref List<CharacterDrop.Drop> ___m_drops, List<CharacterDrop.Drop> __state)
        {
            if (__state != null) ___m_drops = __state;
        }

        private static readonly string[] CallerMouldPrefabNames =
            { "MoldArmorMageChest", "MoldArmorMageHelmet", "MoldArmorMageLegs" };

        private static readonly string[] VanguardMouldPrefabNames =
            { "MoldArmormediumChest", "MoldArmorMediumHelmet", "MoldArmorMediumLegs" };

        private static readonly string[] ProtectorMouldPrefabNames =
            { "MoldArmorGoldChest", "MoldArmorGoldHelmet", "MoldArmorGoldLegs" };

        /// <summary>
        /// Item groups with their own drop chance setting, applied on top of lootDropChanceMultiplier.
        /// To add a group, list its prefab names above and bind its setting in LootDropConfiguration.
        /// </summary>
        private static readonly (string[] Prefabs, Func<LootDropConfiguration, float> Modifier)[] ItemGroups =
        {
            (CallerMouldPrefabNames, config => config.callerMouldDropChanceMultiplier),
            (VanguardMouldPrefabNames, config => config.vanguardMouldDropChanceMultiplier),
            (ProtectorMouldPrefabNames, config => config.protectorMouldDropChanceMultiplier),
        };

        private static readonly Dictionary<string, Func<LootDropConfiguration, float>> ItemGroupChanceModifiers =
            ItemGroups.SelectMany(
                    group => group.Prefabs,
                    (group, prefab) => (prefab, group.Modifier))
                .ToDictionary(entry => entry.prefab, entry => entry.Modifier);
    }
}

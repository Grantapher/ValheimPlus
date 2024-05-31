using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(DropTable), "GetDropList", typeof(int))]
    public static class DropTable_GetDropList_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ref DropTable __instance, ref List<GameObject> __result, int amount,
            ref float __state)
        {
            __state = __instance.m_dropChance; // we have to save the original to change it back after the function

            var config = Configuration.Current.Gathering;
            if (!config.IsEnabled || config.dropChance == 0 || !Mathf.Approximately(__instance.m_dropChance, 1f))
                return;
            float modified = Helper.applyModifierValue(__instance.m_dropChance, config.dropChance);
            __instance.m_dropChance = Helper.Clamp(modified, 0, 1);
        }

        [UsedImplicitly]
        private static void Postfix(ref DropTable __instance, ref List<GameObject> __result, ref float __state)
        {
            __instance.m_dropChance = __state; // Apply the original drop chance in case it was modified

            var config = Configuration.Current.Gathering;
            if (!config.IsEnabled) return;

            var newResultDrops = new List<GameObject>();
            foreach (var drop in __result)
            {
                float dropMultiplier = drop.name switch
                {
                    "Wood" => config.wood,
                    "FineWood" => config.fineWood,
                    "RoundLog" => config.coreWood, // Corewood
                    "ElderBark" => config.elderBark,
                    "YggdrasilWood" => config.yggdrasilWood,
                    "Stone" => config.stone,
                    "BlackMarble" => config.blackMarble,
                    "TinOre" => config.tinOre,
                    "CopperOre" => config.copperOre,
                    "CopperScrap" => config.copperScrap,
                    "IronScrap" => config.ironScrap,
                    "SilverOre" => config.silverOre,
                    "Chitin" => config.chitin,
                    "Feathers" => config.feather,
                    "Grausten" => config.grausten,
                    "Blackwood" => config.blackwood, // Ashwood
                    "FlametalOreNew" => config.flametalOre, // Flametal
                    "ProustitePowder" => config.proustitePowder, // Proustite Powder
                    _ => 1f
                };

                // ReSharper disable once CompareOfFloatsByEqualityOperator expecting exactly 1f.
                if (dropMultiplier == 1f)
                {
                    newResultDrops.Add(drop);
                    continue;
                }

                int modifiedAmount = Helper.applyModifierValueWithChance(1f, dropMultiplier);
                for (int i = 0; i < modifiedAmount; i++) newResultDrops.Add(drop);
            }

            __result = newResultDrops;
        }
    }
}
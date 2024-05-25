using System;
using System.Collections.Generic;
using HarmonyLib;
using ValheimPlus.Configurations;
using UnityEngine;

namespace ValheimPlus.GameClasses
{


    [HarmonyPatch(typeof(DropTable), "GetDropList", new Type[] { typeof(int) })]
    public static class DropTable_GetDropList_Patch
    {

        static float originalDropChance = 0;
        private static void Prefix(ref DropTable __instance, ref List<GameObject> __result, int amount)
        {
            originalDropChance = __instance.m_dropChance; // we have to save the original to change it back after the function
            if (Configuration.Current.Gathering.IsEnabled && Configuration.Current.Gathering.dropChance != 0 && __instance.m_dropChance != 1)
            {
                float modified = Helper.applyModifierValue(__instance.m_dropChance, Configuration.Current.Gathering.dropChance);
                __instance.m_dropChance = Helper.Clamp(modified, 0, 1);
            }
        }

        private static void Postfix(ref DropTable __instance, ref List<GameObject> __result, int amount)
        {
            __instance.m_dropChance = originalDropChance; // Apply the original drop chance in case modified

            if (!Configuration.Current.Gathering.IsEnabled)
                return;

            List<GameObject> newResultDrops = new List<GameObject>();
            foreach (GameObject toDrop in __result)
            {
                switch (toDrop.name)
                {
                    case "Wood": // Wood
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.wood);
                        break;
                    case "FineWood": // Finewood
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.fineWood);
                        break;
                    case "RoundLog": // Corewood
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.coreWood);
                        break;
                    case "ElderBark": // ElderBark
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.elderBark);
                        break;
                    case "YggdrasilWood": // YggdrasilWood
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.yggdrasilWood);
                        break;
                    case "Stone": // Stone
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.stone);
                        break;
                    case "BlackMarble": // BlackMarble
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.blackMarble);
                        break;
                    case "TinOre": // Tin
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.tinOre);
                        break;
                    case "CopperOre": // CopperOre
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.copperOre);
                        break;
                    case "CopperScrap": // CopperScrap
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.copperScrap);
                        break;
                    case "IronScrap": // Iron
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.ironScrap);
                        break;
                    case "SilverOre": // Silver
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.silverOre);
                        break;
                    case "Chitin": // Chitin
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.chitin);
                        break;
                    case "Feathers": // Feather
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.feather);
                        break;
                    case "Grausten": // Grausten
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.grausten);
                        break;
                    case "Blackwood": // Ashwood
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.blackwood);
                        break;
                    case "FlametalOreNew": // Flametal
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.flametalOre);
                        break;
                    case "ProustitePowder": // ProustitePowder
                        AddModifiedDrops(newResultDrops, toDrop, Configuration.Current.Gathering.proustitePowder);
                        break;

                    default:
                        newResultDrops.Add(toDrop);
                        break;
                }
            }

            __result = newResultDrops;
        }

        private static void AddModifiedDrops(List<GameObject> dropList, GameObject dropObject, float modifier)
        {
            for (int i = 0; i < Helper.applyModifierValue(1f, modifier); i++)
            {
                dropList.Add(dropObject);
            }

        }
    }


}

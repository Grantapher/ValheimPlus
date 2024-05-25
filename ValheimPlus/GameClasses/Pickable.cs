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
    public static class PickableYieldState
    {
        private static Dictionary<string, float> _yieldModifierDict;

        public static int CalculateYield(GameObject item, int originalAmount)
        {
            if (!Configuration.Current.Pickable.IsEnabled)
                return originalAmount;

            if (_yieldModifierDict.TryGetValue(item.name, out float yieldModifier))
            {
                return (int)Helper.applyModifierValue(originalAmount, yieldModifier);
            }

            return originalAmount;
        }

        public static void InitialSetup()
        {
            // Called from the transpiler, so this will be run when the game starts, plus when you connect to or disconnect from a server.

            var edibles = new List<string>
            {
                "Carrot",
                "Blueberries",
                "Cloudberry",
                "Raspberry",
                "Mushroom",
                "MushroomBlue",
                "MushroomYellow",
                "MushroomMagecap",
                "MushroomJotunPuffs",
                "MushroomSmokePuff",
                "Fiddleheadfern",
                "Vineberry",
                "Onion"
            };

            var flowersAndIngredients = new List<string>
            {
                "Barley",
                "CarrotSeeds",
                "Dandelion",
                "Flax",
                "Thistle",
                "TurnipSeeds",
                "Turnip",
                "OnionSeeds",
                "RoyalJelly",
                "VoltureEgg"
            };

            var materials = new List<string>
            {
                "BoneFragments",
                "Flint",
                "Stone",
                "Wood",
                "Crystal",
                "Tar",
                "WolfHairBundle",
                "WolfClaw"
            };

            var valuables = new List<string>
            {
                "Amber",
                "AmberPearl",
                "Coins",
                "Ruby"
            };

            var surtlingCores = new List<string>
            {
                "SurtlingCore"
            };

            var blackCores = new List<string>
            {
                "BlackCore"
            };

            var questItems = new List<string>
            {
                "DragonEgg",
                "WitheredBone",
                "GoblinTotem"
            };

            _yieldModifierDict = new Dictionary<string, float>();

            foreach (var item in edibles)
                _yieldModifierDict.Add(item, Configuration.Current.Pickable.edibles);
            foreach (var item in flowersAndIngredients)
                _yieldModifierDict.Add(item, Configuration.Current.Pickable.flowersAndIngredients);
            foreach (var item in materials)
                _yieldModifierDict.Add(item, Configuration.Current.Pickable.materials);
            foreach (var item in valuables)
                _yieldModifierDict.Add(item, Configuration.Current.Pickable.valuables);
            foreach (var item in surtlingCores)
                _yieldModifierDict.Add(item, Configuration.Current.Pickable.surtlingCores);
            foreach (var item in blackCores)
                _yieldModifierDict.Add(item, Configuration.Current.Pickable.blackCores);
            foreach (var item in questItems)
                _yieldModifierDict.Add(item, Configuration.Current.Pickable.questItems);
        }
    }

    /// <summary>
    /// Allow tweaking of Pickable item yield. (E.g. berries, flowers, branches, stones, gemstones.)
    /// </summary>
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.RPC_Pick))]
    public static class Pickable_RPC_Pick_Transpiler
    {
        // Our method and its arguments that we need to patch in
        private static readonly MethodInfo Method_CalculateYield =
            AccessTools.Method(typeof(PickableYieldState), nameof(PickableYieldState.CalculateYield));

        private static readonly FieldInfo Field_ItemPrefab = AccessTools.Field(typeof(Pickable), "m_itemPrefab");

        [HarmonyTranspiler]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            if (!Configuration.Current.Pickable.IsEnabled)
                return instructions;

            List<CodeInstruction> il = instructions.ToList();
            for (int i = 0; i < il.Count; i++)
            {
                if (il[i].opcode == OpCodes.Stloc_1)
                {
                    // Call calculateYield() and replace the original drop amount with the result.
                    // Calling calculateYield takes several instructions:
                    // LdArg.0 (load the "this" pointer), LdFld (load m_itemPrefab from this),
                    // Ldloc.1 (load the amount the game originally wants to drop from local 1), Call.
                    // We then Stloc.1 to store the return value back into local 1 so that the game uses our
                    // modified drop amount rather than the original.
                    il.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                    il.Insert(++i, new CodeInstruction(OpCodes.Ldfld, Field_ItemPrefab));
                    il.Insert(++i, new CodeInstruction(OpCodes.Ldloc_1));
                    il.Insert(++i, new CodeInstruction(new CodeInstruction(OpCodes.Call, Method_CalculateYield)));
                    il.Insert(++i, new CodeInstruction(OpCodes.Stloc_1));

                    // NOTE: This transpiler may be called multiple times, e.g. when starting the game,
                    // when connecting to a server and when disconnecting. We need to re-do the initial
                    // setup every time, since the modifier values may have changed (as the server config
                    // will be used instead of the client config).
                    PickableYieldState.InitialSetup();

                    return il.AsEnumerable();
                }
            }

            ValheimPlusPlugin.Logger.LogError("Unable to transpile Pickable.RPC_Pick to patch item yields");
            return il;
        }
    }
}
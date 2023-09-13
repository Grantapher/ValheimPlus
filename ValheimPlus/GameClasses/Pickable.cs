using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    public static class PickableYieldState
    {
        public static Dictionary<string, float> yieldModifier;
    }

    /// <summary>
    /// Allow tweaking of Pickable item yield. (E.g. berries, flowers, branches, stones, gemstones.)
    /// </summary>
    [HarmonyPatch(typeof(Pickable), "RPC_Pick")]
    public static class Pickable_RPC_Pick_Transpiler
    {
        // Our method and its arguments that we need to patch in
        private static readonly MethodInfo method_calculateYield = AccessTools.Method(typeof(Pickable_RPC_Pick_Transpiler), nameof(calculateYield));
        private static readonly FieldInfo field_ItemPrefab = AccessTools.Field(typeof(Pickable), "m_itemPrefab");
        private static readonly FieldInfo field_amount = AccessTools.Field(typeof(Pickable), "m_amount");

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                    il.Insert(++i, new CodeInstruction(OpCodes.Ldfld, field_ItemPrefab));
                    il.Insert(++i, new CodeInstruction(OpCodes.Ldloc_1));
                    il.Insert(++i, new CodeInstruction(new CodeInstruction(OpCodes.Call, method_calculateYield)));
                    il.Insert(++i, new CodeInstruction(OpCodes.Stloc_1));

                    // NOTE: This transpiler may be called multiple times, e.g. when starting the game, when connecting to a server and when disconnecting.
                    // We need to re-do the initial setup every time, since the modifier values may have changed (as the server config will be used instead of the client config).
                    initialSetup();

                    return il.AsEnumerable();
                }
            }

            ValheimPlusPlugin.Logger.LogError("Unable to transpile Pickable.RPC_Pick to patch item yields");
            return instructions;
        }

        private static int calculateYield(GameObject item, int originalAmount)
        {
            try
            {
                return (int)Helper.applyModifierValue(originalAmount, PickableYieldState.yieldModifier[item.name]);
            }
            catch
            {
                return originalAmount;
            }
        }

        private static void initialSetup()
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
                "OnionSeeds"
            };

            var materials = new List<string>
            {
                "BoneFragments",
                "Flint",
                "Stone",
                "Wood"
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

            PickableYieldState.yieldModifier = new Dictionary<string, float>();

            foreach (var item in edibles)
                PickableYieldState.yieldModifier.Add(item, Configuration.Current.Pickable.edibles);
            foreach (var item in flowersAndIngredients)
                PickableYieldState.yieldModifier.Add(item, Configuration.Current.Pickable.flowersAndIngredients);
            foreach (var item in materials)
                PickableYieldState.yieldModifier.Add(item, Configuration.Current.Pickable.materials);
            foreach (var item in valuables)
                PickableYieldState.yieldModifier.Add(item, Configuration.Current.Pickable.valuables);
            foreach (var item in surtlingCores)
                PickableYieldState.yieldModifier.Add(item, Configuration.Current.Pickable.surtlingCores);
            foreach (var item in blackCores)
                PickableYieldState.yieldModifier.Add(item, Configuration.Current.Pickable.blackCores);
        }
    }
}

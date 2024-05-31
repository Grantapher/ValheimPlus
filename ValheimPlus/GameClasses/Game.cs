using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;
using ValheimPlus.RPC;

namespace ValheimPlus.GameClasses
{
    /// <summary>
    /// Sync server config to clients
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    public static class Game_Start_Patch
    {
        [UsedImplicitly]
        private static void Prefix()
        {
            ZRoutedRpc.instance.Register<ZPackage>("VPlusConfigSync", VPlusConfigSync.RPC_VPlusConfigSync);
            ZRoutedRpc.instance.Register<ZPackage>("VPlusMapSync", VPlusMapSync.RPC_VPlusMapSync);
            ZRoutedRpc.instance.Register<ZPackage>("VPlusMapAddPin", VPlusMapPinSync.RPC_VPlusMapAddPin);
            ZRoutedRpc.instance.Register("VPlusAck", VPlusAck.RPC_VPlusAck);
        }
    }

    /// <summary>
    /// Alter game difficulty damage scale.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.GetDifficultyDamageScalePlayer))]
    public static class Game_GetDifficultyDamageScale_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Game __instance)
        {
            var config = Configuration.Current.Game;
            if (!config.IsEnabled) return;
            __instance.m_damageScalePerPlayer = config.gameDifficultyDamageScale / 100f;
        }
    }

    /// <summary>
    /// Alter game difficulty health scale for enemies.
    /// 
    /// Although the underlying game code seems to just scale the damage down,
    /// in game this results in the same damage but higher enemy health.
    /// Not sure how that is converted in the game code, however. 
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.GetDifficultyDamageScaleEnemy))]
    public static class Game_GetDifficultyHealthScale_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Game __instance)
        {
            var config = Configuration.Current.Game;
            if (!config.IsEnabled) return;
            __instance.m_healthScalePerPlayer = config.gameDifficultyHealthScale / 100f;
        }
    }

    /// <summary>
    /// Disable the "I have arrived" message on spawn.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.UpdateRespawn))]
    public static class Game_UpdateRespawn_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ref Game __instance, float dt)
        {
            var config = Configuration.Current.Player;
            if (!config.IsEnabled || config.iHaveArrivedOnSpawn) return;
            __instance.m_firstSpawn = false;
        }
    }

    /// <summary>
    /// Alter player difficulty scale
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.GetPlayerDifficulty))]
    public static class Game_GetPlayerDifficulty_Patch
    {
        private static readonly FieldInfo Field_M_DifficultyScaleRange =
            AccessTools.Field(typeof(Game), nameof(Game.m_difficultyScaleRange));

        /// <summary>
        /// Patches the range used to check the number of players around.
        /// </summary>
        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.Game.IsEnabled) return instructions;

            float range = Math.Min(Configuration.Current.Game.difficultyScaleRange, 2);

            var il = instructions.ToList();
            for (int i = 0; i < il.Count; i++)
            {
                if (il[i].LoadsField(Field_M_DifficultyScaleRange))
                {
                    il.RemoveAt(i - 1); // remove "this"
                    // replace field with our range as a constant
                    il[i - 1] = new CodeInstruction(OpCodes.Ldc_R4, range);
                    return il.AsEnumerable();
                }
            }

            ValheimPlusPlugin.Logger.LogError("Failed to apply Game_GetPlayerDifficulty_Patch.Transpiler");

            return il;
        }

        [UsedImplicitly]
        private static void Postfix(ref int __result)
        {
            var config = Configuration.Current.Game;
            if (!config.IsEnabled) return;
            if (config.setFixedPlayerCountTo > 0) __result = config.setFixedPlayerCountTo;
            __result += config.extraPlayerCountNearby;
        }
    }
}
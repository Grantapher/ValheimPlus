using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    public static class ZPlayFabMatchmakingHelper
    {
        private const int OriginalMaxPlayers = 10;

        private const int PatchedMinPlayers = 1;
        private const int PatchedMaxPlayers = 32;

        private static bool alreadyWarned;

        public static bool isMaxPlayersDefault
        {
            get
            {
                var config = Configuration.Current.Server;
                return !config.IsEnabled || config.maxPlayers == OriginalMaxPlayers;
            }
        }

        public static int ConfiguredMaxPlayers(int originalMaxPlayers)
        {
            var config = Configuration.Current.Server;
            var newMaxPlayers = Helper.Clamp(config.maxPlayers, PatchedMinPlayers, PatchedMaxPlayers);
            var warnedThisTime = false;

            if (!alreadyWarned && config.maxPlayers != newMaxPlayers)
            {
                ValheimPlusPlugin.Logger.LogWarning(
                    $"maxPlayers must be between {PatchedMinPlayers} and {PatchedMaxPlayers}," +
                    $" but was {config.maxPlayers}, using {newMaxPlayers} instead.");
                warnedThisTime = true;
            }

            if (originalMaxPlayers == OriginalMaxPlayers) return newMaxPlayers;
            
            // On dedicated servers, originalMaxPlayers will be 1 higher to account for the server.

            bool tooFull = newMaxPlayers == PatchedMaxPlayers;
            if (!alreadyWarned && tooFull)
            {
                ValheimPlusPlugin.Logger.LogWarning(
                    $"Couldn't set maxPlayers to {PatchedMaxPlayers} because the dedicated server (this machine)" +
                    $" takes up a slot. This server will support {PatchedMaxPlayers - 1} players instead.");
                warnedThisTime = true;
            }

            if (!tooFull) newMaxPlayers++;

            alreadyWarned = alreadyWarned || warnedThisTime;
            return newMaxPlayers;
        }
    }

    [HarmonyPatch(typeof(ZPlayFabMatchmaking), nameof(ZPlayFabMatchmaking.CreateLobby))]
    public static class ZPlayFabMatchmaking_CreateLobby_Transpiler
    {
        private static readonly FieldInfo Field_CreateLobbyRequest_MaxPlayers = AccessTools.Field(
            typeof(PlayFab.MultiplayerModels.CreateLobbyRequest),
            nameof(PlayFab.MultiplayerModels.CreateLobbyRequest.MaxPlayers)
        );

        /// <summary>
        /// Alter PlayFab server player limit.
        /// Must be between 1 and 32, or 31 if on a dedicated server.
        /// </summary>
        [HarmonyTranspiler]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            if (ZPlayFabMatchmakingHelper.isMaxPlayersDefault) return instructions;

            var il = instructions.ToList();
            try
            {
                var codeMatcher = new CodeMatcher(il, generator);
                sbyte originalMaxPlayers = (sbyte)codeMatcher
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldc_I4_S),
                        new CodeMatch(OpCodes.Stfld, Field_CreateLobbyRequest_MaxPlayers)
                    )
                    .ThrowIfNotMatch("No match for code that sets MaxPlayers.")
                    .Operand;

                int newMaxPlayers = ZPlayFabMatchmakingHelper.ConfiguredMaxPlayers(originalMaxPlayers);
                return codeMatcher.SetOperandAndAdvance(newMaxPlayers).InstructionEnumeration();
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogWarning(
                    "Failed to alter lobby player limit (ZPlayFabMatchmaking_CreateLobby_Transpiler)." +
                    $" This may cause the maxPlayers setting to not function correctly. Exception is:\n{e}");
                return il;
            }
        }
    }

    [HarmonyPatch(typeof(ZPlayFabMatchmaking), nameof(ZPlayFabMatchmaking.CreateAndJoinNetwork))]
    public static class ZPlayFabMatchmaking_CreateAndJoinNetwork_Transpiler
    {
        private static readonly MethodInfo PropertySetter_PlayFabNetworkConfiguration_MaxPlayerCount =
            AccessTools.PropertySetter(
                typeof(PlayFab.Party.PlayFabNetworkConfiguration),
                nameof(PlayFab.Party.PlayFabNetworkConfiguration.MaxPlayerCount)
            );


        /// <summary>
        /// Alter PlayFab network configuration player limit
        /// Must be between 1 and 32, or 31 if on a dedicated server.
        /// </summary>
        [HarmonyTranspiler]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            if (ZPlayFabMatchmakingHelper.isMaxPlayersDefault) return instructions;

            var il = instructions.ToList();
            try
            {
                var codeMatcher = new CodeMatcher(il, generator);
                sbyte originalMaxPlayers = (sbyte)codeMatcher
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldc_I4_S),
                        new CodeMatch(OpCodes.Callvirt, PropertySetter_PlayFabNetworkConfiguration_MaxPlayerCount)
                    )
                    .ThrowIfNotMatch("No match for code that sets MaxPlayerCount.")
                    .Operand;

                int newMaxPlayers = ZPlayFabMatchmakingHelper.ConfiguredMaxPlayers(originalMaxPlayers);
                return codeMatcher.SetOperandAndAdvance(newMaxPlayers).InstructionEnumeration();
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogWarning(
                    "Failed to alter network player limit (ZPlayFabMatchmaking_CreateAndJoinNetwork_Transpiler)." +
                    $" This may cause the maxPlayers setting to not function correctly. Exception is:\n{e}");
                return il;
            }
        }
    }
}
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    // Manually loaded in ValheimPlus.cs because HarmonyPriority wasn't working
    // [HarmonyPatch(typeof(ZPlayFabMatchmaking), "CreateLobby")]
    public static class ZPlayFabMatchmaking_CreateLobby_Transpiler
    {
        private static readonly int ORIGINAL_MAX_PLAYERS = 10;

        /// <summary>
        /// Alter playfab server player limit
        /// Must be between 1 and 32
        /// </summary>
        // Manually loaded in ValheimPlus.cs because HarmonyPriority wasn't working
        // [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.Server.IsEnabled || Configuration.Current.Server.maxPlayers == ORIGINAL_MAX_PLAYERS) return instructions;

            try
            {
                if (ZPlayFabMatchmaking_CreateAndJoinNetwork_Transpiler.CreateLobbySize == -1)
                {
                    ValheimPlusPlugin.Logger.LogError("V+ didn't apply patches in proper order, leaving crossplay max lobby size at 10.");
                    return instructions;
                }

                var ldc = instructions.Single(inst => inst.Is(OpCodes.Ldc_I4_S, operand: ORIGINAL_MAX_PLAYERS));
                ldc.operand = ZPlayFabMatchmaking_CreateAndJoinNetwork_Transpiler.CreateLobbySize;
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogWarning("Failed to alter lobby player limit (ZPlayFabMatchmaking_CreateLobby_Transpiler)." +
                    $" This may cause the maxPlayers setting to not function correctly. Exception is:\n{e}");
            }
            return instructions;
        }
    }

    [HarmonyPatch(typeof(ZPlayFabMatchmaking), "CreateAndJoinNetwork")]
    public static class ZPlayFabMatchmaking_CreateAndJoinNetwork_Transpiler
    {

        public static int CreateLobbySize { get; private set; } = -1;

        private static readonly int ORIGINAL_MAX_PLAYERS = 10;
        private static readonly int ORIGINAL_MAX_PLAYERS_CLIENT = ORIGINAL_MAX_PLAYERS;
        private static readonly int ORIGINAL_MAX_PLAYERS_SERVER = 11;

        private static readonly int PATCHED_MIN_PLAYERS = 1;
        private static readonly int PATCHED_MAX_PLAYERS = 32;

        /// <summary>
        /// Alter playfab network configuration player limit
        /// Must be between 1 and 32
        /// </summary>
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.Server.IsEnabled || Configuration.Current.Server.maxPlayers == ORIGINAL_MAX_PLAYERS) return instructions;

            var newMaxPlayers = Helper.Clamp(Configuration.Current.Server.maxPlayers, PATCHED_MIN_PLAYERS, PATCHED_MAX_PLAYERS);

            if (Configuration.Current.Server.maxPlayers != newMaxPlayers)
            {
                ValheimPlusPlugin.Logger.LogWarning($"maxPlayers must be between {PATCHED_MIN_PLAYERS} and {PATCHED_MAX_PLAYERS}," +
                    $" but was {Configuration.Current.Server.maxPlayers}, using {newMaxPlayers} instead.");
            }

            try
            {
                var ldc = instructions.Single(inst => 
                    inst.opcode == OpCodes.Ldc_I4_S && (inst.OperandIs(ORIGINAL_MAX_PLAYERS_CLIENT) || inst.OperandIs(ORIGINAL_MAX_PLAYERS_SERVER)));

                bool makeRoomForServer = ldc.OperandIs(ORIGINAL_MAX_PLAYERS_SERVER);
                if (makeRoomForServer && newMaxPlayers == PATCHED_MAX_PLAYERS)
                {
                    ValheimPlusPlugin.Logger.LogWarning($"Couldn't set maxPlayers to {PATCHED_MAX_PLAYERS} because the dedicated server" +
                        $" (this machine) takes up a slot. This server will support {PATCHED_MAX_PLAYERS - 1} players instead.");
                }
                else if (makeRoomForServer)
                {
                    newMaxPlayers++;
                }

                ldc.operand = newMaxPlayers;
                CreateLobbySize = newMaxPlayers - (makeRoomForServer ? 1 : 0);
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogWarning("Failed to alter network player limit (ZPlayFabMatchmaking_CreateAndJoinNetwork_Transpiler)." +
                    $" This may cause the maxPlayers setting to not function correctly. Exception is:\n{e}");
            }

            return instructions;
        }
    }
}

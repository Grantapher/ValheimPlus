using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;
using ValheimPlus.Utility;

namespace ValheimPlus
{
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    public static class Player_UpdatePlacementGhost_Transpile
    {
        private static readonly MethodInfo Method_Quaternion_Euler = AccessTools.Method(typeof(Quaternion),
            nameof(Quaternion.Euler), new[] { typeof(float), typeof(float), typeof(float) });

        private static readonly MethodInfo Method_GetRotation =
            AccessTools.Method(typeof(Player_UpdatePlacementGhost_Transpile), nameof(GetRotation));

        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.FreePlacementRotation.IsEnabled) return instructions;

            var il = instructions.ToList();
            for (int i = 0; i < il.Count; ++i)
            {
                if (il[i].Calls(Method_Quaternion_Euler) && il[i + 1].opcode == OpCodes.Stloc_S)
                {
                    var local = il[i + 1].operand;

                    // essentially add a new line of code after the quaternion is set:
                    // quaternion = GetRotation(this, quaternion) 
                    il.InsertRange(index: i + 2, new CodeInstruction[]
                    {
                        new(OpCodes.Ldarg_0), // this
                        new(OpCodes.Ldloc_S, local), // quaternion
                        new(OpCodes.Call, Method_GetRotation), // GetRotation(this, quaternion)
                        new(OpCodes.Stloc_S, local) // assign back to quaternion
                    });
                    return il.AsEnumerable();
                }
            }

            PatchLog.Failed(nameof(Player_UpdatePlacementGhost_Transpile), "Free placement rotation will not work.");
            return il.AsEnumerable();
        }

        public static Quaternion GetRotation(Player __instance, Quaternion quaternion)
        {
            if (ABM.isActive) return quaternion;

            var rotation = FreePlacementRotation.PlayersData.TryGetValue(__instance, out var value)
                ? value.PlaceRotation
                : __instance.m_placeRotation * 22.5f * Vector3.up;

            return Quaternion.Euler(rotation);
        }
    }

    /// <summary>
    /// Rotates placementGhost by 1 degree, if pressed key, or reset to x22.5f degrees usual rotation.
    /// Attaches to nearly placed objects as usual placement
    /// </summary>
    public static class FreePlacementRotation
    {
        public class PlayerData
        {
            public Vector3 PlaceRotation = Vector3.zero;
            public bool Opposite;
            public Piece LastPiece;
            public KeyCode LastKeyCode;
        }

        public static readonly Dictionary<Player, PlayerData> PlayersData = new();

        [HarmonyPatch(typeof(Player), "UpdatePlacement")]
        public static class ModifyPUpdatePlacement
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance, bool takeInput, float dt)
            {
                var config = Configuration.Current.FreePlacementRotation;
                if (!config.IsEnabled) return;
                if (ABM.isActive) return;
                if (!__instance.InPlaceMode()) return;
                if (!takeInput) return;
                if (Hud.IsPieceSelectionVisible()) return;

                if (!PlayersData.ContainsKey(__instance))
                    PlayersData[__instance] = new PlayerData();

                RotateWithWheel(__instance);
                SyncRotationWithTargetInFront(__instance, config.copyRotationParallel, perpendicular: false);
                SyncRotationWithTargetInFront(__instance, config.copyRotationPerpendicular, perpendicular: true);
            }

            private static void RotateWithWheel(Player __instance)
            {
                var wheel = Input.GetAxis("Mouse ScrollWheel");
                var playerData = PlayersData[__instance];
                if (wheel.Equals(0f) && !ZInput.GetButton("JoyRotate")) return;

                if (Input.GetKey(Configuration.Current.FreePlacementRotation.rotateY))
                {
                    playerData.PlaceRotation += Vector3.up * Mathf.Sign(wheel);
                    __instance.m_placeRotation = (int)(playerData.PlaceRotation.y / 22.5f);
                }
                else if (Input.GetKey(Configuration.Current.FreePlacementRotation.rotateX))
                {
                    playerData.PlaceRotation += Vector3.right * Mathf.Sign(wheel);
                }
                else if (Input.GetKey(Configuration.Current.FreePlacementRotation.rotateZ))
                {
                    playerData.PlaceRotation += Vector3.forward * Mathf.Sign(wheel);
                }
                else
                {
                    __instance.m_placeRotation = ClampPlaceRotation(__instance.m_placeRotation);
                    playerData.PlaceRotation = new Vector3(0, __instance.m_placeRotation * 22.5f, 0);
                }

                playerData.PlaceRotation = ClampAngles(playerData.PlaceRotation);
            }

            private static void SyncRotationWithTargetInFront(Player __instance, KeyCode keyCode, bool perpendicular)
            {
                if (__instance.m_placementGhost == null) return;
                if (!Input.GetKeyUp(keyCode)) return;
                if (!__instance.PieceRayTest(out _, out _, out var piece, out _, out _, false) || piece == null) return;
                var playerData = PlayersData[__instance];

                var rotation = piece.transform.rotation;
                if (perpendicular) rotation *= Quaternion.Euler(0, 90, 0);

                if (playerData.LastKeyCode != keyCode || playerData.LastPiece != piece) playerData.Opposite = false;
                playerData.LastKeyCode = keyCode;
                playerData.LastPiece = piece;

                if (playerData.Opposite) rotation *= Quaternion.Euler(0, 180, 0);
                playerData.Opposite = !playerData.Opposite;

                playerData.PlaceRotation = rotation.eulerAngles;
            }
        }

        private static Vector3 ClampAngles(Vector3 angles) =>
            new(ClampAngle(angles.x), ClampAngle(angles.y), ClampAngle(angles.z));

        private const int MaxIndex = 16; // 360/22.5f

        private static int ClampPlaceRotation(int index) => index switch
        {
            < 0 => MaxIndex + index,
            >= MaxIndex => index - MaxIndex,
            _ => index
        };

        private static float ClampAngle(float angle) => angle switch
        {
            < 0 => 360 + angle,
            >= 360 => angle - 360,
            _ => angle
        };
    }
}
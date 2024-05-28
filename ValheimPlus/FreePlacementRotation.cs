using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Runtime;
using IniParser;
using IniParser.Model;
using System.Globalization;
using ValheimPlus;
using UnityEngine.Rendering;
using ValheimPlus.Configurations;
using System.Reflection.Emit;

namespace ValheimPlus
{


    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    public static class Player_UpdatePlacementGhost_Transpile
    {
        private static MethodInfo method_Quaternion_Euler = AccessTools.Method(typeof(Quaternion), nameof(Quaternion.Euler), new Type[] {typeof(float), typeof(float), typeof(float)});
        private static MethodInfo method_GetRotation = AccessTools.Method(typeof(Player_UpdatePlacementGhost_Transpile), nameof(Player_UpdatePlacementGhost_Transpile.GetRotation));

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.FreePlacementRotation.IsEnabled) return instructions;

            List<CodeInstruction> il = instructions.ToList();

            for (int i = 0; i < il.Count; ++i)
            {
                if (il[i].Calls(method_Quaternion_Euler))
                {
                    // remove direct call to Quaternion.Euler and replace with function call to switch
                    il[i-1] = new CodeInstruction(OpCodes.Ldarg_0);
                    il[i] = new CodeInstruction(OpCodes.Call, method_GetRotation);
                    il.RemoveRange(i-8,7);

                    break;
                }
            }

            return il.AsEnumerable();
        }

        public static Quaternion GetRotation(Player __instance)
        {
            if (ABM.isActive){
                return Quaternion.Euler(0f, __instance.m_placeRotationDegrees * (float)__instance.m_placeRotation, 0f);
            }

            var rotation = FreePlacementRotation.PlayersData.ContainsKey(__instance)
                            ? FreePlacementRotation.PlayersData[__instance].PlaceRotation
                            : __instance.m_placeRotation * 22.5f * Vector3.up;

            // ValheimPlusPlugin.Logger.LogMessage($"{rotation}");

            return Quaternion.Euler(rotation);
        }
    }


    /// <summary>
    /// Rotates placementGhost by 1 degree, if pressed key, or reset to x22.5f degrees usual rotation.
    /// Attaches to nearly placed objects as usual placement
    /// </summary>
    public class FreePlacementRotation
    {
        public class PlayerData
        {
            public Vector3 PlaceRotation = Vector3.zero;
            public bool Opposite;
            public Piece LastPiece;
            public KeyCode LastKeyCode;
            
        }
        
        public static readonly Dictionary<Player, PlayerData> PlayersData = new Dictionary<Player, PlayerData>();

        [HarmonyPatch(typeof(Player), "UpdatePlacement")]
        public static class ModifyPUpdatePlacement
        {
            private static void Postfix(Player __instance, bool takeInput, float dt)
            {
                if (!Configuration.Current.FreePlacementRotation.IsEnabled)
                    return;
                
                if (ABM.isActive)
                    return;

                if (!__instance.InPlaceMode())
                    return;

                if (!takeInput)
                    return;

                if (Hud.IsPieceSelectionVisible())
                    return;

                if (!PlayersData.ContainsKey(__instance))
                    PlayersData[__instance] = new PlayerData();

                RotateWithWheel(__instance);
                SyncRotationWithTargetInFront(__instance, Configuration.Current.FreePlacementRotation.copyRotationParallel, false);
                SyncRotationWithTargetInFront(__instance, Configuration.Current.FreePlacementRotation.copyRotationPerpendicular, true);
            }

            private static void RotateWithWheel(Player __instance)
            {
                var wheel = Input.GetAxis("Mouse ScrollWheel");

                var playerData = PlayersData[__instance];
                
                if (!wheel.Equals(0f) || ZInput.GetButton("JoyRotate"))
                {
                    if (Input.GetKey(Configuration.Current.FreePlacementRotation.rotateY))
                    {
                        playerData.PlaceRotation += Vector3.up * Mathf.Sign(wheel);
                        __instance.m_placeRotation = (int) (playerData.PlaceRotation.y / 22.5f);
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

                    ValheimPlusPlugin.Logger.LogInfo("Angle " + playerData.PlaceRotation);
                }
            }
            
            private static void SyncRotationWithTargetInFront(Player __instance, KeyCode keyCode, bool perpendicular)
            {
                if (__instance.m_placementGhost == null)
                    return;

                if (Input.GetKeyUp(keyCode))
                {
                    Vector3 point;
                    Vector3 normal;
                    Piece piece;
                    Heightmap heightmap;
                    Collider waterSurface;
                    if (__instance.PieceRayTest(out point, out normal, out piece, out heightmap, out waterSurface,
                        false) && piece != null)
                    {
                        var playerData = PlayersData[__instance];
                        
                        var rotation = piece.transform.rotation;
                        if (perpendicular)
                            rotation *= Quaternion.Euler(0, 90, 0);

                        if (playerData.LastKeyCode != keyCode || playerData.LastPiece != piece)
                            playerData.Opposite = false;
                        
                        playerData.LastKeyCode = keyCode;
                        playerData.LastPiece = piece;
                        
                        if (playerData.Opposite)
                            rotation *= Quaternion.Euler(0, 180, 0);
                        
                        playerData.Opposite = !playerData.Opposite;
                        
                        playerData.PlaceRotation = rotation.eulerAngles;
                        ValheimPlusPlugin.Logger.LogInfo("Sync Angle " + playerData.PlaceRotation);
                    }
                }
            }
        }

        private static Vector3 ClampAngles(Vector3 angles)
        {
            return new Vector3(ClampAngle(angles.x), ClampAngle(angles.y), ClampAngle(angles.z));
        }
        
        private static int ClampPlaceRotation(int index)
        {
            const int MaxIndex = 16; // 360/22.5f
            
            if (index < 0)
                index = MaxIndex + index;
            else if (index >= MaxIndex)
                index -= MaxIndex;
            return index;
        }
        
        private static float ClampAngle(float angle)
        {
            if (angle < 0)
                angle = 360 + angle;
            else if (angle >= 360)
                angle -= 360;
            return angle;
        }
    }
}
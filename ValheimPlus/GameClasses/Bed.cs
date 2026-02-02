using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    public static class BedHelper
    {
        public static bool CanSleepWithoutSpawn(this Bed bed) =>
            Configuration.Current.Bed.IsEnabled &&
            Configuration.Current.Bed.sleepWithoutSpawn &&
            !bed.IsCurrent() && // Bed is not already the spawn point.
            (!Configuration.Current.Bed.unclaimedBedsOnly || bed.GetOwner() == 0L);

        // This is pretty much an inline of the Interact code with the ownership checks removed.
        public static void InteractWithoutOwnershipReadWrite(this Bed bed, Player player)
        {
            if (!EnvMan.CanSleep())
            {
                player.Message(MessageHud.MessageType.Center, "$msg_cantsleep");
                return;
            }

            if (!bed.CheckEnemies(player) ||
                !bed.CheckExposure(player) ||
                !bed.CheckFire(player) ||
                !bed.CheckWet(player)) return;

            player.AttachStart(bed.m_spawnPoint, bed.gameObject, true, true, false,
                "attach_bed", new Vector3(0.0f, 0.5f, 0.0f));
        }
    }

    [HarmonyPatch(typeof(Bed), nameof(Bed.GetHoverText))]
    public static class Bed_GetHoverText_Patch
    {
        private const string AppendStr = "\n[LShift+<color=yellow><b>$KEY_Use</b></color>] $piece_bed_sleep";

        [UsedImplicitly]
        private static void Postfix(Bed __instance, ref string __result)
        {
            if (!__instance.CanSleepWithoutSpawn()) return;
            __result += ValheimPlus.LocalizationHelper.Localize(AppendStr);
        }
    }


    [HarmonyPatch(typeof(Bed), nameof(Bed.Interact))]
    public static class Bed_Interact_Patch
    {
        // Avoid `shift + use` triggering the `use` path.
        [UsedImplicitly]
        private static bool Prefix(Bed __instance, Humanoid human)
        {
            return !ZInput.GetButtonDown("Use") || !ZInput.GetKey(KeyCode.LeftShift);
        }

        [UsedImplicitly]
        private static void Postfix(Bed __instance, Humanoid human, bool repeat)
        {
            if (repeat ||
                !__instance.CanSleepWithoutSpawn() ||
                !ZInput.GetKey(KeyCode.LeftShift) ||
                !ZInput.GetButtonDown("Use")) return;

            __instance.InteractWithoutOwnershipReadWrite(human as Player);
        }
    }
}

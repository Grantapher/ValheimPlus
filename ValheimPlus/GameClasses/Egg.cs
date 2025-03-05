using HarmonyLib;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    // NOTE: There is no explicit "Egg" class. This is for handling both EggGrow and EggHatch classes.
    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.CanGrow))]
    public class EggGrow_CanGrow_Patch
    {
        public static void Postfix(EggGrow __instance, ref bool __result)
        {
            if (!Configuration.Current.Egg.IsEnabled)
                return;

            var stackSize = __instance.m_item.m_itemData.m_stack;
            if (!Configuration.Current.Egg.canStack && stackSize > 1)
                return;

            __result = __result || !Configuration.Current.Egg.requireShelter;
        }

    }

    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.GrowUpdate))]
    public class EggGrow_GrowUpdate_Patch
    {
        public static bool Prefix(EggGrow __instance)
        {
            if (!Configuration.Current.Egg.IsEnabled)
                return true;

            if (!__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner())
                return true;

            // From here forward we are patching the function so that we omit the original.
            float growStart = __instance.m_nview.GetZDO().GetFloat(ZDOVars.s_growStart);
            var stackSize = __instance.m_item.m_itemData.m_stack;

            var canGrow = __instance.CanGrow();
            if (!canGrow)
                growStart = 0;
            else if (growStart == 0)
                growStart = (float)ZNet.instance.GetTimeSeconds();

            __instance.m_nview.GetZDO().Set(ZDOVars.s_growStart, growStart);
            __instance.UpdateEffects(growStart);

            __instance.m_growTime = Configuration.Current.Egg.hatchTime;
            if (growStart > 0f && ZNet.instance.GetTimeSeconds() > (double)(growStart + __instance.m_growTime))
            {
                for (int i = 0; i < stackSize; i++)
                {
                    Character component = UnityEngine.Object.Instantiate(
                        __instance.m_grownPrefab, __instance.transform.position, __instance.transform.rotation)
                        .GetComponent<Character>();

                    __instance.m_hatchEffect.Create(__instance.transform.position, __instance.transform.rotation);

                    if ((bool)component)
                    {
                        component.SetTamed(__instance.m_tamed);
                        component.SetLevel(__instance.m_item.m_itemData.m_quality);
                    }

                }

                __instance.m_nview.Destroy();
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.GetHoverText))]
    public class EggGrow_GetHoverText_Patch
    {
        private static string GetTimeLeft(EggGrow egg)
        {
            var growStart = egg.m_nview.GetZDO().GetFloat(ZDOVars.s_growStart);
            if (growStart <= 0)
                return "";

            var elapsed = ZNet.instance.GetTimeSeconds() - growStart;
            var timeLeft = Configuration.Current.Egg.hatchTime - elapsed;

            int minutes = (int)timeLeft / 60;

            string info;
            if (((int)timeLeft) >= 120)
                info = minutes + " minutes";

            // grow update is only called every 5 seconds
            else if (timeLeft < 0)
                info = "0 seconds";

            else
                info = (int)timeLeft + " seconds";

            return "\nTime left: " + info;
        }

        public static bool Prefix(EggGrow __instance, ref string __result)
        {
            if (!Configuration.Current.Egg.IsEnabled || !__instance.m_item)
                return true;

            if (!__instance.m_nview || !__instance.m_nview.IsValid())
                return true;

            var isStackInvalid = !Configuration.Current.Egg.canStack && __instance.m_item.m_itemData.m_stack > 1;

            var growStart = __instance.m_nview.GetZDO().GetFloat(ZDOVars.s_growStart);
            bool flag = growStart > 0f;
            string text = isStackInvalid ? "$item_chicken_egg_stacked" : (flag ? "$item_chicken_egg_warm" : "$item_chicken_egg_cold");
            string hoverText = __instance.m_item.GetHoverText();

            int num = hoverText.IndexOf('\n');
            if (num <= 0)
            {
                __result = hoverText;
                return false;
            }

            __result = hoverText.Substring(0, num) + " " + Localization.instance.Localize(text)
                + GetTimeLeft(__instance)
                + hoverText.Substring(num);
            return false;
        }
    }

    [HarmonyPatch(typeof(Growup), nameof(Growup.Start))]
    public class Growup_Start_Patch
    {
        public static void Prefix(Growup __instance)
        {
            if (!Configuration.Current.Egg.IsEnabled)
                return;

            __instance.m_growTime = Configuration.Current.Egg.growTime;
        }
    }
}

using HarmonyLib;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    public class TimeManipulation
    {
        /// <summary>
        /// Hook on EnvMan init to alter total day length
        /// </summary>
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.Awake))]
        public static class TimeInitHook
        {
            private static void Prefix(ref EnvMan __instance)
            {
                if (Configuration.Current.Time.IsEnabled)
                {
                    if (__instance)
                    {
                        __instance.m_dayLengthSec = (long)Configuration.Current.Time.totalDayTimeInSeconds;
                    }
                    else
                    {
                        ValheimPlusPlugin.Logger.LogWarning("EnvMan instance not loaded");
                    }
                }
            }
        }

        /// <summary>
        /// Hook on EnvMan init to alter total day length
        /// </summary>
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.RescaleDayFraction))]
        public static class RescaleDayFractionPatch
        {
            private static void Postfix(ref EnvMan __instance, ref float __result)
            {
                if (Configuration.Current.Time.IsEnabled && Configuration.Current.Time.forcePartOfDay)
                {
                    __result = Configuration.Current.Time.forcePartOfDayTime;
                } else
                if (Configuration.Current.Time.IsEnabled && Configuration.Current.Time.nightDurationModifier != 0) {
                    // Day range is: if 0.25f <= __result <= 0.75f
                    float nightDurationMultiplier = Helper.applyModifierValue(1.0f, Configuration.Current.Time.nightDurationModifier);
                    if (nightDurationMultiplier <= float.Epsilon)
                    {
                        // there is no night, so cut it out.
                        __result = 0.25f + __result * 0.5f;
                    }
                    else
                    {
                        nightDurationMultiplier = 1.0f / nightDurationMultiplier;
                        if (__result >= 0.5f)
                        {
                            //ValheimPlusPlugin.Logger.LogMessage($"{__result}, {nightDurationMultiplier}, {Mathf.Pow((__result - 0.5f) * 2.0f, nightDurationMultiplier)}");

                            float stretchFactor = Mathf.Pow((__result - 0.5f) * 2.0f, nightDurationMultiplier);
                            __result = 0.5f + 0.5f * stretchFactor;
                        }
                        else { 
                            //ValheimPlusPlugin.Logger.LogMessage($"{__result}, {nightDurationMultiplier}, {Mathf.Pow(((1.0f - __result) - 0.5f)*2.0f, nightDurationMultiplier)}");

                            float stretchFactor = 1.0f - Mathf.Pow(((1.0f - __result) - 0.5f) * 2.0f, nightDurationMultiplier);
                            __result = 0.5f * stretchFactor;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetEnv))]
    public static class EnvMan_SetEnv_Patch
    {
        private static void Prefix(ref EnvMan __instance, ref EnvSetup env)
        {
            if (Configuration.Current.Game.IsEnabled && Configuration.Current.Game.disableFog)
            {
                env.m_fogDensityNight = 0f;
                env.m_fogDensityMorning = 0f;
                env.m_fogDensityDay = 0f;
                env.m_fogDensityEvening = 0f;
            }

            if (Configuration.Current.Brightness.IsEnabled)
            {
                applyEnvModifier(env);
            }
        }

        private static void applyEnvModifier(EnvSetup env)
        {
            /* changing brightness during a period of day had a coupling affect with other period, need further development
            env.m_fogColorMorning = applyBrightnessModifier(env.m_fogColorMorning, Configuration.Current.Brightness.morningBrightnessMultiplier);
            env.m_fogColorSunMorning = applyBrightnessModifier(env.m_fogColorSunMorning, Configuration.Current.Brightness.morningBrightnessMultiplier);
            env.m_sunColorMorning = applyBrightnessModifier(env.m_sunColorMorning, Configuration.Current.Brightness.morningBrightnessMultiplier);

            env.m_ambColorDay = applyBrightnessModifier(env.m_ambColorDay, Configuration.Current.Brightness.dayBrightnessMultiplier);
            env.m_fogColorDay = applyBrightnessModifier(env.m_fogColorDay, Configuration.Current.Brightness.dayBrightnessMultiplier);
            env.m_fogColorSunDay = applyBrightnessModifier(env.m_fogColorSunDay, Configuration.Current.Brightness.dayBrightnessMultiplier);
            env.m_sunColorDay = applyBrightnessModifier(env.m_sunColorDay, Configuration.Current.Brightness.dayBrightnessMultiplier);

            env.m_fogColorEvening = applyBrightnessModifier(env.m_fogColorEvening, Configuration.Current.Brightness.eveningBrightnessMultiplier);
            env.m_fogColorSunEvening = applyBrightnessModifier(env.m_fogColorSunEvening, Configuration.Current.Brightness.eveningBrightnessMultiplier);
            env.m_sunColorEvening = applyBrightnessModifier(env.m_sunColorEvening, Configuration.Current.Brightness.eveningBrightnessMultiplier);
            */

            env.m_ambColorNight = applyBrightnessModifier(env.m_ambColorNight, Configuration.Current.Brightness.nightBrightnessMultiplier);
            env.m_fogColorNight = applyBrightnessModifier(env.m_fogColorNight, Configuration.Current.Brightness.nightBrightnessMultiplier);
            env.m_fogColorSunNight = applyBrightnessModifier(env.m_fogColorSunNight, Configuration.Current.Brightness.nightBrightnessMultiplier);
            env.m_sunColorNight = applyBrightnessModifier(env.m_sunColorNight, Configuration.Current.Brightness.nightBrightnessMultiplier);
        }

        private static Color applyBrightnessModifier(Color color, float multiplier)
        {
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            float scaleFunc;
            if (multiplier >= 0)
            {
                scaleFunc = (Mathf.Sqrt(multiplier) * 1.069952679E-4f) + 1f;
            }
            else
            {
                scaleFunc = 1f - (Mathf.Sqrt(Mathf.Abs(multiplier)) * 1.069952679E-4f);
            }
            v = Mathf.Clamp01(v * scaleFunc);
            return Color.HSVToRGB(h, s, v);
        }
    }

    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetParticleArrayEnabled))]
    public static class EnvMan_SetParticleArrayEnabled_Patch
    {
        private static void Postfix(ref MistEmitter __instance, GameObject[] psystems, bool enabled)
        {
            // Disable Mist clouds, does not work on Console Commands (env Misty) but should work in the regular game.
            if (Configuration.Current.Game.IsEnabled && Configuration.Current.Game.disableFog)
            {
                foreach (GameObject gameObject in psystems)
                {
                    MistEmitter componentInChildren = gameObject.GetComponentInChildren<MistEmitter>();
                    if (componentInChildren)
                    {
                        componentInChildren.enabled = false;
                    }
                }
            }
        }
    }
}

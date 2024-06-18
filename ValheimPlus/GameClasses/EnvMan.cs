﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    /// <summary>
    /// Time is stored as total seconds since world creation in `EnvMan.m_totalSeconds`.
    /// A day length is defined at `EnvMan.m_dayLengthSec`.
    /// Dividing these two gives us what day we are on and a fraction that represents the time of the day.
    /// This fraction defines night as within 0.15 of 0.00 (0.85 to 1.00 and 0.00 to 0.15).
    /// This fraction is then partially scaled so that nighttime is between 0.75 and 0.25 (crossing 1 to 0 again).
    ///   The daytime portion (0.15 to 0.85) is shrunk to fit between 0.25 and 0.75.
    ///   The nighttime portion (0.85 to 1.00, 1.00 to 0.15) is expanded to fit between 0.75 and 0.25.
    /// I assume the fraction is scaled this way so that the code can deal with a simpler linear plane of day/night,
    /// since it has to figure out where the sun/moon goes and such. Anyway, we will tackle this scaling math to
    /// manipulate the day/night cycle, as this will be the least intrusive to the rest of the code.
    /// Note: There is a `const c_MorningL = 0.15f` that likely houses our 0.15. So 0.15 is likely safe to replace.
    /// </summary>
    public static class TimeManipulation
    {
        private static bool FloatingEquals(this float f1, float f2)
        {
            return Math.Abs(f1 - f2) < 0.00001;
        }

        /// <summary>
        /// Replaces all LDC.R4 instructions matching the original values (0.15f, 0.85f, 0.7f) with their modded
        /// replacements.
        /// </summary>
        /// <param name="il">The instructions to modify</param>
        /// <returns>An array containing the number of instructions modified for each of [0.15f, 0.85f, 0.7f]
        /// respectively</returns>
        private static int[] ReplaceFloats(List<CodeInstruction> il)
        {
            var originals = new[] { 0.15f, 0.85f, 0.7f };
            var newHalfNightFraction = Mathf.Clamp01(Configuration.Current.Time.nightPercent / 200f);
            var newValues = new[] { newHalfNightFraction, 1 - newHalfNightFraction, 1 - (2 * newHalfNightFraction) };
            var modifications = new[] { 0, 0, 0 };

            foreach (var inst in il)
            {
                if (inst.opcode != OpCodes.Ldc_R4) continue;

                var floatValue = (float)inst.operand;
                for (var i = 0; i < originals.Length; i++)
                {
                    if (!originals[i].FloatingEquals(floatValue)) continue;
                    inst.operand = newValues[i];
                    modifications[i]++;
                    break;
                }
            }

            return modifications;
        }

        /// <summary>
        /// Hook on EnvMan init to alter total day length
        /// </summary>
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.Awake))]
        public static class EnvMan_Awake_Patch
        {
            [UsedImplicitly]
            private static void Postfix(ref EnvMan __instance)
            {
                if (!Configuration.Current.Time.IsEnabled) return;
                __instance.m_dayLengthSec = (long)Configuration.Current.Time.totalDayTimeInSeconds;
            }
        }

        /// <summary>
        /// Force the time of day to a certain time by returning that time as a constant.
        /// </summary>
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.RescaleDayFraction))]
        public static class EnvMan_RescaleDayFraction_Patch
        {
            [UsedImplicitly]
            private static void Postfix(ref float __result)
            {
                if (!Configuration.Current.Time.IsEnabled || !Configuration.Current.Time.forcePartOfDay) return;
                __result = Mathf.Clamp01(Configuration.Current.Time.forcePartOfDayTime);
            }
        }

        /// <summary>
        /// Modify the constants of the scaling function so that the day/night cycle is a different fraction.
        /// </summary>
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.RescaleDayFraction))]
        public static class EnvMan_RescaleDayFraction_Transpiler
        {
            [UsedImplicitly]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (!Configuration.Current.Time.IsEnabled || Configuration.Current.Time.forcePartOfDay)
                    return instructions;

                var il = instructions.ToList();
                var results = ReplaceFloats(il);

                // Should have replaced four 0.15, two 0.85, and one 0.7.
                var expected = new[] { 4, 2, 1 };
                if (!results.SequenceEqual(expected))
                {
                    ValheimPlusPlugin.Logger.LogError(
                        "Failed to apply EnvMan_RescaleDayFraction_Transpiler. " +
                        "Time.nightDurationModifier may not have been applied correctly. " +
                        $"Expected [{expected.Join()}] but was [{results.Join()}].");
                }

                return il;
            }
        }


        /// <summary>
        /// Modify the day start time to match our rescale fraction.
        /// </summary>
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.GetMorningStartSec))]
        public static class EnvMan_GetMorningStartSec_Transpiler
        {
            [UsedImplicitly]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (!Configuration.Current.Time.IsEnabled || Configuration.Current.Time.forcePartOfDay)
                    return instructions;

                var il = instructions.ToList();
                var results = ReplaceFloats(il);

                // Should have replaced a single 0.15.
                var expected = new[] { 1, 0, 0 };
                if (!results.SequenceEqual(expected))
                {
                    ValheimPlusPlugin.Logger.LogError(
                        "Failed to apply EnvMan_GetMorningStartSec_Transpiler. " +
                        "Time.nightDurationModifier may not have been applied correctly. " +
                        $"Expected [{expected.Join()}] but was [{results.Join()}].");
                }

                return il;
            }
        }

        /// <summary>
        /// Modify the day start time to match our rescale fraction.
        /// </summary>
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SkipToMorning))]
        public static class EnvMan_SkipToMorning_Transpiler
        {
            [UsedImplicitly]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (!Configuration.Current.Time.IsEnabled || Configuration.Current.Time.forcePartOfDay)
                    return instructions;

                var il = instructions.ToList();
                var results = ReplaceFloats(il);

                // Should have replaced a single 0.15.
                var expected = new[] { 1, 0, 0 };
                if (!results.SequenceEqual(expected))
                {
                    ValheimPlusPlugin.Logger.LogError(
                        "Failed to apply EnvMan_SkipToMorning_Transpiler. " +
                        "Time.nightDurationModifier may not have been applied correctly. " +
                        $"Expected [{expected.Join()}] but was [{results.Join()}].");
                }

                return il;
            }
        }
    }

    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetEnv))]
    public static class EnvMan_SetEnv_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ref EnvSetup env)
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
                ApplyEnvModifier(env);
            }
        }

        private static void ApplyEnvModifier(EnvSetup env)
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

            env.m_ambColorNight = ApplyBrightnessModifier(env.m_ambColorNight,
                Configuration.Current.Brightness.nightBrightnessMultiplier);
            env.m_fogColorNight = ApplyBrightnessModifier(env.m_fogColorNight,
                Configuration.Current.Brightness.nightBrightnessMultiplier);
            env.m_fogColorSunNight = ApplyBrightnessModifier(env.m_fogColorSunNight,
                Configuration.Current.Brightness.nightBrightnessMultiplier);
            env.m_sunColorNight = ApplyBrightnessModifier(env.m_sunColorNight,
                Configuration.Current.Brightness.nightBrightnessMultiplier);
        }

        private static Color ApplyBrightnessModifier(Color color, float multiplier)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
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
        [UsedImplicitly]
        private static void Postfix(GameObject[] psystems)
        {
            // Disable Mist clouds, does not work on Console Commands (env Misty) but should work in the regular game.
            if (!Configuration.Current.Game.IsEnabled || !Configuration.Current.Game.disableFog) return;
            foreach (var gameObject in psystems)
            {
                var componentInChildren = gameObject.GetComponentInChildren<MistEmitter>();
                if (componentInChildren) componentInChildren.enabled = false;
            }
        }
    }
}
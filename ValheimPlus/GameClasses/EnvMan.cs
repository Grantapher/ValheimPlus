using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;
using ValheimPlus.Utility;

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
        /// Keep the day length in step with the config, which can arrive from a server after EnvMan starts.
        /// </summary>
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.FixedUpdate))]
        public static class EnvMan_FixedUpdate_Patch
        {
            /// <summary>The prefab's day length, captured before the first write so it can be put back.</summary>
            private static long? _vanillaDayLengthSec;

            [UsedImplicitly]
            private static void Prefix(EnvMan __instance)
            {
                _vanillaDayLengthSec ??= __instance.m_dayLengthSec;
                __instance.m_dayLengthSec = Configuration.Current.Time.IsEnabled
                    ? (long)Configuration.Current.Time.totalDayTimeInSeconds
                    : _vanillaDayLengthSec.Value;
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
                    PatchLog.Failed(
                        nameof(EnvMan_RescaleDayFraction_Transpiler),
                        $"`Time.nightDurationModifier` will not work. Expected [{expected.Join()}] but was [{results.Join()}].");
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
                    PatchLog.Failed(
                        nameof(EnvMan_GetMorningStartSec_Transpiler),
                        $"`Time.nightDurationModifier` will not work. Expected [{expected.Join()}] but was [{results.Join()}].");
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
                    PatchLog.Failed(
                        nameof(EnvMan_SkipToMorning_Transpiler),
                        $"`Time.nightDurationModifier` will not work. Expected [{expected.Join()}] but was [{results.Join()}].");
                }

                return il;
            }
        }
    }

    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetEnv))]
    public static class EnvMan_SetEnv_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ref EnvSetup env, out NightColors __state)
        {
            __state = default;

            if (Configuration.Current.Game.IsEnabled && Configuration.Current.Game.disableFog)
            {
                env.m_fogDensityNight = 0f;
                env.m_fogDensityMorning = 0f;
                env.m_fogDensityDay = 0f;
                env.m_fogDensityEvening = 0f;
            }

            if (Configuration.Current.Brightness.IsEnabled)
            {
                __state = ApplyEnvModifier(env);
            }
        }

        /// <summary>
        /// Restores the original night colours once SetEnv has applied the scaled ones, so the shared EnvSetup
        /// and the clones EnvMan makes from it for weather transitions never hold scaled values.
        /// </summary>
        [UsedImplicitly]
        private static void Finalizer(EnvSetup env, NightColors __state)
        {
            if (!__state.Saved) return;

            env.m_ambColorNight = __state.Ambient;
            env.m_fogColorNight = __state.Fog;
            env.m_fogColorSunNight = __state.FogSun;
            env.m_sunColorNight = __state.Sun;
        }

        public struct NightColors
        {
            public bool Saved;
            public Color Ambient, Fog, FogSun, Sun;
        }

        // The old code scaled by (1 + sqrt(m) * PerCallScale) on every SetEnv call; the fix applies one minute
        // of that drift once (CallsPerMinute = 60 s at the 50 Hz FixedUpdate), so 5 gives ~x2.05.
        private const double PerCallScale = 1.069952679E-4;
        private const double CallsPerMinute = 3000.0;

        private static NightColors ApplyEnvModifier(EnvSetup env)
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

            var original = new NightColors
            {
                Saved = true,
                Ambient = env.m_ambColorNight,
                Fog = env.m_fogColorNight,
                FogSun = env.m_fogColorSunNight,
                Sun = env.m_sunColorNight
            };

            env.m_ambColorNight = ApplyBrightnessModifier(original.Ambient,
                Configuration.Current.Brightness.nightBrightnessMultiplier);
            env.m_fogColorNight = ApplyBrightnessModifier(original.Fog,
                Configuration.Current.Brightness.nightBrightnessMultiplier);
            env.m_fogColorSunNight = ApplyBrightnessModifier(original.FogSun,
                Configuration.Current.Brightness.nightBrightnessMultiplier);
            env.m_sunColorNight = ApplyBrightnessModifier(original.Sun,
                Configuration.Current.Brightness.nightBrightnessMultiplier);
            return original;
        }

        private static Color ApplyBrightnessModifier(Color color, float multiplier)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            // Applied once to the original colour; negative values darken by the inverse.
            double perCall = Math.Sqrt(Math.Abs(multiplier)) * PerCallScale;
            double scaleFunc = Math.Exp(CallsPerMinute * Math.Log(1.0 + perCall));
            if (multiplier < 0)
            {
                scaleFunc = 1.0 / scaleFunc;
            }

            v = Mathf.Clamp01(v * (float)scaleFunc);
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
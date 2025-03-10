using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;
using Object = UnityEngine.Object;

namespace ValheimPlus.GameClasses
{
    public static class EggGrowHelpers
    {
        private static readonly FieldInfo Field_ItemData_M_Stack =
            AccessTools.Field(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_stack));

        public static CodeMatcher ApplyStackTranspiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator, string caller)
        {
            var config = Configuration.Current.Egg;
            var il = instructions.ToList();
            var codeMatcher = new CodeMatcher(il, generator);
            if (!config.IsEnabled || !config.canStack) return codeMatcher;

            try
            {
                return codeMatcher
                    .MatchEndForward(
                        new CodeMatch(inst => inst.LoadsField(Field_ItemData_M_Stack)),
                        OpCodes.Ldc_I4_1
                    )
                    .ThrowIfNotMatch("No match for code that checks egg stack size.")
                    .Set(OpCodes.Ldc_I4, int.MaxValue)
                    .Start();
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogError(
                    $"Failed to apply `{caller}`. " +
                    "This may cause the `Egg.canStack` config to not function correctly. " +
                    $"Exception is:\n{e}");
                return new CodeMatcher(il, generator);
            }
        }
    }

    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.Start))]
    public static class EggGrow_Start_Patch
    {
        [UsedImplicitly]
        public static void Prefix(EggGrow __instance)
        {
            var config = Configuration.Current.Egg;
            if (!config.IsEnabled) return;

            __instance.m_growTime = config.hatchTime;
            __instance.m_requireNearbyFire = config.requireShelter;
            __instance.m_requireUnderRoof = config.requireShelter;
        }
    }

    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.CanGrow))]
    public static class EggGrow_CanGrow_Transpiler
    {
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator) =>
            EggGrowHelpers.ApplyStackTranspiler(instructions, generator, nameof(EggGrow_CanGrow_Transpiler))
                .InstructionEnumeration();
    }

    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.GrowUpdate))]
    public static class EggGrow_GrowUpdate_Transpiler
    {
        private static readonly MethodInfo Method_SpawnAll =
            AccessTools.Method(typeof(EggGrow_GrowUpdate_Transpiler), nameof(SpawnAll));

        private static readonly FieldInfo Field_EggGrow_M_Nview =
            AccessTools.Field(typeof(EggGrow), nameof(EggGrow.m_nview));

        private static readonly MethodInfo Method_ZNetView_Destroy =
            AccessTools.Method(typeof(ZNetView), nameof(ZNetView.Destroy));

        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var config = Configuration.Current.Egg;
            if (!config.IsEnabled || !config.canStack) return instructions;

            var il = instructions.ToList();
            var codeMatcher = EggGrowHelpers.ApplyStackTranspiler(il, generator,
                nameof(EggGrow_GrowUpdate_Transpiler));

            try
            {
                return codeMatcher
                    .MatchStartForward(
                        OpCodes.Ldarg_0,
                        new CodeMatch(inst => inst.LoadsField(Field_EggGrow_M_Nview)),
                        new CodeMatch(inst => inst.Calls(Method_ZNetView_Destroy))
                    )
                    .ThrowIfNotMatch("No match for code that destroys the ZNetView.")
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, Method_SpawnAll)
                    )
                    .InstructionEnumeration();
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogError(
                    "Failed to apply `EggGrow_GrowUpdate_Transpiler`. " +
                    "This may cause the `Egg.canStack` config to not function correctly. " +
                    $"Exception is:\n{e}");
                return il;
            }
        }

        // Spawns the rest of the egg stack
        private static void SpawnAll(EggGrow instance)
        {
            var stackSize = instance.m_item.m_itemData.m_stack;
            for (int i = 0; i < stackSize - 1; i++)
            {
                var newSpawn = Object
                    .Instantiate(instance.m_grownPrefab, instance.transform.position, instance.transform.rotation)
                    .GetComponent<Character>();

                instance.m_hatchEffect.Create(instance.transform.position, instance.transform.rotation);

                if (!newSpawn) continue;

                newSpawn.SetTamed(instance.m_tamed);
                newSpawn.SetLevel(instance.m_item.m_itemData.m_quality);
            }
        }
    }

    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.GetHoverText))]
    public static class EggGrow_GetHoverText_Patch
    {
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator) =>
            EggGrowHelpers.ApplyStackTranspiler(instructions, generator, nameof(EggGrow_GetHoverText_Patch))
                .InstructionEnumeration();

        [UsedImplicitly]
        public static void Postfix(EggGrow __instance, ref string __result)
        {
            if (!Configuration.Current.Egg.IsEnabled || !Configuration.Current.Egg.showHatchTime) return;

            int num = __result.IndexOf("\n", StringComparison.Ordinal);
            if (num <= 0) return;

            var firstLine = __result.Substring(0, num);
            if (!firstLine.Contains(Localization.instance.Localize("$item_chicken_egg_warm"))) return;

            var growStart = __instance.m_nview.GetZDO().GetFloat(ZDOVars.s_growStart);
            var timeLeft = GetTimeLeft(growStart);
            var lastLine = __result.Substring(num);
            __result = firstLine + timeLeft + lastLine;
        }

        private static string GetTimeLeft(float growStart)
        {
            var elapsed = ZNet.instance.GetTimeSeconds() - growStart;

            // grow update is only called every 5 seconds
            var timeLeftSeconds = (int)Math.Max(0, Configuration.Current.Egg.hatchTime - elapsed);

            string timeLeftString;
            if (timeLeftSeconds >= 120)
            {
                var timeLeftMinutes = elapsed / 60;
                timeLeftString = timeLeftMinutes + " minutes";
            }
            else
            {
                timeLeftString = timeLeftSeconds + " seconds";
            }

            return "\nTime left: " + timeLeftString;
        }
    }
}

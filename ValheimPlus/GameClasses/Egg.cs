using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    public class EggGrowHelpers
    {
        public static List<CodeInstruction> StackTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.Egg.IsEnabled || !Configuration.Current.Egg.canStack)
                return null;

            var stackField = AccessTools.Field(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_stack));

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_1 && codes[i - 1].LoadsField(stackField))
                {
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, int.MaxValue);
                    break; // replace only the first instance
                }
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.Start))]
    public class EggGrow_Start_Patch
    {
        public static void Prefix(EggGrow __instance)
        {
            if (!Configuration.Current.Egg.IsEnabled)
                return;

            __instance.m_growTime = Configuration.Current.Egg.hatchTime;
            __instance.m_requireNearbyFire = Configuration.Current.Egg.requireShelter;
            __instance.m_requireUnderRoof = Configuration.Current.Egg.requireShelter;
        }
    }

    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.CanGrow))]
    public class EggGrow_CanGrow_Transpiler
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = EggGrowHelpers.StackTranspiler(instructions);
            return codes?.AsEnumerable() ?? instructions;
        }
    }

    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.GrowUpdate))]
    public class EggGrow_GrowUpdate_Transpiler
    {
        // Spawns the rest of the egg stack
        private static void SpawnAll(EggGrow instance)
        {
            var stackSize = instance.m_item.m_itemData.m_stack;
            for (int i = 0; i < stackSize - 1; i++)
            {
                Character component = UnityEngine.Object.Instantiate(
                    instance.m_grownPrefab, instance.transform.position, instance.transform.rotation)
                    .GetComponent<Character>();
                instance.m_hatchEffect.Create(instance.transform.position, instance.transform.rotation);
                if ((bool)component)
                {
                    component.SetTamed(instance.m_tamed);
                    component.SetLevel(instance.m_item.m_itemData.m_quality);
                }
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = EggGrowHelpers.StackTranspiler(instructions);
            if (codes == null)
                return instructions;

            var spawnAllMethod = AccessTools.Method(typeof(EggGrow_GrowUpdate_Transpiler), nameof(SpawnAll));
            var zNetViewField = AccessTools.Field(typeof(EggGrow), nameof(EggGrow.m_nview));
            var zNetViewDestroy = AccessTools.Method(typeof(ZNetView), nameof(ZNetView.Destroy));

            // subtract 2 from count to avoid out of bounds exception
            for (int i = 0; i < codes.Count - 2; i++)
            {
                // Must be inserted before the nview.Destroy call
                if (codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].LoadsField(zNetViewField) && codes[i + 2].Calls(zNetViewDestroy))
                {
                    // Load the instance and call the SpawnAll method
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, spawnAllMethod));
                    break;
                }
            }

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.GetHoverText))]
    public class EggGrow_GetHoverText_Patch
    {
        private static string GetTimeLeft(float growStart)
        {
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

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = EggGrowHelpers.StackTranspiler(instructions);
            return codes?.AsEnumerable() ?? instructions;
        }

        public static void Postfix(EggGrow __instance, ref string __result)
        {
            if (!Configuration.Current.Egg.IsEnabled || !Configuration.Current.Egg.showHatchTime)
                return;

            int num = __result.IndexOf("\n");
            if (num <= 0)
                return;

            var firstLine = __result.Substring(0, num);
            if (!firstLine.Contains(Localization.instance.Localize("$item_chicken_egg_warm")))
                return;

            var growStart = __instance.m_nview.GetZDO().GetFloat(ZDOVars.s_growStart);
            var timeLeft = GetTimeLeft(growStart);
            var lastLine = __result.Substring(num);
            __result = firstLine + timeLeft + lastLine;
        }
    }

    [HarmonyPatch(typeof(Growup), nameof(Growup.Start))]
    public class Growup_Start_Patch
    {
        public static void Prefix(Growup __instance)
        {
            if (!Configuration.Current.Egg.IsEnabled)
                return;

            var humanoid = __instance.m_grownPrefab.GetComponent<Humanoid>();
            if (humanoid && humanoid.m_name == "$enemy_hen")
                __instance.m_growTime = Configuration.Current.Egg.growTime;
        }
    }
}

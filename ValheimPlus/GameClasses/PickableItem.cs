using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(PickableItem), nameof(PickableItem.GetStackSize))]
    public static class PickableItem_GetStackSize_Patch
    {
        [UsedImplicitly]
        public static void Postfix(PickableItem __instance, ref int __result)
        {
            __result = PickableYieldState.CalculateYield(__instance.m_itemPrefab.gameObject, __result);
        }
    }

    [HarmonyPatch(typeof(PickableItem), nameof(PickableItem.Drop))]
    public static class PickableItem_Drop_Prefix
    {
        [UsedImplicitly]
        public static bool Prefix(PickableItem __instance)
        {
            if (!Configuration.Current.Pickable.IsEnabled)
                return true;

            // The original code only drops one item at most, so we have to overwrite it to drop multiple.
            var maxStackSize = __instance.m_itemPrefab.m_itemData.m_shared.m_maxStackSize;
            var stackSize = __instance.GetStackSize();

            var offset = 0;
            while (stackSize > 0)
            {
                Drop(__instance, __instance.m_itemPrefab.gameObject, offset++, Math.Min(maxStackSize, stackSize));
                stackSize -= maxStackSize;
            }

            return false;
        }

        // this is copy/pasted from `Pickable.Drop` with `component` replacing `this`
        // and spawnOffset replaced by 0.2f like in the `PickableItem.Drop`
        private static void Drop(Component component, GameObject prefab, int offset, int stack)
        {
            var vector2 = Random.insideUnitCircle * 0.2f;
            var position = component.transform.position + Vector3.up * 0.2f +
                           new Vector3(vector2.x, 0.2f * offset, vector2.y);
            var rotation = Quaternion.Euler(0.0f, Random.Range(0, 360), 0.0f);
            var gameObject = Object.Instantiate(prefab, position, rotation);
            var itemDrop = gameObject.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                itemDrop.SetStack(stack);
                ItemDrop.OnCreateNew(itemDrop);
            }

            gameObject.GetComponent<Rigidbody>().linearVelocity = Vector3.up * 4f;
        }
    }
}
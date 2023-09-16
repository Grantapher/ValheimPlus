using HarmonyLib;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Demister), "OnEnable")]
    static class Demister_Patch
    {
        static string wispLight = "demister_ball(Clone)";
        static string wispTorch = "piece_groundtorch_mist(Clone)";
        static string mistwalker = "Mistwalker";

        static void Postfix(ref Demister __instance)
        {
            GameObject gameObject = __instance.gameObject;


            if (gameObject.transform.root.name == wispLight && Configuration.Current.Demister.IsEnabled)
            {

                editRange(gameObject, Configuration.Current.Demister.wispLight);
            } 
            else if (gameObject.transform.root.name == wispTorch && Configuration.Current.Demister.IsEnabled)
            {
                editRange(gameObject, Configuration.Current.Demister.wispTorch);
            }
            else if (gameObject.transform.parent.name == mistwalker && Configuration.Current.Demister.IsEnabled)
            {
                editRange(gameObject, Configuration.Current.Demister.Mistwalker);
            }

        }
        static void editRange(GameObject gameObject, float range)
        {
            gameObject.GetComponentInChildren<ParticleSystemForceField>().endRange = range;
        }
    }
}

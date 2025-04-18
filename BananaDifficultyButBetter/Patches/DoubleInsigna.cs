using HarmonyLib;
using UnityEngine;
using System.Collections;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(VirtueInsignia))]
    internal class DoubleInsigna
    {
        [HarmonyPatch(nameof(VirtueInsignia.Start))]
        [HarmonyPostfix]
        public static void StartInsignia(VirtueInsignia __instance)
        {
            int dif = __instance.parentDrone != null ? __instance.parentDrone.difficulty : -1;
            if (!BananaDifficultyPlugin.CanUseIt(dif)) return;
            if (__instance.gameObject.name.StartsWith("DoubleInsig")) return;

            __instance.StartCoroutine(SpawnInsignias(__instance));
        }

        private static IEnumerator SpawnInsignias(VirtueInsignia original)
        {
            yield return new WaitForSeconds(0.1f);
            VirtueInsignia insi = Object.Instantiate(original);
            insi.gameObject.name = "DoubleInsig1";
            insi.transform.Rotate(new Vector3(0, 0, 90));
            insi.transform.localScale /= 3;
            insi.target = original.target;

            yield return new WaitForSeconds(0.1f);

            VirtueInsignia insi2 = Object.Instantiate(original);
            insi2.gameObject.name = "DoubleInsig2";
            insi2.transform.Rotate(new Vector3(90, 0, 0));
            insi2.transform.localScale /= 3;
            insi2.target = original.target;
        }
    }
}
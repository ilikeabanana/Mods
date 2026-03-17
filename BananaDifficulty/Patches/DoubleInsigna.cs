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
            if (!BananaDifficultyPlugin.HardMode.Value) return;
            if (__instance.gameObject.name.StartsWith("DoubleInsig")) return;

            __instance.StartCoroutine(SpawnInsignias(__instance));
        }

        private static IEnumerator SpawnInsignias(VirtueInsignia original)
        {
            SpawnWithOffset(original, Quaternion.Euler(0, 0, 90), "DoubleInsig1");
            yield return new WaitForSeconds(0.1f);
            SpawnWithOffset(original, Quaternion.Euler(90, 0, 0), "DoubleInsig2");
        }

        private static VirtueInsignia SpawnWithOffset(VirtueInsignia original, Quaternion offset, string name)
        {
            GameObject parent = new GameObject(name + "_Parent");
            parent.transform.position = original.transform.position;

            VirtueInsignia clone = Object.Instantiate(original, parent.transform);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;

            parent.transform.rotation = offset;

            clone.gameObject.name = name;
            clone.transform.localScale /= 3;
            clone.target = original.target;

            if (!original.noTracking || original.parentEnemy.eid.enemyType == EnemyType.Virtue)
                clone.gameObject.AddComponent<InsigniaRotationOffset>().offset = offset;

            return clone;
        }


        // Add this component to your clones to store their rotation offset
        public class InsigniaRotationOffset : MonoBehaviour
        {
            public Quaternion offset = Quaternion.identity;
        }

        [HarmonyPatch(typeof(VirtueInsignia), "Update")]
        [HarmonyPrefix]
        public static void PreUpdate(VirtueInsignia __instance)
        {
            InsigniaRotationOffset off = __instance.GetComponent<InsigniaRotationOffset>();
            if (off != null)
                __instance.transform.rotation *= Quaternion.Inverse(off.offset);
        }

        [HarmonyPatch(typeof(VirtueInsignia), "Update")]
        [HarmonyPostfix]
        public static void PostUpdate(VirtueInsignia __instance)
        {
            InsigniaRotationOffset off = __instance.GetComponent<InsigniaRotationOffset>();
            if (off != null)
                __instance.transform.rotation *= off.offset;
        }
    }
}
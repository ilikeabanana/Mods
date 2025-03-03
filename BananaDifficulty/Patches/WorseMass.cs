using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(Mass))]
    internal class WorseMass
    {
        [HarmonyPatch(nameof(Mass.ShootSpear))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Mass __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.eid.target == null || __instance.dead || __instance.difficulty == 0) return;

            __instance.StartCoroutine(ShootSpearsWithDelay(__instance));
        }

        private static IEnumerator ShootSpearsWithDelay(Mass instance)
        {
            int spearCount = 3; // Change this to control how many spears are shot
            float delayBetweenShots = 0.25f; // Change this to control the delay in seconds

            for (int i = 0; i < spearCount; i++)
            {
                yield return new WaitForSeconds(delayBetweenShots);
                instance.inSemiAction = false;
                instance.tailEnd.LookAt(instance.eid.target.position);
                instance.tempSpear = Object.Instantiate(instance.spear, instance.tailSpear.transform.position, instance.tailEnd.rotation);
                instance.tempSpear.transform.LookAt(instance.eid.target.position);

                if (instance.tempSpear.TryGetComponent<MassSpear>(out MassSpear massSpear))
                {
                    massSpear.target = instance.eid.target;
                    massSpear.originPoint = instance.tailSpear.transform;
                    massSpear.damageMultiplier = instance.eid.totalDamageModifier;
                    if (instance.difficulty >= 4)
                    {
                        massSpear.spearHealth *= 3f;
                    }
                }

                instance.tailSpear.SetActive(false);
                instance.spearShot = true;

                
            }
        }
    }
}
using HarmonyLib;
using System.Collections;
using System.Linq;
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
    [HarmonyPatch(typeof(LeviathanHead))]
    internal class WorseLeviathan
    {
        [HarmonyPatch(nameof(LeviathanHead.BiteDamageStop))]
        [HarmonyPostfix]
        public static void Awake_Postfix(LeviathanHead __instance)
        {
            GameObject black = Object.Instantiate(BananaDifficultyPlugin.blackHole, __instance.shootPoint.position, Quaternion.identity);

            black.transform.localScale *= 3;

            BlackHoleProjectile blackhple = black.GetComponent<BlackHoleProjectile>();
            if(blackhple != null)
            {
                blackhple.safeType = EnemyType.Leviathan;
                blackhple.target = __instance.lcon.eid.target;
                blackhple.speed = 15;
                blackhple.Activate();
            }

            __instance.StartCoroutine(explode(25, blackhple));
        }

        public static IEnumerator explode(float seconds, BlackHoleProjectile blackHoleProjectile)
        {
            yield return new WaitForSeconds(seconds);
            blackHoleProjectile.Explode();
        }
    }
}
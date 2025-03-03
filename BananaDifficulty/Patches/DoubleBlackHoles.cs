using HarmonyLib;
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
    [HarmonyPatch("SpawnBlackHole")]
    internal class DoubleBlackHoles
    {
        [HarmonyPatch(typeof(FleshPrison))]
        [HarmonyPostfix]
        public static void Awake_Postfix(FleshPrison __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            GameObject gameObject = Object.Instantiate<GameObject>(__instance.blackHole, __instance.transform);
            gameObject.transform.position = __instance.rotationBone.position + new Vector3(3,3,3);
            BlackHoleProjectile currentBlackHole = gameObject.GetComponent<BlackHoleProjectile>();
            currentBlackHole.target = __instance.eid.target;
            if (currentBlackHole)
            {
                currentBlackHole.safeType = EnemyType.FleshPrison;
                currentBlackHole.Activate();
            }
        }
    }
}
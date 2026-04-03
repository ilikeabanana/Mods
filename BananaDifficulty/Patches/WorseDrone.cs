using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BananaDifficulty.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(Drone))]
    internal class WorseDrone
    {
        [HarmonyPatch(nameof(Drone.FixedUpdate))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Drone __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            if (__instance.crashing && !__instance.parried)
            {
                __instance.transform.forward = (MonoSingleton<NewMovement>.Instance.transform.position - __instance.transform.position).normalized;
            }
        }
        [HarmonyPatch(nameof(Drone.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(Drone __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.eid.enemyType != EnemyType.Drone) return;
            __instance.projectile = new AssetReference("6be53089211b2eb4ab93a26541e4e65b");
        }
    }
   
}
using HarmonyLib;
using System.Collections.Generic;
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
    [HarmonyPatch(typeof(Projectile))]
    internal class EveryProjectileHasABeam
    {
        public static List<ContinuousBeam> alreadyExistingBeams = new List<ContinuousBeam>();
        public static List<Projectile> alreadyExistingProjs = new List<Projectile>();
        [HarmonyPatch(nameof(Projectile.Start))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Projectile __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return;
            if (__instance.decorative) return;
            if (__instance.friendly) return;

            alreadyExistingProjs.RemoveAll((x) => x == null);

            if (BananaDifficultyPlugin.projBeam != null && alreadyExistingProjs.Count > 0)
            {
                ContinuousBeam continuousBeam = Object.Instantiate<ContinuousBeam>(BananaDifficultyPlugin.projBeam.GetComponent<ContinuousBeam>(), __instance.transform.position, __instance.transform.rotation, __instance.transform);
                continuousBeam.safeEnemyType = __instance.safeEnemyType;
                continuousBeam.target = __instance.target;
                continuousBeam.endPoint = alreadyExistingProjs[alreadyExistingProjs.Count - 1].transform;
                __instance.connectedBeams.Add(continuousBeam);
                if (alreadyExistingProjs[alreadyExistingProjs.Count - 1].transform.TryGetComponent<Projectile>(out Projectile component))
                {
                    component.connectedBeams.Add(continuousBeam);
                }
                
                alreadyExistingBeams.Add(continuousBeam);
            }
            alreadyExistingProjs.Add(__instance);
        }
    }
}
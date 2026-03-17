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
        [HarmonyPatch(nameof(LeviathanHead.BeamAttack))]
        [HarmonyPostfix]
        public static void UseTailWhileBeam(LeviathanHead __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.lcon.difficulty)) return;
            __instance.lcon.stopTail = false;
        }

        [HarmonyPatch(nameof(LeviathanHead.BeamAttack))]
        [HarmonyPrefix]
        public static void HarderBeam(LeviathanHead __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.lcon.difficulty)) return;
                __instance.beam.GetComponent<ContinuousBeam>().beamWidth *= 1.5f;
            __instance.beam.GetComponent<LineRenderer>().widthMultiplier *= 1.5f;
        }

        [HarmonyPatch(nameof(LeviathanHead.Update))]
        [HarmonyPrefix]
        public static void ExplodyBeam(LeviathanHead __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.lcon.difficulty)) return;
            if (__instance.beamTime > 0f)
            {
                if(__instance.TryGetComponent<LeviathanCooldowns>(out LeviathanCooldowns levi))
                {
                    if(levi.t <= 0)
                    {
                        // Spawn an explosion
                        if(PortalPhysicsV2.Raycast(__instance.beam.transform.position,
                            __instance.beam.transform.forward, out PhysicsCastResult hit, 10000, LayerMaskDefaults.Get(LMD.EnvironmentAndPlayer)))
                        {
                            Object.Instantiate(BananaDifficultyPlugin.lightningExplosion, hit.point, Quaternion.identity);
                        }

                        levi.t = Mathf.Max(__instance.beamTime / 3, 0.5f);
                    }
                }
            }
        }
        [HarmonyPatch(nameof(LeviathanHead.Start))]
        [HarmonyPrefix]
        public static void ApplyLeviCooldowns(LeviathanHead __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.lcon.difficulty)) return;
            __instance.gameObject.AddComponent<LeviathanCooldowns>();
        }

        [HarmonyPatch(nameof(LeviathanHead.SetSpeed))]
        [HarmonyPostfix]
        public static void Faster(LeviathanHead __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.lcon.difficulty)) return;
            __instance.anim.speed *= 1.5f;
        }
    }

    public class LeviathanCooldowns : MonoBehaviour
    {
        public float t = 0;

        void Update()
        {
            t -= Time.deltaTime;
        }
    }
}
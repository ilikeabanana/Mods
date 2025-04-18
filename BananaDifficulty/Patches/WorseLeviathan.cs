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
        [HarmonyPatch(nameof(LeviathanHead.BeamStart))]
        [HarmonyPostfix]
        public static void ExtendedBeamTime(LeviathanHead __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.lcon.difficulty)) return;
            __instance.beamTime *= 1.5f;
        }
        [HarmonyPatch(nameof(LeviathanHead.BeamAttack))]
        [HarmonyPostfix]
        public static void UseTailWhileBeam(LeviathanHead __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.lcon.difficulty)) return;
            __instance.lcon.stopTail = false;
        }
    }
}
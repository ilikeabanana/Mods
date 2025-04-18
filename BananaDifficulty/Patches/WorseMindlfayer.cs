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
    [HarmonyPatch(typeof(Mindflayer))]
    internal class WorseMindlfayer
    {
        [HarmonyPatch(nameof(Mindflayer.StartBeam))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Mindflayer __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;


            __instance.tempBeam.GetComponent<ContinuousBeam>().beamWidth *= 3;
            __instance.tempBeam.GetComponent<ContinuousBeam>().ignoreInvincibility = true;
            __instance.tempBeam.GetComponent<LineRenderer>().widthMultiplier *= 3;
        }
    }
}
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
    [HarmonyPatch(typeof(CancerousRodent))]
    internal class WorseRodent
    {
        private static Dictionary<CancerousRodent, float> lastDamageTimes = new Dictionary<CancerousRodent, float>();
        private static float damageInterval = 0.35f; // Interval in seconds

        [HarmonyPatch(nameof(CancerousRodent.Update))]
        [HarmonyPostfix]
        public static void Awake_Postfix(CancerousRodent __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.eid.difficulty)) return;

            if (!lastDamageTimes.ContainsKey(__instance))
            {
                lastDamageTimes[__instance] = Time.time;
            }

            if (Time.time - lastDamageTimes[__instance] >= damageInterval)
            {
                DamagePlayer(__instance);
                lastDamageTimes[__instance] = Time.time;
            }
        }

        private static void DamagePlayer(CancerousRodent rodent)
        {
            // Assuming there's a method to damage the player
            MonoSingleton<NewMovement>.instance.GetHurt(10, false); // Example damage value
        }
    }
}
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BananaDifficulty.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    [HarmonyPatch(typeof(ZombieMelee))]
    internal class TeleportOnFall
    {
        // Static variable to track the last teleport time
        private static Dictionary<ZombieMelee, float> lastTeleportTimes = new Dictionary<ZombieMelee, float>();
        private const float TELEPORT_COOLDOWN = 5f; // 5 seconds cooldown, adjust as needed

        [HarmonyPatch(nameof(ZombieMelee.Update))]
        [HarmonyPostfix]
        public static void Awake_Prefix(ZombieMelee __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            // Check if the zombie is falling
            if (__instance.zmb.falling)
            {
                // Get the current time
                float currentTime = Time.time;

                // Check if this zombie has teleported before
                if (!lastTeleportTimes.ContainsKey(__instance))
                {
                    lastTeleportTimes[__instance] = -TELEPORT_COOLDOWN; // Allow first teleport immediately
                }

                // Check if cooldown has elapsed
                if (currentTime - lastTeleportTimes[__instance] >= TELEPORT_COOLDOWN)
                {
                    // Teleport the zombie
                    __instance.transform.position = __instance.eid.target.position;
                    __instance.zmb.falling = false;

                    // Update the last teleport time
                    lastTeleportTimes[__instance] = currentTime;
                }
            }
        }
    }
}
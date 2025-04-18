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
    [HarmonyPatch(typeof(PhysicalShockwave))]
    internal class DoubleShockwaves
    {
        [HarmonyPatch(nameof(PhysicalShockwave.Start))]
        [HarmonyPostfix]
        public static void Awake_Prefix(PhysicalShockwave __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(-1)) return;
            if (__instance.gameObject.name == "DoubleShock") return;
            if (!BananaDifficultyPlugin.HardMode.Value) return;
            PhysicalShockwave shock = Object.Instantiate(__instance);
            shock.gameObject.name = "DoubleShock";
            shock.transform.Rotate(new Vector3(0, 0, 90));
        }
    }
}
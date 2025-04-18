using HarmonyLib;

namespace BananaDifficulty.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch]
    internal class SecuritySystems
    {
        [HarmonyPatch(typeof(MortarLauncher), nameof(MortarLauncher.Start))]
        [HarmonyPostfix]
        public static void FasterFiring(MortarLauncher __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            __instance.difficultySpeedModifier = 8f;
        }
    }
}
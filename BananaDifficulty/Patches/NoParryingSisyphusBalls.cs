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
    internal class NoParryingSisyphusBalls
    {
        [HarmonyPatch(typeof(Cannonball))]
        [HarmonyPatch(nameof(Cannonball.Launch))]
        [HarmonyPrefix]
        public static void Launch_Prefix(Cannonball __instance)
        {
            int dif = __instance.sisy != null ? __instance.sisy.difficulty : 0;
            if (!BananaDifficultyPlugin.CanUseIt(dif)) return;
            if (__instance.sisy)
            {
                __instance.launchable = false;
            }
        }
        [HarmonyPatch(typeof(Sisyphus))]
        [HarmonyPatch(nameof(Sisyphus.Knockdown))]
        [HarmonyPrefix]
        public static bool Knockdown_Prefix(Sisyphus __instance)
        {
            return !BananaDifficultyPlugin.CanUseIt(__instance.difficulty);


        }
    }
}
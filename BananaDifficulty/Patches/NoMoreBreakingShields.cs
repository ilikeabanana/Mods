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
    [HarmonyPatch(typeof(Gutterman))]
    internal class NoMoreBreakingShields
    {
        [HarmonyPatch(nameof(Gutterman.ShieldBreak))]
        [HarmonyPrefix]
        public static bool Awake_Prefix(Gutterman __instance)
        {
            return !BananaDifficultyPlugin.CanUseIt(__instance.difficulty);
        }

        [HarmonyPatch(nameof(Gutterman.Update))]
        [HarmonyPrefix]
        public static void NahDontWindUp(Gutterman __instance)
        {
            if(!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;


            __instance.windup = 1;
            __instance.trackingSpeedMultiplier = 100;
        }
    }
}
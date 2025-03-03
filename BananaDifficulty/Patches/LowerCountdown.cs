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
    [HarmonyPatch(typeof(Countdown))]
    internal class LowerCountdown
    {
        [HarmonyPatch(nameof(Countdown.GetCountdownLength))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Countdown __instance, float __result)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __result = __result - (__result * 0.15f);
        }

        [HarmonyPatch(nameof(Countdown.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(Countdown __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.time != 0f && __instance.done)
            {
                __instance.time = __instance.time - (__instance.time * 0.15f);
            }
        }
    }
}
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
    [HarmonyPatch(typeof(ScreenDistortionField))]
    internal class MoreGlitchy
    {

        /// <summary>
        /// Patches the Player Awake method with postfix code.
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(ScreenDistortionField.Start))]
        [HarmonyPostfix]
        public static void Awake_Postfix(ScreenDistortionField __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return;
            __instance.strength *= 3;
        }
    }
}
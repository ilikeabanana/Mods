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
    [HarmonyPatch(typeof(Minotaur))]
    internal class WorseMinotaur
    {
        [HarmonyPatch(nameof(Minotaur.GotParried))]
        [HarmonyPrefix]
        public static bool Parried_Prefix(Minotaur __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return true;

            __instance.mach.GetHurt(__instance.GetComponentInChildren<EnemyIdentifierIdentifier>().gameObject, Vector3.zero, 20f, 0f, null, false);

            return false;
        }

        [HarmonyPatch(nameof(Minotaur.MeatSplash))]
        [HarmonyPostfix]
        public static void MeatLow_Prefix(Minotaur __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            GameObject gameObject = Object.Instantiate<GameObject>((__instance.difficulty >= 4) ? __instance.toxicCloudLong : __instance.toxicCloud, __instance.meatInHand.transform.position, Quaternion.identity);
            gameObject.transform.SetParent(__instance.gz.transform, true);
        }
        [HarmonyPatch(nameof(Minotaur.MeatExplode))]
        [HarmonyPostfix]
        public static void MeatHigh_Prefix(Minotaur __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            GameObject gameObject = Object.Instantiate<GameObject>((__instance.difficulty >= 4) ? __instance.goopLong : __instance.goop, new Vector3(__instance.meatInHand.transform.position.x, __instance.transform.position.y, __instance.meatInHand.transform.position.z), Quaternion.identity);
            gameObject.transform.SetParent(__instance.gz.transform, true);
        }
    }
}
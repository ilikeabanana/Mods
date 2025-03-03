using BananaDifficulty.Utils;
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
    [HarmonyPatch(typeof(Wicked))]
    internal class WickedShot
    {
        [HarmonyPatch(nameof(Wicked.GetHit))]
        [HarmonyPrefix]
        public static bool Awake_Prefix(Wicked __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return true;
            if (!__instance.gameObject.activeInHierarchy)
            {
                return false;
            }
            Object.Instantiate<GameObject>(__instance.hitSound, __instance.transform.position, Quaternion.identity);
            Vector3 vector = ModUtils.GetRandomNavMeshPoint(__instance.transform.position, 25);
            if (__instance.eid && __instance.eid.hooked)
            {
                Debug.Log("Hooked");
                MonoSingleton<HookArm>.Instance.StopThrow(1f, true);
            }
            MonoSingleton<BestiaryData>.Instance.SetEnemy(EnemyType.Wicked, 2);
            if (__instance.aud.isPlaying)
            {
                __instance.aud.Stop();
            }
            __instance.nma.Warp(vector);
            __instance.playerSpotTime = 0f;
            return false;
        }
    }
}
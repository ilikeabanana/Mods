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
    [HarmonyPatch(typeof(Guttertank))]
    internal class GutterTankNeverFall
    {

        [HarmonyPatch(nameof(Guttertank.PunchStop))]
        [HarmonyPrefix]
        public static bool Awake_Postfix(Guttertank __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return true;
            __instance.sc.DamageStop();
            __instance.moveForward = false;
            if (!__instance.punchHit || __instance.difficulty < 3)
            {
                bool flag = __instance.difficulty < 4 && !__instance.punchHit;
                if (!flag && (!__instance.punchHit || __instance.difficulty < 3))
                {
                    Vector3Int vector3Int = StainVoxelManager.WorldToVoxelPosition(__instance.transform.position + Vector3.down * 1.8333334f);
                    flag = MonoSingleton<StainVoxelManager>.Instance.HasProxiesAt(vector3Int, 3, VoxelCheckingShape.VerticalBox, ProxySearchMode.AnyFloor, true);
                }
            }
            __instance.punchCooldown = 0;
            return false;
        }
    }
}
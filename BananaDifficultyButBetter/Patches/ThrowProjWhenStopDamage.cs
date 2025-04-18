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
    [HarmonyPatch(typeof(Ferryman))]
    internal class ThrowProjWhenStopDamage
    {
        [HarmonyPatch(nameof(Ferryman.StopDamage))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Ferryman __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (!__instance.useKick && !__instance.useOar) return;
            GameObject MeWhenWhenMySnakeIsSolid = Object.Instantiate(BananaDifficultyPlugin.snakeProj, __instance.mach.chest.transform.position, Quaternion.identity);

            Projectile SolidProjectile = MeWhenWhenMySnakeIsSolid.GetComponentInChildren<Projectile>();
            if(SolidProjectile != null)
            {
                SolidProjectile.target = __instance.eid.target;
                SolidProjectile.safeEnemyType = EnemyType.Ferryman;
            }
            

            MeWhenWhenMySnakeIsSolid.transform.SetParent(__instance.eid.gz.transform);

        }
    }
}
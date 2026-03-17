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
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return true;
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

        [HarmonyPatch(nameof(Guttertank.PlaceMine))]
        [HarmonyPrefix]
        public static void PlaceMine_Prefix(Guttertank __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            // spawn kabiem
            FireProjectile(__instance, 0);
            FireProjectile(__instance, -10);
            FireProjectile(__instance, 10);
        }

        [HarmonyPatch(typeof(Guttertank), nameof(Guttertank.FireRocket))]
        [HarmonyPrefix]
        public static void FireRocket_Prefix(Guttertank __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            GameObject shck = Object.Instantiate(BananaDifficultyPlugin.shockwave, __instance.transform.position, Quaternion.identity);
            if(shck.TryGetComponent<PhysicalShockwave>(out PhysicalShockwave shockwave))
            {
                shockwave.enemy = true;
                shockwave.enemyType = __instance.eid.enemyType;
                shockwave.noDamageToEnemy = true;
            }
        }

        static void FireProjectile(Guttertank __instance, float angle)
        {
            GameObject extraHoming = Object.Instantiate(BananaDifficultyPlugin.homingHH, __instance.shootPoint.position, Quaternion.identity);
            Projectile projHHChildren = extraHoming.GetComponentInChildren<Projectile>();

            if (projHHChildren != null)
            {
                projHHChildren.targetHandle = __instance.targetHandle;
                projHHChildren.safeEnemyType = EnemyType.Guttertank;
                projHHChildren.speed *= 1f;
                projHHChildren.damage *= __instance.eid.totalDamageModifier;
            }

            if (extraHoming.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
            {
                rigidbody.AddForce(Vector3.up * 50f, ForceMode.VelocityChange);
            }

            extraHoming.transform.LookAt(__instance.targetPosition);

            extraHoming.transform.Rotate(Vector3.up, angle);
        }
    }
}
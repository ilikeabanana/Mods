using HarmonyLib;
using System.Collections;
using ULTRAKILL.Enemy;
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
    [HarmonyPatch(typeof(Mass))]
    internal class WorseMass
    {
        [HarmonyPatch(nameof(Mass.ShootSpear))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Mass __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.eid.target == null || __instance.dead || __instance.difficulty == 0) return;

            __instance.StartCoroutine(ShootSpearsWithDelay(__instance));
        }

        [HarmonyPatch(nameof(Mass.ShootProjectile))]
        [HarmonyPostfix]
        public static void Shoot_Postfix(Mass __instance, int arm, GameObject projectile, float velocity)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.dead || __instance.eid.target == null)
            {
                return;
            }
            Transform transform = __instance.shootPoints[arm];
            GameObject gameObject = Object.Instantiate<GameObject>(BananaDifficultyPlugin.projNormal, transform.position, transform.rotation);
            gameObject.transform.LookAt(__instance.targetPos, Vector3.up);
            gameObject.transform.localScale *= 2;
            Projectile projectile2;
            if (gameObject.TryGetComponent<Projectile>(out projectile2))
            {
                projectile2.target = __instance.eid.target;
                projectile2.safeEnemyType = EnemyType.HideousMass;
                projectile2.transform.SetParent(__instance.stat.GetGoreZone().transform, true);
                projectile2.damage *= 1.75f;
                projectile2.damage *= __instance.eid.totalDamageModifier;
                projectile2.unparryable = true;
                projectile2.speed *= 2;
                projectile2.ignoreExplosions = true;
            }

            if (gameObject.TryGetComponent<MeshRenderer>(out MeshRenderer rend))
            {
                Material mat = new Material(rend.sharedMaterial);

                mat.color = Color.black;
                rend.sharedMaterial = mat;
            }
        }

        private static IEnumerator ShootSpearsWithDelay(Mass instance)
        {
            int spearCount = 3;
            float delayBetweenShots = 0.25f;

            // Use Harmony's Traverse to grab the private target data the spear needs
            var lastTargetData = instance.lastTargetData;

            for (int i = 0; i < spearCount; i++)
            {
                yield return new WaitForSeconds(delayBetweenShots);

                if (instance.dead || instance.eid.target == null) yield break;

                BananaDifficultyPlugin.Log.LogInfo($"Firing extra spear {i + 1}");

                // Instantiate but DO NOT assign to instance.tempSpear 
                // We use a local variable so we don't overwrite the boss's internal tracking
                GameObject extraSpear = Object.Instantiate(instance.spear, instance.tailSpear.transform.position, instance.tailEnd.rotation);
                extraSpear.transform.LookAt(instance.eid.target.position);

                if (extraSpear.TryGetComponent<MassSpear>(out MassSpear massSpear))
                {
                    massSpear.target = instance.eid.target;
                    // CRITICAL: The spear won't move without this handle!
                    massSpear.targetHandle = lastTargetData.handle;

                    massSpear.originPoint = instance.tailSpear.transform;
                    massSpear.damageMultiplier = instance.eid.totalDamageModifier;
                    massSpear.difficulty = instance.difficulty;

                    if (instance.difficulty >= 4)
                    {
                        massSpear.spearHealth *= 3f;
                    }
                }
            }
        }
    }
}
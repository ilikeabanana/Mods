using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BananaDifficulty.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(ZombieProjectiles))]
    internal class ExponentialStray
    {
        // Dictionary to track projectile counts per zombie instance
        private static readonly Dictionary<int, int> zombieThrowCounts = new Dictionary<int, int>();

        [HarmonyPatch(nameof(ZombieProjectiles.ThrowProjectile), new System.Type[] { })]
        [HarmonyPostfix]
        public static void Awake_Prefix(ZombieProjectiles __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            switch (__instance.eid.enemyType)
            {
                case EnemyType.Stray:
                    FireStray(__instance);
                    break;
                case EnemyType.Soldier:
                    FireSoldier(__instance);
                    break;
            }

        }

        [HarmonyPatch(nameof(ZombieProjectiles.ShootProjectile))]
        [HarmonyPostfix]
        public static void Shoot_Postfix(ZombieProjectiles __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            switch (__instance.eid.enemyType)
            {
                case EnemyType.Schism:
                    FireSchism(__instance);
                    break;
            }

        }



        private static void FireProjectileAtAngle(GameObject projectile, float angleOffset, ZombieProjectiles __instance)
        {
            // Create a new projectile instance
            GameObject newProjectile = Object.Instantiate<GameObject>(projectile, projectile.transform.position, projectile.transform.rotation);

            // Rotate the new projectile by the specified angle offset
            newProjectile.transform.Rotate(Vector3.up, angleOffset);

            // Get the Projectile component and update the target
            Projectile componentInChildren = newProjectile.GetComponentInChildren<Projectile>();
            if (componentInChildren != null)
            {
                componentInChildren.target = __instance.eid.target;
            }
        }

        // Track last homingHH fire time per schism
        private static readonly Dictionary<int, float> schismLastHomingTime = new Dictionary<int, float>();


        static void FireSchism(ZombieProjectiles __instance)
        {
            int id = __instance.GetInstanceID();

            // Instantiate the projectile
            __instance.currentProjectile = Object.Instantiate<GameObject>(__instance.projectile, __instance.shootPos.position, Quaternion.identity);
            Projectile componentInChildren = __instance.currentProjectile.GetComponentInChildren<Projectile>();

            if (componentInChildren != null)
            {
                componentInChildren.targetHandle = __instance.targetHandle;
                componentInChildren.safeEnemyType = EnemyType.Schism;
                componentInChildren.speed *= GetSpeedMultiplier(__instance.difficulty);
                componentInChildren.damage *= __instance.eid.totalDamageModifier;
            }

            Vector3 worldPosition = GetTargetPosition(__instance);
            __instance.currentProjectile.transform.LookAt(worldPosition);

            FireProjectileAtAngle(__instance.currentProjectile, -10f, __instance);
            if (BananaDifficultyPlugin.HardMode.Value)
            {
                FireProjectileAtAngle(__instance.currentProjectile, 10f, __instance);
            }
            

            // --- HOMING PROJECTILE COOLDOWN ---
            if ((!schismLastHomingTime.TryGetValue(id, out float lastTime) || Time.time - lastTime >= 1.25f) || BananaDifficultyPlugin.HardMode.Value)
            {
                schismLastHomingTime[id] = Time.time;

                GameObject extraHoming = Object.Instantiate(BananaDifficultyPlugin.homingHH, __instance.shootPos.position, Quaternion.identity);
                Projectile projHHChildren = extraHoming.GetComponentInChildren<Projectile>();

                if (projHHChildren != null)
                {
                    projHHChildren.targetHandle = __instance.targetHandle;
                    projHHChildren.safeEnemyType = EnemyType.Schism;
                    projHHChildren.speed *= GetSpeedMultiplier(__instance.difficulty);
                    projHHChildren.damage *= __instance.eid.totalDamageModifier;
                }

                if (extraHoming.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
                {
                    rigidbody.AddForce(Vector3.up * 50f, ForceMode.VelocityChange);
                }

                extraHoming.transform.LookAt(worldPosition);
            }
        }

        static void FireSoldier(ZombieProjectiles __instance)
        {
            GameObject gregspawned = Object.Instantiate(BananaDifficultyPlugin.RocketEnemy, __instance.shootPos.position, Quaternion.identity);
            Vector3 directionToPlayer = (MonoSingleton<CameraController>.instance.transform.position - gregspawned.transform.position).normalized;

            Rigidbody rb = gregspawned.GetComponent<Rigidbody>();


            float magnitude = rb.velocity.magnitude;
            // Set velocity towards player with original magnitude
            rb.velocity = directionToPlayer * magnitude;
            gregspawned.transform.forward = directionToPlayer;
            gregspawned.transform.position += __instance.shootPos.forward * 3.5f;
            //gregspawned.GetComponent<Rigidbody>().AddForce(__instance.shootPos.forward * (60 + 10f), ForceMode.VelocityChange);
        }

        static void FireStray(ZombieProjectiles __instance)
        {

            // Get the zombie instance ID to track per-zombie projectile counts
            int zombieId = __instance.GetInstanceID();

            // Initialize or increment the throw count for this zombie
            if (!zombieThrowCounts.ContainsKey(zombieId))
            {
                zombieThrowCounts[zombieId] = 1;
                return; // First throw, just use the original projectile
            }

            // Increment the throw count
            zombieThrowCounts[zombieId]++;

            // Calculate how many additional projectiles to throw using exponential growth
            // 1, 3, 6, 10, 15, 21, etc.
            int projectileCount = (zombieThrowCounts[zombieId] * (zombieThrowCounts[zombieId] + 1)) / 2;

            // If only one projectile, just return
            if (projectileCount <= 1) return;

            projectileCount = projectileCount >= 12 ? 12 : projectileCount;

            // We already have one projectile from the original method, so fire (projectileCount - 1) more
            FireAdditionalProjectiles(__instance, projectileCount - 1);
        }

        private static void FireAdditionalProjectiles(ZombieProjectiles __instance, int count)
        {
            __instance.StartCoroutine(FireProjectilesWithDelay(__instance, count));
        }

        private static IEnumerator FireProjectilesWithDelay(ZombieProjectiles __instance, int count)
        {
            // Destroy any existing decorative projectile if it exists
            if (__instance.currentDecProjectile != null)
            {
                Object.Destroy(__instance.currentDecProjectile);
                __instance.eid.weakPoint = __instance.origWP;
            }

            // Get the target position
            Vector3 targetPosition = GetTargetPosition(__instance);

            // Calculate the angle step for even distribution
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                // Calculate angle for this projectile
                float angle = angleStep * i;

                // Instantiate a new projectile
                GameObject newProjectile = Object.Instantiate<GameObject>(__instance.projectile, __instance.shootPos.position, Quaternion.identity);

                // Set up the projectile properties
                Projectile componentInChildren = newProjectile.GetComponentInChildren<Projectile>();
                if (componentInChildren != null)
                {
                    componentInChildren.target = __instance.eid.target;
                    componentInChildren.safeEnemyType = EnemyType.Stray;
                    componentInChildren.speed *= GetSpeedMultiplier(__instance.difficulty);
                    componentInChildren.damage *= __instance.eid.totalDamageModifier;
                }

                // Make the projectile look at the target
                newProjectile.transform.LookAt(targetPosition);

                // Rotate the projectile by the calculated angle
                newProjectile.transform.Rotate(Vector3.up, angle);

                // Add force if needed (similar to the original implementation)
                Rigidbody rb = newProjectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(newProjectile.transform.forward * (60f + 10f), ForceMode.VelocityChange);
                }

                // Delay before firing the next projectile
                yield return new WaitForSeconds(0.1f);
            }
        }


        private static float GetSpeedMultiplier(int difficulty)
        {
            if (difficulty > 2)
                return 1.35f;
            else if (difficulty == 1)
                return 0.75f;
            else if (difficulty == 0)
                return 0.5f;
            else
                return 1f;
        }

        private static Vector3 GetTargetPosition(ZombieProjectiles __instance)
        {
            EnemyTarget target = __instance.eid.target;
            if (target != null && target.isPlayer)
            {
                if (__instance.difficulty >= 4)
                {
                    return MonoSingleton<PlayerTracker>.Instance.PredictPlayerPosition(
                        Vector3.Distance(__instance.transform.position, __instance.camObj.transform.position) /
                        (float)((__instance.difficulty == 5) ? 90 : Random.Range(110, 180)),
                        true, false);
                }
                else
                {
                    return __instance.camObj.transform.position;
                }
            }
            else if (__instance.eid.target != null)
            {
                EnemyIdentifierIdentifier componentInChildren = __instance.eid.target.targetTransform.GetComponentInChildren<EnemyIdentifierIdentifier>();
                if (componentInChildren)
                {
                    return componentInChildren.transform.position;
                }
                else
                {
                    return __instance.eid.target.position;
                }
            }

            return Vector3.zero;
        }
    }
}
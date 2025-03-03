using HarmonyLib;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(MinosPrime))]
    internal class WorseMinos
    {
        [HarmonyPatch(nameof(MinosPrime.FixedUpdate))]
        [HarmonyPostfix]
        public static void Postfix(MinosPrime __instance)
        {
            __instance.cooldown = 0;
        }
        [HarmonyPatch(nameof(MinosPrime.FixedUpdate))]
        [HarmonyPostfix]
        public static void YOUCANTESCAPE(MinosPrime __instance)
        {
            if (__instance.inAction) return;
            if (BananaDifficultyPlugin.CanUseIt(__instance.difficulty))
            {
                if (Vector3.Distance(__instance.transform.position, __instance.target.position) > 30)
                {
                    __instance.TeleportAnywhere();
                }
            }
        }
        [HarmonyPatch(nameof(MinosPrime.RiderKickActivate))]
        [HarmonyPostfix]
        public static void DoubleShocker(MinosPrime __instance)
        {
            RaycastHit raycastHit;
            Physics.Raycast(__instance.aimingBone.position, __instance.transform.forward, out raycastHit, 250f, LayerMaskDefaults.Get(LMD.Environment));
            GameObject gameObject;
            if (BananaDifficultyPlugin.CanUseIt(__instance.difficulty))
            {
                gameObject = Object.Instantiate<GameObject>(__instance.groundWave, raycastHit.point, Quaternion.identity);
                gameObject.transform.up = raycastHit.normal;
                gameObject.transform.SetParent(__instance.gz.transform);
                gameObject.transform.Rotate(Vector3.forward * 90f, Space.Self);
                PhysicalShockwave physicalShockwave;
                if (gameObject.TryGetComponent<PhysicalShockwave>(out physicalShockwave))
                {
                    physicalShockwave.enemyType = EnemyType.MinosPrime;
                    physicalShockwave.damage = Mathf.RoundToInt((float)physicalShockwave.damage * __instance.eid.totalDamageModifier);
                }
            }
        }
        [HarmonyPatch(nameof(MinosPrime.ProjectileShoot))]
        [HarmonyPostfix]
        public static void IGotTwoSnakes(MinosPrime __instance)
        {
            if (BananaDifficultyPlugin.CanUseIt(__instance.difficulty))
            {
                if (__instance.target == null)
                {
                    return;
                }

                // Base direction towards the target
                Vector3 baseDirection = __instance.target.position - (__instance.transform.position + Vector3.up);
                Quaternion baseRotation = Quaternion.LookRotation(baseDirection);

                // Define the angle offset for the two projectiles
                float angleOffset = 15f; // Adjust this value to change the spread angle

                // Create the first projectile
                Quaternion leftRotation = baseRotation * Quaternion.AngleAxis(-angleOffset, Vector3.up);
                GameObject leftProjectile = Object.Instantiate<GameObject>(
                    __instance.snakeProjectile,
                    __instance.mach.chest.transform.position,
                    __instance.snakeProjectile.transform.rotation
                );
                leftProjectile.transform.Rotate(Vector3.up, -angleOffset);
                ConfigureProjectile(leftProjectile, __instance);

                // Create the second projectile
                Quaternion rightRotation = baseRotation * Quaternion.AngleAxis(angleOffset, Vector3.up);
                GameObject rightProjectile = Object.Instantiate<GameObject>(
                    __instance.snakeProjectile,
                    __instance.mach.chest.transform.position,
                    __instance.snakeProjectile.transform.rotation
                );
                rightProjectile.transform.Rotate(Vector3.up, angleOffset);
                ConfigureProjectile(rightProjectile, __instance);

                // Reset tracking
                __instance.aiming = false;
                __instance.tracking = false;
                __instance.fullTracking = false;
            }
        }

        private static void ConfigureProjectile(GameObject projectile, MinosPrime __instance)
        {
            projectile.transform.SetParent(__instance.gz.transform);
            Projectile componentInChildren = projectile.GetComponentInChildren<Projectile>();
            if (componentInChildren)
            {
                componentInChildren.target = (__instance.target.isPlayer ?
                    new EnemyTarget(MonoSingleton<CameraController>.Instance.transform) :
                    __instance.target);
                componentInChildren.damage *= __instance.eid.totalDamageModifier;
            }
        }
    }
}
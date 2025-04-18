using BananaDifficulty.Utils;
using HarmonyLib;
using UnityEngine;
using System.Collections;

namespace BananaDifficulty.Patches
{ 
    [HarmonyPatch(typeof(FleshPrison))]
    internal class WorseFleshPrison
    {
        [HarmonyPatch(nameof(FleshPrison.SpawnFleshDrones))]
        [HarmonyPrefix]
        public static void Awake_Postfix(FleshPrison __instance)
        {
            if(!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.currentDrones.Count <= 0)
            {
                GameObject spawnedIdol = Object.Instantiate<GameObject>(BananaDifficultyPlugin.idol, ModUtils.GetRandomNavMeshPoint(__instance.transform.position, 20), Quaternion.identity);
                Idol idol;
                if (spawnedIdol.TryGetComponent<Idol>(out idol))
                {
                    idol.target = __instance.eid;
                    idol.eid.dontCountAsKills = true;
                }

                spawnedIdol.name += "DontRadiant";
            }
            
        }
        [HarmonyPatch(nameof(FleshPrison.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(FleshPrison __instance)
        {
            if(!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.inAction)
            {
                HandleShooting(__instance);
            }

        }




        static void HandleShooting(FleshPrison __instance)
        {
            if (!BananaDifficultyPlugin.HardMode.Value) return;
            if (__instance.currentProjectile < __instance.projectileAmount)
            {
                __instance.homingProjectileCooldown = Mathf.MoveTowards(__instance.homingProjectileCooldown, 0f, Time.deltaTime * (Mathf.Abs(__instance.rotationSpeed) / 10f) * __instance.eid.totalSpeedModifier);
                if (__instance.homingProjectileCooldown <= 0f)
                {
                    if (__instance.altVersion)
                    {
                        __instance.StartCoroutine(FireBlueHomingProjectiles(__instance));

                    }
                    else
                    {
                        FireRegularHomingProjectile(__instance);
                    }
                }
            }
        }

        static IEnumerator FireBlueHomingProjectiles(FleshPrison __instance)
        {
            for (int i = 0; i < 25; i++)
            {
                GameObject gameObject2 = Object.Instantiate<GameObject>(BananaDifficultyPlugin.homingBlue, __instance.rotationBone.position + __instance.rotationBone.up * 8f, __instance.rotationBone.rotation);
                Projectile component = gameObject2.GetComponent<Projectile>();
                component.target = __instance.eid.target;
                component.safeEnemyType = EnemyType.FleshPanopticon;
                gameObject2.transform.SetParent(__instance.transform, true);
                Rigidbody rigidbody;
                if (gameObject2.TryGetComponent<Rigidbody>(out rigidbody))
                {
                    rigidbody.AddForce(Vector3.up * 50f, ForceMode.VelocityChange);
                }
                yield return new WaitForSeconds(1 / 25);
            }
        }

        static void FireRegularHomingProjectile(FleshPrison __instance)
        {
            GameObject gameObject2 = Object.Instantiate<GameObject>(BananaDifficultyPlugin.homingHH, __instance.rotationBone.position + __instance.rotationBone.up * 8f, __instance.rotationBone.rotation);
            Projectile component = gameObject2.GetComponent<Projectile>();
            component.target = __instance.eid.target;
            component.safeEnemyType = EnemyType.FleshPrison;
            gameObject2.transform.SetParent(__instance.transform, true);
            Rigidbody rigidbody;
            if (gameObject2.TryGetComponent<Rigidbody>(out rigidbody))
            {
                rigidbody.AddForce(Vector3.up * 50f, ForceMode.VelocityChange);
            }
        }
    }
}
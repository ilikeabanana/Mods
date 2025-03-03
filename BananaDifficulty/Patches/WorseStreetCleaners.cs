using HarmonyLib;
using System.Collections.Generic;
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
    [HarmonyPatch(typeof(Streetcleaner))]
    internal class WorseStreetCleaners
    {
        private static Dictionary<Streetcleaner, float> customCooldowns = new Dictionary<Streetcleaner, float>();

        [HarmonyPatch(nameof(Streetcleaner.Update))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Streetcleaner __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.eid.dead) return;

            if (!__instance.target.isValid) return;
            if (!customCooldowns.ContainsKey(__instance))
            {
                customCooldowns[__instance] = 3f;
            }

            if (customCooldowns[__instance] > 0)
            {
                customCooldowns[__instance] -= Time.deltaTime;
                return;
            }

            if (Vector3.Distance(MonoSingleton<NewMovement>.Instance.transform.position, __instance.transform.position) > 10 && !__instance.attacking)
            {
                
                GameObject gregspawned = Object.Instantiate(BananaDifficultyPlugin.RocketEnemy, __instance.transform.position, Quaternion.identity);
                Vector3 directionToPlayer = (__instance.target.position - gregspawned.transform.position).normalized;

                Rigidbody rb = gregspawned.GetComponent<Rigidbody>();
                Grenade greg = gregspawned.GetComponent<Grenade>();
                float magnitude = rb.velocity.magnitude;

                // Set velocity towards player with original magnitude
                rb.velocity = directionToPlayer * magnitude;
                gregspawned.transform.forward = directionToPlayer;
                gregspawned.transform.position += ( __instance.transform.forward * 7.5f) + new Vector3(0, 2);

                greg.ignoreEnemyType = new List<EnemyType>() { EnemyType.Streetcleaner };

                __instance.anim.SetTrigger("Deflect");

                customCooldowns[__instance] = 3f; // Set custom cooldown time
            }
        }
    }
    [HarmonyPatch(typeof(BulletCheck))]
    public class WorseStreetCleanerBlocker
    {

        [HarmonyPatch(nameof(BulletCheck.OnTriggerEnter))]
        [HarmonyPrefix]
        public static bool Awake_Prefix(BulletCheck __instance, Collider other)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return true;
            Streetcleaner streetcleaner24 = __instance.sc;
            if (streetcleaner24 == null)
            {
                return true;
            }
            CheckerType checkerType = __instance.type;
            if (checkerType != CheckerType.Streetcleaner)
            {
                return true;
            }
            else if (other.gameObject.layer == 14)
            {
                Grenade component2 = other.GetComponent<Grenade>();
                if (component2 != null)
                {
                    component2.enemy = true;
                    component2.CanCollideWithPlayer(true);
                    Streetcleaner streetcleaner = __instance.sc;
                    if (streetcleaner != null)
                    {
                        streetcleaner.DeflectShot();
                    }
                    Rigidbody component3 = other.GetComponent<Rigidbody>();
                    float magnitude = component3.velocity.magnitude;

                    // Calculate direction to player camera
                    Vector3 directionToPlayer = (MonoSingleton<CameraController>.instance.transform.position - other.transform.position).normalized;

                    // Set velocity towards player with original magnitude
                    component3.velocity = directionToPlayer * magnitude;
                    other.transform.forward = directionToPlayer;

                    __instance.aud.Play();
                    return false;
                }
                Streetcleaner streetcleaner2 = __instance.sc;
                if (streetcleaner2 == null)
                {
                    return false;
                }
                streetcleaner2.Dodge();
                return false;
            }
            return false;
        }
    }

}
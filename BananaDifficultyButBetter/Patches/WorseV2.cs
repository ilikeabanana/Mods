using BananaDifficulty.MonoBehaviours;
using HarmonyLib;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(V2))]
    internal class WorseV2
    {
        public static Dictionary<V2, bool> threwSpear = new Dictionary<V2, bool>();
        public static Dictionary<V2, float> spearCooldowns = new Dictionary<V2, float>();
        public static Dictionary<V2, float> knockbackCooldowns = new Dictionary<V2, float>();

        [HarmonyPatch(nameof(V2.Update))]
        [HarmonyPostfix]
        public static void Awake_Postfix(V2 __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            float distanceToPlayer = Vector3.Distance(__instance.transform.position, MonoSingleton<NewMovement>.Instance.transform.position);
            float currentTime = Time.time;

            if (distanceToPlayer > 20)
            {
                /*
                if (threwSpear.ContainsKey(__instance) && threwSpear[__instance])
                {
                    return;
                }
                if (spearCooldowns.ContainsKey(__instance) && currentTime < spearCooldowns[__instance])
                {
                    return;
                }
                threwSpear[__instance] = true;
                spearCooldowns[__instance] = currentTime + 5.0f; // 5 seconds cooldown
                Object.Instantiate<GameObject>(__instance.gunFlash, __instance.aimAtTarget[1].transform.position, Quaternion.LookRotation(__instance.target.position - __instance.aimAtTarget[1].transform.position)).transform.localScale *= 20f;
                __instance.StartCoroutine(ThrowSpearWithDelay(__instance));*/
            }
            else if (distanceToPlayer < 10)
            {
                if (knockbackCooldowns.ContainsKey(__instance) && currentTime < knockbackCooldowns[__instance])
                {
                    return;
                }
                knockbackCooldowns[__instance] = currentTime + 5.0f; // 5 seconds cooldown
                Object.Instantiate<GameObject>(__instance.altFlash, __instance.aimAtTarget[1].transform.position, Quaternion.LookRotation(__instance.target.position - __instance.aimAtTarget[1].transform.position)).transform.localScale *= 20f;
                __instance.StartCoroutine(ApplyKnockbackAndDamage(__instance));
            }
        }

        private static IEnumerator ThrowSpearWithDelay(V2 __instance)
        {
            yield return new WaitForSeconds(1.0f); // 1 second delay

            GameObject tempSpear = Object.Instantiate(BananaDifficultyPlugin.spear, __instance.mac.chest.transform.position, Quaternion.identity);

            // Orient the spear to point at the player
            tempSpear.transform.LookAt(MonoSingleton<NewMovement>.Instance.transform.position);
            V2Spear v2Spear;
            if (tempSpear.TryGetComponent<V2Spear>(out v2Spear))
            {
                v2Spear.target = __instance.eid.target;
                v2Spear.originPoint = __instance.mac.chest.transform;
                v2Spear.damageMultiplier = __instance.eid.totalDamageModifier;
                if (__instance.difficulty >= 4)
                {
                    v2Spear.spearHealth *= 2f;
                }
            }
        }

        private static IEnumerator ApplyKnockbackAndDamage(V2 __instance)
        {
            yield return new WaitForSeconds(0.5f); // 0.5 second delay

            if (Vector3.Distance(__instance.transform.position, MonoSingleton<NewMovement>.Instance.transform.position) < 50)
            {
                // Apply knockback and damage to the player
                MonoSingleton<NewMovement>.Instance.LaunchFromPoint(__instance.transform.position, 50f); // Example knockback force
                MonoSingleton<NewMovement>.Instance.GetHurt(50, false); // Example damage value
            }
        }
    }
}
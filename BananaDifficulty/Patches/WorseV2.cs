using BananaDifficulty.MonoBehaviours;
using HarmonyLib;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(V2))]
    internal class WorseV2
    {
        public static Dictionary<V2, bool> threwSpear = new Dictionary<V2, bool>();
        public static Dictionary<V2, float> spearCooldowns = new Dictionary<V2, float>();
        public static Dictionary<V2, float> knockbackCooldowns = new Dictionary<V2, float>();

        [HarmonyPatch(nameof(V2.Update))]
        [HarmonyPrefix]
        public static bool Awake_Postfix(V2 __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return true;

            if(__instance.inIntro) return true;
            float distanceToPlayer = Vector3.Distance(__instance.transform.position, MonoSingleton<NewMovement>.Instance.transform.position);
            float currentTime = Time.time;
            /*
            if (threwSpear.ContainsKey(__instance))
            {
                if (threwSpear[__instance])
                {
                    __instance.rb.isKinematic = true;
                    __instance.rb.velocity = Vector3.zero;
                    return false;
                }

            }
            else
            {
                __instance.rb.isKinematic = false;
            }*/

            if (distanceToPlayer > 20)
            {
                if (!__instance.weapons.Any((x) => x.gameObject.name.Contains("Nailgun"))) return true;
                if (threwSpear.ContainsKey(__instance) && threwSpear[__instance])
                {
                    return true;
                }
                if (spearCooldowns.ContainsKey(__instance) && currentTime < spearCooldowns[__instance])
                {
                    return true;
                }
                __instance.rb.velocity = Vector3.zero;
                threwSpear[__instance] = true;
                spearCooldowns[__instance] = currentTime + 5.0f; // 5 seconds cooldown
                Object.Instantiate<GameObject>(__instance.altFlash, __instance.aimAtTarget[1].transform.position, Quaternion.LookRotation(__instance.target.position - __instance.aimAtTarget[1].transform.position)).transform.localScale *= 20f;
                __instance.StartCoroutine(ThrowSpearWithDelay(__instance));
            }
            else if (distanceToPlayer < 10)
            {
                if (__instance.weapons.Any((x) => x.gameObject.name.Contains("Nailgun"))) return true;
                if (knockbackCooldowns.ContainsKey(__instance) && currentTime < knockbackCooldowns[__instance])
                {
                    return true;
                }
                knockbackCooldowns[__instance] = currentTime + 5.0f; // 5 seconds cooldown
                Object.Instantiate<GameObject>(BananaDifficultyPlugin.v2FlashUnpariable, __instance.aimAtTarget[1].transform.position, Quaternion.LookRotation(__instance.target.position - __instance.aimAtTarget[1].transform.position)).transform.localScale *= 20f;
                __instance.StartCoroutine(ApplyKnockbackAndDamage(__instance));
            }
            return true;
        }

        private static IEnumerator ThrowSpearWithDelay(V2 __instance)
        {
            yield return new WaitForSeconds(1.0f); // 1 second delay  

            Object.Instantiate(BananaDifficultyPlugin.WhiplashThrow, __instance.transform.position, Quaternion.identity);

            LineRenderer lineRenderer = new GameObject("SpearRay").AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.5f;
            lineRenderer.endWidth = 0.5f;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, __instance.eid.weakPoint.transform.position);
            lineRenderer.material = BananaDifficultyPlugin.WhiplashMat;
            lineRenderer.startColor = new Color(26, 26, 26);
            lineRenderer.endColor = new Color(26, 26, 26);
            float spearSpeed = 150; // Speed of the spear
            Vector3 targetPosition = __instance.target.PredictTargetPosition(Vector3.Distance(__instance.eid.weakPoint.transform.position, __instance.target.position) / spearSpeed);
            Vector3 direction = (targetPosition - __instance.eid.weakPoint.transform.position).normalized;
            float distance = 0f;

            AudioSource source = lineRenderer.gameObject.AddComponent<AudioSource>();
            source.clip = BananaDifficultyPlugin.WhiplashLoop;
            source.loop = true;
            source.playOnAwake = true;
            source.Play();

            int layerMask = (1 << 24) | (1 << 25) | (1 << 12) | (1 << 8) | (1 << 2) | (1 << 18);

            while (distance < 500f) // 50 meters max distance  
            {
                distance += spearSpeed * Time.deltaTime; // 200 meters per second
                Vector3 currentPosition = __instance.eid.weakPoint.transform.position + direction * distance;
                lineRenderer.SetPosition(1, currentPosition);
                lineRenderer.SetPosition(0, __instance.eid.weakPoint.transform.position);

                if (MonoSingleton<FistControl>.Instance.currentPunch.activeFrames > 0)
                {
                    Punch punch = MonoSingleton<FistControl>.Instance.currentPunch;
                    BananaDifficultyPlugin.Log.LogInfo("Punch type: " + punch.type);
                    if (punch.type == FistType.Standard && Vector3.Distance(currentPosition, punch.transform.position) <= 25)
                    {
                        BananaDifficultyPlugin.Log.LogInfo("Parried whiplash!");
                        punch.parriedSomething = true;
                        punch.hitSomething = true;
                        MonoSingleton<NewMovement>.Instance.Parry();
                        punch.anim.Play("Hook", 0, 0.065f);
                        __instance.eid.DeliverDamage(__instance.gameObject, Vector3.zero, __instance.transform.position, 30, false);
                        break;
                    }
                }
                if (Physics.SphereCast(__instance.eid.weakPoint.transform.position + direction * 2, 0.5f, direction, out RaycastHit hit, distance, layerMask))
                {

                    if (hit.collider.gameObject == MonoSingleton<NewMovement>.Instance.gameObject)
                    {
                        // Do something if it hits the player  
                        BananaDifficultyPlugin.Log.LogInfo("Hit player!");
                        MonoSingleton<NewMovement>.Instance.GetHurt(10, false); // Example damage value  
                        MonoSingleton<NewMovement>.Instance.slowMode = true; // Example slow mode

                        // Drag the player towards the instance until within 2 meters
                        Vector3 dragDirection = (__instance.transform.position - MonoSingleton<NewMovement>.Instance.transform.position).normalized;
                        float dragSpeed = 70f; // Example drag speed

                        while (Vector3.Distance(__instance.transform.position, MonoSingleton<NewMovement>.Instance.transform.position) > 2f)
                        {
                            MonoSingleton<NewMovement>.Instance.transform.position = Vector3.MoveTowards(MonoSingleton<NewMovement>.Instance.transform.position, __instance.transform.position, dragSpeed * Time.deltaTime);
                            yield return null;
                        }
                        MonoSingleton<NewMovement>.Instance.slowMode = false; // Example slow mode
                        break;
                    }
                    else if (hit.collider.gameObject != __instance.gameObject && !hit.collider.transform.IsChildOf(__instance.transform))
                    {
                        // Do something if it hits another object
                        BananaDifficultyPlugin.Log.LogInfo("Hit another object!: " + hit.collider.gameObject.name);
                        break;
                    } 
                }

                yield return null;
            }
            threwSpear[__instance] = false;
            Object.Destroy(lineRenderer.gameObject);
        }

        private static IEnumerator ApplyKnockbackAndDamage(V2 __instance)
        {
            yield return new WaitForSeconds(0.5f); // 0.5 second delay

            if (Vector3.Distance(__instance.transform.position, MonoSingleton<NewMovement>.Instance.transform.position) < 10)
            {
                // Apply knockback and damage to the player
                MonoSingleton<NewMovement>.Instance.LaunchFromPoint(__instance.transform.position, -75); // Example knockback force
                MonoSingleton<NewMovement>.Instance.GetHurt(50, false); // Example damage value
            }
        }
    }
}
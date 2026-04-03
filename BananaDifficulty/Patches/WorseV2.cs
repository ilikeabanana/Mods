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
        public static Dictionary<V2, bool> hasAutoEnraged = new Dictionary<V2, bool>();
        public static Dictionary<V2, float> slamCooldowns = new Dictionary<V2, float>();
        public static Dictionary<V2, float> teleportCooldowns = new Dictionary<V2, float>();
        public static Dictionary<V2, float> wallShockCooldowns = new Dictionary<V2, float>();


        [HarmonyPatch(nameof(V2.SetSpeed))]
        [HarmonyPostfix]
        public static void speed_postfix(V2 __instance)
        {
            __instance.anim.speed *= 1.1f;
            __instance.movementSpeed *= 1.1f;
        }


        [HarmonyPatch(nameof(V2.Update))]
        [HarmonyPrefix]
        public static bool Awake_Postfix(V2 __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return true;
            if (__instance.inIntro) return true;

            float dist = Vector3.Distance(__instance.transform.position,
                                    MonoSingleton<NewMovement>.Instance.transform.position);
            float currentTime = Time.time;

            if (dist > 20f)
            {
                if (!__instance.secondEncounter) return true;
                if (threwSpear.ContainsKey(__instance) && threwSpear[__instance]) return true;
                if (spearCooldowns.ContainsKey(__instance) && currentTime < spearCooldowns[__instance]) return true;

                __instance.rb.velocity = Vector3.zero;
                threwSpear[__instance] = true;
                spearCooldowns[__instance] = currentTime + 7f;

                Object.Instantiate<GameObject>(
                    __instance.altFlash,
                    __instance.aimAtTarget[1].transform.position,
                    Quaternion.LookRotation(__instance.target.position
                        - __instance.aimAtTarget[1].transform.position)
                ).transform.localScale *= 20f;

                __instance.StartCoroutine(ThrowSpearWithDelay(__instance));
            }
            else if (dist < 10f)
            {
                if (__instance.secondEncounter) return true;
                if (knockbackCooldowns.ContainsKey(__instance) && currentTime < knockbackCooldowns[__instance]) return true;

                knockbackCooldowns[__instance] = currentTime + 5f;

                Object.Instantiate<GameObject>(
                    BananaDifficultyPlugin.v2FlashUnpariable,
                    __instance.aimAtTarget[1].transform.position,
                    Quaternion.LookRotation(__instance.target.position
                        - __instance.aimAtTarget[1].transform.position)
                ).transform.localScale *= 20f;

                __instance.StartCoroutine(ApplyKnockbackAndDamage(__instance));
            }

            return true;
        }


        [HarmonyPatch("UpdateCooldowns")]
        [HarmonyPostfix]
        public static void UpdateCooldowns_Faster_Postfix(V2 __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            float speedMod = __instance.eid != null ? __instance.eid.totalSpeedModifier : 1f;
            float bonus = Time.deltaTime * speedMod
                        * (__instance.secondEncounter ? 0.75f : 0.4f);

            if (__instance.shootCooldown > 0f)
                __instance.shootCooldown = Mathf.MoveTowards(__instance.shootCooldown, 0f, bonus);
            if (__instance.altShootCooldown > 0f)
                __instance.altShootCooldown = Mathf.MoveTowards(__instance.altShootCooldown, 0f, bonus);
        }


        [HarmonyPatch(nameof(V2.Update))]
        [HarmonyPostfix]
        public static void Update_AutoEnrage_Postfix(V2 __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.inIntro || !__instance.active) return;
            if (__instance.mac == null) return;

            if (!hasAutoEnraged.ContainsKey(__instance))
                hasAutoEnraged[__instance] = false;

            float threshold = __instance.secondEncounter ? 55f : 35f;

            if (!hasAutoEnraged[__instance] && !__instance.isEnraged
                && __instance.mac.health < threshold)
            {
                hasAutoEnraged[__instance] = true;
                __instance.Enrage(__instance.secondEncounter ? "GIVE ME MY FUCKING ARM BACK" : "I AM SUPERIOR");
            }

            if (__instance.mac.health >= threshold)
                hasAutoEnraged[__instance] = false;
        }


        [HarmonyPatch(nameof(V2.Update))]
        [HarmonyPostfix]
        public static void Update_GroundSlam_Postfix(V2 __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.inIntro || !__instance.active) return;
            if (!__instance.gc.onGround) return;

            float dist = Vector3.Distance(__instance.transform.position,
                MonoSingleton<NewMovement>.Instance.transform.position);
            if (dist > 8f) return;

            if (!slamCooldowns.ContainsKey(__instance)) slamCooldowns[__instance] = 0f;
            if (Time.time < slamCooldowns[__instance]) return;

            slamCooldowns[__instance] = Time.time + (__instance.secondEncounter ? 6.5f : 9f);
            __instance.StartCoroutine(GroundSlam(__instance));
        }

        private static IEnumerator GroundSlam(V2 __instance)
        { 
            Object.Instantiate(BananaDifficultyPlugin.v2FlashUnpariable,
                __instance.transform.position + Vector3.up, Quaternion.identity);

            yield return new WaitForSeconds(0.85f);

            if (__instance == null || !__instance.gameObject.activeInHierarchy) yield break;

            void Spawn(Vector3 pos)
            {
                var sw = Object.Instantiate(__instance.shockwave, pos, Quaternion.identity);
                if (sw.TryGetComponent<PhysicalShockwave>(out var psw))
                    psw.enemyType = EnemyType.V2;
            }

            Spawn(__instance.transform.position);

            if (__instance.secondEncounter)
                Spawn(__instance.transform.position + __instance.transform.forward * 2f);
        }


        [HarmonyPatch(nameof(V2.Dodge))]
        [HarmonyPostfix]
        public static void Dodge_Teleport_Postfix(V2 __instance, Transform projectile)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (!__instance.secondEncounter) return;

            if (!teleportCooldowns.ContainsKey(__instance)) teleportCooldowns[__instance] = 0f;
            if (Time.time < teleportCooldowns[__instance]) return;

            teleportCooldowns[__instance] = Time.time + 12f;
            __instance.StartCoroutine(TeleportBehindPlayer(__instance));
        }

        private static IEnumerator TeleportBehindPlayer(V2 __instance)
        {
            yield return new WaitForSeconds(0.6f);
            if (__instance == null || !__instance.gameObject.activeInHierarchy || !__instance.active)
                yield break;

            var player = MonoSingleton<NewMovement>.Instance;
            Vector3 dest = player.transform.position
                         - player.transform.forward * 4f
                         + Vector3.up * 0.05f;

            Object.Instantiate(BananaDifficultyPlugin.v2FlashUnpariable,
                __instance.transform.position + Vector3.up, Quaternion.identity);

            __instance.transform.position = dest;

            Object.Instantiate(BananaDifficultyPlugin.v2FlashUnpariable,
                dest + Vector3.up, Quaternion.identity);
        }


        [HarmonyPatch(nameof(V2.Enrage), new System.Type[] { typeof(string) })]
        [HarmonyPostfix]
        public static void Enrage_Invincibility_Postfix(V2 __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (!__instance.secondEncounter) return;

            __instance.StartCoroutine(EnrageInvincibility(__instance));
        }

        private static IEnumerator EnrageInvincibility(V2 __instance)
        {
            if (__instance.eid == null) yield break;

            float saved = __instance.eid.totalDamageTakenMultiplier;
            __instance.eid.totalDamageTakenMultiplier = 0f;

            yield return new WaitForSeconds(1f);

            if (__instance != null && __instance.eid != null)
                __instance.eid.totalDamageTakenMultiplier = saved;
        }


        [HarmonyPatch("WallJump")]
        [HarmonyPostfix]
        public static void WallJump_Shockwave_Postfix(V2 __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.shockwave == null) return;

            if (!wallShockCooldowns.ContainsKey(__instance)) wallShockCooldowns[__instance] = 0f;
            if (Time.time < wallShockCooldowns[__instance]) return;
            wallShockCooldowns[__instance] = Time.time + 3.5f;

            void Spawn(Vector3 pos)
            {
                var sw = Object.Instantiate(__instance.shockwave, pos, Quaternion.identity);
                if (sw.TryGetComponent<PhysicalShockwave>(out var psw))
                    psw.enemyType = EnemyType.V2;
            }

            Spawn(__instance.transform.position);

            if (__instance.secondEncounter)
                Spawn(__instance.transform.position + __instance.transform.forward * 3f);
        }


        [HarmonyPatch("CheckPattern")]
        [HarmonyPostfix]
        public static void CheckPattern_Faster_Postfix(V2 __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            float mult = __instance.secondEncounter ? 0.75f : 0.95f;
            __instance.patternCooldown *= mult;
        }


        private static IEnumerator ThrowSpearWithDelay(V2 __instance)
        {
            yield return new WaitForSeconds(1.0f);

            Object.Instantiate(BananaDifficultyPlugin.WhiplashThrow, __instance.transform.position, Quaternion.identity);

            LineRenderer lineRenderer = new GameObject("SpearRay").AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.5f;
            lineRenderer.endWidth = 0.5f;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, __instance.eid.weakPoint.transform.position);
            lineRenderer.material = BananaDifficultyPlugin.WhiplashMat;
            lineRenderer.startColor = new Color(26, 26, 26);
            lineRenderer.endColor = new Color(26, 26, 26);

            float spearSpeed = 150f;
            Vector3 targetPosition = __instance.target.PredictTargetPosition(
                Vector3.Distance(__instance.eid.weakPoint.transform.position, __instance.target.position) / spearSpeed);
            Vector3 direction = (targetPosition - __instance.eid.weakPoint.transform.position).normalized;
            float distance = 0f;

            AudioSource source = lineRenderer.gameObject.AddComponent<AudioSource>();
            source.clip = BananaDifficultyPlugin.WhiplashLoop;
            source.loop = true;
            source.playOnAwake = true;
            source.Play();

            int layerMask = (1 << 24) | (1 << 25) | (1 << 12) | (1 << 8) | (1 << 2) | (1 << 18);

            while (distance < 500f)
            {
                distance += spearSpeed * Time.deltaTime;
                Vector3 currentPosition = __instance.eid.weakPoint.transform.position + direction * distance;
                lineRenderer.SetPosition(1, currentPosition);
                lineRenderer.SetPosition(0, __instance.eid.weakPoint.transform.position);

                if (MonoSingleton<FistControl>.Instance.currentPunch.activeFrames > 0)
                {
                    Punch punch = MonoSingleton<FistControl>.Instance.currentPunch;
                    if (punch.type == FistType.Standard && Vector3.Distance(currentPosition, punch.transform.position) <= 25)
                    {
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
                        MonoSingleton<NewMovement>.Instance.GetHurt(7, false);
                        MonoSingleton<NewMovement>.Instance.slowMode = true;

                        float dragSpeed = 45f;
                        while (Vector3.Distance(__instance.transform.position, MonoSingleton<NewMovement>.Instance.transform.position) > 2f)
                        {
                            MonoSingleton<NewMovement>.Instance.transform.position = Vector3.MoveTowards(
                                MonoSingleton<NewMovement>.Instance.transform.position,
                                __instance.transform.position,
                                dragSpeed * Time.deltaTime);
                            yield return null;
                        }
                        MonoSingleton<NewMovement>.Instance.slowMode = false;
                        break;
                    }
                    else if (hit.collider.gameObject != __instance.gameObject
                          && !hit.collider.transform.IsChildOf(__instance.transform))
                    {
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
            yield return new WaitForSeconds(0.5f);

            if (Vector3.Distance(__instance.transform.position, MonoSingleton<NewMovement>.Instance.transform.position) < 10)
            {
                Object.Instantiate<GameObject>(BananaDifficultyPlugin.proximityExplosion, __instance.transform.position, Quaternion.identity).transform.SetParent(__instance.transform, true);
            }
        }
    }
}
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
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
    [HarmonyPatch(typeof(CancerousRodent))]
    internal class WorseRodent
    {

        private static Dictionary<CancerousRodent, float> lastDamageTimes = new Dictionary<CancerousRodent, float>();
        private static float damageInterval = 0.35f;

        [HarmonyPatch(nameof(CancerousRodent.Update))]
        [HarmonyPrefix]
        public static bool UpdatePrefix(CancerousRodent __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.eid.difficulty)) return true;
            if (__instance.eid.dead) return true;


            if (!__instance.harmless)
            {
                if (!lastDamageTimes.ContainsKey(__instance))
                    lastDamageTimes[__instance] = Time.time;

                if (Time.time - lastDamageTimes[__instance] >= damageInterval)
                {
                    DamagePlayer(__instance);
                    lastDamageTimes[__instance] = Time.time;
                }
            }
            else
            {
                if (__instance.TryGetComponent<RodentBoss>(out RodentBoss boss))
                {
                    boss.BossUpdate();
                }
            }

            return true;
        }

        private static void DamagePlayer(CancerousRodent rodent)
        {
            MonoSingleton<NewMovement>.Instance.GetHurt(10, false);
        }



        [HarmonyPatch(nameof(CancerousRodent.Awake))]
        [HarmonyPrefix]
        public static void Start_Postfix(CancerousRodent __instance)
        {
            EnemyIdentifier eid = __instance.GetComponent<EnemyIdentifier>();
            if (!BananaDifficultyPlugin.CanUseIt(eid.difficulty)) return;
            if (__instance.GetComponent<RodentBoss>() == null) return;
            Enemy e = __instance.GetComponent<Enemy>();

            if (!__instance.harmless) return;
            eid.health = 700;
            e.health = 700;
            e.originalHealth = 700;
            __instance.transform.localScale = Vector3.one * 1.5f;

            if(__instance.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = true;
            }
        }

        static Dictionary<CancerousRodent, float> attackCooldown = new Dictionary<CancerousRodent, float>();
        static Dictionary<CancerousRodent, int> previousPhases = new Dictionary<CancerousRodent, int>();
    }

    public class MoveForward : MonoBehaviour
    {
        void Update()
        {
            transform.position += transform.forward * Time.deltaTime * 2.5f;
        }
    }

    public class ShakyRock : MonoBehaviour
    {
        Vector3 originalPos;
        float time;

        float intensity = 2.8f;
        float duration = 5f;

        void Awake()
        {
            originalPos = transform.position;
        }

        void Update()
        {
            time += Time.deltaTime;

            float progress = time / duration;
            float currentIntensity = intensity * progress * progress;

            float shakeX = Random.Range(-1f, 1f) * currentIntensity;
            float shakeZ = Random.Range(-1f, 1f) * currentIntensity;

            transform.position = originalPos + new Vector3(shakeX, 0f, shakeZ);

            if (time >= duration)
            {
                
                foreach (Explosion explosion in Instantiate(BananaDifficultyPlugin.superExplosion, transform.position, Quaternion.identity).GetComponentsInChildren<Explosion>())
                {
                    explosion.toIgnore.Add(EnemyType.Minotaur);
                    explosion.maxSize *= 1.75f;
                    explosion.speed *= 1.75f;
                }
                Destroy(gameObject);
            }
                
        }
    }
}
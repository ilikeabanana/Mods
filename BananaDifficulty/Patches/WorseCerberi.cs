using HarmonyLib;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(StatueBoss))]
    internal class WorseCerberi
    {
        private static void FireProjectileAtAngle(float angleOffset, StatueBoss __instance)
        {
            GameObject gameObject = Object.Instantiate<GameObject>(__instance.orbProjectile.ToAsset(), new Vector3(__instance.orbLight.transform.position.x, __instance.transform.position.y + 3.5f, __instance.orbLight.transform.position.z), Quaternion.identity);
            gameObject.transform.LookAt(__instance.eid.target.PredictTargetPosition(0.4f));

            // Rotate the new projectile by the specified angle offset
            gameObject.transform.Rotate(Vector3.up, angleOffset);
            if (__instance.difficulty > 2)
            {
                gameObject.GetComponent<Rigidbody>().AddForce(gameObject.transform.forward * 20000f);
            }
            else if (__instance.difficulty == 2)
            {
                gameObject.GetComponent<Rigidbody>().AddForce(gameObject.transform.forward * 15000f);
            }
            else
            {
                gameObject.GetComponent<Rigidbody>().AddForce(gameObject.transform.forward * 10000f);
            }
            Projectile projectile;
            if (gameObject.TryGetComponent<Projectile>(out projectile))
            {
                projectile.target = __instance.eid.target;
                if (__instance.difficulty <= 2)
                {
                    if (__instance.difficulty <= 2)
                    {
                        projectile.bigExplosion = false;
                    }
                    projectile.damage *= __instance.eid.totalDamageModifier;
                }
            }
            __instance.orbGrowing = false;
            __instance.orbLight.range = 0f;

        }

        [HarmonyPatch(nameof(StatueBoss.OrbSpawn))]
        [HarmonyPostfix]
        public static void Awake_Postfix(StatueBoss __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            FireProjectileAtAngle(25f, __instance);
            FireProjectileAtAngle(-25f, __instance);
        }
        [HarmonyPatch(nameof(StatueBoss.Tackle))]
        [HarmonyPostfix]
        public static void ThreeTackles(StatueBoss __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __instance.extraTackles += 1;
        }
        /*

        [HarmonyPatch(nameof(StatueBoss.StompHit))]
        [HarmonyPostfix]
        public static void Stomp_Postfix(StatueBoss __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __instance.currentStompWave = Object.Instantiate<GameObject>(__instance.stompWave.ToAsset(), new Vector3(__instance.stompPos.position.x, __instance.transform.position.y, __instance.stompPos.position.z), Quaternion.identity);
            PhysicalShockwave component = __instance.currentStompWave.GetComponent<PhysicalShockwave>();

            __instance.currentStompWave.transform.Rotate(new Vector3(0, 0, 90));

            component.damage = 25;
            if (__instance.difficulty >= 4)
            {
                component.speed = 75f;
            }
            else if (__instance.difficulty == 3)
            {
                component.speed = 50f;
            }
            else if (__instance.difficulty == 2)
            {
                component.speed = 35f;
            }
            else if (__instance.difficulty == 1)
            {
                component.speed = 25f;
            }
            else if (__instance.difficulty == 0)
            {
                component.speed = 15f;
            }
            component.damage = Mathf.RoundToInt((float)component.damage * __instance.eid.totalDamageModifier);
            component.maxSize = 100f;
            component.enemy = true;
            component.enemyType = EnemyType.Cerberus;
        }*/
    }
}
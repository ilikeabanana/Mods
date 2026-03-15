using HarmonyLib;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(Geryon))]
    public class WorseGeryon
    {
        [HarmonyPatch(nameof(Geryon.UpdateCooldowns))]
        [HarmonyPrefix]
        public static bool Updatin(Geryon __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return true;
            if (!__instance.inAction)
                __instance.cooldown = Mathf.MoveTowards(__instance.cooldown, 0f, Time.deltaTime);

            __instance.playerPushBackerCooldown = Mathf.MoveTowards(__instance.playerPushBackerCooldown, 0f, Time.deltaTime);

            if (__instance.stunTime > 0f && __instance.stunned)
            {
                __instance.stunTime = Mathf.MoveTowards(__instance.stunTime, 0f,
                    Time.deltaTime * ((__instance.secondPhase ? 1.5f : 1f) * 2));

                if (__instance.stunTime <= 0f && __instance.stunned)
                    __instance.Unstun();
            }

            if (__instance.cancelledAction && __instance.sinceCancelledAction >= 0.5f
                && !__instance.anim.IsInTransition(0))
                __instance.cancelledAction = false;

            return false;
        }

        [HarmonyPatch(nameof(Geryon.HeadHort))]
        [HarmonyPrefix]
        public static bool NoPain(Geryon __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return true;
            return false;
        }

        [HarmonyPatch("UpdateDifficulty")]
        [HarmonyPostfix]
        public static void HarderHeat(Geryon __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __instance.maximumHeat += 2f;
        }

        [HarmonyPatch("BowUpShoot")]
        [HarmonyPostfix]
        public static void MoreBeams(Geryon __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __instance.beamsAmount += 3; // default 5 → 9 beams
        }

        [HarmonyPatch("BowForwardShoot")]
        [HarmonyPostfix]
        public static void BowForwardHomingVolley(Geryon __instance, int shotNumber)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (shotNumber != 3) return;

            for (int i = 0; i < 3; i++)
            {
                float angle = Mathf.Lerp(-35f, 35f, i / 2f);
                GameObject homing = Object.Instantiate(
                    BananaDifficultyPlugin.homingBlue,
                    __instance.bowShootPoint.position,
                    __instance.transform.rotation * Quaternion.Euler(0f, angle, 0f));
                homing.transform.SetParent(__instance.projectileParent, true);
            }
        }

        [HarmonyPatch("PalmProjectileShoot", new System.Type[] { typeof(int) })]
        [HarmonyPostfix]
        public static void ExtraHomingPerPalmShot(Geryon __instance, int hand)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            Transform shootPoint = (hand == 0)
                ? __instance.leftHandShootPoint
                : __instance.rightHandShootPoint;

            GameObject homing = Object.Instantiate(
                BananaDifficultyPlugin.homingHH,
                shootPoint.position,
                __instance.transform.rotation);
            homing.transform.SetParent(__instance.projectileParent, true);

            Projectile projHHChildren = homing.GetComponentInChildren<Projectile>();

            if (projHHChildren != null)
            {
                projHHChildren.target = __instance.eid.target;
                projHHChildren.safeEnemyType = EnemyType.Geryon;
                projHHChildren.damage *= __instance.eid.totalDamageModifier;
            }

            if (homing.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
            {
                rigidbody.AddForce(Vector3.up * 50f, ForceMode.VelocityChange);
            }

        }

        [HarmonyPatch("UnstunClose")]
        [HarmonyPostfix]
        public static void RecoverySpearBurst(Geryon __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            int spearCount = 8;
            for (int i = 0; i < spearCount; i++)
            {
                float angle = i * (360f / spearCount);
                GameObject spearObj = Object.Instantiate(
                    BananaDifficultyPlugin.gabrielThrownSpear,
                    __instance.weakPointHitbox.transform.position,
                    Quaternion.Euler(0f, angle, 0f));
                spearObj.transform.SetParent(__instance.projectileParent, true);

                Projectile projHHChildren = spearObj.GetComponentInChildren<Projectile>();

                if (projHHChildren != null)
                {
                    projHHChildren.target = __instance.eid.target;
                    projHHChildren.safeEnemyType = EnemyType.Geryon;
                    projHHChildren.damage *= __instance.eid.totalDamageModifier;
                }
            }
            // Because (ngl) this is already hard lmao
            Object.Instantiate(BananaDifficultyPlugin.providenceOrb, __instance.weakPointHitbox.transform.position, Quaternion.identity);

        }
    }
}
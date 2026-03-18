using HarmonyLib;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(Turret))]
    internal class WorseSentries
    {
        [HarmonyPatch(nameof(Turret.Start))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Turret __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            __instance.maxAimTime /= 2;
        }

        [HarmonyPatch(nameof(Turret.ChangeLineColor))]
        [HarmonyPostfix]
        public static void ExtarProjectile(Turret __instance)
        {
            Vector3 position = __instance.isBarrelPortalCrossed ? __instance.barrelPos : new Vector3(__instance.transform.position.x, __instance.barrelTip.transform.position.y, __instance.transform.position.z);
            GameObject proj = Object.Instantiate(BananaDifficultyPlugin.projNormal, position, __instance.shootRotation);

            if(__instance.aimTime >= 1 && (__instance.aimTime >= __instance.maxAimTime && (__instance.hasVision || __instance.isAimFlashing)))
            {
                proj.transform.localScale *= 2.5f;
            }

            if(proj.TryGetComponent<Projectile>(out Projectile p))
            {
                p.safeEnemyType = __instance.eid.enemyType;
                p.targetHandle = __instance.targetHandle;
                p.unparryable = true;
                p.gameObject.layer = 0;
                if (__instance.aimTime >= 1 && (__instance.aimTime >= __instance.maxAimTime && (__instance.hasVision || __instance.isAimFlashing)))
                {
                    p.explosionEffect = BananaDifficultyPlugin.bigExplosion;
                    p.speed *= 2;
                }
            }

            if(proj.TryGetComponent<MeshRenderer>(out MeshRenderer rend))
            {
                Material mat = new Material(rend.sharedMaterial);

                mat.color = Color.black;
                rend.sharedMaterial = mat;
            }
        }
    }
}
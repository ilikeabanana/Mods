using HarmonyLib;
using UnityEngine;

namespace BananaDifficulty.Patches
{

    [HarmonyPatch(typeof(RevolverBeam))]
    public class GoThroughWalls
    {
        [HarmonyPatch("Shoot")]
        public static void Prefix(RevolverBeam __instance)
        {
            if (__instance.beamType == BeamType.Enemy)
            {
                __instance.pierceLayerMask = LayerMaskDefaults.Get(LMD.Player);
                __instance.ignoreEnemyTrigger = LayerMaskDefaults.Get(LMD.Player);
            }
        }
    }

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

        [HarmonyPatch(nameof(Turret.Aiming))]
        [HarmonyPostfix]
        public static void FasterFlash(Turret __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (!__instance.isAimFlashing) return;

            __instance.flashTime = Mathf.MoveTowards(__instance.flashTime, 1f,
                Time.deltaTime * 4f * __instance.eid.totalSpeedModifier);
        }

        [HarmonyPatch(nameof(Turret.Awake))]
        [HarmonyPrefix]
        public static void Dont_Cancerl(Turret __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __instance.outOfSightTimer = 0;
        }

        [HarmonyPatch(nameof(Turret.IsTargetObstructed))]
        [HarmonyPostfix]
        public static void AlwaysVis(ref bool __result, Turret __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __result = false;
        }
        [HarmonyPatch(nameof(Turret.VisionUpdate))]
        [HarmonyPostfix]
        public static void ForceVision(Turret __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            // If the sight query lost the target (wall blocking), restore from last known handle
            if (__instance.targetHandle == null && __instance.lastTargetHandle != null)
            {
                __instance.targetHandle = __instance.lastTargetHandle;
            }
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
using HarmonyLib;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(SisyphusPrime))]
    internal class WorseSisyphus
    {
        [HarmonyPatch(nameof(SisyphusPrime.StompShockwave))]
        [HarmonyPostfix]
        public static void Shocker(SisyphusPrime __instance)
        {
            if (BananaDifficultyPlugin.CanUseIt(__instance.difficulty))
            {
                if (__instance.difficulty >= 2)
                {
                    PhysicalShockwave physicalShockwave = __instance.CreateShockwave(new Vector3(__instance.swingLimbs[2].position.x, __instance.transform.position.y, __instance.swingLimbs[2].position.z));
                    physicalShockwave.target = __instance.target;
                    physicalShockwave.transform.rotation = __instance.transform.rotation;
                    physicalShockwave.transform.Rotate(Vector3.forward * 90f, Space.Self);
                    physicalShockwave.speed *= 2f;
                }
            }
        }
        [HarmonyPatch(nameof(SisyphusPrime.ClapShockwave))]
        [HarmonyPostfix]
        public static void ClapShocker(SisyphusPrime __instance)
        {
            if (BananaDifficultyPlugin.CanUseIt(__instance.difficulty))
            {
                if (__instance.difficulty >= 2)
                {
                    PhysicalShockwave physicalShockwave = __instance.CreateShockwave(new Vector3(__instance.swingLimbs[2].position.x, __instance.transform.position.y, __instance.swingLimbs[2].position.z));
                }
            }
        }
        [HarmonyPatch(nameof(SisyphusPrime.ProjectileShoot))]
        [HarmonyPostfix]
        public static void VirtueProj(SisyphusPrime __instance)
        {
            if (BananaDifficultyPlugin.CanUseIt(__instance.difficulty))
            {
                GameObject gameObject = Object.Instantiate<GameObject>(BananaDifficultyPlugin.insignificant, __instance.target.position, Quaternion.identity);
                VirtueInsignia component = gameObject.GetComponent<VirtueInsignia>();
                component.target = __instance.target;
                if (__instance.enraged)
                {
                    component.predictive = true;
                }
                if (__instance.difficulty == 1)
                {
                    component.windUpSpeedMultiplier = 0.875f;
                }
                else if (__instance.difficulty == 0)
                {
                    component.windUpSpeedMultiplier = 0.75f;
                }
                if (__instance.difficulty >= 4)
                {
                    component.explosionLength = ((__instance.difficulty == 5) ? 5f : 3.5f);
                }
                if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.Platformer)
                {
                    gameObject.transform.localScale *= 0.75f;
                    component.windUpSpeedMultiplier *= 0.875f;
                }
                component.windUpSpeedMultiplier *= __instance.eid.totalSpeedModifier;
                component.damage = Mathf.RoundToInt((float)component.damage * __instance.eid.totalDamageModifier);
            }
        }
        
        [HarmonyPatch(nameof(SisyphusPrime.FixedUpdate))]
        [HarmonyPostfix]
        public static void YOUCANTESCAPE(SisyphusPrime __instance)
        {
            
            if (__instance.inAction) return;
            if (BananaDifficultyPlugin.CanUseIt(__instance.difficulty))
            {
                __instance.cooldown = 0;
                if (Vector3.Distance(__instance.transform.position, __instance.heightAdjustedTargetPos) > 30)
                {
                    __instance.TeleportAnywhere(true);
                }
            }
        }
    }
}
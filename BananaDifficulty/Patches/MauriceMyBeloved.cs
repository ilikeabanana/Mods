using HarmonyLib;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(SpiderBody))]
    internal class MauriceMyBeloved
    {

        [HarmonyPatch(nameof(SpiderBody.BeamFire))]
        [HarmonyPrefix]
        public static bool FuckTonOfBeams(SpiderBody __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return true;

            __instance.parryable = false;
            if (!__instance.eid.dead)
            {
                __instance.currentBeam = Object.Instantiate<GameObject>(__instance.spiderBeam, __instance.mouth.position, __instance.mouth.rotation);
                __instance.rotating = false;
                RevolverBeam revolverBeam;
                if (__instance.eid.totalDamageModifier != 1f && __instance.currentBeam.TryGetComponent<RevolverBeam>(out revolverBeam))
                {
                    revolverBeam.damage *= __instance.eid.totalDamageModifier;
                }
                __instance.currentBeam = Object.Instantiate<GameObject>(__instance.spiderBeam, __instance.mouth.position, Quaternion.Euler(-__instance.mouth.eulerAngles));
                __instance.rotating = false;
                RevolverBeam revolverBeam2;
                if (__instance.eid.totalDamageModifier != 1f && __instance.currentBeam.TryGetComponent<RevolverBeam>(out revolverBeam2))
                {
                    revolverBeam2.damage *= __instance.eid.totalDamageModifier;
                }
                if (__instance.beamsAmount > 1)
                {
                    __instance.beamsAmount--;
                    __instance.chargeEffectAudio.pitch = 4f;
                    __instance.chargeEffectAudio.volume = 1f;
                    __instance.Invoke("BeamChargeEnd", 0.05f / __instance.eid.totalSpeedModifier);
                    return false;
                }
                Object.Destroy(__instance.currentBeam);
                __instance.Invoke("StopWaiting", 1f / __instance.eid.totalSpeedModifier);
            }
            return false;
        }
    }
}

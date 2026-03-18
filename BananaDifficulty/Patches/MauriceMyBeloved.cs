using HarmonyLib;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(MaliciousFace))]
    internal class MauriceMyBeloved
    {
        [HarmonyPatch(nameof(MaliciousFace.BeamFire))]
        [HarmonyPrefix]
        public static bool FuckTonOfBeams(MaliciousFace __instance) 
        {
            BananaDifficultyPlugin.Log.LogInfo($"BeamFire prefix hit, difficulty: {__instance.difficulty}");

            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty))
            {
                BananaDifficultyPlugin.Log.LogInfo("CanUseIt returned false, skipping");
                return true;
            }

            if (__instance.eid.dead)
            {
                return false;
            }
            __instance.spiderParryable = false;
            __instance.beamFiring = false;
            if (!__instance.isBeamPortalBlocked)
            {

                __instance.UpdateBeamMouth();
                if (!__instance.isBeamPortalBlocked)
                {
                    FireBeam(__instance, false);
                    FireBeam(__instance, true);
                }
            }
            if (__instance.beamsAmount > 1)
            {
                __instance.beamsAmount--;
                __instance.ceAud.SetPitch(4f);
                __instance.ceAud.volume = 1f;
                __instance.Invoke("BeamChargeEnd", 0.5f / __instance.eid.totalSpeedModifier);
                return false;
            }
            Object.Destroy(__instance.currentCE);
            __instance.Invoke("StopWaiting", 1f / __instance.eid.totalSpeedModifier);
            return false;
        }
         
        static void FireBeam(MaliciousFace __instance, bool reversed)
        {
            __instance.currentBeam = Object.Instantiate<GameObject>(__instance.spiderBeam, __instance.beamMouthPos, reversed ? Quaternion.Inverse(__instance.beamMouthRot) : __instance.beamMouthRot);
            RevolverBeam revolverBeam;
            if (__instance.eid.totalDamageModifier != 1f && __instance.currentBeam.TryGetComponent<RevolverBeam>(out revolverBeam))
            {
                revolverBeam.damage *= __instance.eid.totalDamageModifier;
            }
        }
    }
}

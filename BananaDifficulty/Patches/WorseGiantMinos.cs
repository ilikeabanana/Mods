using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(MinosBoss))]
    internal class WorseGiantMinos
    {
        [HarmonyPatch(nameof(MinosBoss.Start))]
        [HarmonyPostfix]
        public static void Awake_Postfix(MinosBoss __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __instance.SpawnParasites();
        }

        [HarmonyPatch(nameof(MinosBoss.GotParried))]
        [HarmonyPrefix]
        public static bool Parried_Prefix(MinosBoss __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return true;
            if (!__instance.dead)
            {
                if (!__instance.beenParried)
                {
                    __instance.beenParried = true;
                    if (__instance.parryChallenge)
                    {
                        MonoSingleton<ChallengeManager>.Instance.ChallengeDone();
                    }
                }
                MonoSingleton<StyleHUD>.Instance.AddPoints(500, "ultrakill.downtosize", null, __instance.eid, -1, "", "");
                if (__instance.attackingRight)
                {
                    foreach (Transform transform in __instance.rightHandBones)
                    {
                        __instance.stat.GetHurt(transform.gameObject, Vector3.zero, (float)(35 / __instance.rightHandBones.Length), 0f, transform.position, null, false);
                        transform.gameObject.layer = 10;
                    }
                }
                if (__instance.attackingLeft)
                {
                    foreach (Transform transform2 in __instance.leftHandBones)
                    {
                        __instance.stat.GetHurt(transform2.gameObject, Vector3.zero, (float)(35 / __instance.leftHandBones.Length), 0f, transform2.position, null, false);
                        transform2.gameObject.layer = 10;
                    }
                }
                __instance.stat.partiallyParryable = false;
                __instance.stat.parryables.Clear();
                __instance.eid.hitter = "";
            }
            return false;
        }

        [HarmonyPatch(nameof(MinosBoss.PhaseChange))]
        [HarmonyPostfix]
        public static void Phase_ChangePostfix(MinosBoss __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.phase != 2) return;
            List<Parasite> newParasites = new List<Parasite>();
            foreach (var parasite in __instance.parasites)
            {
                Parasite newPar = Object.Instantiate(parasite);
                newPar.transform.parent = parasite.transform.parent;
                newPar.transform.localPosition = parasite.transform.localPosition;
                newPar.transform.localRotation = parasite.transform.localRotation;
                newPar.transform.localScale = parasite.transform.localScale;
                newParasites.Add(newPar);
            }

            __instance.parasites = __instance.parasites.AddRangeToArray(newParasites.ToArray());
        }
    }
}
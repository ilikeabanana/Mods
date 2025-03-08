using HarmonyLib;
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
    [HarmonyPatch(typeof(Gabriel))]
    internal class WorseGabriel
    {
        [HarmonyPatch(nameof(Gabriel.SpearAttack))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Gabriel __instance)
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

        [HarmonyPatch(nameof(Gabriel.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(Gabriel __instance)
        {
            if (BananaDifficultyPlugin.CanUseIt(__instance.difficulty))
            {
                __instance.attackCooldown = 0;
                __instance.summonedSwordsCooldown = 0;
            }
        }
    }
    [HarmonyPatch(typeof(GabrielSecond))]
    internal class WorseGabriel2nd
    {
        [HarmonyPatch(nameof(GabrielSecond.FastCombo))]
        [HarmonyPostfix]
        public static void Awake_Postfix(GabrielSecond __instance)
        {
            if (BananaDifficultyPlugin.CanUseIt(__instance.difficulty))
            {
                GameObject gameObject = Object.Instantiate<GameObject>(BananaDifficultyPlugin.insignificant, __instance.eid.target.position, Quaternion.identity);
                VirtueInsignia component = gameObject.GetComponent<VirtueInsignia>();
                component.target = __instance.eid.target;
                component.predictive = true;
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

        [HarmonyPatch(nameof(GabrielSecond.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(GabrielSecond __instance)
        {
            if (BananaDifficultyPlugin.CanUseIt(__instance.difficulty))
            {
                __instance.attackCooldown = 0;
                __instance.summonedSwordsCooldown = 0;
                __instance.combinedSwordsCooldown = 0;
            }
        }
    }
}
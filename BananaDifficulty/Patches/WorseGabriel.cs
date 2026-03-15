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
        [HarmonyPatch(nameof(Gabriel.SpearCombo))]
        [HarmonyPostfix]
        public static void Spear_Postfix(Gabriel __instance)
        {
            if (BananaDifficultyPlugin.CanUseIt(__instance.eid.difficulty) && BananaDifficultyPlugin.HardMode.Value)
            {
                GameObject gameObject = Object.Instantiate<GameObject>(BananaDifficultyPlugin.insignificant, __instance.eid.target.position, Quaternion.identity);
                VirtueInsignia component = gameObject.GetComponent<VirtueInsignia>();
                component.target = __instance.eid.target;
                component.predictive = true;
                if (__instance.eid.difficulty == 1)
                {
                    component.windUpSpeedMultiplier = 0.875f;
                }
                else if (__instance.eid.difficulty == 0)
                {
                    component.windUpSpeedMultiplier = 0.75f;
                }
                if (__instance.eid.difficulty >= 4)
                {
                    component.explosionLength = ((__instance.eid.difficulty == 5) ? 5f : 3.5f);
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

        private static void FireProjectileAtAngle(GameObject objectToSpawn, float angleOffset, Gabriel __instance)
        {
            GameObject gameObject = Object.Instantiate<GameObject>(objectToSpawn, __instance.transform.position + __instance.transform.forward * 3f, __instance.transform.rotation);
            if (__instance.eid.difficulty <= 1 || __instance.eid.totalSpeedModifier != 1f || __instance.eid.totalDamageModifier != 1f)
            {
                Projectile componentInChildren = __instance.thrownObject.GetComponentInChildren<Projectile>();
                componentInChildren.target = __instance.target;
                if (componentInChildren)
                {
                    if (__instance.eid.difficulty <= 1)
                    {
                        componentInChildren.speed *= 0.5f;
                    }
                    componentInChildren.damage *= __instance.eid.totalDamageModifier;
                }
            }

            // Rotate the new projectile by the specified angle offset
            gameObject.transform.Rotate(Vector3.up, angleOffset);

        }

        [HarmonyPatch(nameof(Gabriel.ThrowWeapon))]
        [HarmonyPostfix]
        public static void RightHand_Postfix(Gabriel __instance, GameObject projectile)
        {
            if (BananaDifficultyPlugin.CanUseIt(__instance.eid.difficulty))
            {
                if (!__instance.gabe.juggled)
                {
                    FireProjectileAtAngle(projectile, 25, __instance);
                    FireProjectileAtAngle(projectile, -25, __instance);
                }
            }
        }
    }

    [HarmonyPatch(typeof(GabrielBase))]
    internal class GabrielBases
    {
        [HarmonyPatch(nameof(GabrielBase.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(GabrielBase __instance)
        {
            if (BananaDifficultyPlugin.CanUseIt(__instance.eid.difficulty))
            {
                __instance.attackCooldown = 0;
                __instance.summonedSwordsCooldown = 0;
                if(__instance.eid.enemyType == EnemyType.GabrielSecond)
                    __instance.combinedSwordsCooldown = 0;
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
            if (BananaDifficultyPlugin.CanUseIt(__instance.eid.difficulty))
            {
                GameObject gameObject = Object.Instantiate<GameObject>(BananaDifficultyPlugin.insignificant, __instance.eid.target.position, Quaternion.identity);
                VirtueInsignia component = gameObject.GetComponent<VirtueInsignia>();
                component.target = __instance.eid.target;
                component.predictive = true;
                if (__instance.eid.difficulty == 1)
                {
                    component.windUpSpeedMultiplier = 0.875f;
                }
                else if (__instance.eid.difficulty == 0)
                {
                    component.windUpSpeedMultiplier = 0.75f;
                }
                if (__instance.eid.difficulty >= 4)
                {
                    component.explosionLength = ((__instance.eid.difficulty == 5) ? 5f : 3.5f);
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

        private static void FireProjectileAtAngle(float angleOffset, GabrielSecond __instance)
        {
            Projectile gameObject = Object.Instantiate<Projectile>(__instance.combinedSwordsThrown, __instance.fakeCombinedSwords.transform.position, __instance.transform.rotation, __instance.transform.parent);
            gameObject.target = __instance.eid.target;
            gameObject.damage *= __instance.eid.totalDamageModifier;
            GabrielCombinedSwordsThrown gabrielCombinedSwordsThrown;
            if (gameObject.TryGetComponent<GabrielCombinedSwordsThrown>(out gabrielCombinedSwordsThrown))
            {
                gabrielCombinedSwordsThrown.gabe = __instance;
            }

            // Rotate the new projectile by the specified angle offset
            gameObject.transform.Rotate(Vector3.up, angleOffset);

        }
        [HarmonyPatch(nameof(GabrielSecond.ThrowSwords))]
        [HarmonyPostfix]
        public static void Swords_Postfix(GabrielSecond __instance)
        {
            if (BananaDifficultyPlugin.CanUseIt(__instance.eid.difficulty))
            {
                if (__instance.gabe.juggled) return;
                FireProjectileAtAngle(10, __instance);
                FireProjectileAtAngle(-10, __instance);
                FireProjectileAtAngle(20, __instance);
                FireProjectileAtAngle(-20, __instance);
            }
        }

    }
}
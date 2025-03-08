using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(SwordsMachine))]
    internal class WorseSwordsMachine
    {
        private static Dictionary<SwordsMachine, float> lastSpawnTimes = new Dictionary<SwordsMachine, float>();
        private static Dictionary<SwordsMachine, GameObject> currentSwordsDict = new Dictionary<SwordsMachine, GameObject>();

        [HarmonyPatch(nameof(SwordsMachine.EndFirstPhase))]
        [HarmonyPostfix]
        public static void EndPhase_Postfix(SwordsMachine __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            SpawnSwords(__instance);
        }

        [HarmonyPatch(nameof(SwordsMachine.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(SwordsMachine __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            if(__instance.eid.dead && currentSwordsDict.ContainsKey(__instance))
            {
                DestroyCurrentSwords(__instance);
            }

            if (!__instance.firstPhase && (!lastSpawnTimes.ContainsKey(__instance) || Time.time - lastSpawnTimes[__instance] >= 2.5f) && (!currentSwordsDict.ContainsKey(__instance) || currentSwordsDict[__instance] == null))
            {
                SpawnSwords(__instance);
                lastSpawnTimes[__instance] = Time.time;
            }
        }

        [HarmonyPatch(nameof(SwordsMachine.OnDisable))]
        [HarmonyPostfix]
        public static void OnDisable_Postfix(SwordsMachine __instance)
        {
            DestroyCurrentSwords(__instance);
        }

        static void SpawnSwords(SwordsMachine __instance)
        {
            GameObject currentSwords = Object.Instantiate<GameObject>(BananaDifficultyPlugin.summonedSwords, __instance.transform.position, Quaternion.identity);
            currentSwords.transform.SetParent(__instance.transform.parent, true);
            currentSwordsDict[__instance] = currentSwords;

            SummonedSwords summonedSwords;
            if (currentSwords.TryGetComponent<SummonedSwords>(out summonedSwords))
            {
                summonedSwords.target = new EnemyTarget(__instance.transform);
                summonedSwords.speed *= __instance.eid.totalSpeedModifier;
                summonedSwords.targetEnemy = __instance.eid.target;
            }
            foreach (Projectile projectile in currentSwords.GetComponentsInChildren<Projectile>())
            {
                projectile.target = __instance.target;
                projectile.safeEnemyType = __instance.eid.enemyType;
                if (__instance.eid.totalDamageModifier != 1f)
                {
                    projectile.damage *= __instance.eid.totalDamageModifier;
                }
            }
        }

        static void DestroyCurrentSwords(SwordsMachine __instance)
        {
            if (currentSwordsDict.ContainsKey(__instance) && currentSwordsDict[__instance] != null)
            {
                currentSwordsDict[__instance].SetActive(false);
                currentSwordsDict.Remove(__instance);
            }
        }
    }
}
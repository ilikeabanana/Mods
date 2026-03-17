using BananaDifficulty.Utils;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace BananaDifficulty.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(EnemyIdentifier))]
    internal class AwakePatches
    {
        [HarmonyPatch(nameof(EnemyIdentifier.Start))]
        [HarmonyPostfix]
        public static void Awake_Prefix(EnemyIdentifier __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            Debug.Log("Is correct diff");
            /*
            int seed = 12345 + SceneManager.GetActiveScene().buildIndex + (int)__instance.transform.position.magnitude; // Set your seed
            UnityEngine.Random.InitState(seed); // Initialize Unity's random state
            float chance = 0.5f; // 30% chance
            bool outcome = UnityEngine.Random.value < chance; // UnityEngine.Random.value gives a number between 0.0 and 1.0*/
            if (__instance.enemyType == EnemyType.Idol && !__instance.gameObject.name.EndsWith("DontRadiant"))
            {
                __instance.speedBuff = true;
                __instance.damageBuff = true;
                __instance.healthBuff = true;
            }

            if(__instance.enemyType == EnemyType.Mindflayer && !__instance.blessed)
            {
                GameObject spawnedIdol = Object.Instantiate(BananaDifficultyPlugin.idol, ModUtils.GetRandomNavMeshPoint(__instance.transform.position, 10), Quaternion.identity);

                Idol idol;
                if (spawnedIdol.TryGetComponent<Idol>(out idol))
                {
                    idol.target = __instance;
                    idol.eid.dontCountAsKills = true;
                }

                spawnedIdol.name += "DontRadiant";


                spawnedIdol.AddComponent<DestroyOnCheckpointRestart>();
            }
        }


        
        [HarmonyPatch(nameof(EnemyIdentifier.Death), new System.Type[] {typeof(bool)})]
        [HarmonyPrefix]
        public static void OnDeath_Postfix(EnemyIdentifier __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;


            if (AttachToPlayerOnDamage.joints.ContainsKey(__instance))
            {
                FixedJoint joint = AttachToPlayerOnDamage.joints[__instance];
                AttachToPlayerOnDamage.joints.Remove(__instance);
                Object.Destroy(joint);
            }
        }
        [HarmonyPatch(nameof(EnemyIdentifier.DeliverDamage))]
        [HarmonyPrefix]
        public static void Damage_Postfix(ref float multiplier, EnemyIdentifier __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            List<string> hittersToReturn = new List<string>()
            {
                "projectile",
                "ffexplosion",
            };
            if (hittersToReturn.Contains(__instance.hitter))
            {
                multiplier /= 3;
            }
        }

    }
}
using BananaDifficulty.Utils;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch]
    public class WorseReaper
    {
        [HarmonyPatch(typeof(GroundWave), nameof(GroundWave.Start))]
        [HarmonyPostfix]
        public static void MakeThingyBigger(GroundWave __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.eid.enemyType != EnemyType.MirrorReaper) return;
            __instance.transform.localScale *= 2;
        }

        public static Dictionary<MirrorReaper, float> delayBetweenTPS = new Dictionary<MirrorReaper, float>();

        [HarmonyPatch(typeof(MirrorReaper), nameof(MirrorReaper.Update))]
        [HarmonyPostfix]
        public static void HarderTHing(MirrorReaper __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if(delayBetweenTPS.TryGetValue(__instance, out float delayTime))
            {
                delayTime -= Time.deltaTime;
                if(delayTime <= 0)
                {
                    if(__instance.escapePoints.Length <= 0)
                    {
                        List<Transform> newPoints = new List<Transform>();

                        for (int i = 0; i < 10; i++)
                        {
                            GameObject point = new GameObject("EscapePoint");
                            point.transform.position = ModUtils.GetRandomNavMeshPoint(__instance.transform.position, 100);
                            newPoints.Add(point.transform);
                        }

                        __instance.escapePoints = newPoints.ToArray();
                    }

                    __instance.TeleportToEscapePoint();
                    delayTime = Random.Range(5, 15);
                }

                delayBetweenTPS[__instance] = delayTime;
            }
            else
            {
                delayBetweenTPS.Add(__instance, Random.Range(5, 15));
            }
        }

        static IEnumerator fireBeam(MirrorReaper __instance)
        {
            Object.Instantiate(BananaDifficultyPlugin.v2FlashUnpariable, __instance.transform.position, Quaternion.identity);

            float time = Random.Range(0.25f, 1.25f);

            Vector3 dir = (__instance.target.PredictTargetPosition(time) - __instance.projectileSpawnPoints[0].position).normalized;

            yield return new WaitForSeconds(time);

            GameObject proj = Object.Instantiate(
                BananaDifficultyPlugin.projBeamTurret,
                __instance.projectileSpawnPoints[0].position,
                Quaternion.LookRotation(dir)
            );

            proj.transform.forward = dir;
        }


        [HarmonyPatch(typeof(MirrorReaper), nameof(MirrorReaper.SpawnProjectiles))]
        [HarmonyPostfix]
        public static void MOREPROJECTILES(MirrorReaper __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            bool flag = Physics.Raycast(__instance.transform.position, Vector3.up, 31f, LayerMaskDefaults.Get(LMD.Environment));
            int fires = 0;
            for (int l = 0; l < 3; l++)
            {
                for (int i = 0; i < __instance.projectileSpawnPoints.Length; i++)
                {
                    fires++;
                    if (__instance.projectileSpawnPoints[i].childCount > 0)
                    {
                        for (int j = __instance.projectileSpawnPoints[i].childCount - 1; j >= 0; j--)
                        {
                            Object.Destroy(__instance.projectileSpawnPoints[i].GetChild(j).gameObject);
                        }
                    }
                    Projectile projectile = Object.Instantiate<Projectile>(__instance.projectile, __instance.projectileSpawnPoints[i].position, Quaternion.LookRotation(flag ? (__instance.target.position - __instance.transform.position) : __instance.transform.up));
                    projectile.transform.SetParent(__instance.transform.parent, true);
                    projectile.safeEnemyType = EnemyType.MirrorReaper;
                    projectile.speed = (float)Random.Range(15, 25);
                    projectile.target = __instance.target;
                    if (__instance.inMirrorPhase && __instance.difficulty >= 4)
                    {
                        EnemyIdentifier.SendToPortalLayer(projectile.gameObject);
                    }

                    if (fires % 2 == 0)
                    {
                        __instance.StartCoroutine(fireBeam(__instance));
                    }
                }
            }
            
        }
    }
}

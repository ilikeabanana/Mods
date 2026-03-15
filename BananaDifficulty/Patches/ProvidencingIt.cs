using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using ULTRAKILL.Portal;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(Drone), nameof(Drone.Shoot))]
    public class ProvidencingIt
    {
        static IEnumerator fireExtraProj(Drone __instance)
        {
            yield return new WaitForSeconds(0.25f);
            if (!__instance.crashing && __instance.projectile.RuntimeKeyIsValid())
            {
                if (__instance.eid.enemyType == EnemyType.Drone && !__instance.eid.puppet)
                {
                    EnemySimplifier[] componentsInChildren = __instance.modelTransform.GetComponentsInChildren<EnemySimplifier>();
                    for (int i = 0; i < componentsInChildren.Length; i++)
                    {
                        componentsInChildren[i].ChangeMaterialNew(EnemySimplifier.MaterialState.normal, __instance.origMaterial);
                    }
                }
                if (!__instance.gameObject.activeInHierarchy)
                {
                    yield break;
                }
                Vector3 position = __instance.transform.position;
                Vector3 forward = __instance.transform.forward;
                Vector3 position2 = position + forward;
                Quaternion quaternion = __instance.transform.rotation;
                PhysicsCastResult physicsCastResult;
                Vector3 vector;
                PortalTraversalV2[] array;
                PortalPhysicsV2.ProjectThroughPortals(position, forward, default(LayerMask), out physicsCastResult, out vector, out array);
                bool flag = false;
                if (array.Length != 0)
                {
                    PortalTraversalV2 portalTraversalV = array[0];
                    PortalHandle portalHandle = portalTraversalV.portalHandle;
                    Portal portalObject = portalTraversalV.portalObject;
                    if (portalObject.GetTravelFlags(portalHandle.side).HasFlag(PortalTravellerFlags.EnemyProjectile))
                    {
                        Matrix4x4 travelMatrix = PortalUtils.GetTravelMatrix(array);
                        position2 = vector;
                        quaternion = travelMatrix.rotation * quaternion;
                    }
                    else
                    {
                        position2 = portalObject.GetTransform(portalHandle.side).GetPositionInFront(array[0].entrancePoint, 0.05f);
                        flag = !portalObject.passThroughNonTraversals;
                    }
                }
                List<Projectile> list = new List<Projectile>();
                GameObject gameObject = Object.Instantiate<GameObject>(BananaDifficultyPlugin.spinyProvi, position2, quaternion);
                if (__instance.eid.enemyType == EnemyType.Providence)
                {
                    __instance.SetProjectileSettings(gameObject.GetComponent<Projectile>());
                    __instance.rb.AddForce(__instance.transform.forward * -1500f, ForceMode.Impulse);

                    gameObject.AddComponent<SpinYipeeee>();

                    foreach (var beam in gameObject.GetComponentsInChildren<ContinuousBeam>())
                    {
                        beam.ignoreInvincibility = false;
                        beam.GetComponent<LineRenderer>().material = BananaDifficultyPlugin.MindflayerBeamMat;
                    }
                }
                if (flag)
                {
                    for (int j = 1; j < list.Count; j++)
                    {
                        list[j].Explode();
                    }
                }
            }
        }

        public static void Postfix(Drone __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if(__instance.eid.enemyType == EnemyType.Providence)
            {
                __instance.StartCoroutine(fireExtraProj(__instance));
            }
        }
    }

    [HarmonyPatch(typeof(Pincer), nameof(Pincer.Start))]
    public static class FireBeamOnDone
    {
        public static void Postfix(Pincer __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __instance.onComplete.onActivate.AddListener(() =>
            {
                GameObject beam = Object.Instantiate(BananaDifficultyPlugin.projBeamTurret, __instance.transform.position, __instance.transform.rotation);
                if (beam.TryGetComponent<RevolverBeam>(out RevolverBeam revolverBeam2))
                {
                    revolverBeam2.ignoreEnemyType = EnemyType.Providence;
                    revolverBeam2.hitParticle = BananaDifficultyPlugin.bigExplosion;
                }
            });
        }
    }

    public class SpinYipeeee : MonoBehaviour
    {
        void Update()
        {
            transform.Rotate(Vector3.forward * 100 * Time.deltaTime);
        }
    }
}

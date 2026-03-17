using BananaDifficulty.Utils;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(Mannequin))]
    public class WorseMannequin
    {
        [HarmonyPatch(nameof(Mannequin.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(Mannequin __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            EnemyIdentifier[] availableIds = GameObject.FindObjectsOfType<EnemyIdentifier>();
            bool death = false;
            foreach (var item in availableIds)
            {
                if(item.enemyType == EnemyType.Deathcatcher)
                {
                    death = true;
                    break;
                }
            }

            if (!death)
            {
                GameObject newDeath = Object.Instantiate(DefaultReferenceManager.Instance.deathCatcher,
                    ModUtils.GetRandomNavMeshPoint(__instance.transform.position, 10), Quaternion.identity);
                newDeath.transform.parent = __instance.transform.parent;

                newDeath.GetComponent<EnemyIdentifier>().dontCountAsKills = true;

            }
        }

        [HarmonyPatch(nameof(Mannequin.SwingEnd))]
        [HarmonyPostfix]
        public static void Swing_Postfix(Mannequin __instance, int parryEnd)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (parryEnd > 0)
            {
                Vector3 target = __instance.eid.target.position;

                GameObject gameObject = Object.Instantiate<GameObject>(BananaDifficultyPlugin.chargedExplosion, target, Quaternion.identity);
                gameObject.transform.SetParent(__instance.enemy.gz.transform);
                ObjectActivator component = gameObject.GetComponent<ObjectActivator>();
                if (component)
                {
                    component.delay /= __instance.eid.totalSpeedModifier;
                }
                LineRenderer componentInChildren = gameObject.GetComponentInChildren<LineRenderer>();
                if (componentInChildren)
                {
                    componentInChildren.SetPosition(0, target);
                    componentInChildren.SetPosition(1, __instance.shootPoint.position);
                }
                foreach (Explosion explosion in gameObject.GetComponentsInChildren<Explosion>())
                {
                    explosion.damage = Mathf.RoundToInt((float)explosion.damage * __instance.eid.totalDamageModifier);
                    explosion.maxSize *= __instance.eid.totalDamageModifier;
                }
            }
        }
    }

    
}

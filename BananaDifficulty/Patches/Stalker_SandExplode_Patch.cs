using HarmonyLib;
using System.Security.Cryptography;
using ULTRAKILL.Cheats;
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
    /// 

    [HarmonyPatch(typeof(Stalker), nameof(Stalker.SandExplode))]
    public class Sand_Everyone_If_Not_killed
    {
        [HarmonyPostfix]
        public static void Postfix(Stalker __instance, int onDeath)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            BananaDifficultyPlugin.Log.LogInfo($"Sanding stuff... onDeath = {onDeath}, blessed = {__instance.eid.blessed}");
            if (onDeath == 5 && !__instance.eid.blessed)
            {
                foreach (var enemy in EnemyTracker.Instance.enemies)
                {
                    if (enemy.puppet) continue;
                    if (enemy.sandified) continue;
                    BananaDifficultyPlugin.Log.LogInfo($"Sanding {enemy.FullName}");
                    enemy.Sandify();
                }
            }
            else
            {
                GameObject epicExplosion = Object.Instantiate(BananaDifficultyPlugin.bigExplosion, __instance.transform.position, Quaternion.identity);
                foreach (var boom in epicExplosion.GetComponentsInChildren<Explosion>())
                {
                    boom.enemy = true;
                    boom.canHit = AffectedSubjects.PlayerOnly;
                    boom.originEnemy = __instance.eid;
                }
            }
        }
    }

    [HarmonyPatch(typeof(SandificationZone), nameof(SandificationZone.Enter))]
    public class HealThoseInZone
    {
        public static void Postfix(SandificationZone __instance, Collider other)
        {
            BananaDifficultyPlugin.Log.LogInfo("Im patching it");
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            BananaDifficultyPlugin.Log.LogInfo("Passed da check");
            if (other.gameObject == MonoSingleton<NewMovement>.Instance.gameObject)
            {
                return;
            }
            else if (other.gameObject.layer == 10 || other.gameObject.layer == 11)
            {
                BananaDifficultyPlugin.Log.LogInfo("Correct layers :D");
                EnemyIdentifierIdentifier component = other.gameObject.GetComponent<EnemyIdentifierIdentifier>();
                if (component && component.eid && !component.eid.dead)
                {
                    BananaDifficultyPlugin.Log.LogInfo("Restoring health...");
                    if(component.eid.TryGetComponent<Enemy>(out Enemy stats))
                    {
                        BananaDifficultyPlugin.Log.LogInfo("Current health: " + stats.health + " max health " + stats.originalHealth);
                        stats.health = stats.originalHealth;
                        component.eid.health = stats.health;
                    }
                }
            }
        }
    }
}
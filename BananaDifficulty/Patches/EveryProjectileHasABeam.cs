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
    [HarmonyPatch(typeof(Projectile))]
    internal class EveryProjectileHasABeam
    {
        public static List<ContinuousBeam> alreadyExistingBeams = new List<ContinuousBeam>();
        public static List<Projectile> alreadyExistingProjs = new List<Projectile>();
        public static Dictionary<Projectile, ContinuousBeam> connectedBeams = new Dictionary<Projectile, ContinuousBeam>();
        public static int MaxBeams = 50;
        static Dictionary<EnemyType, List<Projectile>> projectilesByEnemy = new Dictionary<EnemyType, List<Projectile>>();
        public static HashSet<EnemyType> AllowedEnemies = new HashSet<EnemyType>()
        {
            EnemyType.Stray,
            EnemyType.Schism,
            EnemyType.Providence,
            EnemyType.FleshPrison,
            EnemyType.FleshPanopticon,
            EnemyType.Soldier,
            EnemyType.Centaur,
            EnemyType.Swordsmachine
        };

        [HarmonyPatch(nameof(Projectile.Start))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Projectile __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            
            if (__instance.decorative) return;
            if (__instance.friendly) return;
            if (alreadyExistingBeams.Count >= MaxBeams)
                return;

            if (!AllowedEnemies.Contains(__instance.safeEnemyType))
                return;


            if (!projectilesByEnemy.TryGetValue(__instance.safeEnemyType, out var list))
            {
                list = new List<Projectile>();
                projectilesByEnemy.Add(__instance.safeEnemyType, list);
            }

            Projectile previous = list.Count > 0 ? list[list.Count - 1] : null;

            list.Add(__instance);



            alreadyExistingProjs.RemoveAll((x) => x == null || !x.gameObject.activeInHierarchy);

            List<Projectile> projsOfItsType = projectilesByEnemy[__instance.safeEnemyType];


            if (previous != null && BananaDifficultyPlugin.projBeam != null)
            {
                ContinuousBeam continuousBeam = Object.Instantiate(
                    BananaDifficultyPlugin.projBeam.GetComponent<ContinuousBeam>(),
                    __instance.transform.position,
                    __instance.transform.rotation,
                    __instance.transform);

                continuousBeam.safeEnemyType = __instance.safeEnemyType;
                continuousBeam.target = __instance.target;
                continuousBeam.endPoint = previous.transform;

                __instance.connectedBeams.Add(continuousBeam);
                previous.connectedBeams.Add(continuousBeam);

                connectedBeams[previous] = continuousBeam;

                alreadyExistingBeams.Add(continuousBeam);
            }

            alreadyExistingProjs.Add(__instance);
        }

    }

    
    [HarmonyPatch(typeof(Punch), nameof(Punch.ParryProjectile))]
    public static class ProjectileParried
    {
        public static void Postfix(Projectile proj)
        {
            if (!BananaDifficultyPlugin.CanUseIt(proj.difficulty)) return;

            if (proj.decorative) return;
            if (proj.friendly) return;

            if(EveryProjectileHasABeam.connectedBeams.ContainsKey(proj))
            {
                proj.connectedBeams.Remove(EveryProjectileHasABeam.connectedBeams[proj]);
                EveryProjectileHasABeam.connectedBeams.Remove(proj);
            }
        }
    }
}
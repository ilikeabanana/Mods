using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch]
    internal class MoreHitsBeforeDeath
    {
        private static readonly Dictionary<int, int> idolHitCount = new Dictionary<int, int>();
        [HarmonyPatch(typeof(Idol), nameof(Idol.Death))]
        [HarmonyPrefix]
        public static bool Death_Prefix(Idol __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return true;
            int idolID = __instance.GetInstanceID();
            // Initialize or increment the throw count for this zombie
            if (!idolHitCount.ContainsKey(idolID))
            {
                idolHitCount[idolID] = 1;
                for (int i = 0; i < 3; i++)
                {
                    GoreZone goreZone = GoreZone.ResolveGoreZone(__instance.transform);
                    GameObject gore = MonoSingleton<BloodsplatterManager>.Instance.GetGore(GoreType.Head, __instance.eid, false);
                    if (!gore)
                    {
                        break;
                    }
                    gore.transform.position = __instance.beam.transform.position;
                    gore.transform.SetParent(goreZone.goreZone, true);
                    gore.SetActive(true);
                    Bloodsplatter bloodsplatter;
                    if (gore.TryGetComponent<Bloodsplatter>(out bloodsplatter))
                    {
                        bloodsplatter.GetReady();
                    }
                }
                __instance.dead = false;
                return false; // First throw, just use the original projectile
            }

            return true;
        }

        [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Death), new System.Type[] {})]
        [HarmonyPrefix]
        public static bool DeathEnemy_Prefix(EnemyIdentifier __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return true;
            Idol idol = __instance.idol;
            if (idol == null)
            {
                return true;
            }
            int idolID = __instance.idol.GetInstanceID();
            if (!idolHitCount.ContainsKey(idolID)) return false;
            return true;
        }
        [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.InstaKill))]
        [HarmonyPrefix]
        public static void InstaDeathEnemy_Prefix(EnemyIdentifier __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return;
            Idol idol = __instance.idol;
            if (idol == null)
            {
                return;
            }
            int idolID = __instance.idol.GetInstanceID();
            idolHitCount[idolID] = 1;
            return;
        }
    }
}
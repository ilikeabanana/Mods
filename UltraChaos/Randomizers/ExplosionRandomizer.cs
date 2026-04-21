using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ultrachaos.Randomizers
{
    [HarmonyPatch]
    public class ExplosionRandomizer : Randomizer<ExplosionController>
    {
        protected override RandomConfigValue GetConfigValue()
        {
            return Plugin.RandomizExplosions.Value;
        }

        public static void GetExplosions()
        {
            ExplosionController[] arG = Resources.FindObjectsOfTypeAll<ExplosionController>();
            Instance.AddRangeToPool(arG);
            Plugin.Logger.LogInfo($"Adding {arG.Length} to the pool, pool length of grenade randomizer is now {Instance.Pool.Count}");
        }

        protected override int GetInstanceID(ExplosionController item)
        {
            if (item == null || item.gameObject == null || item.gameObject.name == null)
                return 0;

            string name = item.gameObject.name.ToLower()
                .Replace(" (clone)", "")
                .Replace("(clone)", "");

            return name.GetHashCode();
        }

        public static ExplosionRandomizer Instance = new ExplosionRandomizer();

        static bool replacing;
        [HarmonyPatch(typeof(ExplosionController), nameof(ExplosionController.Start))]
        [HarmonyPrefix]
        public static bool GrenadeRNG(ExplosionController __instance)
        {
            if (Instance.GetConfigValue() == RandomConfigValue.Disabled) return true;
            if (replacing) return true;
            if (__instance.gameObject.name.Contains("RNG")) return true;

            ExplosionController randomGreg = Instance.GetRandom(__instance);

            replacing = true;
            ExplosionController newGreg = Object.Instantiate(randomGreg, __instance.transform.position, __instance.transform.rotation);
            newGreg.gameObject.name += "RNG";
            replacing = false;
            Object.Destroy(__instance.gameObject);

            return false;
        }

    }
}
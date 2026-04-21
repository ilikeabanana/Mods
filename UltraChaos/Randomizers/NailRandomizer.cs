using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ultrachaos.Randomizers
{
    [HarmonyPatch]
    public class NailRandomizer : Randomizer<Nail>
    {
        protected override RandomConfigValue GetConfigValue()
        {
            return Plugin.RandomizNails.Value;
        }

        public static void GetNails()
        {
            Nail[] arG = Resources.FindObjectsOfTypeAll<Nail>();
            Instance.AddRangeToPool(arG);
            Plugin.Logger.LogInfo($"Adding {arG.Length} to the pool, pool length of nail randomizer is now {Instance.Pool.Count}");
        }

        protected override int GetInstanceID(Nail item)
        {
            int sawType = item.sawblade ? 2 : 4;
            int heat = item.heated ? 1 : 3;

            string objectNameWithoutThing = item.gameObject.name.ToLower().Replace(" (Clone)", "");
            objectNameWithoutThing = objectNameWithoutThing.Replace("(Clone)", "");

            return objectNameWithoutThing.GetHashCode() + sawType + heat;
        }
        public static NailRandomizer Instance = new NailRandomizer();
        public static void ApplyThings(Nail org, Nail newG)
        {
            newG.enemy = org.enemy;
            newG.safeEnemyType = org.safeEnemyType;
            newG.sourceWeapon = org.sourceWeapon;

            if (newG.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb)
                && org.gameObject.TryGetComponent<Rigidbody>(out Rigidbody oRB))
            {
                rb.velocity = oRB.velocity;
                rb.angularVelocity = oRB.angularVelocity;
            }
        }
        static bool replacing;
        [HarmonyPatch(typeof(Nail), nameof(Nail.Start))]
        [HarmonyPrefix]
        public static bool GrenadeRNG(Nail __instance)
        {
            if (Instance.GetConfigValue() == RandomConfigValue.Disabled) return true;
            if (replacing) return true;
            if (__instance.gameObject.name.Contains("RNG")) return true;

            Nail randomGreg = Instance.GetRandom(__instance);

            replacing = true;
            Nail newGreg = Object.Instantiate(randomGreg, __instance.transform.position, __instance.transform.rotation);
            newGreg.gameObject.name += "RNG";
            replacing = false;

            ApplyThings(__instance, newGreg);

            Object.Destroy(__instance.gameObject);

            return false;
        }

    }
}
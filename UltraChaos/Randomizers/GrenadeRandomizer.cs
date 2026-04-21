using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ultrachaos.Randomizers
{
    [HarmonyPatch]
    public class GrenadeRandomizer : Randomizer<Grenade>
    {
        protected override RandomConfigValue GetConfigValue()
        {
            return Plugin.RandomizGrenades.Value;
        }

        public static void GetGrenades()
        {
            Grenade[] arG = Resources.FindObjectsOfTypeAll<Grenade>();
            Instance.AddRangeToPool(arG);
            Plugin.Logger.LogInfo($"Adding {arG.Length} to the pool, pool length of grenade randomizer is now {Instance.Pool.Count}");
        }

        protected override int GetInstanceID(Grenade item)
        {
            string objectNameWithoutThing = item.gameObject.name.ToLower().Replace(" (Clone)", "");
            objectNameWithoutThing = objectNameWithoutThing.Replace("(Clone)", "");

            return objectNameWithoutThing.GetHashCode();
        }
        public static GrenadeRandomizer Instance = new GrenadeRandomizer();
        public static void ApplyThings(Grenade org, Grenade newG)
        {
            newG.enemy = org.enemy;
            newG.originEnemy = org.originEnemy;
            newG.ignoreEnemyType = org.ignoreEnemyType;
            newG.sourceWeapon = org.sourceWeapon;
            newG.hitterWeapon = org.hitterWeapon;
            if (newG.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb)
                && org.gameObject.TryGetComponent<Rigidbody>(out Rigidbody oRB))
            {
                rb.velocity = oRB.velocity;
                rb.angularVelocity = oRB.angularVelocity;

                if (org.rocket && !newG.rocket)
                    rb.AddForce(org.transform.forward * 70f, ForceMode.VelocityChange);
            }

            if (newG.rocket)
            {
                if (!org.rocket)
                {

                    newG.transform.forward = CameraController.Instance.transform.forward;
                }

            }
        }
        static bool replacing;
        [HarmonyPatch(typeof(Grenade), nameof(Grenade.Awake))]
        [HarmonyPrefix]
        public static bool GrenadeRNG(Grenade __instance)
        {
            if (Instance.GetConfigValue() == RandomConfigValue.Disabled) return true;
            if (replacing) return true;

            Grenade randomGreg = Instance.GetRandom(__instance);

            replacing = true;
            Grenade newGreg = Object.Instantiate(randomGreg, __instance.transform.position, __instance.transform.rotation);
            replacing = false;

            ApplyThings(__instance, newGreg);

            Object.Destroy(__instance.gameObject);

            return false;
        }

    }
}
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Ultrachaos.Randomizers
{
    [HarmonyPatch]
    public class ProjectileRandomizer : Randomizer<Projectile>
    {
        protected override RandomConfigValue GetConfigValue()
        {
            return Plugin.ChangeProjectiles.Value;
        }

        protected override int GetInstanceID(Projectile item)
        {
            string objectNameWithoutThing = item.gameObject.name.ToLower().Replace(" (Clone)", "");
            objectNameWithoutThing = objectNameWithoutThing.Replace("(Clone)", "");

            return objectNameWithoutThing.GetHashCode();
        }
        public static ProjectileRandomizer Instance = new ProjectileRandomizer();
        public static void Init()
        {
            Projectile[] projs = Resources.FindObjectsOfTypeAll<Projectile>();
            projs = projs.Where((x) => !x.decorative).ToArray();
            Instance.AddRangeToPool(projs);
        }

        public static void ApplyCorrectThings(Projectile random, Projectile orig)
        {
            random.safeEnemyType = orig.safeEnemyType;
            random.playerBullet = orig.playerBullet;
            random.target = orig.target;
            random.targetHandle = orig.targetHandle;
            random.friendly = orig.friendly;
        }

        public static bool isReplacing = false;

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Awake))]
        [HarmonyPrefix]
        public static bool Randomize(Projectile __instance)
        {
            if (Instance.GetConfigValue() == RandomConfigValue.Disabled) return true;
            if (isReplacing) return true;

            if (__instance.decorative) return true;

            Projectile prefab = Instance.GetRandom(__instance);

            if (prefab == null) return true;
            isReplacing = true;
            Projectile inst = Object.Instantiate(prefab, __instance.transform.position, __instance.transform.rotation);
            isReplacing = false;

            ApplyCorrectThings(inst, __instance);

            Object.Destroy(__instance.gameObject);

            return false;
        }

    }
}
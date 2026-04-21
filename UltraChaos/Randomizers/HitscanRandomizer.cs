using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Ultrachaos.Randomizers
{
    [HarmonyPatch]
    public class HitscanRandomizer : Randomizer<RevolverBeam>
    {
        protected override RandomConfigValue GetConfigValue()
        {
            return Plugin.ChangeHitscans.Value;
        }

        protected override int GetInstanceID(RevolverBeam item)
        {
            string objectNameWithoutThing = item.gameObject.name.ToLower().Replace(" (Clone)", "");
            objectNameWithoutThing = objectNameWithoutThing.Replace("(Clone)", "");

            return objectNameWithoutThing.GetHashCode();
        }
        public static HitscanRandomizer Instance = new HitscanRandomizer();
        public static void Init()
        {
            RevolverBeam[] projs = Resources.FindObjectsOfTypeAll<RevolverBeam>();
            Instance.AddRangeToPool(projs);
        }

        public static void ApplyCorrectThings(RevolverBeam random, RevolverBeam orig)
        {
            random.beamType = orig.beamType;
            random.ignoreEnemyType = orig.ignoreEnemyType;
            random.quickDraw = orig.quickDraw;
        }

        public static bool isReplacing = false;

        [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.Start))]
        [HarmonyPrefix]
        public static bool Randomize(RevolverBeam __instance)
        {
            if (Instance.GetConfigValue() == RandomConfigValue.Disabled) return true;
            if (__instance.gameObject.name.Contains("Randomized")) return true;
            if (isReplacing) return true;

            RevolverBeam prefab = Instance.GetRandom(__instance);

            if (prefab == null) return true;
            isReplacing = true;
            RevolverBeam inst = Object.Instantiate(prefab, __instance.transform.position, __instance.transform.rotation);
            inst.gameObject.name += "Randomized";
            isReplacing = false;

            ApplyCorrectThings(inst, __instance);

            Object.Destroy(__instance.gameObject);

            return false;
        }

    }
}
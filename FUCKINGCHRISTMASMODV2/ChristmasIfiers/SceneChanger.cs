using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FUCKINGCHRISTMASMOD.ChristmasIfiers
{

    [HarmonyPatch]
    public class SceneChanger
    {
        public static Light mainLight;

        public static Color ambientLightColor = new Color(0.1f, 0.1f, 0.2f);

        public static Color mainLightColor = new Color(1f, 0.8f, 0.6f);

        public static float mainLightIntensity = 0.8f;

        public static int numberOfChristmasLights = 50;

        public static Color skyboxColor = new Color(0f, 0f, 0.1f);

        public static bool enableFog = true;

        public static Color fogColor = new Color(0.15f, 0.15f, 0.2f);

        public static float fogDensity = 0.02f;
        public static void Init()
        {
            MaterialSwapper.OnFinish += Done;
        }

        private static void Done()
        {
            Setup();

            HurtZone[] hurtZones = Object.FindObjectsOfType<HurtZone>(true);
            foreach (var zone in hurtZones)
            {
                if(zone.damageType == EnviroDamageType.Burn || zone.damageType == EnviroDamageType.WeakBurn)
                {
                    Object.Destroy(zone); // No More ZONE
                }
            }

            ExplosionController[] controllers = Object.FindObjectsByType<ExplosionController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var con in controllers)
            {
                if (con.gameObject.name.ToLower().Contains("fire"))
                {
                    Object.Destroy(con.gameObject); // no more fire
                }
            }

            ItemIdentifier[] items = Object.FindObjectsByType<ItemIdentifier>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var item in items)
            {
                Transform skul = item.transform.Find("NewSkull");

                if(skul != null)
                {
                    GameObject cookies = Object.Instantiate(Plugin.SkullReplacer, item.transform);
                    Object.Destroy(skul.gameObject);
                }
            }
        }

        public static void Setup()
        {
            SetupFog();
            SetupAmbientLighting();
            SetupMainLighting();
        }

        public static void Update()
        {
            Setup();
        }

        private static void SetupFog()
        {
            RenderSettings.fog = RenderSettings.fog;
            if (RenderSettings.fog)
            {
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogMode = (FogMode)2;
                RenderSettings.fogDensity = fogDensity;
            }
        }

        private static void SetupAmbientLighting()
        {
            RenderSettings.ambientMode = (AmbientMode)3;
            RenderSettings.ambientLight = ambientLightColor;
            if (RenderSettings.skybox != null)
            {
                RenderSettings.skybox = RenderSettings.skybox;
                RenderSettings.skybox.SetColor("_Tint", skyboxColor);
            }
            else
            {
                Debug.LogWarning("Skybox material is not assigned!");
            }
        }

        private static void SetupMainLighting()
        {
            if (mainLight == null)
            {
                foreach (Light item in GameObject.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    item.color = mainLightColor;
                    item.intensity = mainLightIntensity;
                    item.shadows = (LightShadows)2;
                }
            }
            if ((Object)(object)mainLight != (Object)null)
            {
                mainLight.color = mainLightColor;
                mainLight.intensity = mainLightIntensity;
                mainLight.shadows = (LightShadows)2;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TimeOfDayChanger), "Activate")]
        private static void ActivatePostfix(TimeOfDayChanger __instance)
        {
            __instance.fogColor = fogColor;
            RenderSettings.skybox.SetColor("_Tint", skyboxColor);
            if (__instance.newSky != null)
            {
                __instance.newSky.SetColor("_Tint", skyboxColor);
            }
            Light[] newLights = __instance.newLights;
            foreach (Light val in newLights)
            {
                val.color = mainLightColor;
                val.intensity = mainLightIntensity;
            }
        }
    }

    [HarmonyPatch(typeof(PrefsManager))]
    public class DontHaveHeat
    {
        [HarmonyPatch(nameof(PrefsManager.GetBoolLocal))]
        [HarmonyPostfix]
        public static void Local(ref bool __result, string key, bool fallback = false)
        {
            if(key == "disableHeatwaves") __result = true; // always return true if heatwave disabling
        }
        [HarmonyPatch(nameof(PrefsManager.GetBool))]
        [HarmonyPostfix]
        public static void GlobalIG(ref bool __result, string key, bool fallback = false)
        {
            if(key == "disableHeatwaves") __result = true; // always return true if heatwave disabling
        }
    }

    [HarmonyPatch(typeof(ShopZone))]
    public class CoolMusic
    {
        [HarmonyPatch(nameof(ShopZone.Start))]
        [HarmonyPostfix]
        public static void musice(ShopZone __instance)
        {
            __instance.music.clip = Plugin.ChristmasMusic;
        }
    }
}

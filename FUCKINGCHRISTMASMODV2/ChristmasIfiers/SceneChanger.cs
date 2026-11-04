using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FUCKINGCHRISTMASMOD.ChristmasIfiers
{
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

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private static void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            SetupFog();
            SetupAmbientLighting();
            SetupMainLighting();
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

        [HarmonyPrefix]
        [HarmonyPatch("Activate")]
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
}

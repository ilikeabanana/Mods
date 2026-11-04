using BepInEx;
using BepInEx.Logging;
using FUCKINGCHRISTMASMOD.ChristmasIfiers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FUCKINGCHRISTMASMOD
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static bool IgnoreObject(GameObject obj)
        {
            return IsChildOfPlayer(obj) || IsEnemy(obj);
        }

        public static bool IsEnemy(GameObject obj)
        {
            return LayerMaskDefaults.IsMatchingLayer(obj.layer, LMD.Enemies) || obj.layer == 9 || obj.layer == 0;
        }

        public static bool IsChildOfPlayer(GameObject obj)
        {
            if (NewMovement.Instance == null) return false;
            return obj.transform.IsChildOf(NewMovement.Instance.transform);
        }

        internal static new ManualLogSource Logger { get; private set; } = null!;

        public static Plugin Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            BundleLoader.Load();
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            gameObject.hideFlags = HideFlags.DontSaveInEditor;

            MaterialSwapper.Init();
            SceneChanger.Init();
            TreeChristmas.Init();
            WaterToIce.Init();

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        GameObject snowPart = null;
        public static GameObject christmasTree = null;
        public static Material ThickestIce = null;

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if(snowPart != null && NewMovement.Instance != null)
            {
                Instantiate(snowPart, NewMovement.Instance.transform);


                foreach (var part in GameObject.FindObjectsByType<EnviroParticle>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
                {
                    if (part.gameObject.name.ToLower().Contains("rain")) Destroy(part.gameObject);
                } // remove all the rain
            }
        }

        public static void Coroutine(IEnumerator numerator)
        {
            Instance.StartCoroutine(numerator);
        }

        private void Update()
        {
            if(BundleLoader.alrLoaded && snowPart == null)
            {
                snowPart = BundleLoader.LoadObject("Snowing.prefab");
                ThickestIce = BundleLoader.LoadMat("IceThickest.mat"); // Assets/Modding/Christmas/IceThickest.mat
                christmasTree = BundleLoader.LoadObject("ChristmasTree.prefab");
            }


            if (Input.GetKeyDown(KeyCode.End))
            {
                MaterialSwapper.Replace();
            }
        }
    }
}

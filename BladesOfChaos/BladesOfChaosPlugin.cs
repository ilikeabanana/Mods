using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BladesOfChaos
{
    // TODO Review this file and update to your own requirements.

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class BladesOfChaosPlugin : BaseUnityPlugin
    {
        // Mod specific details. MyGUID should be unique, and follow the reverse domain pattern
        // e.g.
        // com.mynameororg.pluginname
        // Version should be a valid version string.
        // e.g.
        // 1.0.0
        private const string MyGUID = "com.banana.BladesOfChaos";
        private const string PluginName = "BladesOfChaos";
        private const string VersionString = "1.0.0";


        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        GameObject blades;
        public static GameObject bladeModel;
        GameObject bladesofthechaos;

        public static ConfigEntry<KeyCode> NemeanCrush;
        public static ConfigEntry<KeyCode> MeteoricSlam;

        /// <summary>
        /// Initialise the configuration settings and patch methods
        /// </summary>
        private void Awake()
        {
            NemeanCrush = Config.Bind<KeyCode>("Keys", "Nemean Crush", KeyCode.Z);
            MeteoricSlam = Config.Bind<KeyCode>("Keys", "Meteoric Slam", KeyCode.X);

            // Apply all of our patches
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");

            // Sets up our static Log, so it can be used elsewhere in code.
            // .e.g.
            // BladesOfChaosPlugin.Log.LogDebug("Debug Message to BepInEx log file");
            Log = Logger;
            var assembly2 = Assembly.GetExecutingAssembly();
            AssetBundle bundle = AssetBundle.LoadFromStream(assembly2.GetManifestResourceStream("BladesOfChaos.bladesofchaos"));
            blades = bundle.LoadAsset<GameObject>("V1RageFinal");
            bladeModel = bundle.LoadAsset<GameObject>("Blade");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }
        void Update()
        {
            if(bladesofthechaos == null)
            {
                bladesofthechaos = MakeGun(5, blades);
            }
        }
        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            //throw new System.NotImplementedException();
        }

        public static GameObject MakeGun(int var, GameObject original)
        {
            int num = var;
            // Making sure it isnt null to prevent errors
            bool flag = MonoSingleton<GunControl>.Instance == null || MonoSingleton<StyleHUD>.Instance == null;
            bool flag2 = flag;
            // defining result
            GameObject result;
            if (flag2)
            {
                result = null;
            }
            else
            {
                // Checking everything so we dont get any errors
                bool flag3 = !MonoSingleton<GunControl>.Instance.enabled || !MonoSingleton<StyleHUD>.Instance.enabled;
                bool flag4 = flag3;
                if (flag4)
                {
                    result = null;
                }
                else
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original);
                    bool flag5 = gameObject == null;
                    bool flag6 = flag5;
                    if (flag6)
                    {
                        result = null;
                    }
                    else
                    {
                        Vector3 pos = gameObject.transform.position;
                        Quaternion rot = gameObject.transform.rotation;
                        // Assigning the transforms
                        gameObject.transform.parent = MonoSingleton<GunControl>.Instance.transform;
                        gameObject.transform.localPosition = pos;
                        gameObject.transform.localRotation = rot;
                        // Adding it to the slots
                        MonoSingleton<GunControl>.Instance.slots[num].Add(gameObject);
                        MonoSingleton<GunControl>.Instance.allWeapons.Add(gameObject);
                        MonoSingleton<GunControl>.Instance.slotDict.Add(gameObject, num);
                        MonoSingleton<StyleHUD>.Instance.weaponFreshness.Add(gameObject, 10f);
                        // Setting the object inactive as default
                        gameObject.SetActive(false);
                        // Setting noweapons to false and doing yesweapons
                        MonoSingleton<GunControl>.Instance.noWeapons = false;
                        MonoSingleton<GunControl>.Instance.YesWeapon();
                        // Setting every child inactive
                        for (int k = 0; k < MonoSingleton<GunControl>.Instance.transform.childCount; k++)
                        {
                            MonoSingleton<GunControl>.Instance.transform.GetChild(k).gameObject.SetActive(false);
                        }
                        result = gameObject;
                    }
                }
            }
            return result;
        }
    }
}

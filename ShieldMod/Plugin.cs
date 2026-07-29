using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shield_Mod
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger { get; private set; } = null!;

        GameObject ShieldPrefab;
        GameObject ShieldPrefabGreen;
        GameObject ShieldPrefabRed;
        GameObject ShieldWeapon;
        GameObject ShieldWeaponGreen;
        GameObject ShieldWeaponRed;

        //GameObject jumpscare;

        Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_NAME);

        public static Plugin Instance { get; set; }
        private void Awake()
        {
            // Plugin startup logic
            Harmony.PatchAll();
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            gameObject.hideFlags = HideFlags.DontSaveInEditor;

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            Instance = this;
        }
        void Update()
        {
            if (BundleLoader.alrLoaded && ShieldPrefab == null)
            {
                ShieldPrefab = BundleLoader.LoadObject("Shield.prefab");
                ShieldPrefabGreen = BundleLoader.LoadObject("ShieldGreen Variant.prefab");
                ShieldPrefabRed = BundleLoader.LoadObject("ShieldRed Variant.prefab");

                //jumpscare = BundleLoader.LoadObject("JumpScare.prefab");
            }

            //if (Input.GetKeyDown(KeyCode.J) && CanUseJumpscare())
            //{
            //    Transform pos = CameraController.Instance == null ? Camera.main.transform : CameraController.Instance.transform;

            //    Instantiate(jumpscare, pos);
            //}
            if (ShieldPrefab == null || NewMovement.Instance == null) return;
            if (ShieldWeapon == null)
            {
                ShieldWeapon = MakeGun(5, ShieldPrefab);
            }
            if (ShieldWeaponGreen == null)
            {
                ShieldWeaponGreen = MakeGun(5, ShieldPrefabGreen);
            }
            if (ShieldWeaponRed == null)
            {
                ShieldWeaponRed = MakeGun(5, ShieldPrefabRed);
            }

        }

        //bool CanUseJumpscare()
        //{
        //    if (SceneHelper.CurrentScene == "Main Menu" || SceneHelper.CurrentScene == "Level 2-S" || SceneHelper.CurrentScene.Contains("Intermission")) return false;

        //    return PlayerPrefs.GetInt("BeatenShild", 0) == 1;
        //}

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

                    if (gameObject.TryGetComponent<Collider>(out Collider col))
                    {
                        Destroy(col);
                    }

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
        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if(SceneHelper.CurrentScene == "Main Menu")
            {
                //AssetsManager.GetAssets();
                BundleLoader.Load();
            }
            //if(arg0.name == SceneName)
            //{
            //    FindObjectOfType<Button>().onClick.AddListener(() => SceneHelper.LoadScene("Main Menu"));
            //}
        }

        //bool LoadingScene = false;
        //public const string SceneName = "CongratYouWin";

        //public IEnumerator LoadMYLevel()
        //{
        //    if (LoadingScene) yield break;
        //    LoadingScene = true;

        //    SceneHelper.PendingScene = SceneName;



        //    // actually fucking load the scene lmao
        //    var op = Addressables.LoadSceneAsync("Assets/Modding/ShieldMod/CongratYouWin.unity", LoadSceneMode.Single);
        //    yield return op;

        //    // set current scene and last scene once the level is done loading
        //    if (SceneHelper.CurrentScene != SceneName)
        //        SceneHelper.LastScene = SceneHelper.CurrentScene;

        //    SceneHelper.CurrentScene = SceneName;

        //    SceneHelper.PendingScene = null;
        //    SceneHelper.SetLoadingSubtext("");
        //    SceneHelper.Instance.loadingBlocker.SetActive(false);

        //    Cursor.lockState = CursorLockMode.None;
        //    Cursor.visible = true;

        //    PlayerPrefs.SetInt("BeatenShild", 1);
        //}
    }

    //[HarmonyPatch(typeof(SceneHelper), nameof(SceneHelper.LoadScene))]
    //public class HahaGoToMYLEVEL
    //{
    //    public static bool Prefix(ref string sceneName)
    //    {
    //        if(sceneName == "EarlyAccessEnd")
    //        {
    //            Plugin.Instance.StartCoroutine(Plugin.Instance.LoadMYLevel());
    //            return false;
    //        }

    //        return true;
    //    }
    //}

    [HarmonyPatch(typeof(StyleCalculator), nameof(StyleCalculator.HitCalculator))]
    public class CustomStyle
    {
        public static void Prefix(StyleCalculator __instance, string hitter, string enemyType, string hitLimb, bool dead, EnemyIdentifier eid = null, GameObject sourceWeapon = null)
        {
            if (eid != null && eid.blessed)
            {
                return;
            }
            if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.Platformer)
            {
                return;
            }

            if(hitter == "shieldcharge")
            {
                __instance.AddPoints(15, "", eid, sourceWeapon);
                if (dead)
                {
                    __instance.AddPoints(75, "BATTERING RAM", eid, sourceWeapon);
                }
            }
            if(hitter == "shield")
            {
                __instance.AddPoints(45, "", eid, sourceWeapon);
                if (dead)
                {
                    __instance.AddPoints(85, "CLANG", eid, sourceWeapon);
                }
            }
            if(hitter == "shieldproj")
            {
                __instance.AddPoints(5, "", eid, sourceWeapon);
                if (dead)
                {
                    __instance.AddPoints(55, "REBOUND", eid, sourceWeapon);
                }
            }
            if(hitter == "shieldnobounce")
            {
                __instance.AddPoints(48, "", eid, sourceWeapon);
                if (dead)
                {
                    __instance.AddPoints(90, "BULLSEYE", eid, sourceWeapon);
                }
            }
        }
    }
}

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;
using Discord;
using System.Text.RegularExpressions;
using System.Reflection;

namespace BananaDifficulty
{
    // TODO Review this file and update to your own requirements.

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class BananaDifficultyPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.michi.BananaDifficulty";
        private const string PluginName = "BananaDifficulty";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        public static GameObject projBeam;
        public static GameObject projHoming;
        public static GameObject idol;
        public static GameObject RocketEnemy;
        public static GameObject snakeProj;
        public static GameObject insignificant;
        public static GameObject bigExplosion;
        public static GameObject blackHole;
        public static GameObject spear;
        public static GameObject v2FlashUnpariable;
        public static GameObject summonedSwords;
        public static GameObject homingHH;
        public static GameObject homingBlue;
        private void Awake()
        {
            // Apply all of our patches
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

            gameObject.hideFlags = HideFlags.DontSaveInEditor;

            Log = Logger;

            GetAssets();
            GetBundleAssets();
        }

        private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            string text = "b3e7f2f8052488a45b35549efb98d902";
            Scene activeScene = SceneManager.GetActiveScene();
            string name = ((Scene)(activeScene)).name;
            if(name == text)
            {
                MakeTheNewDifficulty();
            }
        }

        public static bool CanUseIt(int difficulty)
        {
            if (difficulty < 0)
                difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
            return difficulty == 5;
        }
        
        void OnDestroy()
        {
            Harmony.UnpatchAll();
        }

        public void GetBundleAssets()
        {
            var a = Assembly.GetExecutingAssembly();

            spear = AssetBundle.LoadFromStream(a.GetManifestResourceStream("BananaDifficulty.Bundles.v2spear")).LoadAsset<GameObject>("V2Spear");
        }
        public void GetAssets()
        {
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                projBeam = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Projectile Beam.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                projHoming = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Projectile Homing.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                idol = x;
            }, "Assets/Prefabs/Enemies/Idol.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                RocketEnemy = x;
            }, "Assets/Prefabs/Attacks and Projectiles/RocketEnemy.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                snakeProj = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Projectile Minos Prime Snake.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                insignificant = x;
            }, "Virtue Insignia"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                bigExplosion = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion Big.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                blackHole = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Black Hole Projectile.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                v2FlashUnpariable = x;
            }, "Assets/Particles/Flashes/V2FlashUnparriable.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                summonedSwords = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Gabriel/GabrielSummonedSwords.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                homingBlue = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Projectile Homing.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                homingHH = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Projectile Explosive HH.prefab"));
        }

        public IEnumerator LoadAddressable<T>(Action<T> onLoad, string path)
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);

            yield return new WaitUntil(() => handle.IsDone);

            onLoad.Invoke(handle.Result);
        }


        void MakeTheNewDifficulty()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            activeScene = SceneManager.GetActiveScene();
            Transform val = (from obj in ((Scene)(activeScene)).GetRootGameObjects()
                             where ((Object)obj).name == "Canvas"
                             select obj).First().transform.Find("Difficulty Select (1)").Find("Interactables");
            GameObject newDifobj = Object.Instantiate<GameObject>(((Component)val.Find("Brutal")).gameObject, val);
            DifficultySelectButton newDif = newDifobj.GetComponent<DifficultySelectButton>();
            newDif.difficulty = 5; // Modify the new button
            newDif.transform.position += new Vector3(700, 0);

            Button button = newDif.GetComponent<Button>();

            button.interactable = true;

            //newDif.transform.Find("Under Construction").gameObject.SetActive(false);

            TextMeshProUGUI name = newDif.transform.Find("Name").gameObject.GetComponent<TextMeshProUGUI>();

            name.text = "Bananas Difficulty";
            name.color = Color.white;

            Transform infoTransform = FindDeepChild(newDif.transform.parent, "Brutal Info");

            if (infoTransform == null)
            {
                Debug.LogError("Could not find 'Brutal Info' under " + newDif.transform.parent.name);
                return; // Prevent crash
            }

            GameObject infoObject = Object.Instantiate(infoTransform.gameObject, infoTransform.parent);
            Transform info = infoObject.transform;

            Transform textTransform = info.Find("Text");
            if (textTransform == null)
            {
                Debug.LogError("Could not find 'Text' under 'Brutal Info'");
                return;
            }

                ((Component)info.transform.Find("Text")).GetComponent<TMP_Text>().text = "<color=white>Mod made by banana to make it very VERY difficult\nFast enemies. Changed behaviour. Alot of projectiles. Alot of beams. And much much more.";

            TMP_Text component = ((Component)info.transform.Find("Title (1)")).GetComponent<TMP_Text>();
            component.fontSize = 29f;
            component.text = "--BANANAS DIFFICULTY--";

            EventTrigger component2 = newDif.gameObject.GetComponent<EventTrigger>();

            // Ensure the EventTrigger component exists
            if (component2 == null)
            {
                component2 = newDif.gameObject.AddComponent<EventTrigger>();
            }

            // Ensure info is not null before proceeding
            if (info == null)
            {
                Debug.LogError("Info object is null, cannot set up EventTrigger!");
                return;
            }

            // Clear existing triggers to avoid duplicates
            component2.triggers.Clear();

            // Create new event for PointerEnter
            EventTrigger.Entry val3 = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter // Use proper enum
            };
            val3.callback.AddListener((BaseEventData _) => info.gameObject.SetActive(true));

            // Create new event for PointerExit
            EventTrigger.Entry val4 = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit // Use proper enum
            };
            val4.callback.AddListener((BaseEventData _) => info.gameObject.SetActive(false));

            // Add events to the EventTrigger component
            component2.triggers.Add(val3);
            component2.triggers.Add(val4);
        }
        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                Debug.Log("Checking child: " + child.name); // Debugging log
                if (child.name == name) return child;
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            Debug.LogWarning($"Child '{name}' not found under '{parent.name}'"); // Log warning if not found
            return null;
        }
    }
    [HarmonyPatch(typeof(DiscordController), nameof(DiscordController.SendActivity))]
    internal class DiscordController_SendActivity_Patch
    {
        // Token: 0x0600005D RID: 93 RVA: 0x0000DB64 File Offset: 0x0000BD64
        private static bool Prefix(DiscordController __instance, ref Activity ___cachedActivity)
        {
            bool flag = ___cachedActivity.State != null && ___cachedActivity.State == "DIFFICULTY: UKMD";
            if (flag)
            {
                Regex regex = new Regex("<[^>]*>");
                string text = "DIFFICULTY: " + "BANANAS";
                bool flag2 = regex.IsMatch(text);
                if (flag2)
                {
                    ___cachedActivity.State = regex.Replace(text, string.Empty);
                }
                else
                {
                    ___cachedActivity.State = text;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PrefsManager), nameof(PrefsManager.EnsureValid))]
    public class MakeNewDifficulty
    {

        public static void Postfix(ref object __result, string key, object value)
        {
            if(key == "difficulty" && (int)value == 5)
            {
                __result = 5;
            }
        }


    }
    [HarmonyPatch(typeof(DifficultyTitle), nameof(DifficultyTitle.Check))]
    public class DifficultyTitle_Check_Patch
    {
        // Token: 0x0600005B RID: 91 RVA: 0x0000DAFC File Offset: 0x0000BCFC
        private static void Postfix(DifficultyTitle __instance)
        {
            bool flag = __instance.txt2.text.Contains("ULTRAKILL MUST DIE");
            if (flag)
            {
                __instance.txt2.text = __instance.txt2.text.Replace("ULTRAKILL MUST DIE", "BANANAS DIFFICULTY");
            }
        }
    }

}

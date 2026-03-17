using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Discord;
using HarmonyLib;
using NewBananaWeapons;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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

        public static ConfigEntry<bool> HardMode;

        public static GameObject projBeam;
        public static GameObject projBeamTurret;
        public static GameObject projHoming;
        public static GameObject projNormal;
        public static GameObject idol;
        public static GameObject RocketEnemy;
        public static GameObject snakeProj;
        public static GameObject insignificant;
        public static GameObject bigExplosion; 
        public static GameObject blackHole;
        public static GameObject blackHoleExplosion;
        public static GameObject spear;
        public static GameObject v2FlashUnpariable;
        public static GameObject mindBeam;
        public static GameObject chargedExplosion;
        public static GameObject summonedSwords;
        public static GameObject homingHH;
        public static GameObject homingBlue;
        public static GameObject upArrow;
        public static GameObject superExplosion;
        public static GameObject forwardArrow;
        public static GameObject spinyProvi;
        public static Material WhiplashMat;
        public static Material MindflayerBeamMat;
        public static GameObject WhiplashThrow;
        public static GameObject rubbleBig;
        public static AudioClip WhiplashLoop;
        public static GameObject lightningWindup;
        public static GameObject lightningExplosion;
        public static GameObject shockwave;
        public static GameObject cannonball;
        public static GameObject gabrielThrownSpear;
        public static GameObject mirrorReaperWave;
        public static GameObject thrownSwordH;
        public static GameObject providenceOrb;


        public static GameObject pizzaAttack;
        public static GameObject ratAttack;

        public static GameObject newCancerHallway;
        public static GameObject fallAttack;

        public static AssetBundle bundle;
        private void Awake()
        {
            // Apply all of our patches
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

            gameObject.hideFlags = HideFlags.DontSaveInEditor;

            Log = Logger;

            HardMode = Config.Bind<bool>("Difficulty Settings", "Hard Mode", false, "Makes virtue beams appear on every side, have double shockwaves, and also makes schisms fire thrice as many projectiles.");

            GetAssets();
            //GetBundleAssets();
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

            if (ShaderManager.shaderDictionary.Count == 0)
            {
                StartCoroutine(ShaderManager.LoadShadersAsync());
            }
            else
            {
                GetBundleAssets();
            }

            if (SceneHelper.CurrentScene == "Level 1-2")
            {
                if (!BananaDifficultyPlugin.CanUseIt(-999)) return;
                Log.LogInfo("Replacing hallway");

                GameObject normalHallway = GameObject
                    .FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.name == "CV11 - Hallway")?.gameObject;

                Log.LogInfo("Found Hallway!!!");
                GameObject replacement = Instantiate(newCancerHallway);

                foreach (var door in GameObject.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    for (int i = 0; i < door.activatedRooms.Length; i++)
                    {
                        if (door.activatedRooms[i] == normalHallway)
                            door.activatedRooms[i] = replacement;
                    }
                    for (int i = 0; i < door.deactivatedRooms.Length; i++)
                    {
                        if (door.deactivatedRooms[i] == normalHallway)
                            door.deactivatedRooms[i] = replacement;
                    }
                }

                foreach (var breaka in GameObject.FindObjectsByType<Breakable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    for (int i = 0; i < breaka.activateOnBreak.Length; i++)
                    {
                        if (breaka.activateOnBreak[i] == normalHallway)
                            breaka.activateOnBreak[i] = replacement;
                    }
                }

                foreach (var check in GameObject.FindObjectsByType<CheckPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (check.toActivate == normalHallway)
                        check.toActivate = replacement;

                    for (int i = 0; i < check.rooms.Length; i++)
                    {
                        if (check.rooms[i] == normalHallway)
                            check.rooms[i] = replacement;
                    }
                }

                replacement.SetActive(false);
                Object.Destroy(normalHallway);

            }

        }

        public static bool CanUseIt(int difficulty)
        {
            if (difficulty < 0)
                difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
            return difficulty == 5;
        }

        public void GetBundleAssets()
        {
            var a = Assembly.GetExecutingAssembly();

            if(spear == null)
                spear = AssetBundle.LoadFromStream(a.GetManifestResourceStream("BananaDifficulty.Bundles.v2spear")).LoadAsset<GameObject>("V2Spear");

            if (bundle == null)
                bundle = AssetBundle.LoadFromStream(a.GetManifestResourceStream("BananaDifficulty.Bundles.bananadifficulty"));

            if (newCancerHallway == null)
            {
                newCancerHallway = bundle.LoadAsset<GameObject>("CV11 - Hallway");
                StartCoroutine(ShaderManager.ApplyShaderToGameObject(newCancerHallway));
                foreach (var item in newCancerHallway.GetComponentsInChildren<Breakable>())
                {
                    if(item.breakParticle != null)
                        StartCoroutine(ShaderManager.ApplyShaderToGameObject(item.breakParticle));
                }
                
            }

            if(fallAttack == null)
            {
                fallAttack = bundle.LoadAsset<GameObject>("FallAttack");
                StartCoroutine(ShaderManager.ApplyShaderToGameObject(fallAttack));
            }

            if(pizzaAttack == null)
            {
                pizzaAttack = bundle.LoadAsset<GameObject>("PizzaAttakc");
                StartCoroutine(ShaderManager.ApplyShaderToGameObject(pizzaAttack));
            }
            if(ratAttack == null)
            {
                ratAttack = bundle.LoadAsset<GameObject>("CancerousRodentProj");
                StartCoroutine(ShaderManager.ApplyShaderToGameObject(ratAttack));
                StartCoroutine(ShaderManager.ApplyShaderToGameObject(ratAttack.GetComponent<Projectile>().explosionEffect));
            }
            
        }
        public void GetAssets()
        {
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                lightningWindup = x;
            }, "Assets/Particles/Environment/LightningBoltWindup.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                projNormal = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Projectile.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                chargedExplosion = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion Sisyphus Prime Charged.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                lightningExplosion = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Explosions/Lightning Strike Explosive.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                shockwave = x;
            }, "Assets/Prefabs/Attacks and Projectiles/PhysicalShockwave.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                cannonball = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Cannonball.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                providenceOrb = x;
            }, "Assets/Prefabs/Levels/Interactive/GrapplePointSlingshotProvidence.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                gabrielThrownSpear = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Gabriel/GabrielThrownSpear.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                mirrorReaperWave = x;
            }, "Assets/Prefabs/Attacks and Projectiles/MirrorReaperGroundWave.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                thrownSwordH = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Swordsmachine/Thrown Sword Horizontal.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                projBeamTurret = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Turret Beam.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                rubbleBig = x;
            }, "Assets/Particles/RubbleBigDistant.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                superExplosion = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion Super.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                forwardArrow = x;
            }, "Assets/Prefabs/Attacks and Projectiles/GeryonForwardArrowBeam.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                upArrow = x;
            }, "Assets/Prefabs/Attacks and Projectiles/GeryonUpArrowBeam.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                blackHoleExplosion = x;
            }, "Assets/Particles/BlackHoleExplosion.prefab"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                mindBeam = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Mindflayer Beam.prefab"));
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
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                WhiplashThrow = x;
            }, "Assets/Particles/SoundBubbles/HookArmThrow.prefab"));
            StartCoroutine(LoadAddressable<Material>((x) =>
            {
                WhiplashMat = x;
            }, "Assets/Materials/SnaketrailOpaque.mat"));
            StartCoroutine(LoadAddressable<Material>((x) =>
            {
                MindflayerBeamMat = x;
            }, "Assets/Materials/Sprites/MindflayerBeam.mat"));
            StartCoroutine(LoadAddressable<AudioClip>((x) =>
            {
                WhiplashLoop = x;
            }, "Assets/Sounds/Weapons/Whiplash Throw Loop.wav"));
            StartCoroutine(LoadAddressable<GameObject>((x) =>
            {
                spinyProvi = x;
            }, "Assets/Prefabs/Attacks and Projectiles/Projectile Providence Geryon.prefab"));
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

                ((Component)info.transform.Find("Text")).GetComponent<TMP_Text>().text = "<color=white>Mod made by banana to make it very VERY difficult\r\nFast enemies. A LOT of projectiles. A ridiculous amount of projectiles.\r\nChanged behaviour. A few brand new attacks. More beams.\r\nAnd much, much more. \n\n\n\r meant to be very unfair";

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

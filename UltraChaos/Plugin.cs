using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Ultrachaos.Randomizers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ultrachaos
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger { get; private set; } = null!;
        Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        public static ConfigFile C;

        public static RandomConfig<RandomConfigValue> ChangeMaterials;
        public static RandomConfig<RandomConfigValue> ChangeSounds;
        public static RandomConfig<RandomConfigValue> ChangeEnemies;
        public static RandomConfig<RandomConfigValue> ChangeMeshes;
        public static RandomConfig<RandomConfigValue> ChangeProjectiles;
        public static RandomConfig<RandomConfigValue> ChangeHitscans;
        public static RandomConfig<RandomConfigValue> ChangeMusic;
        public static RandomConfig<RandomConfigValue> ChangeImages;
        public static RandomConfig<RandomConfigValue> ChangeText;
        public static RandomConfig<RandomConfigValue> ChangeItems;
        public static RandomConfig<RandomConfigValue> RandomizeLevels;
        public static RandomConfig<RandomConfigValue> RandomizGrenades;
        public static RandomConfig<RandomConfigValue> RandomizNails;
        public static RandomConfig<RandomConfigValue> RandomizExplosions;
        public static RandomConfig<bool> ChangeAnimator;
        public static RandomConfig<bool> OriginalHealthEID;

        public static ConfigPanel ChaosPanel;
        public static ConfigPanel NormalPanel;
        public static ConfigPanel PlayerPanel;
        public static ConfigPanel EnemyRNGList;

        static PluginConfigurator config;

        public static Plugin Instance { get; private set; }

        public static List<Action<GameObject>> OnInstantiateMethod = new List<Action<GameObject>>();
        private void Awake()
        {
            // Plugin startup logic
            Instance = this;
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            gameObject.hideFlags = HideFlags.DontSaveInEditor;
            Harmony.PatchAll();
            C = Config;
            config = PluginConfigurator.Create("ULTRACHAOS", MyPluginInfo.PLUGIN_GUID);
            ChaosPanel = new ConfigPanel(config.rootPanel, "C H A O S", "panel.chaos");
            NormalPanel = new ConfigPanel(config.rootPanel, "Normal", "panel.normal");
            PlayerPanel = new ConfigPanel(config.rootPanel, "Player", "panel.player");
            EnemyRNGList = new ConfigPanel(config.rootPanel, "Enemies", "panel.enemies");
            SetupConfigs();

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SoundRandomizer.Init();

            var icCheatType = typeof(ICheat);
            var allImplementors = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => icCheatType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            var postfix = typeof(TextRandomizer)
                .GetMethod(nameof(TextRandomizer.CheatName));

            foreach (var type in allImplementors)
            {
                var getter = type.GetProperty("LongName")?.GetGetMethod();
                if (getter != null)
                    Harmony.Patch(getter, postfix: new HarmonyMethod(postfix));
            }


            LevelRandomizer.Init();
            StartCoroutine(EnemyRandomizer.Init());
        }

        public static bool CanChangeObj(GameObject obj)
        {
            return !isConfiggyChild(obj.transform);
        }

        public static bool isConfiggyChild(Transform obj)
        {
            if (obj.gameObject.name.Contains("Configuration")) return true;

            if (obj.parent != null)
                return isConfiggyChild(obj.parent);

            return false;
        }

        public static bool isPlayerChild(Transform obj)
        {
            if (obj.GetComponent<NewMovement>()) return true;
            if (obj.gameObject.name.Contains("Player")) return true;

            if (obj.parent != null)
                return isPlayerChild(obj.parent);

            return false;
        }

        public static void SetupConfigs()
        {
            ChangeMaterials = new RandomConfig<RandomConfigValue>(ChaosPanel, "Change Materials", RandomConfigValue.Disabled);
            ChangeSounds = new RandomConfig<RandomConfigValue>(ChaosPanel, "Change Sounds", RandomConfigValue.Disabled);
            ChangeMeshes = new RandomConfig<RandomConfigValue>(ChaosPanel, "Change Meshes", RandomConfigValue.Disabled);
            ChangeProjectiles = new RandomConfig<RandomConfigValue>(ChaosPanel, "Change Projectiles", RandomConfigValue.Disabled);
            ChangeHitscans = new RandomConfig<RandomConfigValue>(ChaosPanel, "Change Hitscans", RandomConfigValue.Disabled);
            ChangeText = new RandomConfig<RandomConfigValue>(ChaosPanel, "Change Texts", RandomConfigValue.Disabled);
            RandomizGrenades = new RandomConfig<RandomConfigValue>(ChaosPanel, "Change Grenades", RandomConfigValue.Disabled);
            RandomizNails = new RandomConfig<RandomConfigValue>(ChaosPanel, "Change Nails", RandomConfigValue.Disabled);
            RandomizExplosions = new RandomConfig<RandomConfigValue>(ChaosPanel, "Change Explosions", RandomConfigValue.Disabled);
            ChangeImages = new RandomConfig<RandomConfigValue>(ChaosPanel, "Change Images", RandomConfigValue.Disabled);
            ChangeItems = new RandomConfig<RandomConfigValue>(ChaosPanel, "Change Items", RandomConfigValue.Disabled);
            ChangeAnimator = new RandomConfig<bool>(ChaosPanel, "Change Animator", false);
            ChangeEnemies = new RandomConfig<RandomConfigValue>(NormalPanel, "Change Enemies", RandomConfigValue.Disabled);
            OriginalHealthEID = new RandomConfig<bool>(NormalPanel, "Keep original enemys health", false);
            ChangeMusic = new RandomConfig<RandomConfigValue>(NormalPanel, "Change Music", RandomConfigValue.Disabled);
            RandomizeLevels = new RandomConfig<RandomConfigValue>(NormalPanel, "Randomize Levels", RandomConfigValue.Disabled);

            new ButtonField(NormalPanel, "Go To Random Level", "button.normal.random.level").onClick += Plugin_onClick_LVL;
            new ButtonField(config.rootPanel, "Reset Mapping", "button.reset.mapping").onClick += Plugin_onClick_Mapping;
            new ButtonField(EnemyRNGList, "Enable All", "button.enemies.enable.all").onClick += Plugin_onClick_Enable_All_Enemies;
            new ButtonField(EnemyRNGList, "Disable All", "button.enemies.disable.all").onClick += Plugin_onClick_Disable_All_Enemies;

            PlayerRandomizers.GenerateConfigs();
            ScaleRandomizer.GenerateConfigs();
        }

        private static void Plugin_onClick_Enable_All_Enemies()
        {
            foreach (var e in EnemyRandomizer.CanUse.Values)
            {
                e.Value = true;
            }
        }

        private static void Plugin_onClick_Disable_All_Enemies()
        {
            foreach (var e in EnemyRandomizer.CanUse.Values)
            {
                e.Value = false;
            }
        }

        private static void Plugin_onClick_Mapping()
        {
            EnemyRandomizer.Instance.ResetMappings();
            ExplosionRandomizer.Instance.ResetMappings();
            GrenadeRandomizer.Instance.ResetMappings();
            HitscanRandomizer.Instance.ResetMappings();
            ImageRandomizer.Instance.ResetMappings();
            ItemRandomizer.Instance.ResetMappings();
            LevelRandomizer.Instance.ResetMappings();
            MaterialRandomizer.Instance.ResetMappings();
            MeshRandomizer.Instance.ResetMappings();
            MusicRandomizer.Instance.ResetMappings();
            NailRandomizer.Instance.ResetMappings();
            ProjectileRandomizer.Instance.ResetMappings();
            SoundRandomizer.Instance.ResetMappings();
            TextRandomizer._randomizer.ResetMappings();
        }

        private static void Plugin_onClick_LVL()
        {
            LevelRandomizer.LoadRandomScene();
        }

        public static string GetPrefabName(string objName)
        {
            if (string.IsNullOrEmpty(objName))
                return objName;

            // Remove " (Clone)" and " (number)" at the end
            return Regex.Replace(objName, @"\s*\((Clone|\d+)\)$", "");
        }

        public static bool IsGameplayScene()
        {
            string[] source = new string[]
            {
                "Intro",
                "Bootstrap",
                "Main Menu",
                "Level 2-S",
                "Intermission1",
                "Intermission2"
            };
            return !source.Contains(SceneHelper.CurrentScene);
        }
        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            StartCoroutine(EnemyRandomizer.Init());
            MusicRandomizer.FillPool();
            LevelRandomizer.Init();
            SoundRandomizer.Init();
            ProjectileRandomizer.Init();
            HitscanRandomizer.Init();
            GrenadeRandomizer.GetGrenades();
            NailRandomizer.GetNails();
            ExplosionRandomizer.GetExplosions();

            if (SceneHelper.CurrentScene == "Main Menu")
            {
                SoundRandomizer.Instance.Initialize();
                MaterialRandomizer.Instance.Initialize();
                ImageRandomizer.Instance.Initialize();
                HitscanRandomizer.Instance.Initialize();
                ProjectileRandomizer.Instance.Initialize();
                MeshRandomizer.Instance.Initialize();
                MusicRandomizer.Instance.Initialize();

            }

            if (!IsGameplayScene()) return;

            TextRandomizer.SearchTexts();
            if (ChangeMaterials.Value != RandomConfigValue.Disabled)
                StartCoroutine(MaterialRandomizer.ApplyChanges());
            StartCoroutine(ItemRandomizer.ApplyChanges());
            if (ChangeImages.Value != RandomConfigValue.Disabled)
                ImageRandomizer.ApplyChanges();
            if (ChangeSounds.Value != RandomConfigValue.Disabled)
                SoundRandomizer.Init();
            if (ChangeMeshes.Value != RandomConfigValue.Disabled)
                MeshRandomizer.Init();

            MusicRandomizer.Init();

            ScaleRandomizer.Init();
        }

    }



    [HarmonyPatch]
    public class InstantiatePatches
    {
        private static void ProcessInstantiatedObject(UnityEngine.Object obj)
        {
            if (obj == null) return;

            GameObject go = null;
            if (obj is GameObject g) go = g;
            else if (obj is Component c) go = c.gameObject;

            if (go == null) return;

            foreach (var meth in Plugin.OnInstantiateMethod)
            {
                meth.Invoke(go);
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.Instantiate), new System.Type[] { typeof(UnityEngine.Object) })]
        [HarmonyPostfix]
        private static void InstantiatePostfix1(UnityEngine.Object __result) => ProcessInstantiatedObject(__result);

        [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.Instantiate), new System.Type[] { typeof(UnityEngine.Object), typeof(Transform) })]
        [HarmonyPostfix]
        private static void InstantiatePostfix2(UnityEngine.Object __result) => ProcessInstantiatedObject(__result);

        [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.Instantiate), new System.Type[] { typeof(UnityEngine.Object), typeof(Transform), typeof(bool) })]
        [HarmonyPostfix]
        private static void InstantiatePostfix3(UnityEngine.Object __result) => ProcessInstantiatedObject(__result);

        [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.Instantiate), new System.Type[] { typeof(UnityEngine.Object), typeof(Vector3), typeof(Quaternion) })]
        [HarmonyPostfix]
        private static void InstantiatePostfix4(UnityEngine.Object __result) => ProcessInstantiatedObject(__result);

        [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.Instantiate), new System.Type[] { typeof(UnityEngine.Object), typeof(Vector3), typeof(Quaternion), typeof(Transform) })]
        [HarmonyPostfix]
        private static void InstantiatePostfix5(UnityEngine.Object __result) => ProcessInstantiatedObject(__result);
    }

    public class RandomConfig<T>
    {
        private ConfigField field;

        public RandomConfig(ConfigPanel panel, string name, T defaultVal)
        {
            string guid = "guid." + panel.guid + "." + name;
            if (typeof(T) == typeof(float))
                field = new FloatField(panel, name, guid, (float)(object)defaultVal);
            else if (typeof(T) == typeof(int))
                field = new IntField(panel, name, guid, (int)(object)defaultVal);
            else if (typeof(T) == typeof(bool))
                field = new BoolField(panel, name, guid, (bool)(object)defaultVal);
            else if (typeof(T) == typeof(string))
                field = new StringField(panel, name, guid, (string)(object)defaultVal);
            else if(typeof(T) == typeof(RandomConfigValue))
                field = new EnumField<RandomConfigValue>(panel, name, guid, (RandomConfigValue)(object)defaultVal);
            else
                throw new Exception($"Unsupported config type: {typeof(T)}");
        }

        public T Value
        {
            get => (T)field.GetType().GetProperty("value").GetValue(field);
            set => field.GetType().GetProperty("value").SetValue(field, value);
        }
    }
    public enum RandomConfigValue
    {
        Disabled,
        UniquePerKind,
        UniquePerKindWithDuplicates,
        AlwaysUnique
    }
}
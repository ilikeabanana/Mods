using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Functionals;
using PluginConfig.API.Fields;
using System.Collections.Generic;
using System;
using PluginConfig.API.Decorators;
using ULTRACHALLENGE.Utils;
using ULTRACHALLENGE.Patches;
using System.Linq;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine.Events;

namespace ULTRACHALLENGE
{

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class ULTRACHALLENGEPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.banana.ULTRACHALLENGE";
        private const string PluginName = "ULTRACHALLENGE";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        PluginConfigurator config;

        public static List<int> AlreadyUsedGUIDS = new List<int>();
        public static List<ChallengeSetting> ChallengeSettings = new List<ChallengeSetting>();

        public static List<string> explosions;
        public static List<string> explosionNames = new List<string>();
        public static Dictionary<string, GameObject> theExplosions = new Dictionary<string, GameObject>();

        public static List<(string fullPath, string shortPath)> addressables = new List<(string fullPath, string shortPath)>();
        public static List<string> addressableShorts = new List<string>();

        float timer;

        public float radiusForBlood = 2;

        public static string workingPath;
        public static string workingDir;
        private void Awake()
        {
            base.Logger.LogInfo("PluginName: ULTRACHALLENGE, VersionString: 1.0.0 is loading...");
            ULTRACHALLENGEPlugin.Harmony.PatchAll();
            base.Logger.LogInfo("PluginName: ULTRACHALLENGE, VersionString: 1.0.0 is loaded.");
            ULTRACHALLENGEPlugin.workingPath = Assembly.GetExecutingAssembly().Location;
            ULTRACHALLENGEPlugin.workingDir = Path.GetDirectoryName(ULTRACHALLENGEPlugin.workingPath);
            ULTRACHALLENGEPlugin.Log = base.Logger;
            this.SetupChallengeThings();
            ULTRACHALLENGEPlugin.explosions = ResourceLoader.GetExplosionPKeys();
            ULTRACHALLENGEPlugin.explosions = ULTRACHALLENGEPlugin.explosions.Distinct<string>().ToList<string>();
            ULTRACHALLENGEPlugin.theExplosions = ResourceLoader.getExplosions(ULTRACHALLENGEPlugin.explosions);
            ULTRACHALLENGEPlugin.addressables = ResourceLoader.GetAllPKeys();

            base.StartCoroutine(this.LoadExplosions());
            SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.SceneManager_sceneLoaded);
        }

        // Token: 0x06000010 RID: 16 RVA: 0x0000294F File Offset: 0x00000B4F
        private IEnumerator LoadExplosions()
        {
            yield return new WaitUntil(() => ResourceLoader.isDone);
            foreach (KeyValuePair<string, GameObject> item in ULTRACHALLENGEPlugin.theExplosions)
            {
                Debug.Log(item.Key);
                ULTRACHALLENGEPlugin.explosionNames.Add(item.Key);
            }
            Dictionary<string, GameObject>.Enumerator enumerator = default(Dictionary<string, GameObject>.Enumerator);
            Debug.Log(string.Format("Final explosionNames count: {0}", ULTRACHALLENGEPlugin.explosionNames.Count));
            foreach (var item in addressables)
            {
                addressableShorts.Add(item.shortPath);
            }

            yield break;
        }
        void Update()
        {
            foreach (var item in ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnUseInput && Input.GetKeyDown(item.keyField.value))
                {
                    Handlers.HandleThing(item, MonoSingleton<NewMovement>.Instance.transform);
                }
                if(item.situation.value == TypeOfThing.EveryFewSeconds && timer > 0)
                {
                    timer -= Time.deltaTime;
                }
                else if(item.situation.value == TypeOfThing.EveryFewSeconds)
                {
                    timer = item.numberField.value;
                    Handlers.HandleThing(item, MonoSingleton<NewMovement>.Instance.transform);
                }

                if (item.situation.value == TypeOfThing.NearBlood)
                {
                    bool blud = Handlers.IsBloodNearby(radiusForBlood);
                    if (blud)
                    {
                        // Only increment the timer if we haven't exceeded the delay
                        if (item.timer < item.delayField.value)
                        {
                            item.timer += Time.deltaTime;
                        }

                        // Check if we've reached the delay time
                        if (item.timer >= item.delayField.value)
                        {
                            Handlers.HandleThing(item, MonoSingleton<NewMovement>.Instance.transform);
                            item.timer = 0;  // Reset after handling
                        }
                    }
                }
            }
            // While stuff
            foreach (var item in ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnUseInput && Input.GetKey(item.keyField.value) && item.WhileSituation.value)
                {
                    // Check if the delay time has elapsed since the last action
                    if (item.timer >= item.delayField.value)
                    {
                        Handlers.HandleThing(item, MonoSingleton<NewMovement>.Instance.transform);
                        // Reset the timer after handling
                        item.timer = 0;
                    }
                    else
                    {
                        // Increment the timer while the key is held
                        item.timer += Time.deltaTime;
                    }
                }
                else if (!Input.GetKey(item.keyField.value))
                {
                    // Reset the timer when the key is released
                    item.timer = 0;
                }
            }
            // Linking Things
            foreach (var item in ChallengeSettings)
            {
                if (item.challengeType.value != challengeTypes.LinkProperties) continue;
                Handlers.setLinkables(item.Link1.value, item.Link2.value, item.Reversed.value, item.MultiplierField.value, item.OffsetField.value);
            }
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            foreach (var item in ChallengeSettings)
            {

                if (item.saveValue.value && item.ValueAlreadySet)
                {
                    switch (item.whatShouldItDo.value)
                    {
                        case whatShouldHappen.Speed:
                            MonoSingleton<NewMovement>.Instance.walkSpeed = item.savedValue;
                            break;
                        case whatShouldHappen.JumpPower:
                            MonoSingleton<NewMovement>.Instance.jumpPower = item.savedValue;
                            break;
                    }
                }

            }
            foreach (var item in ChallengeSettings)
            {
                if(item.situation.value == TypeOfThing.onSceneChange)
                {
                    Handlers.HandleThing(item, MonoSingleton<NewMovement>.Instance.transform);
                }
            }
        }

        public void SetupChallengeThings()
        {
            config = PluginConfigurator.Create("ULTRA CHALLENGE", MyGUID);
            string text = Path.Combine(workingDir, "icon.png");
            bool flag2 = File.Exists(text);
            if (flag2)
            {
                config.SetIconWithURL(text);
            }
            ButtonField addNewChallenge = new ButtonField(config.rootPanel, "Add new setting.", "button.add.new.setting");
            addNewChallenge.onClick += AddNewChallenge_onClick;
        }

        private void AddNewChallenge_onClick()
        {
            ChallengeSettings.Add(new ChallengeSetting(ChallengeSettings.Count + 1, config.rootPanel));
        }
    }

    public enum TypeOfThing
    {
        OnTouchColor,
        OnTouchGameObject,
        OnDash,
        OnJump,
        OnSlide,
        OnUseInput,
        onSceneChange,
        EveryFewSeconds,
        OnTakeDamage,
        OnDeath,
        OnWeaponSwitch,
        OnHeal,
        OnParry,
        OnEnemyKill,
        Punch,
        Shoot,
        OnPieceMove,
        OnPieceCapture,
        NearBlood,
        OnEnemySpawn,
    }
    public enum whatShouldHappen
    {
        Damage,
        Speed,
        JumpPower,
        SpawnExplosion,
        KillEnemy,
        DupeEnemy,
        BuffEnemy,
        Pixelization,
        FOV,
        VertexWarping,
        Gamma,
        ChangeToRandomLevel,
        ChangeLevel,
        RestartLevel,
        Framerate,
        RemoveGameObject,
        Gravity,
        SpawnAddressable,
        Quit,
        RemoveTriggerer

    }
    public enum math
    {
        Increase,
        Decrease,
        Multiply,
        Divide,
        Complex,
        Set
    }
    public enum distance
    {
        Closest,
        Furthest,
        Inbetween,
        Random
    }
    public enum spawnLocation
    {
        triggerlocation,
        player
    }

    public enum param
    {
        AString,
        Number,
        Color,
        KeyCode,
    }

    public enum challengeTypes
    {
        OnAction,
        LinkProperties
    }

    public enum Linkable
    {
        Health,
        Speed,
        JumpHeight,
        Pixelization,
        FOV,
        VertexWarping,
        Gamma,
        Velocity,
        Sensitivity,
        FrameRate,
        Gravity,
        Time,
        Stamina,

    }

    [Serializable]
    public class ChallengeSetting
    {
        string GUID;

        int ID;

        public FloatField Tolerance;
        public FloatField MultiplierField;
        public FloatField OffsetField;
        public BoolField WhileSituation;
        public BoolField Reversed;
        public IntField amount;
        public StringField amountComplexMath;
        public StringField stringParam;
        public StringListField addressablePath;
        public FloatField numberField;
        public FloatField delayField;
        public FloatField ChanceField;
        public ColorField color;
        public EnumField<TypeOfThing> situation;
        public EnumField<challengeTypes> challengeType;
        public EnumField<Linkable> Link1;
        public EnumField<Linkable> Link2;
        public EnumField<whatShouldHappen> whatShouldItDo;
        public EnumField<spawnLocation> spawnLocation;
        public EnumField<math> weDoALittleMath;
        public EnumField<distance> distanceField;
        public StringListField explosionList;
        public KeyCodeField keyField;

        public float timer;

        

        public BoolField saveValue;

        public float savedValue;
        public bool ValueAlreadySet = false;

        ConfigHeader header;
        public GameObject selectedExplosion = null;
        ButtonField removesetting;

        public List<situationEnum> situationSettings = new List<situationEnum>()
        {
            new situationEnum(TypeOfThing.OnTouchColor, new List<param>()
            {
                param.Color
            }, true),
            new situationEnum(TypeOfThing.OnDash, new List<param>()
            {

            }),
            new situationEnum(TypeOfThing.onSceneChange, new List<param>()
            {

            }),
            new situationEnum(TypeOfThing.OnSlide, new List<param>()
            {

            }),
            new situationEnum(TypeOfThing.OnJump, new List<param>()
            {

            }),
            new situationEnum(TypeOfThing.OnUseInput, new List<param>()
            {
                param.KeyCode
            }, true),
            new situationEnum(TypeOfThing.OnTouchGameObject, new List<param>()
            {
                param.AString
            }, true),
            new situationEnum(TypeOfThing.EveryFewSeconds, new List<param>()
            {
                param.Number
            }),
            new situationEnum(TypeOfThing.OnTakeDamage, new List<param>()
            {

            }),
            new situationEnum(TypeOfThing.OnDeath, new List<param>()
            {

            }),
            new situationEnum(TypeOfThing.OnParry, new List<param>()
            {

            }),
            new situationEnum(TypeOfThing.OnWeaponSwitch, new List<param>()
            {

            }),
            new situationEnum(TypeOfThing.Punch, new List<param>()
            {

            }),
            new situationEnum(TypeOfThing.Shoot, new List<param>()
            {

            }),
            new situationEnum(TypeOfThing.NearBlood, new List<param>()
            {

            }, false, true),

        };
        public List<whatShouldHappenEnum> whatShouldHappenSettings = new List<whatShouldHappenEnum>()
        {
            new whatShouldHappenEnum(whatShouldHappen.Damage, false, false),
            new whatShouldHappenEnum(whatShouldHappen.Speed, true, true),
            new whatShouldHappenEnum(whatShouldHappen.JumpPower, true, true),
            new whatShouldHappenEnum(whatShouldHappen.SpawnExplosion, false, false),
            new whatShouldHappenEnum(whatShouldHappen.KillEnemy, false, false),
            new whatShouldHappenEnum(whatShouldHappen.DupeEnemy, false, false),
            new whatShouldHappenEnum(whatShouldHappen.BuffEnemy, false, false),
            new whatShouldHappenEnum(whatShouldHappen.FOV, true, false),
            new whatShouldHappenEnum(whatShouldHappen.Gamma, true, false),
            new whatShouldHappenEnum(whatShouldHappen.Pixelization, true, false),
            new whatShouldHappenEnum(whatShouldHappen.VertexWarping, true, false),
            new whatShouldHappenEnum(whatShouldHappen.ChangeLevel, false, false),
            new whatShouldHappenEnum(whatShouldHappen.ChangeToRandomLevel, false, false),
            new whatShouldHappenEnum(whatShouldHappen.RestartLevel, false, false),
            new whatShouldHappenEnum(whatShouldHappen.Framerate, true, false),
            new whatShouldHappenEnum(whatShouldHappen.RemoveGameObject, false, false),
            new whatShouldHappenEnum(whatShouldHappen.Gravity, true, false),
            new whatShouldHappenEnum(whatShouldHappen.SpawnAddressable, false, false),
            new whatShouldHappenEnum(whatShouldHappen.Quit, false, false),
            new whatShouldHappenEnum(whatShouldHappen.RemoveTriggerer, false, false),
        };

        public ChallengeSetting(int ID, ConfigPanel panel)
        {
            GenerateGUID();
            this.ID = ID;
            GenerateSettings(panel);
        }

        private void GenerateGUID()
        {
            int number = 0;

            do
            {
                number = UnityEngine.Random.Range(0, int.MaxValue);
            }
            while (ULTRACHALLENGEPlugin.AlreadyUsedGUIDS.Contains(number));

            ULTRACHALLENGEPlugin.AlreadyUsedGUIDS.Add(number);
            GUID = number.ToString();
        }

        private void GenerateSettings(ConfigPanel panel)
        {
            header = new ConfigHeader(panel, "Challenge setting " + ID);
            challengeType = new EnumField<challengeTypes>(panel, "Challenge Type", "challengetype.enum." + GUID, challengeTypes.OnAction, false);
            situation = new EnumField<TypeOfThing>(panel, "Situation", "situation.enum." + GUID, TypeOfThing.OnTouchColor, false);
            WhileSituation = new BoolField(panel, "While the situation", "color.tolerance." + GUID, false, false);
            Reversed = new BoolField(panel, "Reversed", "reversed.bool." + GUID, false, false);
            delayField = new FloatField(panel, "Delay", "delay.float." + GUID, 0, false);
            MultiplierField = new FloatField(panel, "Multiplier", "multiplier.float." + GUID, 1, false);
            ChanceField = new FloatField(panel, "Chance", "chance.float." + GUID, 100, false);
            OffsetField = new FloatField(panel, "Offset", "offset.float." + GUID, 0, false);

            // Color
            Tolerance = new FloatField(panel, "Tolerance", "color.tolerance." + GUID, 0.5f, false);
            color = new ColorField(panel, "Color", "color.color." + GUID, Color.green, false);

            // String
            stringParam = new StringField(panel, "String", "string.string." + GUID, "%name%");
            

            // String
            numberField = new FloatField(panel, "Number", "number.number." + GUID, 5);

            // Keycode
            keyField = new KeyCodeField(panel, "Key", "key.keycode." + GUID, KeyCode.W, false);

            // What it should do
            whatShouldItDo = new EnumField<whatShouldHappen>(panel, "What Should Happen", "whatshouldhappen.enum." + GUID, whatShouldHappen.Damage, false);
            distanceField = new EnumField<distance>(panel, "Distance", "distance.enum." + GUID, distance.Closest, false);
            saveValue = new BoolField(panel, "Save Value Through Levels", "save.value." + GUID, false, false);

            explosionList = new StringListField(panel, "Explosions", "gameobjects.explosions." + GUID, ULTRACHALLENGEPlugin.explosionNames, ULTRACHALLENGEPlugin.explosionNames[0], false);

            selectedExplosion = ULTRACHALLENGEPlugin.theExplosions[explosionList.value];

            addressablePath = new StringListField(panel, "Addressable", "addressable.string." + GUID, ULTRACHALLENGEPlugin.addressableShorts, ULTRACHALLENGEPlugin.addressableShorts[0]);
            spawnLocation = new EnumField<spawnLocation>(panel, "Spawn Location", "spawnlocation.enum." + GUID, ULTRACHALLENGE.spawnLocation.player, false);
            // Math
            weDoALittleMath = new EnumField<math>(panel, "How should it happen", "math.enum." + GUID, math.Set, false);
            amount = new IntField(panel, "Amount", "whatshouldhappen.amount." + GUID, 10, false);
            amountComplexMath = new StringField(panel, "Math Amount", "whatshouldhappen.complexmathamount." + GUID, "x+10", false);

            Link1 = new EnumField<Linkable>(panel, "Source", "link.1.enum." + GUID, Linkable.Health);
            Link2 = new EnumField<Linkable>(panel, "Target", "link.2.enum." + GUID, Linkable.Speed);

            removesetting = new ButtonField(panel, "Remove Setting", "button.remove.setting." + GUID);


            SwitchEnum(TypeOfThing.OnTouchColor);
            SwitchHappen(whatShouldHappen.Damage);

            whatShouldItDo.onValueChange += WhatShouldItDo_onValueChange;
            situation.onValueChange += Situation_onValueChange;
            weDoALittleMath.onValueChange += WeDoALittleMath_onValueChange;
            removesetting.onClick += ChallengeSetting_onClick;
            explosionList.onValueChange += ExplosionList_onValueChange;
            challengeType.onValueChange += ChallengeType_onValueChange;

            challengeType.TriggerValueChangeEvent();
        }

        private void ChallengeType_onValueChange(EnumField<challengeTypes>.EnumValueChangeEvent data)
        {
            situation.hidden = true;
            Tolerance.hidden = true;
            color.hidden = true;
            whatShouldItDo.hidden = true;
            saveValue.hidden = true;
            weDoALittleMath.hidden = true;
            amount.hidden = true;
            amountComplexMath.hidden = true;
            keyField.hidden = true;
            explosionList.hidden = true;
            distanceField.hidden = true;
            WhileSituation.hidden = true;
            stringParam.hidden = true;
            numberField.hidden = true;
            Link1.hidden = true;
            Link2.hidden = true;
            Reversed.hidden = true;
            MultiplierField.hidden = true;
            OffsetField.hidden = true;
            addressablePath.hidden = true;
            ChanceField.hidden = true;
            delayField.hidden = true;
            spawnLocation.hidden = true;

            if (data.value == challengeTypes.OnAction)
            {
                SwitchEnum(situation.value);
                SwitchHappen(whatShouldItDo.value);
                whatShouldItDo.hidden = false;
                situation.hidden = false;
                ChanceField.hidden = false;
            }
            else
            {
                Link1.hidden = false;
                Link2.hidden = false;
                Reversed.hidden = false;
                MultiplierField.hidden = false;
                OffsetField.hidden = false;
            }
        }

        private void ExplosionList_onValueChange(StringListField.StringListValueChangeEvent data)
        {
            selectedExplosion = ULTRACHALLENGEPlugin.theExplosions[data.value];
        }

        private void ChallengeSetting_onClick()
        {
            header.hidden = true;
            challengeType.hidden = true;
            situation.hidden = true;
            Tolerance.hidden = true;
            color.hidden = true;
            whatShouldItDo.hidden = true;
            saveValue.hidden = true;
            weDoALittleMath.hidden = true;
            amount.hidden = true;
            amountComplexMath.hidden = true;
            removesetting.hidden = true;
            keyField.hidden = true;
            explosionList.hidden = true;
            distanceField.hidden = true;
            WhileSituation.hidden = true;
            stringParam.hidden = true;
            Link1.hidden = true;
            Link2.hidden = true;
            Reversed.hidden = true;
            MultiplierField.hidden = true;
            OffsetField.hidden = true;
            addressablePath.hidden = true;
            ChanceField.hidden = true;
            delayField.hidden = true;
            spawnLocation.hidden = true;

            ULTRACHALLENGEPlugin.ChallengeSettings.Remove(this);
            for (int i = 0; i < ULTRACHALLENGEPlugin.ChallengeSettings.Count; i++)
            {
                ULTRACHALLENGEPlugin.ChallengeSettings[i].ResetID(i + 1);
            }
        }

        private void WeDoALittleMath_onValueChange(EnumField<math>.EnumValueChangeEvent data)
        {
            if(data.value == math.Complex)
            {
                amountComplexMath.hidden = false;
            }
            else
            {
                amountComplexMath.hidden = true;
            }
        }

        private void Situation_onValueChange(EnumField<TypeOfThing>.EnumValueChangeEvent data)
        {
            SwitchEnum(data.value);
        }

        private void WhatShouldItDo_onValueChange(EnumField<whatShouldHappen>.EnumValueChangeEvent data)
        {
            SwitchHappen(data.value);
        }

        public void SwitchEnum(TypeOfThing switchedSituation)
        {
            Tolerance.hidden = true;
            color.hidden = true;
            keyField.hidden = true;
            stringParam.hidden = true;
            numberField.hidden = true;
            WhileSituation.hidden = true;
            delayField.hidden = true;
            situationEnum situation = situationSettings.Find((x) => x.situation == switchedSituation);
            if (situation == null) return;
            foreach (param parm in situation.requiredParams)
            {
                switch (parm)
                {
                    case param.Color:
                        Tolerance.hidden = false;
                        color.hidden = false;
                        break;
                    case param.KeyCode:
                        keyField.hidden = false;
                        break;
                    case param.AString:
                        stringParam.hidden = false;
                        break;
                    case param.Number:
                        numberField.hidden = false;
                        break;
                }
            }
            if (situation.canDoWhile)
            {
                WhileSituation.hidden = false;
                delayField.hidden = false;
            }
            if (situation.justDelayNoWhile)
            {
                delayField.hidden = false;
            }
        }
        public void ResetID(int id)
        {
            ID = id;
            header.text = "Challenge setting " + ID;
        }
        public void SwitchHappen(whatShouldHappen switchedSituation)
        {
            whatShouldHappenEnum whatShouldHappenEnum = whatShouldHappenSettings.Find((x) => x.shouldHappen == switchedSituation);
            bool can = whatShouldHappenEnum.canDoMath;
            weDoALittleMath.hidden = !can;
            amountComplexMath.hidden = true;
            explosionList.hidden = true;
            if (weDoALittleMath.value == math.Complex && can)
            {
                amountComplexMath.hidden = false;
            }

            switch (switchedSituation)
            {
                case whatShouldHappen.Damage:
                case whatShouldHappen.Speed:
                case whatShouldHappen.JumpPower:
                case whatShouldHappen.RemoveGameObject:
                case whatShouldHappen.KillEnemy:
                case whatShouldHappen.DupeEnemy:
                case whatShouldHappen.BuffEnemy:
                case whatShouldHappen.SpawnAddressable:
                    amount.hidden = false;
                    break;
                default:
                    amount.hidden = !can;
                    break;
            }

            saveValue.hidden = !whatShouldHappenEnum.canSaveThroughLevels;

            if(switchedSituation == whatShouldHappen.SpawnExplosion)
            {
                explosionList.hidden = false;
            }
            distanceField.hidden = true;
            switch (switchedSituation)
            {
                case whatShouldHappen.RemoveGameObject:
                case whatShouldHappen.KillEnemy:
                case whatShouldHappen.DupeEnemy:
                case whatShouldHappen.BuffEnemy:
                    distanceField.hidden = false;
                    break;
                
            }

            addressablePath.hidden = true;
            if(switchedSituation == whatShouldHappen.SpawnAddressable)
            {
                addressablePath.hidden = false;
            }
            spawnLocation.hidden = true;
            if (switchedSituation == whatShouldHappen.SpawnExplosion || switchedSituation == whatShouldHappen.SpawnAddressable)
            {
                spawnLocation.hidden = false;
            }
        }

        [Serializable]
        public class situationEnum
        {
            public situationEnum(TypeOfThing sit, List<param> theParams, bool canDoWhile = false, bool justDelayNoWhile = false)
            {
                situation = sit;
                requiredParams = theParams;
                this.canDoWhile = canDoWhile;
                this.justDelayNoWhile = justDelayNoWhile;
            }
            public TypeOfThing situation;
            public bool canDoWhile;
            public bool justDelayNoWhile;
            public List<param> requiredParams;
        }
        [Serializable]
        public class whatShouldHappenEnum
        {
            public whatShouldHappenEnum(whatShouldHappen sit, bool canMath, bool canDoSave)
            {
                shouldHappen = sit;
                canDoMath = canMath;
                canSaveThroughLevels = canDoSave;
            }
            public whatShouldHappen shouldHappen;
            public bool canSaveThroughLevels;
            
            public bool canDoMath;
        }
    }
}

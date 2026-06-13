using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using GameConsole;
using GameConsole.CommandTree;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using ULTRAKILL.Enemy;
using Ultrarogue.Characters;
using Ultrarogue.Curses;
using Ultrarogue.Items;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Ultrarogue.Plugin;
using Random = UnityEngine.Random;

// gffg

namespace Ultrarogue
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        internal static new ManualLogSource Logger { get; private set; } = null!;

        public static List<DeathEffect> deathEffects = new List<DeathEffect>();
        public static List<HitEffect> hitEffects = new List<HitEffect>();
        public static List<DamageTakenEffect> onDamageEffects = new List<DamageTakenEffect>();
        public static List<DamageModifier> dmgModifiers = new List<DamageModifier>();

        public static List<BaseItem> possibleItems = new List<BaseItem>();

        public static Dictionary<BaseItem, int> items = new Dictionary<BaseItem, int>();
        public static Dictionary<string, BaseItem> nameToItem = new Dictionary<string, BaseItem>();

        public static List<PlayerChange> playerChanges = new List<PlayerChange>();

        public static List<AWeapon> weapons = new List<AWeapon>();

        public static List<BaseCharacter> characters = new List<BaseCharacter>();

        public static string GameSeed = "Banana";

#if RUNTIME_ROOMS
        DebugRoomGenerator debugGen;
#endif

        public enum Weapon
        {
            Revolver,
            Shotgun,
            Nailgun,
            Railcannon,
            RocketLauncher,
            Arm
        }

        public enum Variant
        {
            Blue,
            Green,
            Red
        }

        public static string getWeaponString(Weapon weapon, Variant variant)
        {
            return "weapon." + getWeaponString(weapon) + (int)variant;
        }
        public static string getWeaponString(Weapon weapon)
        {
            switch (weapon)
            {
                case Weapon.Revolver:
                    return "rev";
                case Weapon.Shotgun:
                    return "sho";
                case Weapon.Nailgun:
                    return "nai";
                case Weapon.Railcannon:
                    return "rai";
                case Weapon.RocketLauncher:
                    return "rock";
                case Weapon.Arm:
                    return "arm";
            }

            return "i dont fucking know????";
        }

        public static bool RogueMode = false;

        public static bool isInRogueMode()
        {
            return RogueMode;
        }

        public static bool isInRogueScene()
        {
            return SceneHelper.CurrentScene == SceneLoader.SceneName;
        }
        public static BaseItem getItem(string name)
        {
            if (!nameToItem.ContainsKey(name)) return null;
            return nameToItem[name];
        }

        public static int GetItemCount(string name)
        {
            BaseItem item = getItem(name);
            if (item == null) return 0;
            if (items.ContainsKey(item)) return items[item];

            return 0;
        }
        public static int GetItemCount(BaseItem item)
        {
            if (item == null) return 0;
            if (items.ContainsKey(item)) return items[item];

            return 0;
        }

        public float normalMoveSpeed = 0f;
        public float normalairAccelaration = 0f;
        float normalJumpHeight = 0f;
        public static int MaxHealth = 100;
        public static Change AttackSpeed;
        public static Change DamageReduction;
        public static Change cooldownReduction = new Change();
        public static Plugin Instance { get; private set; }

        private void Awake()
        {
            CurrentDifficulty = 1;
            // Plugin startup logic
            Instance = this;
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            gameObject.hideFlags = HideFlags.DontSaveInEditor;
            Harmony.PatchAll();
            GatherItems();
            CurseManager.LoadCurses();
            //LoadBundle();
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            weapons.Clear();
            weapons.Add(new AWeapon(Weapon.Revolver, Variant.Blue));
            weapons.Add(new AWeapon(Weapon.Arm, Variant.Blue));
            AssetsManager.Init();
            BundleLoader.Load();

            characters.Add(new V1());
            characters.Add(new Ultrarogue.Characters.V2());
            characters.Add(new Ultrarogue.Characters.Streetcleaner());
            characters.Add(new Ultrarogue.Characters.GreedMachine());
            characters.Add(new Ultrarogue.Characters.RandomCharacter());
            characters.Add(new Ultrarogue.Characters.Filth());

#if RUNTIME_ROOMS
            var genObj = new GameObject("DebugRoomGenerator");
            DontDestroyOnLoad(genObj);
            debugGen = genObj.AddComponent<DebugRoomGenerator>();
            genObj.hideFlags = HideFlags.DontSaveInEditor;
            debugGen.minRooms = 5;
            debugGen.maxRooms = 10;
            debugGen.baseSpawnCredits = 40;
#endif
        }
        public static BaseCharacter SelectedChar = null;
        int currentIndex;
        void Next(TMP_Text info, TMP_Text name)
        {
            if (characters.Count == 0) return;
            currentIndex++;
            if (currentIndex >= characters.Count)
                currentIndex = 0;

            UpdateState(info, name);
        }

        void Previous(TMP_Text info, TMP_Text name)
        {
            if (characters.Count == 0) return;

            currentIndex--;
            if (currentIndex < 0)
                currentIndex = characters.Count - 1;

            UpdateState(info, name);
        }

        void UpdateState(TMP_Text info, TMP_Text name)
        {
            SelectedChar = characters[currentIndex];
            info.text = SelectedChar.Name;
            info.GetComponent<ScrollingText>().message = SelectedChar.Description;
            info.GetComponent<ScrollingText>().activated = true;
            base.StartCoroutine(info.GetComponent<ScrollingText>().PrepText());
            name.text = SelectedChar.Name;
        }

        RogueSaveData GetBest()
        {
            string pref = PlayerPrefs.GetString(RogueFinalRank.PREF_KEY, "");
            RogueSaveData? save = JsonConvert.DeserializeObject<RogueSaveData>(pref);
            RogueSaveData toReturn = new RogueSaveData();
            if (save == null)
            {
                toReturn.datas = new System.Collections.Generic.List<RogueSaveDataData>();
                toReturn.BestRun = new RogueSaveDataData(); // all fields default to 0
            }
            else
            {
                toReturn = save;
            }
            return toReturn;
        }

        public static int CurrentDifficulty;
        public static string GenerateRandomString(int length)
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var chars = Enumerable.Range(0, length)
                .Select(_ => pool[Random.Range(0, pool.Length)])
                .ToArray();
            return new string(chars).ToUpper();
        }

        public static void LoadLevel(string seed)
        {
            if (string.IsNullOrEmpty(seed))
                GameSeed = GenerateRandomString(6);
            else
                GameSeed = seed;
            SelectedChar.OnRunStart();
            foreach (var tiem in items)
            {
                tiem.Key.OnGotten(0, false);
                tiem.Key.OnUpdate(0);
                tiem.Key.OnRemoval();
            }
            items.Clear();
            weapons.Clear();

            if (SelectedChar.StartingWeapons == null || SelectedChar.StartingWeapons.Count == 0)
            {
                Logger.LogWarning($"[Play] {SelectedChar.Name} has no StartingWeapons.");

            }
            else
            {
                weapons.AddRange(SelectedChar.StartingWeapons);
            }
            Logger.LogInfo($"Item count: " + SelectedChar.StartingItems.Count);
            if (SelectedChar.StartingItems.Count != 0)
            {
                foreach (var item in SelectedChar.StartingItems)
                {
                    Logger.LogInfo($"Giving item: " + item);
                    Plugin.GiveItem(item);
                }
            }

            Plugin.Instance.StartCoroutine(SceneLoader.LoadLevelAsync(false));
        }
        static List<string> inComMods = new List<string>()
        {
            "billy.spawnerarmextras"
        };
        public static bool userHasIncomaptibleMods()
        {
            foreach (var key in Chainloader.PluginInfos.Keys)
                Logger.LogInfo($"Loaded plugin GUID: {key}");
            foreach (string inmod in inComMods)
            {
                Logger.LogInfo($"Checking mod: {inmod}");
                if (Chainloader.PluginInfos.ContainsKey(inmod)) return true;
            }
            return false;
        }
        public static bool IsOtherModLoaded()
        {
            return Chainloader.PluginInfos.ContainsKey("duviz.ultrakill.ultraeditor");
        }
        IEnumerator SpawnThings()
        {
            yield return new WaitForSeconds(2f); // idk why 24 but lmao
            Logger.LogInfo($"I have reset difficulty to {CurrentDifficulty}");
            yield return null;
            AsyncOperationHandle<GameObject> RogueButtonPref = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/RogueMode.prefab");
            yield return new WaitUntil(() => RogueButtonPref.IsDone);
            GameObject parent = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault((x) => x.name.Contains(!IsOtherModLoaded() ? "Chapters" : "TopRightChapterSelect"));

            if (parent == null)
            {
                Logger.LogError("[SpawnThings] Could not find a GameObject whose name contains 'Chapters'. Aborting.");
                yield break;
            }

            GameObject but = Instantiate(RogueButtonPref.Result, parent.transform);
            but.transform.Find("RankPanel/RankText").GetComponent<TMP_Text>().text = GetBest().BestRun.Floor.ToString();
            AsyncOperationHandle<GameObject> RogueMenu = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/RogueMenu.prefab");
            yield return new WaitUntil(() => RogueMenu.IsDone);
            GameObject parentMen = GameObject.FindObjectOfType<OptionsMenuToManager>().gameObject;

            GameObject men = Instantiate(RogueMenu.Result, parentMen.transform);
            men.SetActive(false);

            TMP_InputField seedField = men.transform.Find("SeedField").GetComponent<TMP_InputField>();

            men.transform.Find("Play").GetComponent<Button>().onClick.AddListener(() =>
            {
                if (SelectedChar == null)
                {
                    Logger.LogError("[Play] SelectedChar is null!");
                    return;
                }
                LoadLevel(seedField.text);
            });

            TMP_Text info = men.transform.Find("Info/InfoText").GetComponent<TMP_Text>();
            TMP_Text cName = men.transform.Find("Info/CharName").GetComponent<TMP_Text>();
            Button lBut = men.transform.Find("Info/LeftButt").GetComponent<Button>();
            Button rBut = men.transform.Find("Info/LeftButt (1)").GetComponent<Button>();

            Button butt = but.GetComponent<Button>();


            SelectedChar = characters[0];
            currentIndex = 0;
            info.text = SelectedChar.Description;
            info.GetComponent<ScrollingText>().message = SelectedChar.Description;

            cName.text = SelectedChar.Name;

            lBut.onClick.AddListener(() =>
            {
                Previous(info, cName);
            });
            rBut.onClick.AddListener(() =>
            {
                Next(info, cName);
            });

            butt.onClick.AddListener(() =>
            {
                men.SetActive(true);
                GameObject.Find("Chapter Select").SetActive(false);
            });

            men.transform.Find("Back").GetComponent<Button>().onClick.AddListener(() =>
            {
                men.transform.Find("Back").parent.parent.Find("Chapter Select").gameObject.SetActive(true);
                men.SetActive(false);
            });

            TMP_Dropdown drop = men.transform.Find("Info/Dropdown").gameObject.GetComponent<TMP_Dropdown>();

            drop.onValueChanged.AddListener((i) =>
            {
                CurrentDifficulty = i + 1;
                Logger.LogInfo($"Difficulty is {CurrentDifficulty}");
            });

            CurrentDifficulty = 1;
            if (userHasIncomaptibleMods())
            {
                Logger.LogInfo("AAAAAAAAA INCOMPATIBLE MOD DETECTEDDD");
                men.transform.Find("IncomModsMessage").gameObject.SetActive(true);
            }
        }


        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {

            if (FindObjectOfType<RogueDifficultyManager>())
                RogueMode = true;
            else
                RogueMode = false;
            if (SceneHelper.CurrentScene == "Main Menu")
            {
                if (!AssetsManager.IsReady)
                    AssetsManager.Init();
                StartCoroutine(SpawnThings());
            }
            else
            {
                foreach (var tiem in items)
                {
                    tiem.Key.OnGotten(0, false);
                }
                items.Clear();
            }

            if (NewMovement.Instance == null) return;
            normalMoveSpeed = NewMovement.Instance.walkSpeed;
            normalairAccelaration = NewMovement.Instance.airAcceleration;
            normalJumpHeight = NewMovement.Instance.jumpPower;



        }

        void Update()
        {
            if (!isInRogueScene()) return;
            /*
            if (Input.GetKeyDown(KeyCode.X))
            {
                Transform cam = CameraController.Instance.transform;

                Vector3 initPos = NewMovement.Instance.transform.position;

                // Base position 3 units in front of the camera
                Vector3 forwardOffset = cam.forward * 3f;

                // Right offset spacing
                Vector3 rightOffset = cam.right * 3f;

                // List of items to spawn
                List<BaseItem> items = new List<BaseItem>
                {
                    Plugin.getItem("Eye of God"),
                    Plugin.getItem("Small Kit"),
                    Plugin.getItem("Fusion"),
                    Plugin.getItem("Improvement"),
                    Plugin.getItem("Dual Gun"),
                    Plugin.getItem("Jumper Cable"),
                    Plugin.getItem("Ration Card")
                };

                for (int i = 0; i < items.Count; i++)
                {
                    GameObject obj = new GameObject($"Pickup{i + 1}");

                    // Center the lineup around the middle item
                    float offsetIndex = i - (items.Count - 1) / 2f;

                    obj.transform.position =
                        initPos +
                        forwardOffset +
                        (rightOffset * offsetIndex);

                    ItemPickup.CreatePickup(items[i], obj.transform);
                }
            }*/
#if RUNTIME_ROOMS
            if (Input.GetKeyDown(KeyCode.F5))
            {
                // Make sure RogueDifficultyManager exists
                if (RogueDifficultyManager.Instance == null)
                {
                    var mgrObj = new GameObject("RogueDifficultyManager");
                    DontDestroyOnLoad(mgrObj);
                    mgrObj.AddComponent<RogueDifficultyManager>();
                    Logger.LogInfo("[DEBUG] Created RogueDifficultyManager.");
                }

                Logger.LogInfo("[DEBUG] Generating room layout...");
                debugGen.SpawnLayout();
                RogueDifficultyManager.Instance.MoveStage();
                HudMessageReceiver.Instance.SendHudMessage($"Difficulty: {RogueDifficultyManager.Instance.Difficulty}");
                Logger.LogInfo("[DEBUG] Layout ready! Enemies spawned in all non-start rooms.");
                
            }

            // ── F6 → Destroy layout ─────────────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.F6))
            {
                debugGen.ClearLayout();
                Logger.LogInfo("[DEBUG] Layout cleared.");
            }
#endif
            foreach (var ch in characters)
            {
                ch.Update(SelectedChar == ch);
            }

            foreach (var item in items)
            { // ok
                item.Key.OnUpdate(item.Value);
            }

            CurseManager.Update();

            ApplyPlayerChanges();
            ApplyWeaponSpeeds();
            HandleBonk();
        }
        List<EnemyIdentifier> hits = new List<EnemyIdentifier>();
        void HandleBonk()
        {
            if (SelectedChar == null) return;
            if (SelectedChar.HasPassive(Passive.HeadBonk))
            {
                NewMovement.Instance.dodgeSound = AssetsManager.FilthAttack;
                if (NewMovement.Instance != null)
                {
                    if (NewMovement.Instance.boost)
                    {
                        if (!NewMovement.Instance.sliding)
                        {
                            Collider[] cols = Physics.OverlapSphere(NewMovement.Instance.transform.position, 5, LayerMaskDefaults.Get(LMD.Enemies));
                            foreach (var col in cols)
                            {
                                EnemyIdentifierIdentifier eidd = col.gameObject.GetComponent<EnemyIdentifierIdentifier>();

                                EnemyIdentifier eid;
                                if (eidd == null)
                                    eid = col.gameObject.GetComponent<EnemyIdentifier>();
                                else
                                    eid = eidd.eid;

                                if (eid == null) continue;
                                if (hits.Contains(eid)) continue;
                                eid.hitter = "filthbonk";

                                float damageMult = NewMovement.Instance.walkSpeed / normalMoveSpeed;

                                eid.SimpleDamage(3 * damageMult);
                                hits.Add(eid);
                            }
                        }
                        if (NewMovement.Instance.sliding)
                        {
                            Collider[] cols = Physics.OverlapSphere(NewMovement.Instance.transform.position, 5, LayerMaskDefaults.Get(LMD.Enemies));
                            foreach (var col in cols)
                            {
                                EnemyIdentifierIdentifier eidd = col.gameObject.GetComponent<EnemyIdentifierIdentifier>();

                                EnemyIdentifier eid;
                                if (eidd == null)
                                    eid = col.gameObject.GetComponent<EnemyIdentifier>();
                                else
                                    eid = eidd.eid;

                                if (eid == null) continue;
                                if (hits.Contains(eid)) continue;
                                eid.hitter = "filthbonk";

                                float damageMult = NewMovement.Instance.walkSpeed / normalMoveSpeed;

                                eid.SimpleDamage(1 * damageMult);
                                hits.Add(eid);
                            }
                        }
                    }
                    else
                    {
                        hits.Clear();
                    }
                }
            }
        }

        void ApplyWeaponSpeeds()
        {
            if (NewMovement.Instance == null) return;
            foreach (var anim in NewMovement.Instance.GetComponentsInChildren<Animator>())
            {
                anim.speed = AttackSpeed.CalculateChanges(1);
            }
        }

        void ApplyPlayerChanges()
        {
            if (NewMovement.Instance == null) return;
            Change moveChange = new Change();
            Change jumpChange = new Change();
            Change hpChange = new Change();
            Change atkSpeedChange = new Change();
            Change globalDamageChange = new Change();
            Change cooldownChange = new Change();
            Change dRedChange = new Change();
            Dictionary<Weapon, DamageChange> damageChanges = new Dictionary<Weapon, DamageChange>();

            foreach (var changes in playerChanges)
            {
                moveChange.ApplyChangeToChange(changes.moveSpeed);

                jumpChange.ApplyChangeToChange(changes.jumpHeight);

                hpChange.ApplyChangeToChange(changes.maxHealth);

                atkSpeedChange.ApplyChangeToChange(changes.attackSpeed);

                cooldownChange.ApplyChangeToChange(changes.cooldownRed);

                dRedChange.ApplyChangeToChange(changes.damageReduction);

                globalDamageChange.ApplyChangeToChange(changes.globalDamageMult);

                foreach (var damageChange in changes.damageChanges)
                {
                    if (!damageChanges.ContainsKey(damageChange.WeaponType))
                        damageChanges.Add(damageChange.WeaponType, new DamageChange(damageChange.WeaponType, new Change()));

                    DamageChange dChange = damageChanges[damageChange.WeaponType];
                    dChange.damageChange.ApplyChangeToChange(damageChange.damageChange);
                }
            }

            // Filth passive: attack speed bonuses are converted into movement speed instead
            if (SelectedChar != null && SelectedChar.GetType() == typeof(Filth))
            {
                moveChange.ApplyChangeToChange(atkSpeedChange);
                atkSpeedChange = new Change(); // zero out attack speed
            }

            NewMovement.Instance.walkSpeed = Mathf.Max(moveChange.CalculateChanges(normalMoveSpeed), normalMoveSpeed * 0.01f);
            NewMovement.Instance.airAcceleration = Mathf.Max(moveChange.CalculateChanges(normalairAccelaration), normalairAccelaration * 0.01f);
            NewMovement.Instance.jumpPower = jumpChange.CalculateChanges(normalJumpHeight);
            globalDamageMult = globalDamageChange;
            MaxHealth = Mathf.RoundToInt(hpChange.CalculateChanges(100f));
            AttackSpeed = atkSpeedChange;
            DamageReduction = dRedChange;
            cooldownReduction = cooldownChange;
            foreach (var key in damageMultipliers.Keys.ToList())
                damageMultipliers[key] = new Change();

            foreach (var dChange in damageChanges)
            {
                damageMultipliers[dChange.Key] = dChange.Value.damageChange;
            }
        }

        public static float LogarithmicChance(int stacks, float scaling, float startValue, float maxValue)
        {
            // scaling = how fast the curve rises
            // startValue = minimum/base value at 0 stacks
            // maxValue = maximum cap

            return startValue + (maxValue - startValue) * (1f - Mathf.Exp(-scaling * stacks));
        }
        public static void AddWeapon(AWeapon weapon)
        {
            weapons.RemoveAll(w => w.weapon == weapon.weapon && w.variant == weapon.variant);

            weapons.Add(weapon);

            if (weapon.weapon == Weapon.Arm)
            {
                FistControl.Instance.ResetFists();
                if (weapon.variant != Variant.Green)
                    FistControl.Instance.ArmChange(weapon.variant == Variant.Red ? 1 : 0);
                return;
            }
            else
            {
                GunSetter.Instance.ResetWeapons();
            }

            Instance.StartCoroutine(SwitchToNewWeapon(weapon));
        }


        private static IEnumerator SwitchToNewWeapon(AWeapon weapon)
        {
            yield return null; // wait one frame for ResetWeapons/ResetFists to repopulate slots

            if (GunControl.Instance == null) yield break;

            int slot = WeaponToSlot(weapon.weapon);
            if (slot < 0) yield break;

            int variantIndex = (int)weapon.variant;
            GunControl.Instance.SwitchWeapon(slot, variantIndex);
        }

        private static int WeaponToSlot(Weapon weapon)
        {
            switch (weapon)
            {
                case Weapon.Revolver: return 1;
                case Weapon.Shotgun: return 2;
                case Weapon.Nailgun: return 3;
                case Weapon.Railcannon: return 4;
                case Weapon.RocketLauncher: return 5;
                case Weapon.Arm: return 6;
                default: return -1;
            }
        }
        #region item helpers



        public static List<BaseItem> getRarityItems(Rarity rarity, List<ItemTag> allowedTags = null)
        {
            return possibleItems.Where(x =>
                x.Rarity == rarity &&
                (!SelectedChar.HasPassive(Passive.HealFromBlood) || !x.itemTags.Contains(ItemTag.Health)) &&
                (!SelectedChar.HasPassive(Passive.Greedy) || !x.itemTags.Contains(ItemTag.Health)) &&
                (SelectedChar.GetType() != typeof(Filth) || !x.itemTags.Contains(ItemTag.Healing))
                &&
                (
                    x.ItemName != "Gasoline" ||
                    SelectedChar.GetType() == typeof(Ultrarogue.Characters.Streetcleaner)
                ) &&

                (
                    x.WeaponRequirements.Count == 0 ||
                    x.WeaponRequirements.Any(req =>
                        weapons.Any(w => w.weapon == req)
                    )
                ) && (
                    !x.CanOnlyHaveOne ||
                    GetItemCount(x) <= 0
                ) && (
                    x.ItemName != "Thunder Boomerang" ||
                    weapons.Any(w => w.weapon == Weapon.Revolver && w.variant == Variant.Green)
                ) && (
                    // If allowedTags is null or empty, allow everything.
                    // Otherwise, the item must share at least one tag with the allowed list.
                    allowedTags == null ||
                    allowedTags.Count == 0 ||
                    x.itemTags.Any(tag => allowedTags.Contains(tag))
                )
            ).ToList();
        }


        public static Rarity getRarityBasedOnDropTable(DropTable table, System.Random rng)
        {
            float chance = (float)rng.NextDouble();
            float cumulative = 0f;

            foreach (var entry in table.weights)
            {
                cumulative += entry.Value;
                if (chance < cumulative)
                {
                    return entry.Key;
                }
            }

            return table.weights.Keys.Last();
        }

        #region Tables
        public static DropTable NormalTable = new DropTable(new Dictionary<Rarity, float>()
        {
            {Rarity.Common, 0.80f },
            {Rarity.Uncommon, 0.15f },
            {Rarity.Legendary, 0.05f }
        });

        public static DropTable CommonTable = new DropTable(new Dictionary<Rarity, float>()
        {
            {Rarity.Common, 1f }
        });

        public static DropTable UnCommonTable = new DropTable(new Dictionary<Rarity, float>()
        {
            {Rarity.Uncommon, 1f }
        });

        public static DropTable LegendaryTable = new DropTable(new Dictionary<Rarity, float>()
        {
            {Rarity.Legendary, 1f }
        });

        public static DropTable PlanetTable = new DropTable(new Dictionary<Rarity, float>()
        {
            {Rarity.Alchemy, 1f }
        });

        public static DropTable RationTable = new DropTable(new Dictionary<Rarity, float>()
        {
            { Rarity.Common,    0.25f },
            { Rarity.Uncommon,  0.65f },
            { Rarity.Legendary, 0.1f  }
        });

        public static DropTable BloodTable = new DropTable(new Dictionary<Rarity, float>()
        {
            { Rarity.Common,    0.25f },
            { Rarity.Uncommon,  0.65f },
            { Rarity.Legendary, 0.1f  }
        }, new List<ItemTag>() { ItemTag.Health });
        #endregion Tables

        static DropTable getDroptable(DroptableType type)
        {
            switch (type)
            {
                case DroptableType.Planetarium:
                    return PlanetTable;
                case DroptableType.CommonOnly:
                    return CommonTable;
                case DroptableType.UncommonOnly:
                    return UnCommonTable;
                case DroptableType.LegendaryOnly:
                    return LegendaryTable;
                case DroptableType.RationShop:
                    return RationTable;
                case DroptableType.BloodMachine:
                    return BloodTable;

                case DroptableType.Boss:
                case DroptableType.RandomDrop:
                case DroptableType.Shop:
                default:
                    return NormalTable;
            }
        }

        public static BaseItem GiveRandomItem(System.Random rng = null, DroptableType table = DroptableType.RandomDrop)
        {
            DropTable dTable = getDroptable(table);

            if (rng == null)
                rng = RogueDifficultyManager.ItemRNG;

            Rarity rarity = getRarityBasedOnDropTable(dTable, rng);

            // Use the drop table's allowedTags to restrict the item pool.
            // An empty allowedTags list on DropTable means "all tags allowed".
            List<BaseItem> tiems = getRarityItems(rarity, dTable.allowedTags);

            // Safety fallback: if the tag filter produced an empty pool, ignore tags and pick from everything.
            if (tiems.Count == 0)
            {
                Plugin.Logger.LogWarning($"[GiveRandomItem] Tag-filtered pool for rarity {rarity} was empty — falling back to unfiltered pool.");
                tiems = getRarityItems(rarity);
            }

            return tiems[rng.Next(0, tiems.Count)];
        }

        public static void GiveItem(BaseItem item)
        {
            if (items.ContainsKey(item))
            {
                items[item]++;
                item.OnGotten(items[item], false);
            }
            else
            {
                items.Add(item, 1);
                item.OnGotten(items[item], true);
            }

            if (RogueDifficultyManager.Instance != null)
                RogueDifficultyManager.Instance.AddItem(item);
        }

        public static void GiveItem(string name)
        {
            BaseItem itemToGive = getItem(name);
            GiveItem(itemToGive);
        }

        /// <summary>
        /// Removes <paramref name="stacksToRemove"/> stacks of <paramref name="item"/> from the player's inventory.
        /// Calls <see cref="BaseItem.OnRemoval"/> once per stack removed, then fully removes the entry
        /// when the stack count reaches zero.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <param name="stacksToRemove">Number of stacks to remove. Defaults to 1. Pass -1 to remove all stacks.</param>
        public static void RemoveItem(BaseItem item, int stacksToRemove = 1)
        {
            if (item == null || !items.ContainsKey(item)) return;

            int currentStacks = items[item];

            // -1 means "remove everything"
            if (stacksToRemove < 0 || stacksToRemove >= currentStacks)
                stacksToRemove = currentStacks;

            for (int i = 0; i < stacksToRemove; i++)
                item.OnRemoval();

            int remaining = currentStacks - stacksToRemove;
            if (remaining <= 0)
                items.Remove(item);
            else
                items[item] = remaining;

            RogueDifficultyManager.Instance?.RemoveItem(item, stacksToRemove);
        }

        /// <summary>
        /// Removes <paramref name="stacksToRemove"/> stacks of the item with the given <paramref name="name"/>.
        /// </summary>
        public static void RemoveItem(string name, int stacksToRemove = 1)
        {
            BaseItem item = getItem(name);
            RemoveItem(item, stacksToRemove);
        }

        public void GatherItems()
        {
            possibleItems = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                typeof(BaseItem).IsAssignableFrom(t) && // inherits from base
                t != typeof(BaseItem) &&                // not the base class itself
                !t.IsAbstract)                  // not abstract
            .Select(t => (BaseItem)Activator.CreateInstance(t))
            .ToList();

            foreach (var tiem in possibleItems)
            {
                Logger.LogInfo(tiem);

                nameToItem.Add(tiem.ItemName, tiem);
                tiem.OnStart();
            }

            Logger.LogInfo($"Items registered: {possibleItems.Count}");
        }

        public static Weapon HitterToWeapon(string hitter)
        {
            switch (hitter)
            {
                case "revolver":
                    return Weapon.Revolver;
                case "shotgun":
                case "shotgunzone":
                    return Weapon.Shotgun;
                case "nail":
                case "sawblade":
                    return Weapon.Nailgun;
                case "explosion":
                    return Weapon.RocketLauncher;
                case "railcannon":
                case "drill":
                    return Weapon.Railcannon;
                case "punch":
                case "heavypunch":
                    return Weapon.Arm;
                default:
                    return (Weapon)100;
            }
        }

        public static List<string> WeaponToHitter(Weapon weapon)
        {
            List<string> result = new List<string>();
            switch (weapon)
            {
                case Weapon.Revolver:
                    result.Add("revolver");
                    break;
                case Weapon.Shotgun:
                    result.Add("shotgun");
                    result.Add("shotgunzone");
                    break;
                case Weapon.Nailgun:
                    result.Add("sawblade");
                    result.Add("nail");
                    break;
                case Weapon.RocketLauncher:
                    result.Add("explosion");
                    break;
                case Weapon.Railcannon:
                    result.Add("railcannon");
                    result.Add("drill");
                    break;
                case Weapon.Arm:
                    result.Add("punch");
                    result.Add("heavypunch");
                    break;
                default:
                    result.Add("none");
                    break;

            }

            return result;
        }
        public static Change globalDamageMult;

        public static Dictionary<Weapon, Change> damageMultipliers = new Dictionary<Weapon, Change>()
        {
            { Weapon.Revolver,       new Change() },
            { Weapon.Shotgun,        new Change() },
            { Weapon.Nailgun,        new Change() },
            { Weapon.Railcannon,     new Change() },
            { Weapon.RocketLauncher, new Change() },
            { Weapon.Arm,            new Change() }
        };

        public static int luck = 0;

        static Dictionary<string, float> procCoeffiecents = new Dictionary<string, float>()
        {
            {"nail", 0.25f },
            {"chainsawprojectile", 0.55f },
            {"sawblade", 0.75f },
            {"shotgun", 0.25f },
            {"railcannon", 0.8f },
            {"drill", 0.25f },
        };

        public static float getChanceVal(bool luckaffected = true)
        {
            float value = Random.value;
            if (luck >= 0)
            {
                for (int i = 0; i < luck; i++)
                {
                    float luckedVal = Random.value;
                    if (luckedVal > value) value = luckedVal;
                }
            }
            else
            { // negative luck, dunno if ever used but :P
                for (int i = 0; i < luck; i++)
                {
                    float luckedVal = Random.value;
                    if (luckedVal <= value) value = luckedVal;
                }
            }


            return value;
        }

        public static bool canExecute(float chance, string hitter, bool luckaffected = true)
        {
            float value = Random.value;
            for (int i = 0; i < luck; i++)
            {
                float luckedVal = Random.value;
                if (luckedVal > value) value = luckedVal;
            }

            if (procCoeffiecents.ContainsKey(hitter))
            {
                chance *= procCoeffiecents[hitter];
            }

            if (value <= chance / 100) return true;
            return false;
        }
        #endregion
        // Just for tests :DDD
        void OnGUI()
        {
            if (NewMovement.Instance == null) return;
            return;
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 14;
            labelStyle.normal.textColor = Color.white;

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = Color.yellow;

            int x = 10, y = 10, lineHeight = 20;

            GUI.Label(new Rect(x, y, 300, lineHeight), "=== ULTRAROGUE STATS ===", headerStyle);
            y += lineHeight + 4;

            // Movement
            float currentSpeed = NewMovement.Instance.walkSpeed;
            float speedDiff = currentSpeed - normalMoveSpeed;
            GUI.Label(new Rect(x, y, 300, lineHeight), $"Move Speed: {currentSpeed:F1} ({(speedDiff >= 0 ? "+" : "")}{speedDiff:F1})", labelStyle);
            y += lineHeight;

            // Jump
            float currentJump = NewMovement.Instance.jumpPower;
            float jumpDiff = currentJump - normalJumpHeight;
            GUI.Label(new Rect(x, y, 300, lineHeight), $"Jump Power: {currentJump:F1} ({(jumpDiff >= 0 ? "+" : "")}{jumpDiff:F1})", labelStyle);
            y += lineHeight;

            // Global Damage
            float globalMult = globalDamageMult.CalculateChanges(1f);
            GUI.Label(new Rect(x, y, 300, lineHeight), $"Global Damage Mult: x{globalMult:F2}", labelStyle);
            y += lineHeight;
            float speedatkDiff = AttackSpeed.CalculateChanges(1f) - 1;
            GUI.Label(new Rect(x, y, 300, lineHeight), $"Attack Speed: {AttackSpeed.CalculateChanges(1f):F1} ({(speedatkDiff >= 0 ? "+" : "")}{speedatkDiff:F1})", labelStyle);
            y += lineHeight + 4;

            // Per-weapon damage
            GUI.Label(new Rect(x, y, 300, lineHeight), "-- Weapon Damage --", headerStyle);
            y += lineHeight + 2;

            foreach (var kvp in damageMultipliers)
            {
                float weaponMult = kvp.Value.CalculateChanges(1f);
                Color color = weaponMult > 1f ? Color.green : weaponMult < 1f ? Color.red : Color.white;
                labelStyle.normal.textColor = color;
                GUI.Label(new Rect(x, y, 300, lineHeight), $"{kvp.Key}: x{weaponMult:F2}", labelStyle);
                y += lineHeight;
            }
            if (RogueDifficultyManager.Instance != null)
            {
                y += 4;
                labelStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(x, y, 300, lineHeight), "-- ROGUE STATS --", headerStyle);
                y += lineHeight + 2;

                GUI.Label(new Rect(x, y, 300, lineHeight), $"Difficulty: {RogueDifficultyManager.Instance.Difficulty}", labelStyle);
                y += lineHeight;
                GUI.Label(new Rect(x, y, 300, lineHeight), $"Gold: {RogueDifficultyManager.Instance.Gold}", labelStyle);
                y += lineHeight;
            }




            // Items
            y += 4;
            labelStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y, 300, lineHeight), "-- Items --", headerStyle);
            y += lineHeight + 2;

            if (items.Count == 0)
            {
                labelStyle.normal.textColor = Color.gray;
                GUI.Label(new Rect(x, y, 300, lineHeight), "No items", labelStyle);
                y += lineHeight;
            }
            else
            {
                foreach (var kvp in items)
                {
                    Color rarityColor = kvp.Key.Rarity switch
                    {
                        Rarity.Common => Color.white,
                        Rarity.Uncommon => Color.green,
                        Rarity.Legendary => Color.yellow,
                        _ => Color.white
                    };
                    labelStyle.normal.textColor = rarityColor;
                    GUI.Label(new Rect(x, y, 300, lineHeight), $"{kvp.Key.ItemName} x{kvp.Value}", labelStyle);
                    y += lineHeight;
                }
            }
        }
    }

    public enum Rarity
    {
        Common,
        Uncommon,
        Legendary,
        Alchemy
    }

    public enum Team
    {
        Player,
        Enemies
    }

    public class AWeapon
    {
        public Plugin.Weapon weapon;
        public Plugin.Variant variant;
        public bool Alternate;
        public static AWeapon GenerateWeapon()
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                AWeapon generated;

                if (SelectedChar != null && SelectedChar.GetType() == typeof(Ultrarogue.Characters.Streetcleaner))
                {
                    int choice = RogueDifficultyManager.RoomRNG.Next(0, 5);
                    Plugin.Logger.LogInfo($"Weapon choice: {choice}");
                    switch (choice)
                    {
                        case 0: generated = new AWeapon(Weapon.Arm, Variant.Red, false); break;
                        case 1: generated = new AWeapon(Weapon.Nailgun, Variant.Green, false); break;
                        case 2:
                            generated = new AWeapon(
                                Weapon.Shotgun,
                                Variant.Blue,
                                (float)RogueDifficultyManager.ItemRNG.NextDouble() <= 0.5f
                            );
                            break;
                        case 3: generated = new AWeapon(Weapon.RocketLauncher, Variant.Red, false); break;
                        default: generated = new AWeapon(Weapon.Railcannon, Variant.Red, false); break;
                    }
                }
                else
                {
                    Weapon weaponEnum = (Weapon)RogueDifficultyManager.RoomRNG.Next(0, System.Enum.GetValues(typeof(Weapon)).Length);
                    Variant variantEnum = (Variant)RogueDifficultyManager.RoomRNG.Next(0, System.Enum.GetValues(typeof(Variant)).Length);

                    bool alt = false;
                    if (CanBeAlternate(weaponEnum))
                        alt = (float)RogueDifficultyManager.ItemRNG.NextDouble() <= 0.5f;

                    generated = new AWeapon(weaponEnum, variantEnum, alt);
                }

                bool alreadyOwned = Plugin.weapons.Any(w =>
                    w.weapon == generated.weapon &&
                    w.variant == generated.variant &&
                    w.Alternate == generated.Alternate
                );

                if (!alreadyOwned)
                    return generated;
            }

            Plugin.Logger.LogWarning("All weapon variants owned, returning random duplicate.");
            return new AWeapon(Weapon.Revolver, Variant.Blue);
        }



        public static bool CanBeAlternate(Plugin.Weapon wp)
        {
            switch (wp)
            {
                case Weapon.Revolver:
                case Weapon.Nailgun:
                case Weapon.Shotgun:
                    return true;
                default:
                    return false;
            }
        }

        public AWeapon(Plugin.Weapon weapon, Plugin.Variant variant, bool isAlternate = false)
        {
            this.weapon = weapon;
            this.variant = variant;
            this.Alternate = isAlternate;
        }
        public override string ToString()
        {
            return Plugin.getWeaponString(weapon, variant);
        }
    }

    #region Patches

    [HarmonyPatch]
    public class WeaponPatches
    {
        [HarmonyPatch(typeof(PrefsManager), nameof(PrefsManager.GetInt))]
        [HarmonyPostfix]
        public static void ModifyGuns(ref int __result, string key, int fallback = 0)
        {
            if (!Plugin.isInRogueMode()) return;
            if (key.StartsWith("weapon."))
            {
                if (Plugin.weapons.Any((x) => key.StartsWith(x.ToString())))
                {
                    int p = Plugin.weapons.First((x) => key.StartsWith(x.ToString())).Alternate ? 2 : 1;
                    __result = p;
                }
                else __result = 0;
            }
        }
    }

    [HarmonyPatch(typeof(StockMapInfo), nameof(StockMapInfo.Awake))]
    internal static class StockMapInfoPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!isInRogueScene()) return;
            StatsManager sman = GameObject.FindObjectOfType<StatsManager>();
            if (sman != null)
                sman.levelNumber = -1;

            string currentPath = SceneManager.GetActiveScene().path;
            foreach (ExecuteOnSceneLoadRogue obj in Resources.FindObjectsOfTypeAll<ExecuteOnSceneLoadRogue>().Where(o => o.gameObject.scene.path == currentPath).OrderBy(exe => exe.relativeExecutionOrder))
            {
                try
                {
                    obj.Execute();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError($"Error while executing OnSceneLoad script for {obj.gameObject.name}: {e}");
                }
            }
        }
    }

    [HarmonyPatch]
    public class PlayerPatches
    {

        static Dictionary<Slider, float> sliderPercentageMax = new Dictionary<Slider, float>();
        static Dictionary<Slider, float> sliderPercentageMin = new Dictionary<Slider, float>();

        [HarmonyPatch(typeof(HealthBar), nameof(HealthBar.Update))]
        [HarmonyPrefix]
        public static void DisplayCorrectMaxHP(HealthBar __instance)
        {
            if (__instance.hpSliders.Length != 0)
            {
                foreach (Slider slider in __instance.hpSliders)
                {
                    if (!sliderPercentageMax.ContainsKey(slider))
                        sliderPercentageMax[slider] = slider.maxValue / 100; // 100 being the default value
                    if (!sliderPercentageMin.ContainsKey(slider))
                        sliderPercentageMin[slider] = slider.minValue / 100; // 100 being the default value

                    slider.maxValue = sliderPercentageMax[slider] * Plugin.MaxHealth;
                    slider.minValue = sliderPercentageMin[slider] * Plugin.MaxHealth;

                    //if (slider.gameObject.name.StartsWith("Supercharge"))
                    //{
                    //    if (slider.maxValue != Plugin.MaxHealth * 2)
                    //    {
                    //        Plugin.Logger.LogInfo($"Applying thing to {slider.gameObject.name} that has max of {slider.maxValue}");
                    //        slider.maxValue = Plugin.MaxHealth * 2;
                    //        slider.minValue = Plugin.MaxHealth;
                    //    }
                    //}
                    //else
                    //{
                    //    if (slider.maxValue != Plugin.MaxHealth)
                    //    {
                    //        Plugin.Logger.LogInfo($"Applying thing to {slider.gameObject.name} that has max of {slider.maxValue}");
                    //        slider.maxValue = Plugin.MaxHealth;
                    //    }
                    //}

                }
            }
            if (__instance.afterImageSliders != null)
            {
                foreach (Slider slider2 in __instance.afterImageSliders)
                {
                    if (!sliderPercentageMax.ContainsKey(slider2))
                        sliderPercentageMax[slider2] = slider2.maxValue / 100; // 100 being the default value
                    if (!sliderPercentageMin.ContainsKey(slider2))
                        sliderPercentageMin[slider2] = slider2.minValue / 100; // 100 being the default value

                    slider2.maxValue = sliderPercentageMax[slider2] * Plugin.MaxHealth;
                    slider2.minValue = sliderPercentageMin[slider2] * Plugin.MaxHealth;
                }
            }
        }

        [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.Update))]
        [HarmonyPrefix]
        public static bool DontRestartLevelRhing(StatsManager __instance)
        {
            if (!Plugin.isInRogueScene()) return true;
            if (__instance.timer)
            {
                __instance.seconds += Time.deltaTime * GameStateManager.Instance.TimerModifier;
            }
            if (__instance.stylePoints < 0)
            {
                __instance.stylePoints = 0;
            }
            if (!__instance.endlessMode)
            {
                DiscordController.UpdateStyle(__instance.stylePoints);
            }
            return false;

        }

        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
        [HarmonyPrefix]
        public static void DamageLess(ref int damage, ref bool ignoreInvincibility, NewMovement __instance)
        {
            if (damage > 0)
                damage = (int)Mathf.Max(DamageReduction.CalculateChanges(damage), 1); // cannot go below 1
            foreach (var effect in Plugin.onDamageEffects)
            {
                effect.effect.Invoke(damage);
            }
            if (SelectedChar?.HasPassive(Passive.Greedy) != true) return;
            if (RogueDifficultyManager.Instance.Gold <= 0)
            {
                damage = 999;
            }
        }

        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
        [HarmonyPostfix]
        public static void Damage(NewMovement __instance)
        {
            if (__instance.dead && Plugin.isInRogueScene())
            {
                __instance.deathSequence.gameObject.SetActive(false);
                RogueFinalRank.Instance.GameOver();

            }

        }

        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
        [HarmonyPostfix]
        public static void DamageGreed(ref int damage, NewMovement __instance)
        {
            if (SelectedChar?.HasPassive(Passive.Greedy) != true) return;

            damage = ShopItem.GetScaledCost(damage / 10);

            if (damage > 0)
                RogueDifficultyManager.Instance.Gold -= damage;
            if (RogueDifficultyManager.Instance.Gold <= 0) return;
            __instance.ResetHardDamage();
            __instance.hp = 100;
        }

        [HarmonyPatch(typeof(GasolineStain), nameof(GasolineStain.AttachTo))]
        [HarmonyPostfix]
        public static void Stret(GasolineStain __instance)
        {
            if (!SelectedChar.HasPassive(Passive.GasolineFire)) return;

            StainVoxelManager instance = MonoSingleton<StainVoxelManager>.Instance;
            Vector3 forward = __instance.transform.forward;
            Vector3 worldPosition = __instance.transform.position + forward * -0.5f;
            StainVoxel stainVoxel = instance.CreateOrGetVoxel(worldPosition, false);
            VoxelProxy voxelProxy = stainVoxel.CreateOrGetProxyFor(__instance);
            voxelProxy.StartBurningOrRefuel();
        }

        [HarmonyPatch(typeof(FireZone), "OnTriggerStay")]
        [HarmonyPrefix]
        public static bool doNotDamage(FireZone __instance, Collider other)
        {
            if (!SelectedChar.HasPassive(Passive.NoFireDamage)) return true;
            if (other.CompareTag("Player"))
            {
                return false;
            }

            return true;
        }
        [HarmonyPatch(typeof(Bloodsplatter), nameof(Bloodsplatter.Collide))]
        [HarmonyPrefix]
        public static bool DontHealIfV2(Bloodsplatter __instance, Collider other)
        {
            if (!Plugin.isInRogueScene()) return true;
            if (__instance.ready)
            {
                if (__instance.bsm == null)
                {
                    return false;
                }
                BloodFiller bloodFiller;
                if (__instance.bsm.hasBloodFillers && ((__instance.bsm.bloodFillers.Contains(other.gameObject) && other.gameObject.TryGetComponent<BloodFiller>(out bloodFiller)) || (other.attachedRigidbody && __instance.bsm.bloodFillers.Contains(other.attachedRigidbody.gameObject) && other.attachedRigidbody.TryGetComponent<BloodFiller>(out bloodFiller))))
                {
                    bloodFiller.FillBloodSlider((float)__instance.hpAmount, __instance.transform.position, __instance.eidID);
                    return false;
                }
                if (!SelectedChar.HasPassive(Passive.HealFromBlood))
                {
                    int c = GetItemCount("Blood Flowing Plating");
                    if (c > 0)
                    {
                        MonoSingleton<NewMovement>.Instance.GetHealth(Mathf.FloorToInt(__instance.hpAmount * (0.1f * c)), false, __instance.fromExplosion, true);
                        __instance.DisableCollider();
                    }

                    return false;
                }
                if (__instance.canCollide && other.gameObject.CompareTag("Player"))
                {
                    MonoSingleton<NewMovement>.Instance.GetHealth(__instance.hpAmount, false, __instance.fromExplosion, true);
                    __instance.DisableCollider();
                }
            }
            return false;
        }
        [HarmonyPatch(typeof(Bloodsplatter), nameof(Bloodsplatter.CreateBloodstain))]
        [HarmonyPrefix]
        public static bool DontHealIfV2Create(Bloodsplatter __instance)
        {
            if (!Plugin.isInRogueScene()) return true;
            if (!SelectedChar.HasPassive(Passive.HealFromBlood))
                __instance.hpOnParticleCollision = false;
            return true;
        }
        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Parry))]
        [HarmonyPrefix]
        public static bool DontHealIfV2Parry(NewMovement __instance, EnemyIdentifier eid = null, string customParryText = "")
        {
            Plugin.Logger.LogInfo($"Parrying as {SelectedChar.Name} and we are in the rogue mode {isInRogueMode()}");
            if (!Plugin.isInRogueMode()) return true;
            MonoSingleton<TimeController>.Instance.ParryFlash();
            __instance.exploded = false;
            if (SelectedChar.HasPassive(Passive.HealFromBlood))
                __instance.GetHealth(999, false, false, true);

            __instance.FullStamina();
            if (__instance.shud == null)
            {
                __instance.shud = MonoSingleton<StyleHUD>.Instance;
            }
            if (!eid || !eid.blessed)
            {
                __instance.shud.AddPoints(100, (customParryText != "") ? ("<color=green>" + customParryText + "</color>") : "ultrakill.parry", null, null, -1, "", "");
            }

            return false;
        }
        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHealth))]
        [HarmonyPrefix]
        public static bool HealthChange(NewMovement __instance, int health, bool silent, bool fromExplosion = false, bool bloodsplatter = true)
        {
            if (!__instance.dead && (!__instance.exploded || !fromExplosion))
            {
                float num = (float)health;
                float num2 = MaxHealth;
                if (__instance.difficulty == 0 || (__instance.difficulty == 1 && __instance.sameCheckpointRestarts > 2))
                {
                    num2 = MaxHealth * 2;
                }
                if (num < 1f)
                {
                    num = 1f;
                }
                if ((float)__instance.hp <= num2)
                {
                    if ((float)__instance.hp + num < num2 - (float)Mathf.RoundToInt(__instance.antiHp))
                    {
                        __instance.hp += Mathf.RoundToInt(num);
                    }
                    else if ((float)__instance.hp != num2 - (float)Mathf.RoundToInt(__instance.antiHp))
                    {
                        __instance.hp = Mathf.RoundToInt(num2) - Mathf.RoundToInt(__instance.antiHp);
                    }
                    __instance.hpFlash.Flash(1f);
                    if (!silent && health > 5)
                    {
                        if (__instance.greenHpAud == null)
                        {
                            __instance.greenHpAud = __instance.hpFlash.GetComponent<AudioSource>();
                        }
                        __instance.greenHpAud.Play(true);
                    }
                }
                if (!silent && health > 5 && MonoSingleton<PrefsManager>.Instance.GetBoolLocal("bloodEnabled", false))
                {
                    UnityEngine.Object.Instantiate<GameObject>(__instance.scrnBlood, __instance.fullHud.transform);
                }
            }
            return false;

        }

        [HarmonyPatch(typeof(WeaponCharges), nameof(WeaponCharges.Charge))]
        [HarmonyPrefix]
        public static void ApplyCooldownPatch(ref float amount)
        {
            amount = cooldownReduction.CalculateChanges(amount);
        }

        #region Weapon Patches

        [HarmonyPatch(typeof(Revolver), nameof(Revolver.Update))]
        public static class Revolver_Update_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Ldc_R4)
                    {
                        float val = (float)instr.operand;

                        if (val == 200f || val == 40f || val == 480f)
                        {
                            yield return instr; // keep original constant
                            yield return new CodeInstruction(
                                OpCodes.Call,
                                AccessTools.Method(typeof(Revolver_Update_Patch), nameof(ModifyRate))
                            );
                            continue;
                        }
                    }

                    yield return instr;
                }
            }

            public static float ModifyRate(float amount)
            {
                return cooldownReduction.CalculateChanges(amount);
            }
        }
        [HarmonyPatch(typeof(Revolver), nameof(Revolver.Shoot))]
        public static class Revolver_Shoot_V2_Patch
        {
            private static bool _isExtraShot = false;

            [HarmonyPostfix]
            public static void TripleShot(Revolver __instance, int shotType)
            {
                if (_isExtraShot) return;
                if (shotType != 1) return;
                if (SelectedChar?.HasPassive(Passive.TripleShot) != true) return;

                __instance.StartCoroutine(ExtraShots(__instance));
            }

            private static IEnumerator ExtraShots(Revolver __instance)
            {
                for (int i = 0; i < 2; i++)
                {
                    yield return new WaitForSeconds((__instance.altVersion ? 0.5f : 0.2f) / AttackSpeed.CalculateChanges(1f));
                    if (__instance == null || !__instance.gameObject.activeInHierarchy)
                        yield break;
                    if (!__instance.inman.InputSource.Fire1.IsPressed) yield break;
                    _isExtraShot = true;
                    __instance.Shoot(1);
                    _isExtraShot = false;
                }
            }
        }
        [HarmonyPatch(typeof(Revolver), nameof(Revolver.ThrowCoin))]
        public static class Revolver_ThrowCoin_Greed_Patch
        {

            [HarmonyPostfix]
            public static void GreedCoin(Revolver __instance)
            {
                if (SelectedChar?.HasPassive(Passive.Greedy) != true) return;

                __instance.wc.rev1charge = 400f;
                RogueDifficultyManager.Instance.Gold -= 1;

            }
        }
        [HarmonyPatch(typeof(Coin), nameof(Coin.GetDeleted))]
        public static class Coin_GetDeleted_Greed_Patch
        {
            private static readonly HashSet<Coin> RefundedCoins = new();
            [HarmonyPostfix]
            public static void GreedRefund(Coin __instance)
            {
                if (SelectedChar?.HasPassive(Passive.Greedy) != true) return;
                if (!RefundedCoins.Add(__instance)) return;
                RogueDifficultyManager.Instance.Gold += 1;

            }
        }

        [HarmonyPatch(typeof(Nailgun), nameof(Nailgun.Update))]
        public static class Nailgun_Update_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var moveTowards = AccessTools.Method(typeof(Mathf), nameof(Mathf.MoveTowards));
                var modify = AccessTools.Method(typeof(Nailgun_Update_Patch), nameof(ModifyDelta));

                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Call && instr.operand as MethodInfo == moveTowards)
                    {
                        yield return new CodeInstruction(OpCodes.Call, modify); // modifies top of stack
                        yield return instr;
                    }
                    else
                    {
                        yield return instr;
                    }
                }
            }

            public static float ModifyDelta(float maxDelta)
            {
                return AttackSpeed.CalculateChanges(maxDelta);
            }
        }

        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Update))]
        public static class FilthBoostRechargeTranspiler
        {
            public static float GetRechargeRate(float baseRate)
            {
                if (SelectedChar != null && SelectedChar.GetType() == typeof(Filth))
                    return cooldownReduction.CalculateChanges(baseRate);

                return baseRate;
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var getRechargeRate = AccessTools.Method(
                    typeof(FilthBoostRechargeTranspiler),
                    nameof(GetRechargeRate));

                bool patched = false;

                foreach (var instr in instructions)
                {
                    if (!patched
                        && instr.opcode == OpCodes.Ldc_R4
                        && instr.operand is float f
                        && Mathf.Approximately(f, 70f))
                    {
                        yield return instr;
                        yield return new CodeInstruction(OpCodes.Call, getRechargeRate);
                        patched = true;
                        continue;
                    }

                    yield return instr;
                }

                if (!patched)
                    Plugin.Logger.LogWarning(
                        "[FilthBoostRechargeTranspiler] Could not find the 70f constant in NewMovement.Update. " +
                        "The patch was NOT applied – the game may have been updated.");
            }
        }
        [HarmonyPatch(typeof(RocketLauncher), nameof(RocketLauncher.Update))]
        public static class RocketLauncher_Update_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var moveTowards = AccessTools.Method(typeof(Mathf), nameof(Mathf.MoveTowards));
                var modify = AccessTools.Method(typeof(RocketLauncher_Update_Patch), nameof(ModifyDelta));

                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Call && instr.operand as MethodInfo == moveTowards)
                    {
                        yield return new CodeInstruction(OpCodes.Call, modify);
                        yield return instr;
                    }
                    else
                    {
                        yield return instr;
                    }
                }
            }

            public static float ModifyDelta(float maxDelta)
            {
                return AttackSpeed.CalculateChanges(maxDelta);
            }
        }

        #endregion Weapon Patches
    }


    [HarmonyPatch]
    public class EnemyPatches
    {
        [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Death), new System.Type[] { typeof(bool) })]
        [HarmonyPrefix]
        public static void ActivateDeathEffects(EnemyIdentifier __instance)
        {
            if (!Plugin.isInRogueMode()) return;
            if (__instance.dead) return;

            /*
                bool rogueScene = Plugin.isInRogueScene();
                bool canExec = Plugin.canExecute(25f, "");

                if (!rogueScene && canExec)
                {
                    if (Random.value <= 0.25f)
                    {
                        Weapon weaponEnum = (Weapon)Random.Range(0, Enum.GetValues(typeof(Weapon)).Length);
                        Variant variantEnum = (Variant)Random.Range(0, Enum.GetValues(typeof(Variant)).Length);

                        AWeapon weapon = new AWeapon(weaponEnum, variantEnum);
                        HudMessageReceiver.Instance?.SendHudMessage(weapon.ToString());
                        Plugin.weapons.Add(weapon);
                        if (weaponEnum == Weapon.Arm)
                            FistControl.Instance.ResetFists();
                        else
                            GunSetter.Instance.ResetWeapons();
                    }
                    else
                    {
                        ItemPickup.CreatePickup(GiveRandomItem(), __instance.transform.position);
                    }

                }*/

            foreach (var deathEffect in Plugin.deathEffects)
            {
                if (Plugin.GetItemCount(deathEffect.itemName) <= 0)
                {
                    continue;
                }
                deathEffect.effect.Invoke(__instance);
            }


            if (Plugin.SelectedChar.HasPassive(Passive.Greedy))
            {
                if (RogueDifficultyManager.Instance != null) RogueDifficultyManager.Instance.Gold++;

            }

        }

        [HarmonyPatch(typeof(Enemy), nameof(Enemy.GetHurt))]
        [HarmonyPrefix]
        public static void ActivateHitEffects(ref float multiplier, GameObject sourceWeapon, Enemy __instance)
        {
            if (!Plugin.isInRogueMode()) return;
            if (__instance.eid.dead) return;
            Weapon weaponUsed = Plugin.HitterToWeapon(__instance.eid.hitter);
            if (Plugin.damageMultipliers.ContainsKey(weaponUsed))
                multiplier = Plugin.damageMultipliers[weaponUsed].CalculateChanges(multiplier);
            multiplier = Plugin.globalDamageMult.CalculateChanges(multiplier);
            foreach (var mod in Plugin.dmgModifiers)
            {
                float mult = mod.damageModifier(__instance.eid);
                multiplier *= mult;
            }

            foreach (var hitEffect in Plugin.hitEffects)
            {
                if (Plugin.GetItemCount(hitEffect.itemName) <= 0)
                {
                    continue;
                }
                hitEffect.effect.Invoke(__instance.eid, multiplier);
            }

        }
    }


    [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.StartBurning))]
    public static class BurningHeal
    {
        static void Postfix(EnemyIdentifier __instance)
        {
            if (!Plugin.isInRogueMode()) return;

            if (SelectedChar.HasPassive(Passive.NoFireDamage))
                NewMovement.Instance.GetHealth(5, false);
        }
    }

    [HarmonyPatch(typeof(DiscordController), nameof(DiscordController.SendActivity))]
    public class ReplaceActivity
    {
        public static void Prefix(DiscordController __instance)
        {
            if (!isInRogueScene())
            {

                return;
            }

            __instance.cachedActivity.State = "ROGUE MODE";

            __instance.cachedActivity.Details = "Floor: " + RogueDifficultyManager.Instance.floor;
            __instance.cachedActivity.Assets.LargeImage = "level_0-1";

            string LargeText = $"Go: {RogueDifficultyManager.Instance.Gold} " +
                $"Ke: {RogueDifficultyManager.Instance.Keys}";

            __instance.cachedActivity.Assets.LargeText = LargeText; // ONLY PRELUDE FOR NOW
        }
    }

    [HarmonyPatch(typeof(GameConsole.Console), nameof(GameConsole.Console.Awake))]
    public class AddCommands
    {
        public static void Postfix(GameConsole.Console __instance)
        {

            __instance.RegisterCommand(new Ultrarogue(__instance));
        }

        public class Ultrarogue : CommandRoot, IConsoleLogger
        {
            public Ultrarogue(GameConsole.Console con) : base(con)
            {
            }

            public override string Name => "ULTRAROGUE";

            public override string Description => "Commands for ultrarogue";

            public plog.Logger Log => new plog.Logger("Buffs");

            public override Branch BuildTree(GameConsole.Console con)
            {
                string name = "ultrarogue";
                Node[] array = new Node[4];

                List<Node> list = new List<Node>();

                // Add the "addall" leaf first
                list.Add(CommandRoot.Leaf("addall", delegate ()
                {
                    foreach (var item in Plugin.nameToItem)
                    {
                        Plugin.GiveItem(item.Value);
                    }
                    Log.Info($"Gave all the items, item count: {Plugin.nameToItem.Count}");
                }, true));

                // Then add all item leaves
                foreach (var item in Plugin.nameToItem)
                {
                    list.Add(CommandRoot.Leaf("add_" + item.Value.ItemName.Replace(" ", "_"), () =>
                    {
                        Plugin.GiveItem(item.Value);
                        Log.Info($"Gave item {item.Value.ItemName}");
                    }, true));
                }

                // Remove commands
                list.Add(CommandRoot.Leaf("removeall", delegate ()
                {
                    foreach (var item in Plugin.items.Keys.ToList())
                        Plugin.RemoveItem(item, -1);
                    Log.Info("Removed all items.");
                }, true));

                foreach (var item in Plugin.nameToItem)
                {
                    list.Add(CommandRoot.Leaf("remove_" + item.Value.ItemName.Replace(" ", "_"), () =>
                    {
                        Plugin.RemoveItem(item.Value);
                        Log.Info($"Removed 1 stack of {item.Value.ItemName}");
                    }, true));
                }

                // Now build the branch with the complete array
                GameConsole.CommandTree.Branch br = CommandRoot.Branch("items", list.ToArray());

                array[0] = br;

                array[1] = Branch("floor", new Node[]
                {
                    CommandRoot.Leaf("revealfloor", delegate ()
                    {
                        if(MinimapUI.Instance == null)
                        {
                            Log.Error("No minimap found!");
                            return;
                        }
                        MinimapUI.Instance.RevealAll();
                    }, true),
                    CommandRoot.Leaf("nextstage", delegate ()
                    {
                        NewMovement.Instance.transform.position = GameObject.Find("PortalPos").transform.position;
                        RoomGenerator.Instance.RegenerateRooms();
                    }, true),
                });
                List<Node> listC = new List<Node>();
                foreach (var curse in CurseManager.possibleCurses)
                {
                    listC.Add(CommandRoot.Leaf("add_" + curse.CurseName.Replace(" ", "_"), () =>
                    {
                        CurseManager.FloorExit();
                        CurseManager.ActiveCurse = curse;
                        CurseManager.FloorEnter();
                        Log.Info($"Gave curse {curse.CurseName}");
                    }, true));
                }

                // Now build the branch with the complete array
                GameConsole.CommandTree.Branch brC = CommandRoot.Branch("curses", listC.ToArray());
                array[2] = brC;

                return CommandRoot.Branch(name, array);
            }
        }
    }

    #endregion
    public class RogueSaveData
    {
        public List<RogueSaveDataData> datas { get; set; }
        public RogueSaveDataData BestRun { get; set; }
    }
    public class RogueSaveDataData
    {
        public int Floor { get; set; }
        public int Kills { get; set; }
        public int ItemsGotten { get; set; }
        public float Time { get; set; }
    }

    #region item helper classes

    public class DeathEffect
    {
        public string itemName;
        public Action<EnemyIdentifier> effect;

        public DeathEffect(string itemName, Action<EnemyIdentifier> effect)
        {
            this.itemName = itemName;
            this.effect = effect;
            Plugin.deathEffects.Add(this);
        }
    }

    public class DamageModifier
    {
        public string itemName;
        public Func<EnemyIdentifier, float> damageModifier;

        public DamageModifier(string itemName, Func<EnemyIdentifier, float> damageModifier)
        {
            this.itemName = itemName;
            this.damageModifier = damageModifier;

            Plugin.dmgModifiers.Add(this);
        }
    }

    public class HitEffect
    {
        public string itemName;
        public Action<EnemyIdentifier, float> effect;

        public HitEffect(string itemName, Action<EnemyIdentifier, float> effect)
        {
            this.itemName = itemName;
            this.effect = effect;

            Plugin.hitEffects.Add(this);
        }
    }
    public class DamageTakenEffect
    {
        public string itemName;
        public Action<int> effect;

        public DamageTakenEffect(string itemName, Action<int> effect)
        {
            this.itemName = itemName;
            this.effect = effect;

            Plugin.onDamageEffects.Add(this);
        }
    }

    public class DropTable
    {
        public Dictionary<Rarity, float> weights = new Dictionary<Rarity, float>();
        public List<ItemTag> allowedTags = new List<ItemTag>();

        /// <summary>
        /// Creates a DropTable.
        /// </summary>
        /// <param name="weights">Rarity weights that sum to 1.</param>
        /// <param name="allowedTags">
        /// Optional tag whitelist. When null or empty the table may drop ANY item
        /// (no tag filter). When populated, only items whose itemTags share at
        /// least one entry with this list will be eligible for selection.
        /// Example: new List&lt;ItemTag&gt; { ItemTag.Healing } restricts drops to
        /// healing items only.
        /// </param>
        public DropTable(Dictionary<Rarity, float> weights, List<ItemTag> allowedTags = null)
        {
            this.weights = weights;
            // Null means no filter; store an empty list so Count == 0 checks work cleanly.
            this.allowedTags = allowedTags ?? new List<ItemTag>();
        }
    }

    public class PlayerChange
    {
        public Change moveSpeed;
        public Change jumpHeight;
        public Change maxHealth;
        public Change attackSpeed;
        public Change cooldownRed;
        public Change damageReduction;
        public List<DamageChange> damageChanges;
        public Change globalDamageMult;

        public PlayerChange(Change moveSpeed = null, Change jumpHeight = null, Change maxHealth = null, Change attackSpeed = null, Change cooldownReduction = null, Change damageReduction = null, List<DamageChange> damageChanges = null, Change globalDamageMult = null)
        {
            if (moveSpeed == null) moveSpeed = new Change();
            if (jumpHeight == null) jumpHeight = new Change();
            if (damageChanges == null) damageChanges = new List<DamageChange>();
            if (globalDamageMult == null) globalDamageMult = new Change();
            if (maxHealth == null) maxHealth = new Change();
            if (attackSpeed == null) attackSpeed = new Change();
            if (cooldownReduction == null) cooldownReduction = new Change();
            if (damageReduction == null) damageReduction = new Change();

            this.moveSpeed = moveSpeed;
            this.jumpHeight = jumpHeight;
            this.damageChanges = damageChanges;
            this.globalDamageMult = globalDamageMult;
            this.maxHealth = maxHealth;
            this.attackSpeed = attackSpeed;
            this.damageReduction = damageReduction;
            this.cooldownRed = cooldownReduction;

            Plugin.playerChanges.Add(this);

        }
    }

    public class Change
    {
        public float addition;
        public float percentage;
        public float multiplier;
        public float postMultiplier;

        public Change(float addition = 0, float percentage = 0, float multiplier = 1, float postMultiplier = 1)
        {
            this.addition = addition;
            this.percentage = percentage;
            this.multiplier = multiplier;
            this.postMultiplier = postMultiplier;
        }

        public void ApplyChangeToChange(Change change)
        {
            this.addition += change.addition;
            this.percentage += change.percentage;
            this.multiplier *= change.multiplier;
            this.postMultiplier *= change.postMultiplier;
        }

        public float CalculateChanges(float normalVal)
        {
            float fullPercentage = percentage + 1;
            float Val = normalVal;
            Val *= fullPercentage;
            Val *= multiplier;
            Val += addition;
            Val *= postMultiplier;
            return Val;
        }
    }

    public class DamageChange
    {
        public Plugin.Weapon WeaponType;
        public Change damageChange;

        public DamageChange(Weapon weaponType, Change damageChange)
        {
            WeaponType = weaponType;
            this.damageChange = damageChange;
        }
    }


    #endregion


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

            if (hitter == "filthbonk")
            {
                __instance.AddPoints(25, "", eid, sourceWeapon);
                if (dead)
                {
                    __instance.AddPoints(200, "FILTH BONKED", eid, sourceWeapon);
                }
            }

        }
    }
}

// Every day, i imagine a future where i can be with you
// In my hand is a pen that will write a poem of me and you
// The ink flows down into a dark puddle
// Just move your hand, write the way into his heart
// But in this world with infinite choices
// What will it take just to find that special day?
// What will it take just to find that special day?
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ULTRAKILL.Enemy;
using Ultrarogue.Items;
using UnityEngine;
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
        public static List<DamageModifier> dmgModifiers = new List<DamageModifier>();

        public static List<BaseItem> possibleItems = new List<BaseItem>();

        public static Dictionary<BaseItem, int> items = new Dictionary<BaseItem, int>();
        public static Dictionary<string, BaseItem> nameToItem = new Dictionary<string, BaseItem>();

        public static List<PlayerChange> playerChanges = new List<PlayerChange>();

        public static List<AWeapon> weapons = new List<AWeapon>();

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

        public static bool isInRogueMode()
        {
            return true; // temp
        }

        public static bool isInRogueScene()
        {
            return false;
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
            if(item == null) return 0;
            if(items.ContainsKey(item)) return items[item];

            return 0;
        }
        public static int GetItemCount(BaseItem item)
        {
            if (item == null) return 0;
            if (items.ContainsKey(item)) return items[item];

            return 0;
        }

        float normalMoveSpeed = 0f;
        float normalJumpHeight = 0f;
        public static int MaxHealth = 100;
        public static Change AttackSpeed;
        public static Change cooldownReduction = new Change();
        public static Plugin Instance { get; private set;  }

        private void Awake()
        {
            // Plugin startup logic
            Instance = this;
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            gameObject.hideFlags = HideFlags.DontSaveInEditor;
            Harmony.PatchAll();
            GatherItems();
            //LoadBundle();
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            weapons.Clear();
            weapons.Add(new AWeapon(Weapon.Revolver, Variant.Blue));

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

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (ShaderManager.shaderDictionary.Count <= 0) StartCoroutine(ShaderManager.LoadShadersAsync());
            if (NewMovement.Instance == null) return;
            normalMoveSpeed = NewMovement.Instance.walkSpeed;
            normalJumpHeight = NewMovement.Instance.jumpPower;
           
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                BaseItem item = getItem("Bouncy Hitscans");
                GiveItem(item);
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                StartCoroutine(SceneLoader.LoadLevelAsync(false)); 
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                if(RogueDifficultyManager.Instance == null)
                {
                    new GameObject("DiffMan").AddComponent<RogueDifficultyManager>();
                    return;
                }

                SpawnEnemiesTest(5);
            }
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

            // ── F7 → Give random item (quick item test) ─────────────────────────
            if (Input.GetKeyDown(KeyCode.F7))
            {
                var item = GiveRandomItem();
                GiveItem(item);
                Logger.LogInfo($"[DEBUG] Gave item: {item}");
            }

            // ── F8 → Give 10 gold ───────────────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.F8))
            {
                if (RogueDifficultyManager.Instance != null)
                {
                    RogueDifficultyManager.Instance.Gold += 10;
                    Logger.LogInfo($"[DEBUG] Gold: {RogueDifficultyManager.Instance.Gold}");
                }
            }

            // ── F9 → Log difficulty ─────────────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (RogueDifficultyManager.Instance != null)
                    Logger.LogInfo($"[DEBUG] Difficulty: {RogueDifficultyManager.Instance.Difficulty:F3} | Gold: {RogueDifficultyManager.Instance.Gold}");
            }

            foreach (var item in items)
            { // ok
                item.Key.OnUpdate(item.Value);
            }

            ApplyPlayerChanges();
            ApplyWeaponSpeeds();
        }

        void SpawnEnemiesTest(int SpawnCredits)
        {
            SpawnCredits = Mathf.RoundToInt((float)SpawnCredits * RogueDifficultyManager.Instance.Difficulty);
            Logger.LogInfo($"Spawncredits: {SpawnCredits}, Difficulty: {RogueDifficultyManager.Instance.Difficulty}");
            if (SpawnCredits == 0) return;
            while (SpawnCredits > 0)
            {
                EnemyType randomEnemy = (EnemyType)Random.Range(0, System.Enum.GetValues(typeof(EnemyType)).Length);
                int Cost = RogueDifficultyManager.Instance.GetCost(randomEnemy);
                if (SpawnCredits - Cost < 0) continue;

                // Check how many we can spawn.
                int amountCanSpawn = Mathf.FloorToInt(SpawnCredits / Cost);
                int amountToSpawn = (int)Random.Range((int)1, (int)amountCanSpawn + 1);
                SpawnCredits -= amountToSpawn * Cost;
                // How many do we radiance
                int amountBeforeRadiance = RogueDifficultyManager.Instance.GetCountBeforeRadiance(randomEnemy);
                int amountRadiance = 0;
                if (amountToSpawn >= amountBeforeRadiance)
                {
                    amountRadiance = Mathf.FloorToInt((float)amountToSpawn / (float)amountBeforeRadiance);
                    // The amount we radiance we remove that amount from how much we spawn
                    // so for example we spawn 15 filth, 1 filth will be radiance and 5 filth will spawn normally
                    amountToSpawn -= amountRadiance * amountBeforeRadiance;
                    amountToSpawn += amountRadiance;
                }
                Logger.LogInfo($"We spawn {amountToSpawn} of {randomEnemy.ToString()} and {amountRadiance} will be radianced");
                for (int i = 0; i < amountToSpawn; i++)
                {
                    GameObject enemy = DefaultReferenceManager.Instance.GetEnemyPrefab(randomEnemy);
                    if (enemy == null) continue;
                    Transform randomSpawnPoint = NewMovement.Instance.transform;
                    GameObject inst = Instantiate(enemy, randomSpawnPoint.position, enemy.transform.rotation);
                    // when we need to radiance an enemy, we radiance them, and remove the amount of enemies we need to radiance
                    if (amountRadiance != 0)
                    {
                        inst.GetComponent<EnemyIdentifier>().BuffAll();
                        amountRadiance--;
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
            Dictionary<Weapon, DamageChange> damageChanges = new Dictionary<Weapon, DamageChange>();

            foreach (var changes in playerChanges)
            {
                moveChange.ApplyChangeToChange(changes.moveSpeed);

                jumpChange.ApplyChangeToChange(changes.jumpHeight);

                hpChange.ApplyChangeToChange(changes.maxHealth);

                atkSpeedChange.ApplyChangeToChange(changes.attackSpeed);

                cooldownChange.ApplyChangeToChange(changes.cooldownRed);

                globalDamageChange.ApplyChangeToChange(changes.globalDamageMult);

                foreach (var damageChange in changes.damageChanges)
                {
                    if (!damageChanges.ContainsKey(damageChange.WeaponType))
                        damageChanges.Add(damageChange.WeaponType, new DamageChange(damageChange.WeaponType, new Change()));

                    DamageChange dChange = damageChanges[damageChange.WeaponType];
                    dChange.damageChange.ApplyChangeToChange(damageChange.damageChange);
                }
            }

            NewMovement.Instance.walkSpeed = moveChange.CalculateChanges(normalMoveSpeed);
            NewMovement.Instance.jumpPower = jumpChange.CalculateChanges(normalJumpHeight);
            globalDamageMult = globalDamageChange;
            MaxHealth = Mathf.RoundToInt(hpChange.CalculateChanges(100f));
            AttackSpeed = atkSpeedChange;
            cooldownReduction = cooldownChange;
            foreach (var key in damageMultipliers.Keys.ToList())
                damageMultipliers[key] = new Change();

            foreach (var dChange in damageChanges)
            {
                damageMultipliers[dChange.Key] = dChange.Value.damageChange;
            }
        }

        public static DropTable testTable = new DropTable(new Dictionary<Rarity, float>()
        {
            {Rarity.Common, 0.80f },
            {Rarity.Uncommon, 0.15f },
            {Rarity.Legendary, 0.05f }
        });

        public static void AddWeapon(AWeapon weapon)
        {
            weapons.Add(weapon);
        }

        #region item helpers

        public static List<BaseItem> getRarityItems(Rarity rarity)
        {
            return possibleItems.Where((x) => x.Rarity == rarity).ToList();
        }

        public static Rarity getRarityBasedOnDropTable(DropTable table)
        {
            float chance = Random.value;
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

        public static BaseItem GiveRandomItem()
        {
            List<BaseItem> tiems = getRarityItems(getRarityBasedOnDropTable(testTable));
            return tiems[Random.Range(0, tiems.Count)];
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
        }

        public static void GiveItem(string name)
        {
            BaseItem itemToGive = getItem(name);
            GiveItem(itemToGive);
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

        void OnGUI()
        {
            if (NewMovement.Instance == null) return;

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
            if(RogueDifficultyManager.Instance != null)
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
        Legendary
    }

    public enum Team
    {
        Player,
        Enemies
    }

    public class TeamComponent : MonoBehaviour
    {
        public Team teamId = Team.Player;
    }
    public class AWeapon
    {
        public Plugin.Weapon weapon;
        public Plugin.Variant variant;
        public AWeapon(Plugin.Weapon weapon, Plugin.Variant variant)
        {
            this.weapon = weapon;
            this.variant = variant;
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
            if(!Plugin.isInRogueMode()) return;
            if (key.StartsWith("weapon."))
            {
                if (Plugin.weapons.Any((x) => key.StartsWith(x.ToString())))
                {
                    __result = 1;
                }
                else __result = 0;
            }
        }
    }

    [HarmonyPatch]
    public class PlayerPatches
    {
        [HarmonyPatch(typeof(HealthBar), nameof(HealthBar.Update))]
        [HarmonyPrefix]
        public static void DisplayCorrectMaxHP(HealthBar __instance)
        {
            if (__instance.hpSliders.Length != 0)
            {
                foreach (Slider slider in __instance.hpSliders)
                {
                    if (slider.gameObject.name.StartsWith("Supercharge"))
                    {
                        if (slider.maxValue != Plugin.MaxHealth * 2)
                        {
                            slider.maxValue = Plugin.MaxHealth * 2;
                            slider.minValue = Plugin.MaxHealth;
                        }
                    }
                    else
                    {
                        if (slider.maxValue != Plugin.MaxHealth)
                        {
                            slider.maxValue = Plugin.MaxHealth;
                        }
                    }
                    
                }
            }
            if (__instance.afterImageSliders != null)
            {
                foreach (Slider slider2 in __instance.afterImageSliders)
                {
                    if (slider2.maxValue != Plugin.MaxHealth)
                    {
                        slider2.maxValue = Plugin.MaxHealth;
                    }
                }
            }
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
                return cooldownReduction.CalculateChanges(maxDelta);
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
                return cooldownReduction.CalculateChanges(maxDelta);
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
            if(__instance.dead) return;

            TeamComponent tComp = __instance.GetComponent<TeamComponent>();

            if(tComp.teamId == Team.Enemies)
            {
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
                    Plugin.Logger.LogInfo($"Checking {deathEffect.itemName}....");
                    if (Plugin.GetItemCount(deathEffect.itemName) <= 0)
                    {
                        Plugin.Logger.LogInfo($"{deathEffect.itemName} has an itemcount of 0 skipping...");
                        continue;
                    }
                    deathEffect.effect.Invoke(__instance);
                }
            }



        }

        [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
        [HarmonyPrefix]
        public static void ActivateHitEffects(ref float multiplier, EnemyIdentifier __instance)
        {
            if (!Plugin.isInRogueMode()) return;
            if (__instance.dead) return;

            Weapon weaponUsed = Plugin.HitterToWeapon(__instance.hitter);
            if (Plugin.damageMultipliers.ContainsKey(weaponUsed))
                multiplier = Plugin.damageMultipliers[weaponUsed].CalculateChanges(multiplier);

            multiplier = Plugin.globalDamageMult.CalculateChanges(multiplier);

            foreach (var mod in Plugin.dmgModifiers)
            {
                float mult = mod.damageModifier(__instance);
                multiplier *= mult;
            }

            foreach (var hitEffect in Plugin.hitEffects)
            {
                Plugin.Logger.LogInfo($"Checking {hitEffect.itemName}....");
                if (Plugin.GetItemCount(hitEffect.itemName) <= 0)
                {
                    Plugin.Logger.LogInfo($"{hitEffect.itemName} has an itemcount of 0 skipping...");
                    continue;
                }
                hitEffect.effect.Invoke(__instance, multiplier);
            }
        }
    }

    [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.UpdateTarget))]
    public static class EnemyIdentifier_UpdateTarget_Patch
    {
        static bool Prefix(EnemyIdentifier __instance)
        {
            if (!Plugin.isInRogueMode()) return true;

            var myTeam = __instance.GetComponent<TeamComponent>();
            if (myTeam == null)
            {
                myTeam = __instance.gameObject.AddComponent<TeamComponent>();
                myTeam.teamId = Team.Enemies;
            }

            if (__instance.target != null && !__instance.target.isValid)
                __instance.target = null;

            if (__instance.dead) return false;

            if (myTeam.teamId == Team.Player)
                __instance.ignorePlayer = true;

            EnemyIdentifier bestTarget = null;
            float bestDist = float.MaxValue;

            foreach (var eid in MonoSingleton<EnemyTracker>.Instance.GetCurrentEnemies())
            {
                if (eid == __instance || eid.dead) continue;

                var theirTeam = eid.GetComponent<TeamComponent>();
                Team theirTeamId = theirTeam != null ? theirTeam.teamId : Team.Enemies;

                if (theirTeamId == myTeam.teamId) continue;

                float dist = Vector3.Distance(__instance.transform.position, eid.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = eid;
                }
            }

            if (bestTarget != null)
            {
                __instance.target = new EnemyTarget(bestTarget.transform);
                return false;
            }

            if (myTeam.teamId == Team.Player)
            {
                __instance.target = null;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(EnemyIdentifier), "Start")]
    public static class EnemyIdentifier_Awake_Patch
    {
        static void Postfix(EnemyIdentifier __instance)
        {
            if (!Plugin.isInRogueMode()) return;

            if (__instance.GetComponent<TeamComponent>() == null)
            {
                var team = __instance.gameObject.AddComponent<TeamComponent>();
                team.teamId = Team.Enemies;
            }
        }
    }
    #endregion


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

    public class DropTable
    {
        public Dictionary<Rarity, float> weights = new Dictionary<Rarity, float>();

        public DropTable(Dictionary<Rarity, float> weights)
        {
            this.weights = weights;
        }
    }

    public class PlayerChange
    {
        public Change moveSpeed;
        public Change jumpHeight;
        public Change maxHealth;
        public Change attackSpeed;
        public Change cooldownRed;
        public List<DamageChange> damageChanges;
        public Change globalDamageMult;

        public PlayerChange(Change moveSpeed = null, Change jumpHeight = null, Change maxHealth = null, Change attackSpeed = null, Change cooldownReduction = null, List<DamageChange> damageChanges = null, Change globalDamageMult = null)
        {
            if (moveSpeed == null) moveSpeed = new Change();
            if (jumpHeight == null) jumpHeight = new Change();
            if (damageChanges == null) damageChanges = new List<DamageChange>();
            if(globalDamageMult == null) globalDamageMult = new Change();
            if(maxHealth == null) maxHealth = new Change();
            if(attackSpeed == null) attackSpeed = new Change();
            if(cooldownReduction == null) cooldownReduction = new Change(); 

            this.moveSpeed = moveSpeed;
            this.jumpHeight = jumpHeight;
            this.damageChanges = damageChanges;
            this.globalDamageMult = globalDamageMult;
            this.maxHealth = maxHealth;
            this.attackSpeed = attackSpeed;
            this.cooldownRed = cooldownReduction;

            Plugin.playerChanges.Add(this);
            
        }
    }

    public class Change
    {
        public float addition;
        public float percentage;
        public float multiplier;

        public Change(float addition = 0, float percentage = 0, float multiplier = 1)
        {
            this.addition = addition;
            this.percentage = percentage;
            this.multiplier = multiplier;
        }

        public void ApplyChangeToChange(Change change)
        {
            this.addition += change.addition;
            this.percentage += change.percentage;
            this.multiplier *= change.multiplier;
        }

        public float CalculateChanges(float normalVal)
        {
            float fullPercentage = percentage + 1;
            float Val = normalVal + addition;

            Val *= fullPercentage;
            Val *= multiplier;
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
}

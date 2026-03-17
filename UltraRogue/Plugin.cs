using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ULTRAKILL.Enemy;
using Ultrarogue.Items;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Ultrarogue
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        internal static new ManualLogSource Logger { get; private set; } = null!;

        public static List<DeathEffect> deathEffects = new List<DeathEffect>();
        public static List<HitEffect> hitEffects = new List<HitEffect>();

        public static List<BaseItem> possibleItems = new List<BaseItem>();

        public static Dictionary<BaseItem, int> items = new Dictionary<BaseItem, int>();
        public static Dictionary<string, BaseItem> nameToItem = new Dictionary<string, BaseItem>();

        public static List<PlayerChange> playerChanges = new List<PlayerChange>();

        public static List<AWeapon> weapons = new List<AWeapon>();

        public enum Weapon
        {
            Revolver,
            Shotgun,
            Nailgun,
            Railcannon,
            RocketLauncher,
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
            }

            return "i dont fucking know????";
        }

        public static bool isInRogueMode()
        {
            return SceneHelper.CurrentScene == SceneLoader.SceneName; // Temporary
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
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (ShaderManager.shaderDictionary.Count <= 0) StartCoroutine(ShaderManager.LoadShadersAsync());
            if (NewMovement.Instance == null) return;
            normalMoveSpeed = NewMovement.Instance.walkSpeed;
            normalJumpHeight = NewMovement.Instance.jumpPower;
            weapons.Clear();
            weapons.Add(new AWeapon(Weapon.Revolver, Variant.Blue));
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                BaseItem item = GiveRandomItem();
                HudMessageReceiver.Instance.SendHudMessage(item.ToString());

                GiveItem(item);
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                StartCoroutine(SceneLoader.LoadLevelAsync(false)); 
            }

            foreach (var item in items)
            { // ok
                item.Key.OnUpdate(item.Value);
            }

            ApplyPlayerChanges();
        }

        void ApplyPlayerChanges()
        {
            if (NewMovement.Instance == null) return;
            Change moveChange = new Change();
            Change jumpChange = new Change();
            foreach (var changes in playerChanges)
            {
                moveChange.addition += changes.moveSpeed.addition;
                moveChange.percentage += changes.moveSpeed.percentage;
                moveChange.multiplier *= changes.moveSpeed.multiplier;

                jumpChange.addition += changes.jumpHeight.addition;
                jumpChange.percentage += changes.jumpHeight.percentage;
                jumpChange.multiplier *= changes.jumpHeight.multiplier;
            }

            NewMovement.Instance.walkSpeed = moveChange.CalculateChanges(normalMoveSpeed);
            NewMovement.Instance.jumpPower = jumpChange.CalculateChanges(normalJumpHeight);
        }

        public static DropTable testTable = new DropTable(new Dictionary<Rarity, float>()
        {
            {Rarity.Common, 0.80f },
            {Rarity.Uncommon, 0.15f },
            {Rarity.Legendary, 0.05f }
        });

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
        }
        static int luck = 0;

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
    public class EnemyPatches
    {
        [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Death), new System.Type[] { typeof(bool) })]
        [HarmonyPrefix]
        public static void ActivateDeathEffects(EnemyIdentifier __instance)
        {
            if (!Plugin.isInRogueMode()) return;
            if(__instance.dead) return;

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

        [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
        [HarmonyPostfix]
        public static void ActivateHitEffects(EnemyIdentifier __instance)
        {
            if (!Plugin.isInRogueMode()) return;
            if (__instance.dead) return;

            foreach (var hitEffect in Plugin.hitEffects)
            {
                Plugin.Logger.LogInfo($"Checking {hitEffect.itemName}....");
                if (Plugin.GetItemCount(hitEffect.itemName) <= 0)
                {
                    Plugin.Logger.LogInfo($"{hitEffect.itemName} has an itemcount of 0 skipping...");
                    continue;
                }
                hitEffect.effect.Invoke(__instance);
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

    public class HitEffect
    {
        public string itemName;
        public Action<EnemyIdentifier> effect;

        public HitEffect(string itemName, Action<EnemyIdentifier> effect)
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

        public PlayerChange(Change moveSpeed = null, Change jumpHeight = null)
        {
            if (moveSpeed == null) moveSpeed = new Change();
            if (jumpHeight == null) jumpHeight = new Change();

            this.moveSpeed = moveSpeed;
            this.jumpHeight = jumpHeight;

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

        public float CalculateChanges(float normalVal)
        {
            float fullPercentage = percentage + 1;
            float Val = normalVal + addition;

            Val *= fullPercentage;
            Val *= multiplier;
            return Val;
        }
    }

    #endregion
}

using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Ultrachaos.Randomizers
{
    [HarmonyPatch]
    public class EnemyRandomizer : Randomizer<SpawnableObject>
    {
        public static bool SeededRandom = true;

        public static EnemyRandomizer Instance = new EnemyRandomizer();
        static int PrevCount = 0;
        public static IEnumerator Init()
        {
            Plugin.Logger.LogInfo("Getting addressables!!!");
            yield return Instance.GetAllSpawnables();


            if (Instance.Pool.Count > PrevCount)
                CreateConfigs();
        }
        public static Dictionary<SpawnableObject, RandomConfig<bool>> CanUse = new Dictionary<SpawnableObject, RandomConfig<bool>>();
        static List<string> DefaultBlacklistedEnemies = new List<string>()
        {
            "Leviathan", "The Corpse of King Minos", "Earthmover"
        };
        
        public static void CreateConfigs()
        {
            foreach (var entry in Instance.Pool)
            {
                if (CanUse.ContainsKey(entry)) continue;

                Plugin.Logger.LogInfo("Creating config for " + entry.objectName);
                CanUse.Add(entry, new RandomConfig<bool>(Plugin.EnemyRNGList, entry.objectName, !DefaultBlacklistedEnemies.Contains(entry.objectName)));
            }
        }


        static void ApplyRequiredThings(EnemyIdentifier eid, EnemyIdentifier randomedEID)
        {
            randomedEID.dontCountAsKills = eid.dontCountAsKills;
            randomedEID.destroyOnDeath.AddRange(eid.destroyOnDeath);
            randomedEID.activateOnDeath = eid.activateOnDeath;
            randomedEID.sandified = eid.sandified;
            randomedEID.usingDoor = eid.usingDoor;
            randomedEID.madness = eid.madness;
            randomedEID.ignorePlayer = eid.ignorePlayer;
            randomedEID.ignoredByEnemies = eid.ignoredByEnemies;
            randomedEID.attackEnemies = eid.attackEnemies;
            if (Plugin.OriginalHealthEID.Value)
            {
                randomedEID.health = eid.health;
                Enemy e = FindEnemyComponent(eid.gameObject);
                Enemy eR = FindEnemyComponent(randomedEID.gameObject);

                if (Plugin.OriginalHealthEID.Value && e != null && eR != null)
                {
                    randomedEID.health = eid.health;

                    eR.health = e.health;
                    eR.originalHealth = e.originalHealth;
                }


            }
            if (eid.TryGetComponent<BossHealthBar>(out BossHealthBar bar))
            {
                Plugin.Logger.LogInfo($"Replacing name {bar.bossName}");
                BossHealthBar rBar = randomedEID.gameObject.AddComponent<BossHealthBar>();
                if (bar.bossName.ToLower().Contains("judge of hell"))
                {
                    rBar.bossName += ", JUDGE OF HELL";
                }
                else if (bar.bossName.ToLower().Contains("apostate of hate"))
                {
                    rBar.bossName += ", THE APOSTATE OF HATE";
                }
                else if (bar.bossName.ToLower().Contains("guardian of hell"))
                {
                    rBar.bossName += ", GUARDIAN OF HELL";
                }
                else if (bar.bossName.ToLower().Contains("prime"))
                {
                    rBar.bossName += " PRIME";
                }
                float healthDivision = randomedEID.health / eid.health;
                rBar.healthLayers = bar.healthLayers;

                if (rBar.healthLayers != null)
                {
                    foreach (var hpL in rBar.healthLayers)
                    {
                        if (!Plugin.OriginalHealthEID.Value)
                            hpL.health *= healthDivision;
                    }
                }

                rBar.secondaryBar = bar.secondaryBar;

                if (rBar.secondaryBar)
                {
                    rBar.secondaryBarColor = bar.secondaryBarColor;
                    rBar.secondaryBarValue = bar.secondaryBarValue;
                }
                    

            }


            UnityEvent eventtt = eid.onDeath;
            randomedEID.onDeath.AddListener(() =>
            {
                eventtt.Invoke();
            });

            bool flag = randomedEID.enemyType == EnemyType.Stalker;
            if (flag)
            {
                randomedEID.sandified = true;
            }

            Idol NIdol = eid.GetComponent<Idol>();
            Idol RIdol = randomedEID.GetComponent<Idol>();

            if (NIdol != null && RIdol != null)
            {
                RIdol.target = NIdol.target;
            }
            else if (NIdol != null && RIdol == null)
            {
                RIdol = randomedEID.gameObject.AddComponent<Idol>();
                RIdol.target = NIdol.target;
            }

            foreach (var item in Object.FindObjectsByType<Idol>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (item.target == eid)
                    item.target = randomedEID;
                if (item.overrideTarget == eid)
                    item.overrideTarget = randomedEID;
            }
        }
        private static Enemy FindEnemyComponent(GameObject obj)
        {
            if (obj == null) return null;

            // Try self
            Enemy e = obj.GetComponent<Enemy>();
            if (e != null) return e;

            // Try children
            e = obj.GetComponentInChildren<Enemy>(true);
            if (e != null) return e;

            // Try parent
            e = obj.GetComponentInParent<Enemy>();
            return e;
        }
        static bool alreadyAddressed = false;
        IEnumerator GetAllSpawnables()
        {
            if (alreadyAddressed) yield break;
            // Make sure addressables are initialized
            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            // Collect every resource location that resolves to a SpawnableObject
            var allLocations = new List<IResourceLocation>();

            foreach (var locator in Addressables.ResourceLocators)
            {
                // locator.Keys can contain strings, Guids, etc.
                var keys = locator.Keys.ToList();

                if (keys.Count == 0) continue;

                var locHandle = Addressables.LoadResourceLocationsAsync(
                    keys,
                    Addressables.MergeMode.Union,
                    typeof(SpawnableObject)
                );
                yield return locHandle;

                if (locHandle.Status == AsyncOperationStatus.Succeeded)
                    allLocations.AddRange(locHandle.Result);

                Addressables.Release(locHandle);
            }

            // Deduplicate (same asset can appear under multiple keys)
            allLocations = allLocations
                .GroupBy(l => l.InternalId)
                .Select(g => g.First())
                .ToList();

            Plugin.Logger.LogInfo($"Found {allLocations.Count} SpawnableObject locations");

            foreach (var location in allLocations)
            {
                var loadHandle = Addressables.LoadAssetAsync<SpawnableObject>(location);
                yield return loadHandle;

                if (loadHandle.Status == AsyncOperationStatus.Succeeded && loadHandle.Result.spawnableObjectType == SpawnableObject.SpawnableObjectDataType.Enemy)
                {
                    AddToPool(loadHandle.Result);
                }
            }
            alreadyAddressed = true;
        }


        private static bool _isSpawningReplacement = false;

        [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Awake))]
        [HarmonyPrefix]
        public static bool ReplaceEnemy(EnemyIdentifier __instance)
        {
            if (Plugin.ChangeEnemies.Value == RandomConfigValue.Disabled) return true;
            if (_isSpawningReplacement) return true;
            if (__instance.enemyType == EnemyType.Idol) return true;
            if (__instance.enemyType == EnemyType.Deathcatcher) return true;
            if (__instance.dead) return true;
            SpawnableObject obj = Instance.Pool.First((x) => x.enemyType == __instance.enemyType);

            List<SpawnableObject> pool = Instance.Pool.Where((x) => CanUse[x].Value).ToList();

            GameObject prefab = Instance.GetRandom(obj, pool).gameObject;

            _isSpawningReplacement = true;
            Vector3 pos = __instance.transform.position;
            if (__instance.enemyType == EnemyType.MaliciousFace)
                pos = __instance.transform.parent.position;
            GameObject instantiated = Object.Instantiate(prefab, pos, prefab.transform.rotation);
            Plugin.Logger.LogInfo($"Created {prefab.name} instead of {__instance.gameObject.name}, the place where prefab is is {instantiated.transform.position} and the original is at {__instance.transform.position}");
            _isSpawningReplacement = false;
            if (__instance.enemyType != EnemyType.MaliciousFace)
                instantiated.transform.parent = __instance.transform.parent;
            else
                instantiated.transform.parent = __instance.transform.parent.parent;

            EnemyIdentifier eid = instantiated.GetComponent<EnemyIdentifier>();
            if (eid == null)
            {
                eid = instantiated.GetComponentInChildren<EnemyIdentifier>();
            }
            if (eid == null)
            {
                Object.Destroy(instantiated);
                return true;
            }
            ActivateArena[] allArenas = Object.FindObjectsByType<ActivateArena>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var arena in allArenas)
            {
                for (int i = 0; i < arena.enemies.Length; i++)
                {
                    if (arena.enemies[i] == eid.gameObject)
                        arena.enemies[i] = instantiated;
                }
            }

            ApplyRequiredThings(__instance, eid);
            Object.Destroy(__instance.gameObject);
            return false;
        }

        protected override int GetInstanceID(SpawnableObject item)
        {
            return item.GetInstanceID();
        }

        protected override RandomConfigValue GetConfigValue()
        {
            return Plugin.ChangeEnemies.Value;
        }
    }
}
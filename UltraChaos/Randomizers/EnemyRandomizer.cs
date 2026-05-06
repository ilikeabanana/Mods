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
using UnityEngine.SceneManagement;
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

        static bool _sceneListenerRegistered = false;

        public static IEnumerator Init()
        {
            _replacementInstanceIds.Clear();
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

        protected override void Log(string message) => Plugin.Logger.LogInfo(message);

        private static string CleanGoName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            int paren = name.IndexOf('(');
            if (paren > 0) name = name.Substring(0, paren);
            return name.Trim().ToLowerInvariant();
        }

        private static int GetNameSimilarity(string spawnableName, string goName)
        {
            if (string.IsNullOrEmpty(spawnableName) || string.IsNullOrEmpty(goName))
                return 0;

            string a = spawnableName.ToLowerInvariant().Trim();
            string b = CleanGoName(goName);

            if (a == b) return int.MaxValue;
            if (b.Contains(a)) return a.Length * 2;

            string[] words = a.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            int score = 0;
            foreach (string word in words)
                if (b.Contains(word)) score++;
            return score;
        }

        private static SpawnableObject FindBestMatch(EnemyIdentifier eid)
        {
            string goName = eid.gameObject.name;

            var candidates = Instance.Pool
                .Where(x => x.enemyType == eid.enemyType)
                .Select(x => (spawnable: x, score: GetNameSimilarity(x.objectName, goName)))
                .OrderByDescending(t => t.score)
                .ToList();

            if (candidates.Count == 0)
                return null;

            string best = $"{candidates[0].spawnable.objectName} (score {candidates[0].score})";
            string runnerUp = candidates.Count > 1
                ? $", runner-up: {candidates[1].spawnable.objectName} (score {candidates[1].score})"
                : string.Empty;
            Plugin.Logger.LogInfo($"FindBestMatch '{goName}' -> {best}{runnerUp}");

            return candidates[0].spawnable;
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

                if (e != null && eR != null)
                {
                    eR.health = e.health;
                    eR.originalHealth = e.originalHealth;
                }
            }
            if (eid.TryGetComponent<BossHealthBar>(out BossHealthBar bar))
            {
                Plugin.Logger.LogInfo($"Replacing name {bar.bossName}");
                BossHealthBar rBar = randomedEID.gameObject.AddComponent<BossHealthBar>();
                if (bar.bossName.ToLower().Contains("judge of hell"))
                    rBar.bossName += ", JUDGE OF HELL";
                else if (bar.bossName.ToLower().Contains("apostate of hate"))
                    rBar.bossName += ", THE APOSTATE OF HATE";
                else if (bar.bossName.ToLower().Contains("guardian of hell"))
                    rBar.bossName += ", GUARDIAN OF HELL";
                else if (bar.bossName.ToLower().Contains("prime"))
                    rBar.bossName += " PRIME";

                float healthDivision = randomedEID.health / eid.health;
                rBar.healthLayers = bar.healthLayers;
                if (rBar.healthLayers != null)
                    foreach (var hpL in rBar.healthLayers)
                        if (!Plugin.OriginalHealthEID.Value)
                            hpL.health *= healthDivision;

                rBar.secondaryBar = bar.secondaryBar;
                if (rBar.secondaryBar)
                {
                    rBar.secondaryBarColor = bar.secondaryBarColor;
                    rBar.secondaryBarValue = bar.secondaryBarValue;
                }
            }

            Enemy eee = FindEnemyComponent(eid.gameObject);
            Enemy eeer = FindEnemyComponent(randomedEID.gameObject);
            if(eee && eeer)
                eeer.dontDie = eee.dontDie;

            UnityEvent eventtt = eid.onDeath;
            randomedEID.onDeath.AddListener(() => eventtt.Invoke());

            if (randomedEID.enemyType == EnemyType.Stalker)
                randomedEID.sandified = true;

            Idol NIdol = eid.GetComponent<Idol>();
            Idol RIdol = randomedEID.GetComponent<Idol>();

            if (NIdol != null && RIdol != null)
                RIdol.target = NIdol.target;
            else if (NIdol != null && RIdol == null)
            {
                RIdol = randomedEID.gameObject.AddComponent<Idol>();
                RIdol.target = NIdol.target;
            }

            foreach (var item in Object.FindObjectsByType<Idol>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (item.target == eid) item.target = randomedEID;
                if (item.overrideTarget == eid) item.overrideTarget = randomedEID;
            }
        }

        private static Enemy FindEnemyComponent(GameObject obj)
        {
            if (obj == null) return null;
            Enemy e = obj.GetComponent<Enemy>();
            if (e != null) return e;
            e = obj.GetComponentInChildren<Enemy>(true);
            if (e != null) return e;
            return obj.GetComponentInParent<Enemy>();
        }

        static bool alreadyAddressed = false;
        IEnumerator GetAllSpawnables()
        {
            if (alreadyAddressed) yield break;
            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            var allLocations = new List<IResourceLocation>();
            foreach (var locator in Addressables.ResourceLocators)
            {
                var keys = locator.Keys.ToList();
                if (keys.Count == 0) continue;

                var locHandle = Addressables.LoadResourceLocationsAsync(
                    keys, Addressables.MergeMode.Union, typeof(SpawnableObject));
                yield return locHandle;

                if (locHandle.Status == AsyncOperationStatus.Succeeded)
                    allLocations.AddRange(locHandle.Result);
                Addressables.Release(locHandle);
            }

            allLocations = allLocations
                .GroupBy(l => l.InternalId)
                .Select(g => g.First())
                .ToList();

            Plugin.Logger.LogInfo($"Found {allLocations.Count} SpawnableObject locations");

            foreach (var location in allLocations)
            {
                var loadHandle = Addressables.LoadAssetAsync<SpawnableObject>(location);
                yield return loadHandle;

                if (loadHandle.Status == AsyncOperationStatus.Succeeded &&
                    loadHandle.Result.spawnableObjectType == SpawnableObject.SpawnableObjectDataType.Enemy)
                    AddToPool(loadHandle.Result);
            }
            alreadyAddressed = true;
        }


        private static readonly HashSet<int> _replacementInstanceIds = new HashSet<int>();
        private static bool _isSpawningReplacement = false;


        [HarmonyPatch(typeof(EnemySpawnRadius), "SpawnEnemy")]
        internal class SpawnEnemiesRandom
        {
            static bool Prefix(
                EnemySpawnRadius __instance,
                ref List<GameObject> ___spawnedObjects,
                ref List<EnemyIdentifier> ___currentEnemies,
                ref float ___cooldown,
                ref GoreZone ___gz)
            {
                if (Plugin.ChangeEnemies.Value == RandomConfigValue.Disabled)
                    return true;

                GameObject originalGO = __instance.spawnables[UnityEngine.Random.Range(0, __instance.spawnables.Length)];

                EnemyIdentifier originalEID =
                    originalGO.GetComponent<EnemyIdentifier>() ??
                    originalGO.GetComponentInChildren<EnemyIdentifier>();
                if (originalEID == null)
                {
                    Plugin.Logger.LogWarning($"Spawnable '{originalGO.name}' has no EnemyIdentifier.");
                    return true;
                }

                SpawnableObject original = FindBestMatch(originalEID);

                if (original == null)
                {
                    Plugin.Logger.LogWarning($"No match for spawner '{__instance.gameObject.name}'");
                    return true;
                }
                List<SpawnableObject> pool = Instance.Pool
                    .Where(x => CanUse.ContainsKey(x) && CanUse[x].Value)
                    .ToList();

                if (pool.Count == 0)
                    return true;

                // Get randomized enemy
                SpawnableObject chosen = Instance.GetRandom(original, pool);

                Vector3 normalized = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    0f,
                    UnityEngine.Random.Range(-1f, 1f)
                ).normalized;

                if (Physics.Raycast(
                    __instance.transform.position + normalized * UnityEngine.Random.Range(__instance.minimumDistance, __instance.maximumDistance),
                    Vector3.down,
                    out RaycastHit hit,
                    25f,
                    LayerMaskDefaults.Get(LMD.Environment)))
                {
                    ___cooldown = __instance.spawnCooldown;

                    GameObject go = UnityEngine.Object.Instantiate(
                        chosen.gameObject,
                        hit.point,
                        Quaternion.identity
                    );

                    _replacementInstanceIds.Add(go.GetInstanceID());

                    go.transform.SetParent(___gz.transform, true);
                    ___spawnedObjects.Add(go);

                    EnemyIdentifier eid = go.GetComponentInChildren<EnemyIdentifier>();
                    if (eid != null)
                    {
                        ___currentEnemies.Add(eid);

                        if (__instance.spawnAsPuppets)
                            eid.puppet = true;
                    }
                    else
                    {
                        ___currentEnemies.Add(null);
                    }

                    go.SetActive(true);
                    return false;
                }

                ___cooldown = 1f;
                return false;
            }
        }


        [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Start))]
        [HarmonyPrefix]
        public static bool ReplaceEnemy(EnemyIdentifier __instance)
        {
            if (Plugin.ChangeEnemies.Value == RandomConfigValue.Disabled) return true;

            if (__instance.transform.parent != null &&
                __instance.transform.parent.gameObject.name == "4 - Swordsmachine Hallway") return true;

            if (_replacementInstanceIds.Contains(__instance.gameObject.GetInstanceID())) return true;
            if (__instance.enemyType == EnemyType.Idol) return true;
            if (__instance.enemyType == EnemyType.Deathcatcher) return true;
            if (__instance.dead) return true;
            if (!__instance.enabled) return true;

            SpawnableObject obj = FindBestMatch(__instance);
            if (obj == null)
            {
                Plugin.Logger.LogWarning($"No matching SpawnableObject for '{__instance.gameObject.name}' (type: {__instance.enemyType}), skipping.");
                return true;
            }

            List<SpawnableObject> pool = Instance.Pool
                .Where(x => CanUse.ContainsKey(x) && CanUse[x].Value)
                .ToList();

            if (pool.Count == 0) return true;

            SpawnableObject chosen = Instance.GetRandom(obj, pool);
            if (chosen == null) return true;

            GameObject prefab = chosen.gameObject;

            Vector3 pos = __instance.transform.position;
            if (__instance.enemyType == EnemyType.MaliciousFace)
                pos = __instance.transform.parent.position;

            GameObject instantiated = Object.Instantiate(prefab, pos, prefab.transform.rotation);

            _replacementInstanceIds.Add(instantiated.GetInstanceID());

            Plugin.Logger.LogInfo($"Spawned {prefab.name} instead of {__instance.gameObject.name}");
            _isSpawningReplacement = false;

            instantiated.transform.parent = __instance.enemyType != EnemyType.MaliciousFace
                ? __instance.transform.parent
                : __instance.transform.parent.parent;

            EnemyIdentifier eid = instantiated.GetComponent<EnemyIdentifier>()
                ?? instantiated.GetComponentInChildren<EnemyIdentifier>();
            if (eid == null)
            {
                Object.Destroy(instantiated);
                return true;
            }

            foreach (var arena in Object.FindObjectsByType<ActivateArena>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                for (int i = 0; i < arena.enemies.Length; i++)
                    if (arena.enemies[i] == __instance.gameObject)
                        arena.enemies[i] = instantiated;

            ApplyRequiredThings(__instance, eid);
            Object.Destroy(__instance.gameObject);
            return false;
        }

        protected override int GetInstanceID(SpawnableObject item) => item.GetInstanceID();
        protected override RandomConfigValue GetConfigValue() => Plugin.ChangeEnemies.Value;
    }
    public class RandomizerSpawnedEnemy : MonoBehaviour { }

}
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Ultrarogue;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AssetsManager
{
    public static List<WeaponDescriptor> descriptors = new List<WeaponDescriptor>();
    public static Material weaponMat;

    public static GameObject Agony;
    public static GameObject Tundra;
    public static GameObject MirrorReaper;

    public static GameObject VirtueBeam;

    // Enemies grouped by their EnemyType enum
    public static Dictionary<EnemyType, List<SpawnableObject>> enemiesByType
        = new Dictionary<EnemyType, List<SpawnableObject>>();

    // Tracks how many of our two load coroutines have finished
    private static int _loadsDone = 0;
    private const int TotalLoads = 2;

    public static bool IsReady { get; private set; } = false;

    public static GameObject napalmProj;
    public static GameObject mindflayerBeam;
    public static GameObject funnyPowerIntroSpawn;
    public static GameObject zapThingy;
    public static GameObject spawnEffect;
    public static AudioClip StalkerWarning;

    public static Sprite ArmFeedbacker;
    public static Sprite ArmKnuckleBlaster;
    public static Sprite ArmWhiplash;

    public static void Init()
    {
        Plugin.Instance.StartCoroutine(GetAllSpawnables());
        Plugin.Instance.StartCoroutine(GetAllEnemies());

        if (napalmProj == null)
            napalmProj = Addressables
                .LoadAssetAsync<GameObject>(
                    "Assets/Prefabs/Attacks and Projectiles/GasolineProjectile.prefab")
                .WaitForCompletion();

        if (VirtueBeam == null)
            VirtueBeam = Addressables
                .LoadAssetAsync<GameObject>(
                    "Virtue Insignia")
                .WaitForCompletion();

        if (Agony == null)
            Agony = Addressables
                .LoadAssetAsync<GameObject>(
                    "Assets/Prefabs/Enemies/SwordsMachine Agony.prefab")
                .WaitForCompletion();

        if (funnyPowerIntroSpawn == null)
            funnyPowerIntroSpawn = Addressables
                .LoadAssetAsync<EndlessEnemy>(
                    "Assets/Data/Cyber Grind Patterns/Data/PowerEndlessData.asset")
                .WaitForCompletion().prefab;

        if (Tundra == null)
            Tundra = Addressables
                .LoadAssetAsync<GameObject>(
                    "Assets/Prefabs/Enemies/SwordsMachine Tundra.prefab")
                .WaitForCompletion();

        if (zapThingy == null)
            zapThingy = Addressables
                .LoadAssetAsync<GameObject>(
                    "Assets/Particles/HitSparkElectricity.prefab")
                .WaitForCompletion();

        if (spawnEffect == null)
            spawnEffect = Addressables
                .LoadAssetAsync<GameObject>(
                    "Assets/Particles/Spawn Effects/SpawnEffect Melee Hard.prefab")
                .WaitForCompletion();

        if (mindflayerBeam == null)
            mindflayerBeam = Addressables
                .LoadAssetAsync<GameObject>(
                    "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Mindflayer Beam.prefab")
                .WaitForCompletion();

        if (StalkerWarning == null)
            StalkerWarning = Addressables
                .LoadAssetAsync<AudioClip>(
                    "Assets/Sounds/Enemies/StalkerWarning.wav")
                .WaitForCompletion();

        if (ArmFeedbacker == null)
            ArmFeedbacker = Addressables
                .LoadAssetAsync<Sprite>(
                    "Assets/Textures/UI/ArmFeedbacker.png")
                .WaitForCompletion();

        if (ArmKnuckleBlaster == null)
            ArmKnuckleBlaster = Addressables
                .LoadAssetAsync<Sprite>(
                    "Assets/Textures/UI/ArmKnuckleblaster.png")
                .WaitForCompletion();

        if (ArmWhiplash == null)
            ArmWhiplash = Addressables
                .LoadAssetAsync<Sprite>(
                    "Assets/Textures/UI/ArmWhiplash.png")
                .WaitForCompletion();

    }

    // ── Signals one coroutine finished; flips IsReady when both are done ────
    private static void OnLoadComplete()
    {
        _loadsDone++;
        if (_loadsDone >= TotalLoads)
        {
            IsReady = true;
            Plugin.Logger.LogInfo(
                $"AssetsManager ready — {descriptors.Count} weapon descriptors, " +
                $"{enemiesByType.Values.Sum(l => l.Count)} enemies across " +
                $"{enemiesByType.Count} type(s) loaded.");
        }
    }

    // ── Weapon descriptors (unchanged logic, just calls OnLoadComplete) ──────
    static IEnumerator GetAllSpawnables()
    {
        var initHandle = Addressables.InitializeAsync();
        yield return initHandle;

        var allLocations = new List<IResourceLocation>();

        foreach (var locator in Addressables.ResourceLocators)
        {
            var keys = locator.Keys.ToList();
            if (keys.Count == 0) continue;

            var locHandle = Addressables.LoadResourceLocationsAsync(
                keys, Addressables.MergeMode.Union, typeof(WeaponDescriptor));
            yield return locHandle;

            if (locHandle.Status == AsyncOperationStatus.Succeeded)
                allLocations.AddRange(locHandle.Result);

            Addressables.Release(locHandle);
        }

        allLocations = allLocations
            .GroupBy(l => l.InternalId)
            .Select(g => g.First())
            .ToList();

        Plugin.Logger.LogInfo($"Found {allLocations.Count} WeaponDescriptor locations");

        foreach (var location in allLocations)
        {
            var loadHandle = Addressables.LoadAssetAsync<WeaponDescriptor>(location);
            yield return loadHandle;

            if (loadHandle.Status == AsyncOperationStatus.Succeeded)
                descriptors.Add(loadHandle.Result);
        }

        OnLoadComplete();
    }

    // ── Enemy SpawnableObjects, grouped by EnemyType ─────────────────────────
    static IEnumerator GetAllEnemies()
    {
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

        // Deduplicate by internal ID, same as weapon descriptors
        allLocations = allLocations
            .GroupBy(l => l.InternalId)
            .Select(g => g.First())
            .ToList();

        Plugin.Logger.LogInfo($"Found {allLocations.Count} SpawnableObject locations");

        int enemyCount = 0;

        foreach (var location in allLocations)
        {
            var loadHandle = Addressables.LoadAssetAsync<SpawnableObject>(location);
            yield return loadHandle;

            if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                continue;

            SpawnableObject obj = loadHandle.Result;

            if (obj.spawnableObjectType != SpawnableObject.SpawnableObjectDataType.Enemy)
                continue;

            if (!enemiesByType.ContainsKey(obj.enemyType))
                enemiesByType[obj.enemyType] = new List<SpawnableObject>();

            enemiesByType[obj.enemyType].Add(obj);
            enemyCount++;
        }

        Plugin.Logger.LogInfo(
            $"Loaded {enemyCount} enemy SpawnableObjects across {enemiesByType.Count} type(s).");

        foreach (var kvp in enemiesByType)
            Plugin.Logger.LogInfo($"  {kvp.Key}: {kvp.Value.Count} variant(s)");

        OnLoadComplete();
    }

    // ── Weapon lookup (unchanged) ─────────────────────────────────────────────
    public static Sprite prefToDescriptor(string pref, bool alternate)
    {
        if (!IsReady)
        {
            Plugin.Logger.LogWarning(
                $"prefToDescriptor called before assets finished loading! pref={pref}");
            return null;
        }

        IEnumerable<WeaponDescriptor> pool;
        char variant = pref.Last();

        if (pref.Contains("rev"))
        {
            pool = descriptors.Where(x => x.weaponName.StartsWith(
                !alternate ? "Revolver" : "Alternative Revolver"));
            if (variant == '0') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Pierce")).icon;
            if (variant == '1') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Twirl")).icon;
            if (variant == '2') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Ricochet")).icon;
        }
        else if (pref.Contains("sho"))
        {
            pool = descriptors.Where(x => x.weaponName.StartsWith(
                !alternate ? "Shotgun" : "Hammer"));
            if (variant == '0') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Grenade")).icon;
            if (variant == '1') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Pump")).icon;
            if (variant == '2') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Saw")).icon;
        }
        else if (pref.Contains("nai"))
        {
            pool = descriptors.Where(x => x.weaponName.StartsWith(
                !alternate ? "Nailgun" : "Sawblade Launcher"));
            if (variant == '0') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Magnet")).icon;
            if (variant == '1') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Overheat")).icon;
            if (variant == '2') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Jumpstart")).icon;
        }
        else if (pref.Contains("rai"))
        {
            pool = descriptors.Where(x => x.weaponName.StartsWith("Railcannon"));
            if (variant == '0') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Electric")).icon;
            if (variant == '1') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Harpoon")).icon;
            if (variant == '2') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Malicious")).icon;
        }
        else if (pref.Contains("rock"))
        {
            pool = descriptors.Where(x => x.weaponName.StartsWith("Rocket Launcher"));
            if (variant == '0') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Freeze")).icon;
            if (variant == '1') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Cannonball")).icon;
            if (variant == '2') return pool.FirstOrDefault(x => x.weaponName.EndsWith("Napalm")).icon;
        } else if (pref.Contains("arm"))
        {
            if (variant == '0') return ArmFeedbacker;
            if (variant == '1') return ArmKnuckleBlaster;
            if (variant == '2') return ArmWhiplash;
        }

        Plugin.Logger.LogWarning($"prefToDescriptor: no match for pref='{pref}'");
        return descriptors.Find(x => x.weaponName == "UNKNOWN").icon;
    }

    // ── Convenience: get all enemies of a specific type ──────────────────────
    public static List<SpawnableObject> GetEnemiesOfType(EnemyType type)
    {
        if (!IsReady)
        {
            Plugin.Logger.LogWarning("GetEnemiesOfType called before assets finished loading!");
            return null;
        }

        return enemiesByType.TryGetValue(type, out var list) ? list : new List<SpawnableObject>();
    }
}
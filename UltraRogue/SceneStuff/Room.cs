using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ULTRAKILL.Portal;
using ULTRAKILL.Portal.Geometry;
using Ultrarogue;
using Ultrarogue.Characters;
using Ultrarogue.Curses;
using Ultrarogue.Items;
using Ultrarogue.SceneStuff;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using static Ultrarogue.Plugin;
using Random = UnityEngine.Random;

public enum RoomType
{
    Normal,
    Start,
    Boss,
    Planetarium,
    Treasure,
    Shop,
    Gambling,
    Secret,
    OtherSpecialRoom
}

public class Room : MonoBehaviour
{
    public Vector2Int position;
    public float spawnChance;

    [Header("Multi-Tile Exit Arrays")]
    [Tooltip("Size must equal RoomSizeHeight")] public Transform[] exitsLeft;
    [Tooltip("Size must equal RoomSizeHeight")] public Transform[] exitsRight;
    [Tooltip("Size must equal RoomSizeWidth")] public Transform[] exitsTop;
    [Tooltip("Size must equal RoomSizeWidth")] public Transform[] exitsBottom;

    [Header("Legacy Single Exits (Fallback)")]
    public Transform exitLeft;
    public Transform exitRight;
    public Transform exitTop;
    public Transform exitBottom;

    public int SpawnCredits = 0;

    public List<Transform> spawnPoints = new List<Transform>();

    public RoomType roomType = RoomType.Normal;

    public static int roomIndex;
    System.Random enemyRando;
    public bool isBossRoom => roomType == RoomType.Boss;

    public BossPick bossEnemyType;

    public GameObject doorPrefab;
    public GameObject wallPrefab;

    private readonly List<GameObject> _boundaryObstacles = new List<GameObject>();
    private float _obstacleCheckTimer = 0f;
    private const float ObstacleCheckInterval = 0.2f;
    private bool hasSpawnedEnemies = false;
    private bool rewardGiven = false;

    [Tooltip("Enemy that will activate the object instead of itself (default to wicked for nothing)")]
    public EnemyType ReplacerType = EnemyType.Wicked;
    [Tooltip("The objects that will be activated, will pick a random one in the list each time and will remove that one from the list once chosen. If none are left, it will just spawn the enemy like normal")]
    public List<GameObject> allObjectActivators = new List<GameObject>();

    public static bool isFighting = false;

    [Header("Room sizes")]
    public int RoomSizeWidth = 1;
    public int RoomSizeHeight = 1;

    public bool TriggerSoftlockCheck = true;

    /// <summary>
    /// Dynamically fetches the correct exit transform based on which grid cell is being checked.
    /// </summary>
    public Transform GetExit(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
            return (exitsTop != null && exitsTop.Length > 0) ? exitsTop[0] : exitTop;
        if (direction == Vector2Int.down)
            return (exitsBottom != null && exitsBottom.Length > 0) ? exitsBottom[0] : exitBottom;
        if (direction == Vector2Int.left)
            return (exitsLeft != null && exitsLeft.Length > 0) ? exitsLeft[0] : exitLeft;
        if (direction == Vector2Int.right)
            return (exitsRight != null && exitsRight.Length > 0) ? exitsRight[0] : exitRight;
        return null;
    }



    private void SpawnBoundaryObstacles()
    {
        // Room half-extents (matching the gizmo: Width*60 x Height*30)
        float halfW = RoomSizeWidth * 30f;   // half-width  on X
        float halfH = RoomSizeHeight * 15f;   // half-depth  on Z

        // How far outside the room edge each obstacle is placed (centre of obstacle)
        const float offset = 5f;
        // Thickness of the obstacle slab
        const float thickness = 4f;
        // Tall enough to block navmesh agents in any vertical situation
        const float height = 20f;

        // (localPos, size) pairs for Left / Right / Front / Back
        var walls = new (Vector3 localPos, Vector3 size)[]
        {
            // Left  (-X)
            (new Vector3(-(halfW + offset),       0f, (halfH - halfH) * 0.5f), new Vector3(thickness, height, halfH * 2f * RoomSizeHeight)),
            // Right (+X)
            (new Vector3( (halfW + offset),       0f, (halfH - halfH) * 0.5f), new Vector3(thickness, height, halfH * 2f * RoomSizeHeight)),
            // Front (-Z)
            (new Vector3(0f, 0f, -(halfH + offset)), new Vector3(halfW * 2f * RoomSizeWidth, height, thickness)),
            // Back  (+Z)
            (new Vector3(0f, 0f,  (halfH + offset)), new Vector3(halfW * 2f * RoomSizeWidth, height, thickness)),
        };

        string[] names = { "BoundaryObstacle_Left", "BoundaryObstacle_Right", "BoundaryObstacle_Front", "BoundaryObstacle_Back" };

        for (int i = 0; i < walls.Length; i++)
        {
            GameObject obs = new GameObject(names[i]);
            obs.transform.SetParent(transform, false);
            obs.transform.localPosition = walls[i].localPos;

            NavMeshObstacle obstacle = obs.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = walls[i].size;
            obstacle.center = Vector3.zero;

            obs.SetActive(false);
            _boundaryObstacles.Add(obs);
        }
    }

    void OnGizmosDraw()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(RoomSizeWidth * 60f, 100f, RoomSizeHeight * 30f));
    }

    static bool HasAnyWeaponsThatCanBreakThroughGlass()
    {
        foreach (var weapon in Plugin.weapons)
        {
            Plugin.Logger.LogInfo("User has: " + weapon.ToString());
        }

        return Plugin.weapons.Any(w =>
            (w.weapon == Weapon.Revolver && (w.variant == Variant.Blue || w.variant == Variant.Green)) ||
            (w.weapon == Weapon.Shotgun && (w.variant == Variant.Blue || w.variant == Variant.Green)) ||
            (w.weapon == Weapon.Railcannon && (w.variant == Variant.Blue || w.variant == Variant.Red)) ||
            (w.weapon == Weapon.RocketLauncher) ||
            (w.weapon == Weapon.Arm && w.variant == Variant.Green)
        );
    }

    public void OnRoomEnter()
    {
        foreach (var item in Plugin.items)
        { // ok
            item.Key.RoomEnter();
        }
        switch (roomType)
        {
            case RoomType.Boss:
                StartCoroutine(SpawnBoss());
                break;

            case RoomType.Normal:
                int c = Plugin.GetItemCount("Dual Gun");
                if (c > 0)
                    if (Plugin.canExecute(Plugin.LogarithmicChance(c - 1, 0.15f, 0.25f, 0.9f) * 100, ""))
                    {
                        MonoSingleton<CameraController>.Instance.CameraShake(0.35f);
                        if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.Platformer)
                        {
                            MonoSingleton<PlatformerMovement>.Instance.AddExtraHit(3);
                            return;
                        }
                        GameObject gameObject = new GameObject();
                        gameObject.transform.SetParent(MonoSingleton<GunControl>.Instance.transform, true);
                        gameObject.transform.localRotation = Quaternion.identity;
                        DualWield[] componentsInChildren = MonoSingleton<GunControl>.Instance.GetComponentsInChildren<DualWield>();
                        if (componentsInChildren != null && componentsInChildren.Length % 2 == 0)
                        {
                            gameObject.transform.localScale = new Vector3(-1f, 1f, 1f);
                        }
                        else
                        {
                            gameObject.transform.localScale = Vector3.one;
                        }
                        if (componentsInChildren == null || componentsInChildren.Length == 0)
                        {
                            gameObject.transform.localPosition = Vector3.zero;
                        }
                        else if (componentsInChildren.Length % 2 == 0)
                        {
                            gameObject.transform.localPosition = new Vector3((float)(componentsInChildren.Length / 2) * -1.5f, 0f, 0f);
                        }
                        else
                        {
                            gameObject.transform.localPosition = new Vector3((float)((componentsInChildren.Length + 1) / 2) * 1.5f, 0f, 0f);
                        }
                        DualWield dualWield = gameObject.AddComponent<DualWield>();
                        dualWield.delay = 0.05f;
                        dualWield.juiceAmount = 30f;
                        if (componentsInChildren != null && componentsInChildren.Length != 0)
                        {
                            dualWield.delay += (float)componentsInChildren.Length / 20f;
                        }
                    }
                StartCoroutine(SpawnEnemies());
                break;

            case RoomType.Treasure:
                RoomGenerator.Instance.planetChance -= 0.2f;
                break;
            case RoomType.Shop:
            case RoomType.Gambling:
            case RoomType.Start:
            default:
                break;
        }
        if (TriggerSoftlockCheck)
        {
            if (!HasAnyWeaponsThatCanBreakThroughGlass())
            {
                Glass[] allGlass = gameObject.GetComponentsInChildren<Glass>();

                foreach (var glass in allGlass)
                {
                    glass.Shatter();
                }
            }
        }

    }

    private int playerHealthAtFightStart = -1;

    private readonly struct PlannedSpawn
    {
        public readonly EnemyType type;
        public readonly int radianceBuffs;
        public PlannedSpawn(EnemyType type, int radianceBuffs)
        {
            this.type = type;
            this.radianceBuffs = radianceBuffs;
        }
    }

    private const int WaveThreshold = 28;
    private const int WaveSize = 12;
    private const int WaveResumeBelow = 8;
    private const float WavePollRate = 0.5f;

    private readonly List<GameObject> _pendingActivators = new List<GameObject>();
    private bool _activatorsWereUsed = false;
    private float _activatorSettleUntil = 0f;
    private const float ActivatorSettleTime = 6f;

    IEnumerator SpawnEnemies()
    {
        if (SpawnCredits == 0) yield break;
        CloseOffRoom();
        playerHealthAtFightStart = MonoSingleton<NewMovement>.Instance.hp;
        SpawnCredits = Mathf.RoundToInt((float)SpawnCredits * RogueDifficultyManager.Instance.Difficulty);
        SpawnCredits = Mathf.Max(SpawnCredits, 3);
        Plugin.Logger.LogInfo($"Room has {SpawnCredits} spawn credits because difficulty is {RogueDifficultyManager.Instance.Difficulty}");
        isFighting = true;

        var enemyCounts = new Dictionary<EnemyType, int>();

        if (CurseManager.HasCurse("Curse of The Champion"))
        {
            // Build a list of all spawnable enemy types sorted most expensive -> cheapest.
            // Apply curse remapping first, deduplicate, then filter & sort.
            var allTypes = System.Enum.GetValues(typeof(EnemyType))
                .Cast<EnemyType>()
                .Where(t => RogueDifficultyManager.Instance.CanSpawn(t))
                .Select(t => CurseManager.getCursedEnemy(t))
                .Distinct()
                .Where(t => RogueDifficultyManager.Instance.CanSpawn(t))
                .OrderByDescending(t => RogueDifficultyManager.Instance.GetCost(t))
                .ToList();

            // Walk the sorted list, spending as many credits as possible on each type in order.
            foreach (EnemyType enemyType in allTypes)
            {
                if (SpawnCredits <= 0) break;

                int cost = RogueDifficultyManager.Instance.GetCost(enemyType);
                if (cost <= 0 || SpawnCredits < cost) continue;

                int amountToSpawn = Mathf.FloorToInt(SpawnCredits / cost);
                SpawnCredits -= amountToSpawn * cost;

                if (!enemyCounts.ContainsKey(enemyType))
                    enemyCounts[enemyType] = 0;
                enemyCounts[enemyType] += amountToSpawn;
            }
        }
        else
        {
            int attempts = 0;
            while (SpawnCredits > 0 && attempts < 250)
            {
                attempts++;
                EnemyType randomEnemy = (EnemyType)enemyRando.Next(0, System.Enum.GetValues(typeof(EnemyType)).Length);

                randomEnemy = CurseManager.getCursedEnemy(randomEnemy);
                if (!RogueDifficultyManager.Instance.CanSpawn(randomEnemy)) continue;

                int cost = RogueDifficultyManager.Instance.GetCost(randomEnemy);
                if (SpawnCredits - cost < 0) continue;

                int amountCanSpawn = Mathf.FloorToInt(SpawnCredits / cost);
                int amountToSpawn = enemyRando.Next(1, Mathf.Max(1, (amountCanSpawn + 1) / 2));
                SpawnCredits -= amountToSpawn * cost;

                if (!enemyCounts.ContainsKey(randomEnemy))
                    enemyCounts[randomEnemy] = 0;
                enemyCounts[randomEnemy] += amountToSpawn;
            }
        }

        var spawnPlan = new List<PlannedSpawn>();

        foreach (var kvp in enemyCounts)
        {
            EnemyType enemyType = kvp.Key;
            int totalCount = kvp.Value;

            int baseC = RogueDifficultyManager.Instance.GetCountBeforeRadiance(enemyType);
            List<int> thresholds = new List<int>();
            float t = baseC;
            while (true)
            {
                int rounded = Mathf.RoundToInt(t);
                if (rounded > totalCount) break;
                thresholds.Add(rounded);
                float next = t * Mathf.Sqrt(t);
                if (next <= t) break;
                t = next;
            }

            var radianceBuffCounts = new List<int>();
            int remaining = totalCount;
            for (int tier = thresholds.Count - 1; tier >= 0; tier--)
            {
                int count = remaining / thresholds[tier];
                remaining %= thresholds[tier];
                for (int i = 0; i < count; i++)
                    radianceBuffCounts.Add(tier + 1);
            }

            for (int i = 0; i < remaining; i++)
                spawnPlan.Add(new PlannedSpawn(enemyType, 0));
            foreach (int buffs in radianceBuffCounts)
                spawnPlan.Add(new PlannedSpawn(enemyType, buffs));
        }

        Plugin.Logger.LogInfo($"[Room] Spawn plan built: {spawnPlan.Count} enemies total.");

        bool useWaves = spawnPlan.Count >= WaveThreshold;
        if (useWaves)
            Plugin.Logger.LogInfo($"[Room] Large room ({spawnPlan.Count} enemies) — using wave-based spawning.");

        BaseItem mask = Plugin.getItem("Agonized Mask");
        int maskCount = Plugin.GetItemCount(mask);

        int waveStart = 0;

        while (waveStart < spawnPlan.Count)
        {
            if (useWaves && waveStart > 0)
            {
                Plugin.Logger.LogInfo($"[Room] Waiting to spawn wave starting at index {waveStart}…");
                while (true)
                {
                    int alive = GetComponentsInChildren<EnemyIdentifier>()
                                    .Count(e => !e.dead);
                    if (alive <= WaveResumeBelow) break;
                    yield return new WaitForSeconds(WavePollRate);
                }
                yield return new WaitForSeconds(0.5f);
                Plugin.Logger.LogInfo($"[Room] Spawning next wave (index {waveStart}).");
            }

            int waveEnd = useWaves
                ? Mathf.Min(waveStart + WaveSize, spawnPlan.Count)
                : spawnPlan.Count;

            for (int spawnedEnemies = waveStart; spawnedEnemies < waveEnd; spawnedEnemies++)
            {
                int localIndex = spawnedEnemies - waveStart;
                if (localIndex < 100)
                {
                    float delay = localIndex < 25
                        ? 0.05f
                        : 0.05f / (localIndex - 24);
                    yield return new WaitForSeconds(delay);
                }

                PlannedSpawn planned = spawnPlan[spawnedEnemies];

                GameObject enemyPrefab = DefaultReferenceManager.Instance.GetEnemyPrefab(planned.type);
                if (planned.type == EnemyType.Power)
                    enemyPrefab = AssetsManager.funnyPowerIntroSpawn;
                if (planned.type == EnemyType.MirrorReaper)
                    enemyPrefab = AssetsManager.GetEnemiesOfType(EnemyType.MirrorReaper).FirstOrDefault()?.gameObject;
                if (enemyPrefab == null) continue;

                Transform spawnPt = spawnPoints[enemyRando.Next(0, spawnPoints.Count)];
                if (spawnPt == null)
                {
                    Debug.LogWarning($"[Room] No fitting spawn point for {planned.type} — skipping.");
                    continue;
                }

                Vector3 pos;
                do
                {
                    pos = spawnPt.position + new Vector3(
                        (float)((enemyRando.NextDouble() * 4.0) - 2.0), 0,
                        (float)((enemyRando.NextDouble() * 4.0) - 2.0));
                } while (IsOutOfBounds(pos));

                if (isFlying(planned.type)) pos += Vector3.up * 3f;

                if (planned.type == ReplacerType && allObjectActivators.Count > 0)
                {
                    GameObject objectToEnable = allObjectActivators[Random.Range(0, allObjectActivators.Count)];
                    objectToEnable.SetActive(true);
                    allObjectActivators.Remove(objectToEnable);
                    _activatorsWereUsed = true;
                    continue;
                }

                GameObject inst = Instantiate(enemyPrefab, pos, enemyPrefab.transform.rotation);
                inst.transform.parent = transform;
                KeepInBoundsRoom kibr = inst.AddComponent<KeepInBoundsRoom>();
                kibr.RoomInside = this;

                EnemyIdentifier eid = inst.GetComponent<EnemyIdentifier>()
                                   ?? inst.GetComponentInChildren<EnemyIdentifier>();
                kibr.eid = eid;
                if (maskCount > 0 && Random.value <= (0.25f + (0.10f * maskCount)))
                    eid.puppet = true;
                if (planned.radianceBuffs > 0 && eid != null)
                {
                    for (int b = 0; b < planned.radianceBuffs; b++)
                        eid.BuffAll();
                }

                CurseManager.OnEnemySpawn(eid);
            }

            waveStart = waveEnd;
        }
        if (_activatorsWereUsed)
            _activatorSettleUntil = Time.time + ActivatorSettleTime;

        hasSpawnedEnemies = true;
    }

    public bool IsOutOfBounds(Vector3 worldPosition)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        return localPos.x < -60f || localPos.x > (60f * RoomSizeWidth) ||
               localPos.z < -30f || localPos.z > (30f * RoomSizeHeight);
    }

    public static Room getObjectInsideRoom(Vector3 position)
    {
        Vector2Int grid = RoomGenerator.Instance.WorldToGrid(position);
        Room[] rooms = FindObjectsByType<Room>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Room room in rooms)
        {
            // Accommodate multi-tile grid occupancy checks
            if (grid.x >= room.position.x && grid.x < room.position.x + room.RoomSizeWidth &&
                grid.y >= room.position.y && grid.y < room.position.y + room.RoomSizeHeight)
            {
                return room;
            }
        }
        return null;
    }

    IEnumerator SpawnBoss()
    {
        CloseOffRoom();
        yield return new WaitForSeconds(0.5f);
        isFighting = true;
        if (bossEnemyType == null)
        {
            try
            {
                bossEnemyType = RogueDifficultyManager.Instance.GetBoss();
            }
            catch (Exception e)
            {
                Debug.LogError($"GetBoss failed: {e}");
            }
        }
        if (bossEnemyType == null || bossEnemyType.waves == null || bossEnemyType.waves.Count == 0)
        {
            Debug.LogError("[Room] BossPick has no waves defined.");
            yield break;
        }

        for (int w = 0; w < bossEnemyType.waves.Count; w++)
        {
            Debug.Log($"[Room] Starting Boss Wave {w + 1}/{bossEnemyType.waves.Count}");
            List<BossEntry> currentWave = bossEnemyType.waves[w];
            List<EnemyIdentifier> waveEnemies = new List<EnemyIdentifier>();

            foreach (BossEntry bossEntry in currentWave)
            {
                if (bossEntry.prefab == null) continue;

                Vector3 spawnPos = transform.position + Vector3.up * 1f + new Vector3(UnityEngine.Random.Range(-4f, 4f), 0f, UnityEngine.Random.Range(-4f, 4f));
                GameObject bossInst = Instantiate(bossEntry.prefab, spawnPos, bossEntry.prefab.transform.rotation);
                bossInst.transform.parent = transform;

                EnemyIdentifier eid = bossInst.GetComponent<EnemyIdentifier>() ?? bossInst.GetComponentInChildren<EnemyIdentifier>();

                if (eid != null)
                {
                    waveEnemies.Add(eid);
                    if (bossEntry.healthMod != 0 || bossEntry.healthPerFloorMod != 0 || bossEntry.healthAddition != 0)
                    {
                        Enemy e = FindEnemyComponent(bossInst);
                        if (bossEntry.healthMod == 0) bossEntry.healthMod = eid.health;
                        int floorsActive = Mathf.Max(0, RogueDifficultyManager.Instance.floor - bossEntry.startFloor);
                        float totalHealth = bossEntry.healthMod + bossEntry.healthAddition + bossEntry.healthPerFloorMod * floorsActive;
                        eid.health = totalHealth;
                        e.health = totalHealth;
                        e.originalHealth = totalHealth;
                    }

                    int floorsForRadiance = Mathf.Max(0, RogueDifficultyManager.Instance.floor - bossEntry.startFloor);
                    int totalRadiance = bossEntry.radianceBuffs
                        + Mathf.FloorToInt(bossEntry.radianceBuffsPerFloor * floorsForRadiance);

                    for (int r = 0; r < totalRadiance; r++)
                        eid.BuffAll();

                    if (totalRadiance > 0)
                        Debug.Log($"[Room] Applied {totalRadiance} radiance buff(s) to {eid.enemyType}.");

                    bossEnemyType.onSpawn?.Invoke(eid);
                    if (eid.gameObject.GetComponent<BossHealthBar>() == null)
                        eid.gameObject.AddComponent<BossHealthBar>();
                    if (eid.enemyType == EnemyType.Gabriel || eid.enemyType == EnemyType.GabrielSecond)
                    {
                        eid.onDeath.AddListener(() =>
                        {
                            Destroy(bossInst);
                        });
                    }
                }
            }

            bool waveAlive = true;
            while (waveAlive)
            {
                yield return new WaitForSeconds(0.15f);
                waveAlive = waveEnemies.Any(e => e != null && !e.dead);
            }

            if (w < bossEnemyType.waves.Count - 1)
                yield return new WaitForSeconds(0.25f);
        }

        hasSpawnedEnemies = true;
    }

    public static Enemy FindEnemyComponent(GameObject obj)
    {
        if (obj == null) return null;

        Enemy e = obj.GetComponent<Enemy>();
        if (e != null) return e;
        e = obj.GetComponentInChildren<Enemy>(true);
        if (e != null) return e;

        e = obj.GetComponentInParent<Enemy>();
        return e;
    }

    public void CreateDoor(Transform exit)
    {
#if RUNTIME_ROOMS
    var rh = GetComponent<RuntimeRoomDoorHandler>();
    if (rh != null) { rh.PlaceDoor(exit); return; }
#endif
        GameObject door = null;
        if (doorPrefab != null) door = Instantiate(doorPrefab, exit.position, exit.rotation * Quaternion.Euler(0, 90, 0), transform);
        
        if (door != null)
        {
            RoomGenerator.Instance.Doors.Add(door);
            door.transform.parent = null;
            door.SetActive(true);
            if (roomType == RoomType.Normal || roomType == RoomType.Boss || roomType == RoomType.Start) return;
            if (RogueDifficultyManager.Instance.floor == 1) return;
            if (Random.value <= 0.75f && Plugin.CurrentDifficulty != 2) return;
            door.GetComponentInChildren<Door>().gameObject.AddComponent<Lockable>();
        }
        
    }

    public void CreateWall(Transform exit)
    {
#if RUNTIME_ROOMS
    var rh = GetComponent<RuntimeRoomDoorHandler>();
    if (rh != null) { rh.PlaceWall(exit); return; }
#endif
        if (wallPrefab != null) RoomGenerator.Instance.Doors.Add(Instantiate(wallPrefab, exit.position, exit.rotation));
    }

    public void DisableExit(Transform exit) => exit.gameObject.SetActive(false);
    private readonly List<NavMeshObstacle> _exitObstacles = new();

    public void CloseOffRoom()
    {
        foreach (var door in FindObjectsByType<Door>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            door.Lock();
        }
        BlockExitsWithObstacles();
    }

    void BlockExitsWithObstacles()
    {
        List<Transform> allExits = new List<Transform>();
        if (exitsLeft != null) allExits.AddRange(exitsLeft);
        if (exitsRight != null) allExits.AddRange(exitsRight);
        if (exitsTop != null) allExits.AddRange(exitsTop);
        if (exitsBottom != null) allExits.AddRange(exitsBottom);

        if (exitLeft != null) allExits.Add(exitLeft);
        if (exitRight != null) allExits.Add(exitRight);
        if (exitTop != null) allExits.Add(exitTop);
        if (exitBottom != null) allExits.Add(exitBottom);

        foreach (var exit in allExits)
        {
            if (exit == null) continue;
            var obs = exit.gameObject.GetComponent<NavMeshObstacle>()
                   ?? exit.gameObject.AddComponent<NavMeshObstacle>();
            obs.carving = true;
            obs.size = new Vector3(4f, 4f, 4f);
            obs.enabled = true;
            _exitObstacles.Add(obs);
        }
    }

    void UnblockExits()
    {
        foreach (var obs in _exitObstacles)
            if (obs != null) obs.enabled = false;
        _exitObstacles.Clear();
    }

    void Awake()
    {
        SpawnBoundaryObstacles();

        enemyRando = new System.Random(Plugin.GameSeed.GetHashCode() ^ roomIndex + 1);
        roomIndex++;
        gameObject.AddComponent<GoreZone>();
        try
        {
            bossEnemyType = RogueDifficultyManager.Instance.GetBoss();
        }
        catch (Exception e)
        {
            Debug.LogError($"GetBoss failed: {e}");
        }

        foreach (var zone in GetComponentsInChildren<DeathZone>())
        {
            zone.respawnTarget = spawnPoints[Random.Range(0, spawnPoints.Count)].position;
            zone.notInstakill = true;
        }
    }

    void Update()
    {
        // Throttled boundary obstacle toggle — avoids per-frame overhead
        if (_boundaryObstacles.Count > 0 && NewMovement.Instance != null)
        {
            _obstacleCheckTimer -= Time.deltaTime;
            if (_obstacleCheckTimer <= 0f)
            {
                _obstacleCheckTimer = ObstacleCheckInterval;

                // Cheap local-space bounds check — no FindObjectsByType
                Vector3 local = transform.InverseTransformPoint(NewMovement.Instance.transform.position);
                bool playerIsHere = local.x >= -(RoomSizeWidth * 30f) && local.x <= (RoomSizeWidth * 30f) &&
                                    local.z >= -(RoomSizeHeight * 15f) && local.z <= (RoomSizeHeight * 15f);

                foreach (GameObject obs in _boundaryObstacles)
                    if (obs != null && obs.activeSelf != playerIsHere)
                        obs.SetActive(playerIsHere);
            }
        }

        foreach (var zone in GetComponentsInChildren<DeathZone>())
        {
            if (zone.respawnTarget.y == 0)
                zone.respawnTarget = spawnPoints[Random.Range(0, spawnPoints.Count)].position;
        }
        if (tookNoDamage && hasSpawnedEnemies)
        {
            if (playerHealthAtFightStart < NewMovement.Instance.hp)
                tookNoDamage = false;
        }

        if (!hasSpawnedEnemies || rewardGiven) return;

        if (_activatorsWereUsed && Time.time < _activatorSettleUntil) return;

        EnemyIdentifier[] enemies = GetComponentsInChildren<EnemyIdentifier>();
        EnemyIdentifier[] aliveEnemies = enemies.Where(x => !x.dead).ToArray();

        if (aliveEnemies.Length == 0)
        {
            OnRoomCleared();
        }
    }

    bool isFlying(EnemyType type)
    {
        List<EnemyType> flyers = new List<EnemyType>() { EnemyType.Drone, EnemyType.Mindflayer, EnemyType.Providence };
        return flyers.Contains(type);
    }

    bool tookNoDamage = true;
    void OnRoomCleared()
    {
        UnblockExits();
        MonoSingleton<MusicManager>.Instance.ArenaMusicEnd();
        MonoSingleton<TimeController>.Instance.SlowDown(0.15f);
        MonoSingleton<StainVoxelManager>.Instance.ClearAll();
        GasolineProjectile[] projs = GameObject.FindObjectsByType<GasolineProjectile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var proj in projs)
        {
            Destroy(proj.gameObject);
        }
        rewardGiven = true;
        isFighting = false;

        if (!isBossRoom)
        {
            if (!Plugin.SelectedChar.HasPassive(Passive.HealFromBlood))
            {
                int currentHp = MonoSingleton<NewMovement>.Instance.hp;
                int maxHp = Plugin.MaxHealth;

                if (currentHp < maxHp)
                {
                    int healAmt = Random.Range(25, 50);
                    MonoSingleton<NewMovement>.Instance.GetHealth(healAmt, false);
                }
            }

            float itemChance = tookNoDamage ? 0.05f : 0.015f;

            if (enemyRando.NextDouble() <= itemChance)
            {
                Vector3 itemPos = spawnPoints[Random.Range(0, spawnPoints.Count)].position;
                GameObject plc = new GameObject("ItemDropAnchor");
                plc.transform.position = itemPos;
                plc.transform.parent = transform;
                StartCoroutine(spawnItem(plc.transform));
            }
            else
            {
                float chanceVal = (float)enemyRando.NextDouble() + (tookNoDamage ? 0.20f : 0f);

                if (chanceVal <= 0.22f)
                {
                    // Nothing
                }
                else if (chanceVal <= 0.44f)
                {
                    RogueDifficultyManager.Instance.Keys++;
                    if (tookNoDamage)
                        Debug.Log("[Room] Flawless clear! Awarded a key.");
                }
                else
                {
                    int goldAmount = enemyRando.Next(1, tookNoDamage ? 4 : 3);
                    for (int i = 0; i < goldAmount; i++)
                        RogueDifficultyManager.Instance.Gold++;
                    if (tookNoDamage)
                        Debug.Log($"[Room] Flawless clear! Awarded {goldAmount} gold.");
                }
            }
        }
        else
        {
            Vector3 spawnPos = transform.position + new Vector3(
                Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f));
            GameObject plc = new GameObject("aaaaaaaaaaaa");
            plc.transform.position = spawnPos;
            plc.transform.parent = transform;
            StartCoroutine(spawnItem(plc.transform));
            NewMovement.Instance.FullHeal();
            StartCoroutine(SpawnPortalWhenClear());
        }

        foreach (var door in FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (door.TryGetComponent<Lockable>(out var lockable))
            {
                if (lockable.locked)
                    continue;
            }
            door.Unlock();
        }
    }

    public static GameObject pedestalItem = null;

    IEnumerator spawnItem(Transform plc)
    {
        yield return new WaitForSeconds(0.15f);
        if (pedestalItem == null)
            pedestalItem = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/Draghtnim/Pedestal.prefab").WaitForCompletion();
        ItemPickup.CreatePickup(Plugin.GiveRandomItem(), plc);
        Instantiate(AssetsManager.spawnEffect, plc.position, Quaternion.identity);
        if (pedestalItem != null)
        {
            GameObject ped = Instantiate(pedestalItem, plc.transform.position + Vector3.up, Quaternion.identity);
            ped.transform.parent = transform;
        }
    }

    IEnumerator SpawnPortalWhenClear()
    {
        yield return new WaitForSeconds(1f);
        GameObject portalPlace = GameObject.Find("PortalPlace");
        if (portalPlace == null) yield break;

        const float halfExtent = 5f;
        const float aboveThreshold = 40f;

        Vector3 portalPos = portalPlace.transform.position;

        while (true)
        {
            Vector3 playerPos = NewMovement.Instance.transform.position;

            float dx = Mathf.Abs(playerPos.x - portalPos.x);
            float dz = Mathf.Abs(playerPos.z - portalPos.z);
            float dy = playerPos.y - portalPos.y;

            bool playerIsAbovePortal =
                dx <= halfExtent &&
                dz <= halfExtent &&
                dy >= 0f && dy <= aboveThreshold;

            if (!playerIsAbovePortal)
            {
                CreatePortal();
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    public void CreatePortal()
    {
        GameObject quad1 = new GameObject("PortalEntry");
        quad1.transform.position = GameObject.Find("PortalPlace").transform.position;
        quad1.transform.Rotate(90, 0, 0);

        GameObject quad2 = new GameObject("PortalExit");
        quad2.transform.position = GameObject.Find("PortalPos").transform.position;
        quad2.transform.Rotate(-90, 0, 0);

        Portal portal1 = quad1.AddComponent<Portal>();
        portal1.shape = new PlaneShape { width = 10, height = 10 };
        portal1.entry = quad2.transform;
        portal1.exit = quad1.transform;
        portal1.supportInfiniteRecursion = true;
        portal1.appearsInRecursions = true;
        portal1.canSeeItself = true;
        portal1.clippingMethod = PortalClippingMethod.Default;
        portal1.maxRecursions = 3;
        portal1.renderSettings = PortalSideFlags.Enter | PortalSideFlags.Exit | PortalSideFlags.None;
        portal1.useFogEnter = true;
        portal1.useFogExit = true;
        portal1.canSeePortalLayer = true;

        Portal portal2 = quad2.AddComponent<Portal>();
        portal2.shape = new PlaneShape { width = 10, height = 10 };
        portal2.entry = quad1.transform;
        portal2.exit = quad2.transform;
        portal2.supportInfiniteRecursion = true;
        portal2.appearsInRecursions = true;
        portal2.canSeeItself = true;
        portal2.clippingMethod = PortalClippingMethod.Default;
        portal2.maxRecursions = 3;
        portal2.renderSettings = PortalSideFlags.Enter | PortalSideFlags.Exit | PortalSideFlags.None;
        portal2.useFogEnter = true;
        portal2.useFogExit = true;
        portal2.canSeePortalLayer = true;

        PortalIdentifier portalIdent = quad2.AddComponent<PortalIdentifier>();
        portalIdent.isTraversable = true;

        GameObject.Find("PortalPlace").SetActive(false);

        StartCoroutine(funnies(portal1, quad2));
    }

    IEnumerator funnies(Portal port, GameObject eixt)
    {
        yield return new WaitForEndOfFrame();
        if (port.onExitTravel == null) port.onExitTravel = new UnityEventPortalTravel();
        port.onExitTravel.AddListener((IP, D) =>
        {
            if (IP.travellerType == PortalTravellerType.PLAYER)
            {
                Destroy(eixt);
                RoomGenerator.Instance.RegenerateRooms();
                Destroy(port.gameObject);
            }
        });
    }

    public Vector3 GetOffset(Transform exit)
    {
        float dist = Vector3.Distance(exit.position, transform.position);
        Vector3 dir = (exit.position - transform.position).normalized;
        return dir * dist;
    }
}

public class KeepInBoundsRoom : MonoBehaviour
{
    public Room RoomInside;
    public EnemyIdentifier eid;

    private NavMeshAgent _agent;
    private Rigidbody _rb;

    private int _consecutiveFramesOut;
    private const int MaxFramesOut = 30;

    public bool ResetVelocity = true;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>() ?? GetComponentInChildren<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>() ?? GetComponentInChildren<Rigidbody>();
    }

    void LateUpdate()
    {
        if (RoomInside == null) return;

        Vector3 local = RoomInside.transform.InverseTransformPoint(transform.position);

        // Scaled bounds limits checking dynamically via grid configuration sizes
        float xLimitMin = -55f;
        float xLimitMax = 55f * RoomInside.RoomSizeWidth;
        float zLimitMin = -25f;
        float zLimitMax = 25f * RoomInside.RoomSizeHeight;

        bool outOfBounds = local.x < xLimitMin || local.x > xLimitMax ||
                           local.z < zLimitMin || local.z > zLimitMax;

        if (!outOfBounds)
        {
            return;
        }
        _consecutiveFramesOut++;
        if (_consecutiveFramesOut >= MaxFramesOut)
        {
            if (eid != null) eid.InstaKill();
            Destroy(this);
            return;
        }

        local.x = Mathf.Clamp(local.x, xLimitMin, xLimitMax);
        local.z = Mathf.Clamp(local.z, zLimitMin, zLimitMax);
        Vector3 clampedWorldPos = RoomInside.transform.TransformPoint(local);

        if (_rb != null && ResetVelocity)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        if (_agent != null && _agent.isActiveAndEnabled)
        {
            _agent.ResetPath();
            _agent.Warp(clampedWorldPos);
        }
        else
        {
            transform.position = clampedWorldPos;
        }
    }
}
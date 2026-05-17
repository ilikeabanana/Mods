using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ULTRAKILL.Portal;
using ULTRAKILL.Portal.Geometry;
using Ultrarogue;
using Ultrarogue.Characters;
using Ultrarogue.Items;
using Ultrarogue.SceneStuff;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public enum RoomType
{
    Normal,
    Start,
    Boss,
    Treasure,
    Shop,
    Gambling,
}

public class Room : MonoBehaviour
{
    public Vector2Int position;
    public float spawnChance;

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

    private bool hasSpawnedEnemies = false;
    private bool rewardGiven = false;

    [Tooltip("Enemy that will activate the object instead of itself (default to wicked for nothing)")]
    public EnemyType ReplacerType = EnemyType.Wicked;
    [Tooltip("The objects that will be activated, will pick a random one in the list each time and will remove that one from the list once chosen. If none are left, it will just spawn the enemy like normal")]
    public List<GameObject> allObjectActivators = new List<GameObject>();

    public static bool isFighting = false;
    public void OnRoomEnter()
    {
        switch (roomType)
        {
            case RoomType.Boss:
                StartCoroutine(SpawnBoss());
                break;

            case RoomType.Normal:
                int c = Plugin.GetItemCount("Dual Gun");
                if(c > 0)
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
            case RoomType.Shop:
            case RoomType.Gambling:
            case RoomType.Start:
            default:
                break;
        }
    }

    

    private int playerHealthAtFightStart = -1;

    // Represents a single planned enemy spawn: its type and how many radiance buffs it gets.
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

    IEnumerator SpawnEnemies()
    {
        if (SpawnCredits == 0) yield break;
        CloseOffRoom();
        playerHealthAtFightStart = MonoSingleton<NewMovement>.Instance.hp;
        SpawnCredits = Mathf.RoundToInt((float)SpawnCredits * RogueDifficultyManager.Instance.Difficulty);
        SpawnCredits = Mathf.Max(SpawnCredits, 3);
        Plugin.Logger.LogInfo($"Room has {SpawnCredits} spawn credits because difficulty is {RogueDifficultyManager.Instance.Difficulty}");
        isFighting = true;

        // ── Phase 1a: Spend credits, accumulate total counts per enemy type ─────
        var enemyCounts = new Dictionary<EnemyType, int>();

        while (SpawnCredits > 0)
        {
            EnemyType randomEnemy = (EnemyType)enemyRando.Next(0, System.Enum.GetValues(typeof(EnemyType)).Length);
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

        // ── Phase 1b: Resolve radiance using TOTAL count per type, build spawn list
        var spawnPlan = new List<PlannedSpawn>();

        foreach (var kvp in enemyCounts)
        {
            EnemyType enemyType = kvp.Key;
            int totalCount = kvp.Value;

            // Build radiance tier thresholds against the full count for this type.
            int baseC = RogueDifficultyManager.Instance.GetCountBeforeRadiance(enemyType);
            List<int> thresholds = new List<int>();
            float t = baseC;
            while (true)
            {
                int rounded = Mathf.RoundToInt(t);
                if (rounded > totalCount) break;
                thresholds.Add(rounded);
                float next = t * Mathf.Sqrt(t);
                if (next <= t) break; // guard against infinite loop when baseC == 1
                t = next;
            }

            // Work top-down: highest tier first, consuming from remaining count.
            var radianceBuffCounts = new List<int>();
            int remaining = totalCount;
            for (int tier = thresholds.Count - 1; tier >= 0; tier--)
            {
                int count = remaining / thresholds[tier];
                remaining %= thresholds[tier];
                for (int i = 0; i < count; i++)
                    radianceBuffCounts.Add(tier + 1); // tier 0 → 1 buff, tier 1 → 2 buffs, etc.
            }

            // Plain (unbuffed) enemies first, then buffed ones.
            for (int i = 0; i < remaining; i++)
                spawnPlan.Add(new PlannedSpawn(enemyType, 0));
            foreach (int buffs in radianceBuffCounts)
                spawnPlan.Add(new PlannedSpawn(enemyType, buffs));
        }

        Plugin.Logger.LogInfo($"[Room] Spawn plan built: {spawnPlan.Count} enemies total.");

        // ── Phase 2: Spawn every planned enemy ───────────────────────────────────
        BaseItem mask = Plugin.getItem("Agonized Mask");
        int maskCount = Plugin.GetItemCount(mask);

        for (int spawnedEnemies = 0; spawnedEnemies < spawnPlan.Count; spawnedEnemies++)
        {
            // First 100 enemies stagger in; everything beyond spawns instantly.
            if (spawnedEnemies < 100)
            {
                float delay = spawnedEnemies < 25
                    ? 0.05f
                    : 0.05f / (spawnedEnemies - 24);
                yield return new WaitForSeconds(delay);
            }

            PlannedSpawn planned = spawnPlan[spawnedEnemies];

            GameObject enemyPrefab = DefaultReferenceManager.Instance.GetEnemyPrefab(planned.type);
            if (planned.type == EnemyType.Power)
                enemyPrefab = AssetsManager.funnyPowerIntroSpawn;
            if (planned.type == EnemyType.MirrorReaper)
                enemyPrefab = AssetsManager.GetEnemiesOfType(EnemyType.MirrorReaper).FirstOrDefault().gameObject;
            if (enemyPrefab == null) continue;

            Transform spawnPt = spawnPoints[enemyRando.Next(0, spawnPoints.Count)];
            if (spawnPt == null)
            {
                Debug.LogWarning($"[Room] No fitting spawn point for {planned.type} — skipping this unit.");
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

            if(planned.type == ReplacerType && allObjectActivators.Count > 0)
            {
                GameObject objectToEnable = allObjectActivators[Random.Range(0, allObjectActivators.Count)];
                objectToEnable.SetActive(true);
                allObjectActivators.Remove(objectToEnable);
                continue;
            }

            GameObject inst = Instantiate(enemyPrefab, pos, enemyPrefab.transform.rotation);
            inst.transform.parent = transform;
            KeepInBoundsRoom kibr = inst.AddComponent<KeepInBoundsRoom>();
            kibr.RoomInside = this;
            if (planned.radianceBuffs > 0)
            {
                EnemyIdentifier eid = inst.GetComponent<EnemyIdentifier>()
                                   ?? inst.GetComponentInChildren<EnemyIdentifier>();
                if (eid != null)
                {
                    for (int b = 0; b < planned.radianceBuffs; b++)
                        eid.BuffAll();

                    if (maskCount > 0 && Random.value <= (0.25f + (0.10f * maskCount)))
                        eid.puppet = true;

                    kibr.eid = eid;
                }
            }
        }

        hasSpawnedEnemies = true;
    }

    bool IsOutOfBounds(Vector3 worldPosition)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);

        return localPos.x < -60 || localPos.x > 60 ||
               localPos.z < -30 || localPos.z > 30;
    }

    IEnumerator SpawnBoss()
    {
        CloseOffRoom();
        yield return new WaitForSeconds(0.5f);

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
                    if (bossEntry.healthMod != 0 || bossEntry.healthPerFloorMod != 0)
                    {
                        Enemy e = FindEnemyComponent(bossInst);
                        int floorsActive = Mathf.Max(0, RogueDifficultyManager.Instance.floor - bossEntry.startFloor);
                        float totalHealth = bossEntry.healthMod + bossEntry.healthPerFloorMod * floorsActive;
                        eid.health = totalHealth;
                        e.health = totalHealth;
                        e.originalHealth = totalHealth;
                    }
                    bossEnemyType.onSpawn?.Invoke(eid);
                    if (eid.gameObject.GetComponent<BossHealthBar>() == null)
                        eid.gameObject.AddComponent<BossHealthBar>();
                    if (eid.enemyType == EnemyType.Gabriel || eid.enemyType == EnemyType.GabrielSecond)
                    {
                        eid.onDeath.AddListener(() =>
                        {
                            Destroy(bossInst); // Prevent that stupid fucking gabe bug
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
            if (roomType == RoomType.Normal || roomType == RoomType.Boss || roomType == RoomType.Start) return;
            if (Random.value <= 0.75f) return;
            door.GetComponentInChildren<Door>().gameObject.AddComponent<Lockable>();
        }
    }

    public void CreateWall(Transform exit)
    {
#if RUNTIME_ROOMS
    var rh = GetComponent<RuntimeRoomDoorHandler>();
    if (rh != null) { rh.PlaceWall(exit); return; }
#endif
        if (wallPrefab != null) Instantiate(wallPrefab, exit.position, exit.rotation, transform);
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
        Transform[] exits = { exitLeft, exitRight, exitTop, exitBottom };
        foreach (var exit in exits)
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
        enemyRando = new System.Random(Plugin.GameSeed.GetHashCode() ^ roomIndex + 1);
        roomIndex++;
        gameObject.AddComponent<GoreZone>();
        bossEnemyType = RogueDifficultyManager.Instance.GetBoss();

        foreach (var zone in GetComponentsInChildren<DeathZone>())
        {
            zone.respawnTarget = spawnPoints[Random.Range(0, spawnPoints.Count)].position;
        }
    }

    void Update()
    {
        foreach (var zone in GetComponentsInChildren<DeathZone>())
        {
            if(zone.respawnTarget.y == 0)
                zone.respawnTarget = spawnPoints[Random.Range(0, spawnPoints.Count)].position;
        }
        if (tookNoDamage && hasSpawnedEnemies)
        {
            if (playerHealthAtFightStart < NewMovement.Instance.hp)
                tookNoDamage = false;
        }

        if (!hasSpawnedEnemies || rewardGiven) return;

        EnemyIdentifier[] enemies = GetComponentsInChildren<EnemyIdentifier>();

        EnemyIdentifier[] aliveEnemies = enemies.Where((x) => !x.dead).ToArray();

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
                    float missingRatio = 1f - ((float)currentHp / maxHp);
                    float healChance = Mathf.Lerp(0.10f, 0.65f, missingRatio);

                    if (Random.value <= healChance)
                    {
                        int healAmt = Random.Range(25, 50);
                        MonoSingleton<NewMovement>.Instance.GetHealth(healAmt, false);
                    }
                }
            }

            float itemChance = tookNoDamage ? 0.05f : 0.015f;

            if (enemyRando.NextDouble() <= itemChance)
            {
                Vector3 itemPos = transform.position + new Vector3(
                    Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f));
                GameObject plc = new GameObject("ItemDropAnchor");
                plc.transform.position = itemPos;
                StartCoroutine(spawnItem(plc.transform));
            }
            else
            {
                float chanceVal = (float)enemyRando.NextDouble() + (tookNoDamage ? 0.20f : 0f);

                float keyThreshold = Mathf.Clamp(
                    0.1f - RogueDifficultyManager.Instance.Keys * 0.008f,
                    0.008f,
                    0.1f   // was 0.2f
                );
                if (chanceVal <= 0.30f)
                {
                    // Nothing
                }
                else if (chanceVal <= keyThreshold + 0.30f)
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

    IEnumerator spawnItem(Transform plc)
    {
        yield return new WaitForSeconds(0.15f);
        ItemPickup.CreatePickup(Plugin.GiveRandomItem(), plc);
        Instantiate(AssetsManager.spawnEffect, plc.position, Quaternion.identity);
    }
    IEnumerator SpawnPortalWhenClear()
    {
        GameObject portalPlace = GameObject.Find("PortalPlace");
        if (portalPlace == null) yield break;

        // The portal quad is 10x10, rotated flat on XZ — half-extent is 5 units per axis.
        const float halfExtent = 5f;
        // How far above the portal plane we consider the player "on top of it".
        const float aboveThreshold = 4f;

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
    private const float XLimit = 58f; // slightly inside wall
    private const float ZLimit = 28f;
    private int attempts = 0;
    private const int MaxAttempts = 10;

    void Start()
    {
        // Enemy prefabs often nest the agent on a child
        _agent = GetComponent<NavMeshAgent>()
              ?? GetComponentInChildren<NavMeshAgent>();
    }

    void Update()
    {
        if (RoomInside == null) return;

        Vector3 local = RoomInside.transform.InverseTransformPoint(transform.position);

        bool outOfBounds = local.x < -XLimit || local.x > XLimit ||
                           local.z < -ZLimit || local.z > ZLimit;

        if (!outOfBounds) { attempts = 0; return; }

        attempts++;
        if (attempts >= MaxAttempts)
        {
            if (eid != null) eid.InstaKill();
            Destroy(this);
            return;
        }

        local.x = Mathf.Clamp(local.x, -XLimit, XLimit);
        local.z = Mathf.Clamp(local.z, -ZLimit, ZLimit);
        Vector3 clamped = RoomInside.transform.TransformPoint(local);

        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            _agent.Warp(clamped);   // the correct way to relocate a NavMeshAgent
        else
            transform.position = clamped;
    }
}
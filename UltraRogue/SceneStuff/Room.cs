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
using Random=UnityEngine.Random;

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

    public static bool isFighting = false;
    public void OnRoomEnter()
    {
        switch (roomType)
        {
            case RoomType.Boss:
                StartCoroutine(SpawnBoss());
                break;

            case RoomType.Normal:

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
    IEnumerator SpawnEnemies()
    {
        if (SpawnCredits == 0) yield break;
        CloseOffRoom();
        playerHealthAtFightStart = MonoSingleton<NewMovement>.Instance.hp;
        SpawnCredits = Mathf.RoundToInt((float)SpawnCredits * RogueDifficultyManager.Instance.Difficulty);
        SpawnCredits = Mathf.Max(SpawnCredits, 3);
        Plugin.Logger.LogInfo($"Room has {SpawnCredits} spawn credits because difficulty is {RogueDifficultyManager.Instance.Difficulty}");
        isFighting = true;

        int spawnedEnemies = 0;
        while (SpawnCredits > 0)
        {
            EnemyType randomEnemy = (EnemyType)enemyRando.Next(0, System.Enum.GetValues(typeof(EnemyType)).Length);
            if (!RogueDifficultyManager.Instance.CanSpawn(randomEnemy)) continue;
            int cost = RogueDifficultyManager.Instance.GetCost(randomEnemy);
            if (SpawnCredits - cost < 0) continue;

            int amountCanSpawn = Mathf.FloorToInt(SpawnCredits / cost);
            int amountToSpawn = enemyRando.Next(1, amountCanSpawn + 1);
            SpawnCredits -= amountToSpawn * cost;

            int baseC = RogueDifficultyManager.Instance.GetCountBeforeRadiance(randomEnemy);
            var radianceBuffCounts = new List<int>();
            int threshold = baseC;
            int remaining = amountToSpawn;
            int tierLevel = 1;

            while (remaining >= threshold)
            {
                radianceBuffCounts.Add(tierLevel);
                remaining -= threshold;
                float fThreshold = (float)threshold * Mathf.Sqrt(baseC);
                threshold = Mathf.RoundToInt(fThreshold);
                tierLevel++;
            }
            amountToSpawn = remaining + radianceBuffCounts.Count;

            GameObject enemyPrefab = DefaultReferenceManager.Instance.GetEnemyPrefab(randomEnemy);
            if (randomEnemy == EnemyType.Power)
                enemyPrefab = AssetsManager.funnyPowerIntroSpawn;
            if (enemyPrefab == null) continue;

            for (int i = 0; i < amountToSpawn; i++)
            {
                spawnedEnemies++;
                if (spawnedEnemies <= 25)
                    yield return new WaitForSeconds(0.05f);
                else
                    yield return new WaitForSeconds(0.05f / (spawnedEnemies - 24));

                Transform spawnPt = spawnPoints[enemyRando.Next(0, spawnPoints.Count)];

                if (spawnPt == null)
                {
                    Debug.LogWarning($"[Room] No fitting spawn point for {randomEnemy} — skipping this unit.");
                    continue;
                }

                Vector3 pos = spawnPt.position;
                if (isFlying(randomEnemy)) pos += Vector3.up * 3f;

                pos += new Vector3((float)((enemyRando.NextDouble() * 4.0) - 2.0), 0, (float)((enemyRando.NextDouble() * 4.0) - 2.0));

                GameObject inst = Instantiate(enemyPrefab, pos, enemyPrefab.transform.rotation);
                inst.transform.parent = transform;

                if (radianceBuffCounts.Count > 0)
                {
                    EnemyIdentifier eid = inst.GetComponent<EnemyIdentifier>();
                    if (eid == null) eid = inst.GetComponentInChildren<EnemyIdentifier>();
                    int buffCount = radianceBuffCounts[0];
                    radianceBuffCounts.RemoveAt(0);
                    for (int b = 0; b < buffCount; b++)
                        eid.BuffAll();

                    BaseItem mask = Plugin.getItem("Agonized Mask");
                    int c = Plugin.GetItemCount(mask);

                    if(c > 0)
                    {
                        if(Random.value <= (0.25f + (0.10f * c)))
                        {
                            eid.puppet = true;
                        }
                    }
                }
            }
        }

        hasSpawnedEnemies = true;
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
                    if(eid.enemyType == EnemyType.Gabriel || eid.enemyType == EnemyType.GabrielSecond)
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

    public void CloseOffRoom()
    {
        foreach (var door in FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {

            door.Lock();
        }
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
            if (Plugin.SelectedChar.GetType() != typeof(V1))
            {
                int currentHp = MonoSingleton<NewMovement>.Instance.hp;
                int maxHp = Plugin.MaxHealth;

                if (currentHp < maxHp)
                {
                    float missingRatio = 1f - ((float)currentHp / maxHp);
                    float healChance = Mathf.Lerp(0.10f, 0.65f, missingRatio);

                    if (Random.value <= healChance)
                    {
                        int healAmt = Random.Range(5, 50);
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
                ItemPickup.CreatePickup(Plugin.GiveRandomItem(), plc.transform);
                Debug.Log("[Room] Bonus item dropped on room clear.");
            }
            else
            {
                float chanceVal = (float)enemyRando.NextDouble() + (tookNoDamage ? 0.20f : 0f);

                float keyThreshold = Mathf.Min(0.75f + RogueDifficultyManager.Instance.Keys * 0.08f, 0.97f);

                if (chanceVal <= 0.22f)
                {
                    // Nothing
                }
                else if (chanceVal <= keyThreshold)
                {
                    int goldAmount = enemyRando.Next(1, tookNoDamage ? 4 : 3);
                    for (int i = 0; i < goldAmount; i++)
                        RogueDifficultyManager.Instance.Gold++;
                    if (tookNoDamage)
                        Debug.Log($"[Room] Flawless clear! Awarded {goldAmount} gold.");
                }
                else
                {
                    RogueDifficultyManager.Instance.Keys++;
                    if (tookNoDamage)
                        Debug.Log("[Room] Flawless clear! Awarded a key.");
                }
            }
        }
        else
        {
            Vector3 spawnPos = transform.position + new Vector3(
                Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f));
            GameObject plc = new GameObject("aaaaaaaaaaaa");
            plc.transform.position = spawnPos;
            ItemPickup.CreatePickupConditional(Plugin.GiveRandomItem(), plc.transform, () =>
            {
                CreatePortal();
                return true;
            });
            NewMovement.Instance.FullHeal();
            
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
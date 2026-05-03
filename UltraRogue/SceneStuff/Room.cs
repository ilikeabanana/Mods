using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ULTRAKILL.Portal;
using ULTRAKILL.Portal.Geometry;
using Ultrarogue;
using Ultrarogue.Characters;
using Ultrarogue.SceneStuff;
using UnityEngine;

// ── Room type ──────────────────────────────────────────────────────────────────
// Used by both runtime generation and (optionally) prefab-based generation.
public enum RoomType
{
    Normal,
    Start,
    Boss,
    Treasure,   // One free item on a pedestal; no enemies.
    Shop,       // 2–3 items purchasable with gold; no enemies.
    Gambling,   // A Gambler machine; no enemies.
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

    // ── Room type ─────────────────────────────────────────────────────────────

    public RoomType roomType = RoomType.Normal;

    /// <summary>Convenience accessor — true when this is a boss room.</summary>
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
        isFighting = true;

        int spawnedEnemies = 0;
        while (SpawnCredits > 0)
        {
            EnemyType randomEnemy = (EnemyType)Random.Range(0, System.Enum.GetValues(typeof(EnemyType)).Length);
            int cost = RogueDifficultyManager.Instance.GetCost(randomEnemy);
            if (SpawnCredits - cost < 0) continue;

            int amountCanSpawn = Mathf.FloorToInt(SpawnCredits / cost);
            int amountToSpawn = Random.Range(1, amountCanSpawn + 1);
            SpawnCredits -= amountToSpawn * cost;

            int amountBeforeRadiance = RogueDifficultyManager.Instance.GetCountBeforeRadiance(randomEnemy);
            int amountRadiance = 0;
            if (amountToSpawn >= amountBeforeRadiance)
            {
                amountRadiance = Mathf.FloorToInt((float)amountToSpawn / amountBeforeRadiance);
                amountToSpawn -= amountRadiance * amountBeforeRadiance;
                amountToSpawn += amountRadiance;
            }

            GameObject enemyPrefab = DefaultReferenceManager.Instance.GetEnemyPrefab(randomEnemy);
            if (enemyPrefab == null) continue;

            // ── Fit check: sample once per enemy type, reuse for all spawns ──
            Vector3 halfExtents = GetPrefabHalfExtents(enemyPrefab);

            for (int i = 0; i < amountToSpawn; i++)
            {
                spawnedEnemies++;
                yield return new WaitForSeconds(0.05f);

                Transform spawnPt = FindFittingSpawnPoint(halfExtents);

                if (spawnPt == null)
                {
                    Debug.LogWarning($"[Room] No fitting spawn point for {randomEnemy} — skipping this unit.");
                    continue; // enemy simply won't spawn rather than clipping through a wall
                }

                Vector3 pos = spawnPt.position;
                if (isFlying(randomEnemy)) pos += Vector3.up;

                GameObject inst = Instantiate(enemyPrefab, spawnPt.position, enemyPrefab.transform.rotation);
                inst.transform.parent = transform;

                if (amountRadiance != 0)
                {
                    EnemyIdentifier eid = inst.GetComponent<EnemyIdentifier>();
                    if (eid == null) eid = inst.GetComponentInChildren<EnemyIdentifier>();
                    eid.BuffAll();
                    amountRadiance--;
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

                Vector3 spawnPos = transform.position + Vector3.up * 1f + new Vector3(UnityEngine.Random.Range(-2f, 2f), 0f, UnityEngine.Random.Range(-2f, 2f));
                GameObject bossInst = Instantiate(bossEntry.prefab, spawnPos, bossEntry.prefab.transform.rotation);
                bossInst.transform.parent = transform;

                EnemyIdentifier eid = bossInst.GetComponent<EnemyIdentifier>() ?? bossInst.GetComponentInChildren<EnemyIdentifier>();

                if (eid != null)
                {
                    waveEnemies.Add(eid);
                    if (bossEntry.healthMod != 0)
                    {
                        Enemy e = FindEnemyComponent(bossInst);
                        eid.health = bossEntry.healthMod;
                        e.health = bossEntry.healthMod;
                        e.originalHealth = bossEntry.healthMod;
                    }
                    bossEnemyType.onSpawn?.Invoke(eid);
                }

                if (bossInst.GetComponent<BossHealthBar>() == null)
                    bossInst.AddComponent<BossHealthBar>();
            }

            // WAIT for the current wave to be cleared
            bool waveAlive = true;
            while (waveAlive)
            {
                yield return new WaitForSeconds(0.5f); // Check every half second
                waveAlive = waveEnemies.Any(e => e != null && !e.dead);
            }

            Debug.Log($"[Room] Wave {w + 1} cleared!");

            // Brief pause between waves
            if (w < bossEnemyType.waves.Count - 1)
                yield return new WaitForSeconds(1.5f);
        }

        hasSpawnedEnemies = true; // This triggers OnRoomCleared via Update()
    }

    public static Enemy FindEnemyComponent(GameObject obj)
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

        // Check if any enemies are still alive in this room
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

        rewardGiven = true;
        isFighting = false;

        if (!isBossRoom)
        {

            // ── 1. Heal chance (non-V1 characters only) ───────────────────────────
            if (Plugin.SelectedChar.GetType() != typeof(V1))
            {
                if (Random.value <= 0.35f)                       // 35 % to heal
                {
                    int healAmt = Random.Range(5, 20);
                    MonoSingleton<NewMovement>.Instance.GetHealth(healAmt, false);
                    Debug.Log($"[Room] Healed player for {healAmt} HP (non-V1 perk).");
                }
            }

            // ── 2 & 3. Item / gold / key rewards ─────────────────────────────────
            // Item chance is small; slightly higher on a flawless clear.
            float itemChance = tookNoDamage ? 0.05f : 0.015f;  // 1.5 % normal, 5 % flawless

            if (Random.value <= itemChance)
            {
                // Spawn a random item at a point near the room centre.
                Vector3 itemPos = transform.position + new Vector3(
                    Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f));
                GameObject plc = new GameObject("ItemDropAnchor");
                plc.transform.position = itemPos;
                ItemPickup.CreatePickup(Plugin.GiveRandomItem(), plc.transform);
                Debug.Log("[Room] Bonus item dropped on room clear.");
            }
            else
            {
                // Both flawless and normal clears share the same loot table.
                // Flawless clears get a boosted roll (+0.20) so gold/keys are
                // more likely, but the key threshold is reachable either way.
                float chanceVal = Plugin.getChanceVal() + (tookNoDamage ? 0.20f : 0f);
                if (chanceVal <= 0.22f)
                {
                    // Nothing
                }
                else if (chanceVal <= 0.75f)
                {
                    int goldAmount = Random.Range(1, 3);
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
        else  // ── Boss room reward (unchanged) ───────────────────────────────────
        {
            Vector3 spawnPos = transform.position + new Vector3(
                Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f));
            GameObject plc = new GameObject("aaaaaaaaaaaa");
            plc.transform.position = spawnPos;
            ItemPickup.CreatePickup(Plugin.GiveRandomItem(), plc.transform);

            CreatePortal();
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

    private Vector3 GetPrefabHalfExtents(GameObject prefab)
    {
        // Temporarily instantiate *disabled* so Awake/Start don't fire,
        // sample the collider, then immediately destroy.
        GameObject temp = Instantiate(prefab);
        temp.SetActive(false);

        Collider col = temp.GetComponent<Collider>();
        if (col == null) col = temp.GetComponentInChildren<Collider>();

        Vector3 halfExtents = (col != null)
            ? col.bounds.extents * 0.85f   // 15 % margin keeps us conservative
            : new Vector3(0.4f, 0.8f, 0.4f); // sensible humanoid fallback

        Destroy(temp);
        return halfExtents / 2;
    }


    private Transform FindFittingSpawnPoint(Vector3 halfExtents)
    {
        // Shuffle a copy so we don't bias toward index 0 every time.
        List<Transform> shuffled = spawnPoints.OrderBy(_ => Random.value).ToList();

        foreach (Transform pt in shuffled)
        {
            // Raise the centre by halfExtents.y so the box sits *on* the floor
            // rather than half-buried in it.
            Vector3 centre = pt.position + Vector3.up * halfExtents.y;

            bool blocked = Physics.CheckBox(
                centre,
                halfExtents,
                Quaternion.identity,
                ~0,                          // every layer
                QueryTriggerInteraction.Ignore
            );

            if (!blocked) return pt;
        }

        return shuffled[0]; // no point has room
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ultrarogue;
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

    public EnemyType bossEnemyType = EnemyType.MinosPrime;

    public GameObject doorPrefab;
    public GameObject wallPrefab;

    private bool hasSpawnedEnemies = false;
    private bool rewardGiven = false;

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

    IEnumerator SpawnEnemies()
    {
        SpawnCredits = Mathf.RoundToInt((float)SpawnCredits * RogueDifficultyManager.Instance.Difficulty);

        if (SpawnCredits == 0) yield break;

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

            for (int i = 0; i < amountToSpawn; i++)
            {
                yield return new WaitForSeconds(0.05f);

                GameObject enemyPrefab = DefaultReferenceManager.Instance.GetEnemyPrefab(randomEnemy);
                if (enemyPrefab == null) continue;

                Transform spawnPt = spawnPoints[Random.Range(0, spawnPoints.Count)];
                GameObject inst = Instantiate(enemyPrefab, spawnPt.position, enemyPrefab.transform.rotation);
                inst.transform.parent = transform;

                if (amountRadiance != 0)
                {
                    inst.GetComponent<EnemyIdentifier>().BuffAll();
                    amountRadiance--;
                }
            }
        }
        hasSpawnedEnemies = true;
    }

    IEnumerator SpawnBoss()
    {
        yield return new WaitForSeconds(0.5f);

        if (RogueDifficultyManager.Instance == null)
            Debug.LogWarning("[Room] RogueDifficultyManager not found — boss will spawn without difficulty scaling.");

        GameObject bossPrefab = DefaultReferenceManager.Instance.GetEnemyPrefab(bossEnemyType);
        if (bossPrefab == null)
        {
            Debug.LogError($"[Room] Boss prefab for {bossEnemyType} is null — cannot spawn boss.");
            yield break;
        }

        Vector3 spawnPos = transform.position + Vector3.up * 1f;
        GameObject bossInst = Instantiate(bossPrefab, spawnPos, bossPrefab.transform.rotation);
        bossInst.transform.parent = transform;

        var eid = bossInst.GetComponent<EnemyIdentifier>();
        if (eid != null && RogueDifficultyManager.Instance != null)
        {
            int cost = RogueDifficultyManager.Instance.GetCost(bossEnemyType);
            if (SpawnCredits > cost) eid.BuffAll();
        }

        if (bossInst.GetComponent<BossHealthBar>() == null)
            bossInst.AddComponent<BossHealthBar>();

        Debug.Log($"[Room] Boss ({bossEnemyType}) spawned in room {position}.");
        hasSpawnedEnemies = true;

    }

    public void CreateDoor(Transform exit)
    {
#if RUNTIME_ROOMS
        var rh = GetComponent<RuntimeRoomDoorHandler>();
        if (rh != null) { rh.PlaceDoor(exit); return; }
#endif
        if (doorPrefab != null) Instantiate(doorPrefab, exit.position, exit.rotation, transform);
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

    public void CloseOffRoom() { }

    void Awake()
    {
        gameObject.AddComponent<GoreZone>();
    }

    void Update()
    {
        if (!hasSpawnedEnemies || rewardGiven) return;

        // Check if any enemies are still alive in this room
        EnemyIdentifier[] enemies = GetComponentsInChildren<EnemyIdentifier>();

        EnemyIdentifier[] aliveEnemies = enemies.Where((x) => !x.dead).ToArray();

        if (aliveEnemies.Length == 0)
        {
            OnRoomCleared();
        }
    }

    void OnRoomCleared()
    {
        rewardGiven = true;
        if (!isBossRoom)
        {
            int goldAmount = Random.Range(5, 15);

            for (int i = 0; i < goldAmount; i++)
            {
                Vector3 spawnPos = transform.position + new Vector3(
                    Random.Range(-2f, 2f),
                    1f,
                    Random.Range(-2f, 2f)
                );

                GoldPickup.CreatePickup(spawnPos);
            }

            Debug.Log($"[Room] Cleared! Spawned {goldAmount} gold.");
        }
        else
        {
            Vector3 spawnPos = transform.position + new Vector3(
                    Random.Range(-2f, 2f),
                    1f,
                    Random.Range(-2f, 2f)
                );
            ItemPickup.CreatePickup(Plugin.GiveRandomItem(), spawnPos);
        }
        
    }
    public Vector3 GetOffset(Transform exit)
    {
        float dist = Vector3.Distance(exit.position, transform.position);
        Vector3 dir = (exit.position - transform.position).normalized;
        return dir * dist;
    }
}
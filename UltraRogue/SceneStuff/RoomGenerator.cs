using System;
using System.Collections;
using System.Collections.Generic;
using ULTRAKILL.Portal;
using ULTRAKILL.Portal.Geometry;
using Ultrarogue;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class RoomGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    int minRooms = 5;
    int maxRooms = 12;
    [Header("Room Prefabs")]
    [Tooltip("Normal combat room prefabs — one is chosen at random per room.")]
    public List<Room> roomPrefabs = new List<Room>();

    [Tooltip("Optional dedicated prefab for each special room type.\nFalls back to a random roomPrefab when left empty.")]
    public Room treasureRoomPrefab;
    public Room shopRoomPrefab;
    public Room gamblingRoomPrefab;
    public Room bossRoomPrefab;

    [Header("Boss Room Settings")]
    [Tooltip("EnemyType spawned in the boss room.")]
    public EnemyType bossEnemyType = EnemyType.MinosPrime;

    public Dictionary<Vector2Int, Room> placedRooms = new Dictionary<Vector2Int, Room>();

    List<Vector2Int> path = new List<Vector2Int>();

    readonly Vector2Int[] directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public static RoomGenerator Instance { get; private set; }

    [Header("Room Size")]
    float roomWidth = 59.5f;
    float roomHeight = 30f;

    // Add to [Header("Generation Settings")] block:
    [Header("Performance")]
    [Tooltip("How many grid cells away from the player rooms stay active (1 = current + immediate neighbors).")]
    int activationRadius = 2;

    // Add field near the other privates:
    private bool _generationComplete = false;
    private float _nextActivationCheck = 0f;
    private const float ActivationCheckInterval = 0.3f;

    void Awake()
    {
        Instance = this;
        StartCoroutine(GenerateRooms());
    }

    public void RegenerateRooms()
    {
        StopAllCoroutines();
        StatsManager.Instance.StopTimer();
        MusicManager.Instance.StopMusic();
        // Destroy all existing rooms
        foreach (var room in placedRooms.Values)
        {
            if (room != null)
                Destroy(room.gameObject);
        }

        // Clear data
        placedRooms.Clear();
        path.Clear();
        // Add Reset in RegenerateRooms(), before StartCoroutine:
        _generationComplete = false;
        if (MinimapUI.Instance != null) MinimapUI.Instance.ClearAndReset();
        // Start fresh generation
        StartCoroutine(GenerateRooms(false));
    }

    IEnumerator GenerateRooms(bool firstTime = true)
    {
        yield return new WaitUntil(() => DefaultReferenceManager.Instance != null);
        if (!firstTime) yield return new WaitForSeconds(6f);
        if (roomPrefabs == null || roomPrefabs.Count == 0)
        {
            Debug.LogWarning("[RoomGenerator] No room prefabs assigned — skipping generation.");
            yield break;
        }

        int count = Mathf.RoundToInt((float)Random.Range(minRooms, maxRooms)
                      * Plugin.CurrentDifficulty);

        Vector2Int current = Vector2Int.zero;
        PlaceRoom(current, isStart: true);

        int placed = 1;
        int safetyBreak = 0;

        while (placed < count && safetyBreak++ < 1000)
        {
            Vector2Int dir = directions[Random.Range(0, directions.Length)];
            int steps = Random.Range(1, 4);

            for (int i = 0; i < steps && placed < count; i++)
            {
                Vector2Int next = current + dir;
                if (placedRooms.ContainsKey(next)) break;

                PlaceRoom(next);
                current = next;
                placed++;
            }

            int back = Random.Range(1, path.Count);
            current = path[path.Count - 1 - Mathf.Min(back, path.Count - 1)];
        }

        PlaceSpecialRooms();
        DesignateBossRoom();
        FinalizeConnections();
        BuildNavMesh();
        // Add after BuildNavMesh(); in GenerateRooms():


        int special = 3;
        Debug.Log($"[RoomGenerator] Spawned {placed} combat rooms + {special} special rooms + 1 boss room.");

        if (!firstTime)
        {
            // Place epic portal
            GameObject quad1 = new GameObject("PortalEntry");
            quad1.transform.position = new Vector3(0, 10, 0);
            quad1.transform.Rotate(-90, 0, 0);
            GameObject quad2 = new GameObject("PortalExit");
            quad2.transform.position = GameObject.Find("Pit").transform.Find("Cube (2)").position + Vector3.up;
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
            yield return new WaitForEndOfFrame();
            portal1.onExitTravel = new UnityEventPortalTravel();
            portal1.onExitTravel.AddListener((IP, D) =>
            {
                if(IP.travellerType == PortalTravellerType.PLAYER)
                {
                    StartCoroutine(StartThingggg(quad1, quad2));
                }
            });
            RogueDifficultyManager.Instance.MoveStage();
            StartCoroutine(StartThingggg(quad1, quad2));
        }
    }
    IEnumerator StartThingggg(GameObject quad1, GameObject quad2)
    {
        yield return new WaitForSeconds(0.5f);
        NewMovement nm = MonoSingleton<NewMovement>.Instance;
        GunControl gc = MonoSingleton<GunControl>.Instance;
        GameStateManager.Instance.PopState("pit-falling");
        if (!nm.activated)
        {
            nm.activated = true;
            nm.cc.activated = true;
            nm.cc.CameraShake(1f);
            nm.cc.enabled = true;
        }

        gc.YesWeapon();
        MonoSingleton<PlayerActivatorRelay>.Instance.ResetIndex();
        MonoSingleton<PlayerActivatorRelay>.Instance.Activate();
        if (nm.levelOver)
        {
            nm.levelOver = false;
            MonoSingleton<StatsManager>.Instance.UnhideShit();
        }
        PlayerActivator.lastActivatedPosition = MonoSingleton<NewMovement>.Instance.transform.position;
        MonoSingleton<FistControl>.Instance.YesFist();

        quad1.transform.position = Vector3.one * 9999; // moving it fucking far away because idk why it doesnt want to remove the portal :(
        quad2.transform.position = Vector3.one * 9999; // moving it fucking far away because idk why it doesnt want to remove the portal :(

        yield return new WaitForSeconds(0.5f);
        Destroy(quad2);
        Destroy(quad1);
        StatsManager.Instance.StartTimer();
        MusicManager.Instance.StartMusic();
    }

    Transform GetExitFacing(Room room, Vector2Int dir)
    {
        if (dir == Vector2Int.up) return room.exitTop;
        if (dir == Vector2Int.down) return room.exitBottom;
        if (dir == Vector2Int.left) return room.exitLeft;
        if (dir == Vector2Int.right) return room.exitRight;
        return null;
    }


    void AlignRoomToNeighborExit(Room room, Vector2Int gridPos)
    {
        foreach (var dir in directions)
        {
            Vector2Int neighborPos = gridPos + dir;
            if (!placedRooms.TryGetValue(neighborPos, out Room neighbor)) continue;

            Transform neighborExit = GetExitFacing(neighbor, -dir);

            Transform myExit = GetExitFacing(room, dir);

            if (neighborExit == null || myExit == null) continue;

            float myExitLocalY = myExit.position.y - room.transform.position.y;

            float targetY = neighborExit.position.y - myExitLocalY;

            Vector3 p = room.transform.position;
            room.transform.position = new Vector3(p.x, targetY, p.z);

            Debug.Log($"[RoomGenerator] Aligned {gridPos} via {dir}: " +
                      $"room.y={targetY:F2}  (neighbourExitY={neighborExit.position.y:F2}  myExitLocalY={myExitLocalY:F2})");
            return;
        }

        Debug.LogWarning($"[RoomGenerator] Could not find a valid exit pair to align room at {gridPos} — left at y=0.");
    }


    void PlaceRoom(Vector2Int gridPos, bool isStart = false)
    {
        Room prefab = isStart
            ? roomPrefabs[0]
            : roomPrefabs[Random.Range(0, roomPrefabs.Count)];

        Vector3 worldPos = new Vector3(gridPos.x * roomWidth, 0f, gridPos.y * roomHeight);

        Room room = Instantiate(prefab, worldPos, Quaternion.identity);
        room.position = gridPos;
        room.roomType = RoomType.Normal;
        if (isStart)
            room.SpawnCredits = 0;
        if (!isStart)
            AlignRoomToNeighborExit(room, gridPos);

        placedRooms[gridPos] = room;
        path.Add(gridPos);

        if (isStart)
        {
            var player = NewMovement.Instance;
            if (player != null)
                player.transform.position = worldPos + Vector3.up * 2f;
        }
    }

    void PlaceSpecialRooms()
    {
        List<Vector2Int> candidates = FindDeadEndCandidates();

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        TryPlaceSpecialRoom(ref candidates, RoomType.Treasure);
        TryPlaceSpecialRoom(ref candidates, RoomType.Shop);
        TryPlaceSpecialRoom(ref candidates, RoomType.Gambling);
    }

    List<Vector2Int> FindDeadEndCandidates()
    {
        var deadEnds = new List<Vector2Int>();

        foreach (var pos in placedRooms.Keys)
        {
            foreach (var dir in directions)
            {
                Vector2Int candidate = pos + dir;
                if (placedRooms.ContainsKey(candidate)) continue;

                int neighbourCount = 0;
                foreach (var d in directions)
                    if (placedRooms.ContainsKey(candidate + d))
                        neighbourCount++;

                // Only true dead-ends: exactly one existing-room neighbour,
                // and not already in the list.
                if (neighbourCount == 1 && !deadEnds.Contains(candidate))
                    deadEnds.Add(candidate);
            }
        }

        if (deadEnds.Count < 3)
            Debug.LogWarning("[RoomGenerator] Fewer than 3 dead-end slots found for special rooms " +
                             $"({deadEnds.Count} available). Some special rooms may be skipped.");

        return deadEnds;
    }

    void TryPlaceSpecialRoom(ref List<Vector2Int> candidates, RoomType roomType)
    {
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[RoomGenerator] No candidate position for {roomType} room — skipping.");
            return;
        }

        Vector2Int pos = candidates[0];
        candidates.RemoveAt(0);

        // ── After picking this slot, remove every candidate that sits directly
        //    next to it so special rooms can never be adjacent to each other. ──
        candidates.RemoveAll(c =>
        {
            foreach (var d in directions)
                if (c == pos + d) return true;
            return false;
        });

        Room prefab = roomType switch
        {
            RoomType.Treasure => treasureRoomPrefab != null ? treasureRoomPrefab : roomPrefabs[Random.Range(0, roomPrefabs.Count)],
            RoomType.Shop => shopRoomPrefab != null ? shopRoomPrefab : roomPrefabs[Random.Range(0, roomPrefabs.Count)],
            RoomType.Gambling => gamblingRoomPrefab != null ? gamblingRoomPrefab : roomPrefabs[Random.Range(0, roomPrefabs.Count)],
            _ => roomPrefabs[Random.Range(0, roomPrefabs.Count)],
        };

        Vector3 worldPos = new Vector3(pos.x * roomWidth, 0f, pos.y * roomHeight);

        Room room = Instantiate(prefab, worldPos, Quaternion.identity);
        room.position = pos;
        room.roomType = roomType;
        if(roomType != RoomType.Boss)
            room.SpawnCredits = 0;

        AlignRoomToNeighborExit(room, pos);

        placedRooms[pos] = room;
        Debug.Log($"[RoomGenerator] {roomType} room placed at grid {pos}.");
    }


    void DesignateBossRoom()
    {
        Vector2Int bossPos = Vector2Int.zero;
        int bestManhattan = -1;

        foreach (var kvp in placedRooms)
        {
            if (kvp.Key == Vector2Int.zero) continue;
            if (kvp.Value.roomType != RoomType.Normal) continue;

            int neighbourCount = 0;
            foreach (var dir in directions)
                if (placedRooms.ContainsKey(kvp.Key + dir))
                    neighbourCount++;

            if (neighbourCount != 1) continue;

            int manhattan = Mathf.Abs(kvp.Key.x) + Mathf.Abs(kvp.Key.y);
            if (manhattan > bestManhattan)
            {
                bestManhattan = manhattan;
                bossPos = kvp.Key;
            }
        }

        if (bestManhattan < 0)
        {
            Debug.LogWarning("[RoomGenerator] No dead-end normal room found for boss — falling back to farthest normal room.");
            foreach (var kvp in placedRooms)
            {
                if (kvp.Key == Vector2Int.zero) continue;
                if (kvp.Value.roomType != RoomType.Normal) continue;

                int manhattan = Mathf.Abs(kvp.Key.x) + Mathf.Abs(kvp.Key.y);
                if (manhattan > bestManhattan)
                {
                    bestManhattan = manhattan;
                    bossPos = kvp.Key;
                }
            }
        }

        if (bestManhattan < 0)
        {
            Debug.LogWarning("[RoomGenerator] Could not find a valid boss room candidate.");
            return;
        }

        Room oldRoom = placedRooms[bossPos];
        Vector3 oldWorldPos = oldRoom.transform.position;
        Destroy(oldRoom.gameObject);

        placedRooms.Remove(bossPos);

        Room prefab = bossRoomPrefab != null
            ? bossRoomPrefab
            : roomPrefabs[Random.Range(0, roomPrefabs.Count)];

        Room bossRoom = Instantiate(prefab, new Vector3(oldWorldPos.x, 0f, oldWorldPos.z), Quaternion.identity);
        bossRoom.position = bossPos;
        bossRoom.roomType = RoomType.Boss;

        AlignRoomToNeighborExit(bossRoom, bossPos);

        placedRooms[bossPos] = bossRoom;
        Debug.Log($"[RoomGenerator] Boss room at grid {bossPos} (Manhattan {bestManhattan}).");
    }

    void BuildNavMesh() => StartCoroutine(buildDaMesh());

    void NavmeshBuilt()
    {
        SandboxNavmesh instance = MonoSingleton<SandboxNavmesh>.Instance;
        instance.navmeshBuilt = (UnityAction)Delegate.Remove(
            instance.navmeshBuilt, new UnityAction(NavmeshBuilt));
        _generationComplete = true;
        UpdateRoomActivation(); 
    }

    IEnumerator buildDaMesh()
    {
        yield return new WaitForSeconds(0.1f);

        if (SandboxNavmesh.Instance != null)
        {
            yield return null;
            SandboxNavmesh instance = MonoSingleton<SandboxNavmesh>.Instance;
            instance.navmeshBuilt = (UnityAction)Delegate.Combine(
                instance.navmeshBuilt, new UnityAction(NavmeshBuilt));
            MonoSingleton<SandboxNavmesh>.Instance.Rebake();
            yield break;
        }

        NavMeshSurface surface = FindObjectOfType<NavMeshSurface>();
        if (surface == null) surface = gameObject.AddComponent<NavMeshSurface>();
        surface.navMeshData = null;
        surface.BuildNavMesh();
        yield return new WaitUntil(() => surface.navMeshData != null);
        yield return new WaitForSeconds(0.25f);
        _generationComplete = true;
        UpdateRoomActivation();
    }

    void FinalizeConnections()
    {
        foreach (var kvp in placedRooms)
        {
            Vector2Int pos = kvp.Key;
            Room room = kvp.Value;

            HandleExit(room, pos, Vector2Int.up, room.exitTop);
            HandleExit(room, pos, Vector2Int.down, room.exitBottom);
            HandleExit(room, pos, Vector2Int.left, room.exitLeft);
            HandleExit(room, pos, Vector2Int.right, room.exitRight);
        }
        if (MinimapUI.Instance != null) MinimapUI.Instance.BuildMinimap(placedRooms);
    }

    void HandleExit(Room room, Vector2Int pos, Vector2Int dir, Transform exit)
    {
        if (exit == null) return;

        Vector2Int neighborPos = pos + dir;

        if (placedRooms.TryGetValue(neighborPos, out Room neighbor))
        {
            Transform neighborExit = GetExitFacing(neighbor, -dir);

            if (neighborExit == null)
            {
                room.CreateWall(exit);
                return;
            }

            float yDiff = Mathf.Abs(exit.position.y - neighborExit.position.y);
            const float yTolerance = 0.1f;

            if (yDiff > yTolerance)
            {
                room.CreateWall(exit);

                Debug.LogWarning(
                    $"[RoomGenerator] Exit mismatch at {pos} → {neighborPos} (ΔY={yDiff:F2}) → wall placed."
                );

                return;
            }

            // --- ORIGINAL LOGIC ---
            bool useMyDoor =
                IsSpecialRoomPriority(room.roomType, neighbor.roomType) ||
                (room.roomType == neighbor.roomType && IsPrimary(pos, neighborPos));

            if (useMyDoor)
                room.CreateDoor(exit);
            else
                room.DisableExit(exit);
        }
        else
        {
            room.CreateWall(exit);
        }
    }
    // Add new methods:
    void Update()
    {
        if (!_generationComplete) return;
        if (Time.time < _nextActivationCheck) return;
        _nextActivationCheck = Time.time + ActivationCheckInterval;
        UpdateRoomActivation();
    }

    void UpdateRoomActivation()
    {
        var player = NewMovement.Instance;
        if (player == null) return;

        Vector2Int playerGrid = WorldToGrid(player.transform.position);

        foreach (var kvp in placedRooms)
        {
            if (kvp.Value == null) continue;

            int manhattanDist = Mathf.Abs(kvp.Key.x - playerGrid.x)
                              + Mathf.Abs(kvp.Key.y - playerGrid.y);

            bool shouldBeActive = manhattanDist <= activationRadius;

            if (kvp.Value.gameObject.activeSelf != shouldBeActive)
                kvp.Value.gameObject.SetActive(shouldBeActive);
        }
    }

    public Vector2Int WorldToGrid(Vector3 worldPos) => new Vector2Int(
        Mathf.RoundToInt(worldPos.x / roomWidth),
        Mathf.RoundToInt(worldPos.z / roomHeight)
    );

    bool IsSpecialRoomPriority(RoomType mine, RoomType theirs) =>
        mine != RoomType.Normal && theirs == RoomType.Normal;

    bool IsPrimary(Vector2Int a, Vector2Int b) =>
        a.x != b.x ? a.x < b.x : a.y < b.y;
}
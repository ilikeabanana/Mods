using BepInEx.Bootstrap;
using Steamworks.Ugc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ULTRAKILL.Portal;
using ULTRAKILL.Portal.Geometry;
using Ultrarogue;
using Ultrarogue.SceneStuff;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class RoomGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    int minRooms = 5;
    int maxRooms = 13;
    [Header("Room Prefabs")]
    [Tooltip("Normal combat room prefabs — one is chosen at random per room.")]
    public List<Room> roomPrefabs = new List<Room>();

    [Tooltip("Optional dedicated prefab for each special room type.\nFalls back to a random roomPrefab when left empty.")]
    public Room treasureRoomPrefab;
    public Room shopRoomPrefab;
    public Room gamblingRoomPrefab;
    public Room bossRoomPrefab;
    public Room planetariumPrefab;
    public Room startRoomPrefab;
    [Tooltip("Other special rooms")]
    public List<Room> specialRoomPrefabs = new List<Room>();
    [Tooltip("SECRET ROOOMMMS")]
    public List<Room> SecretRoomPrefabs = new List<Room>();

    [Tooltip("Large room prefabs (RoomSizeWidth > 1 or RoomSizeHeight > 1). " +
             "These are never instantiated directly — they are used as data sources " +
             "to spawn sub-room GameObjects that each occupy one grid cell.")]
    public List<Room> largeRoomPrefabs = new List<Room>();

    [Header("Boss Room Settings")]
    [Tooltip("EnemyType spawned in the boss room.")]
    public EnemyType bossEnemyType = EnemyType.MinosPrime;

    public Dictionary<Vector2Int, Room> placedRooms = new Dictionary<Vector2Int, Room>();

    // Tracks which grid cells are part of a large room group, mapping every cell
    // back to the anchor cell of that group. Used to avoid partial overlaps and
    // to clean up all sub-rooms when a large room needs to be replaced.
    private Dictionary<Vector2Int, Vector2Int> _largeRoomAnchorOf = new Dictionary<Vector2Int, Vector2Int>();
    // Maps anchor → list of all cells that belong to that large room group.
    private Dictionary<Vector2Int, List<Vector2Int>> _largeRoomCells = new Dictionary<Vector2Int, List<Vector2Int>>();
    // Maps anchor → the actual instantiated room geometry GameObject.
    private Dictionary<Vector2Int, GameObject> _largeRoomGeometry = new Dictionary<Vector2Int, GameObject>();

    List<Vector2Int> path = new List<Vector2Int>();

    readonly Vector2Int[] directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public List<AudioClip> CalmMusic = new List<AudioClip>();
    public List<AudioClip> UnCalmMusic = new List<AudioClip>();

    public static RoomGenerator Instance { get; private set; }

    [Header("Room Size")]
    float roomWidth = 60f;
    float roomHeight = 30f;

    [Header("Performance")]
    [Tooltip("How many grid cells away from the player rooms stay active (1 = current + immediate neighbors).")]
    int activationRadius = 2;

    private bool _generationComplete = false;
    private float _nextActivationCheck = 0f;
    private const float ActivationCheckInterval = 0.3f;
    private int guaranteedCombatRoomsWithCredits;
    private const int MinCombatRoomsWithCredits = 4;

    public float planetChance = 0.01f;

    void Awake()
    {
        Room.roomIndex = 0;
        Instance = this;
        StartCoroutine(GenerateRooms());
    }
    bool canDoTheErrorRoom = false;

    public void RegenerateRooms()
    {
        StopAllCoroutines();
        planetChance += 0.2f;
        StatsManager.Instance.StopTimer();
        MusicManager.Instance.StopMusic();

        foreach (var room in placedRooms.Values)
        {
            if (room != null)
                Destroy(room.gameObject);
        }

        foreach (var geometry in _largeRoomGeometry.Values)
        {
            if (geometry != null)
                Destroy(geometry);
        }

        placedRooms.Clear();
        path.Clear();
        _largeRoomAnchorOf.Clear();
        _largeRoomCells.Clear();
        _largeRoomGeometry.Clear();

        _generationComplete = false;
        if (MinimapUI.Instance != null) MinimapUI.Instance.ClearAndReset();
        RogueDifficultyManager.Instance.MoveStage();
        StartCoroutine(GenerateRooms(false));
    }
    IEnumerator GenerateRooms(bool firstTime = true)
    {
        canDoTheErrorRoom = false;
        guaranteedCombatRoomsWithCredits = 0;

        yield return new WaitUntil(() => DefaultReferenceManager.Instance != null);
        if (!firstTime) yield return new WaitForSeconds(6f);
        if (roomPrefabs == null || roomPrefabs.Count == 0)
        {
            Debug.LogWarning("[RoomGenerator] No room prefabs assigned — skipping generation.");
            yield break;
        }

        float floor = RogueDifficultyManager.Instance.floor;

        int count = Mathf.RoundToInt(
            6f + Mathf.Log(floor + 1f, 2f) * 2f
            + RogueDifficultyManager.RoomRNG.Next(-1, 2)
        );

        Vector2Int current = Vector2Int.zero;
        _ = PlaceRoom(current, isStart: true, direction: Vector2Int.zero);

        int placed = 1;
        int safetyBreak = 0;

        while (placed < count && safetyBreak++ < 1000)
        {
            Vector2Int dir = directions[RogueDifficultyManager.RoomRNG.Next(0, directions.Length)];
            int steps = RogueDifficultyManager.RoomRNG.Next(1, 2);

            for (int i = 0; i < steps && placed < count; i++)
            {
                Vector2Int next = current + dir;
                if (placedRooms.ContainsKey(next)) break;

                // The room we're expanding from must have an exit facing this direction.
                if (!RoomHasExit(placedRooms[current], dir)) break;

                // At least one normal prefab must have a return exit (facing back toward current).
                List<Room> compatible = CompatiblePrefabs(-dir);
                if (compatible.Count == 0) break;

                current = PlaceRoom(next, prefabPool: compatible, direction: dir);
                placed++;
            }

            int back = RogueDifficultyManager.RoomRNG.Next(1, path.Count);
            current = path[path.Count - 1 - Mathf.Min(back, path.Count - 1)];
        }

        DesignateBossRoom();
        PlaceSpecialRooms();
        EnforceEmptyRoomConnectivity();
        FinalizeConnections();
        BuildNavMesh();

        int special = 3;
        Debug.Log($"[RoomGenerator] Spawned {placed} combat rooms + {special} special rooms + 1 boss room.");

        if (!firstTime)
        {
            yield return new WaitForSeconds(2);
            // Place epic portal
            GameObject quad1 = new GameObject("PortalEntry");
            quad1.transform.position = new Vector3(0, 65, 0);
            quad1.transform.Rotate(-90, 0, 0);
            GameObject quad2 = new GameObject("PortalExit");
            quad2.transform.position = GameObject.Find("Pit").transform.Find("Cube (2)").position + Vector3.up * 4;
            quad2.transform.Rotate(90, 0, 0);

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
            portal1.onEntryTravel = new UnityEventPortalTravel();
            portal1.onEntryTravel.AddListener((IP, D) =>
            {
                if (IP.travellerType == PortalTravellerType.PLAYER)
                {
                    StartCoroutine(StartThingggg(quad1, quad2));
                }
            });
        }
        else
        {
            canDoTheErrorRoom = true;
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

        quad1.transform.position = Vector3.one * 9999;
        quad2.transform.position = Vector3.one * 9999;

        yield return new WaitForSeconds(0.5f);
        Destroy(quad2);
        Destroy(quad1);
        StatsManager.Instance.StartTimer();

        // choose random music
        int index = Random.Range(0, CalmMusic.Count);
        MusicManager.Instance.cleanTheme.clip = CalmMusic[index];
        MusicManager.Instance.bossTheme.clip = UnCalmMusic[index];
        MusicManager.Instance.battleTheme.clip = UnCalmMusic[index];

        MusicManager.Instance.StartMusic();
        canDoTheErrorRoom = true;

        foreach (var item in Plugin.items)
        { // ok
            item.Key.OnNewFloor(item.Value);
        }
    }

    // ─── Exit helpers ────────────────────────────────────────────────────────

    Transform GetExitFacing(Room room, Vector2Int dir)
    {
        Plugin.Logger.LogInfo($"Getting exit for room {room.gameObject.name}");
        return room.GetExit(dir);
    }

    /// <summary>Returns true when the room (or prefab) has a non-null exit in <paramref name="dir"/>.</summary>
    bool RoomHasExit(Room room, Vector2Int dir) => GetExitFacing(room, dir) != null;

    /// <summary>
    /// Returns the subset of <see cref="roomPrefabs"/> that have a non-null exit
    /// in <paramref name="dir"/>. Used to guarantee a compatible prefab exists
    /// before committing to a grid cell.
    /// </summary>
    List<Room> CompatiblePrefabs(Vector2Int dir) =>
        roomPrefabs.FindAll(p => RoomHasExit(p, dir));

    /// <summary>
    /// Returns true when <paramref name="prefab"/> has exits in every direction
    /// required by its already-placed neighbours at <paramref name="gridPos"/>.
    /// </summary>
    bool PrefabFitsNeighbours(Room prefab, Vector2Int gridPos)
    {
        foreach (var dir in directions)
        {
            if (!placedRooms.ContainsKey(gridPos + dir)) continue;

            Room neighbour = placedRooms[gridPos + dir];
            bool neighbourFacesUs = RoomHasExit(neighbour, -dir);
            bool weFaceNeighbour = RoomHasExit(prefab, dir);

            if (neighbourFacesUs && !weFaceNeighbour) return false;
        }
        return true;
    }

    // ─── Room alignment ───────────────────────────────────────────────────────

    void AlignRoomToNeighborExit(Room room, Vector2Int gridPos)
    {
        foreach (var dir in directions)
        {
            Vector2Int neighborPos = gridPos + dir;
            if (!placedRooms.TryGetValue(neighborPos, out Room neighbor)) continue;

            // Don't align to a sibling sub-room of the same large room.
            if (_largeRoomAnchorOf.TryGetValue(gridPos, out Vector2Int myAnchor) &&
                _largeRoomAnchorOf.TryGetValue(neighborPos, out Vector2Int theirAnchor) &&
                myAnchor == theirAnchor)
                continue;

            Transform neighborExit = GetExitFacing(neighbor, -dir);
            Transform myExit = GetExitFacing(room, dir);

            if (neighborExit == null || myExit == null) continue;

            float myExitLocalY = myExit.position.y - room.transform.position.y;
            float targetY = neighborExit.position.y - myExitLocalY;

            Vector3 p = room.transform.position;
            room.transform.position = new Vector3(p.x, targetY, p.z);
            return;
        }
    }

    // ─── PlaceRoom ────────────────────────────────────────────────────────────

    // Returns the furthest grid cell that was actually occupied in the placement
    // direction. For a 1x1 room this is just gridPos. For a large room it's the
    // far edge cell in `direction` so the generation loop doesn't walk into it.
    Vector2Int PlaceRoom(Vector2Int gridPos, Vector2Int direction, bool isStart = false, List<Room> prefabPool = null)
    {
        // Hard guard: never place anything at a cell that's already occupied.
        if (!isStart && placedRooms.ContainsKey(gridPos))
        {
            Debug.LogError($"[RoomGenerator] PlaceRoom called on already-occupied cell {gridPos} — skipping!");
            return gridPos;
        }

        Room prefab;

        if (isStart)
        {
            prefab = startRoomPrefab;
        }
        else
        {
            List<Room> pool = prefabPool ?? roomPrefabs;

            // Try to place a large room (25% chance when candidates exist and all their
            // cells are free — including cells beyond the anchor).
            if (largeRoomPrefabs != null && largeRoomPrefabs.Count > 0)
            {
                List<Room> largeCandidates = largeRoomPrefabs.FindAll(p =>
                    LargeRoomCellsFree(gridPos, p.RoomSizeWidth, p.RoomSizeHeight));

                if (largeCandidates.Count > 0 && RogueDifficultyManager.RoomRNG.Next(0, roomPrefabs.Count) == 0)
                {
                    prefab = largeCandidates[RogueDifficultyManager.RoomRNG.Next(0, largeCandidates.Count)];
                    ExpandLargeRoom(prefab, gridPos, isStart);
                    return FarEdgeCell(gridPos, prefab.RoomSizeWidth, prefab.RoomSizeHeight, direction);
                }
            }

            List<Room> fullyCompatible = pool.FindAll(p => PrefabFitsNeighbours(p, gridPos));

            if (fullyCompatible.Count > 0)
                prefab = fullyCompatible[RogueDifficultyManager.RoomRNG.Next(0, fullyCompatible.Count)];
            else
            {
                Debug.LogWarning($"[RoomGenerator] No fully-compatible prefab found at {gridPos}; " +
                                  "falling back to partially-compatible pool.");
                prefab = pool[RogueDifficultyManager.RoomRNG.Next(0, pool.Count)];
            }
        }

        // Safety: if a normal prefab somehow has a large size, expand it.
        if (prefab.RoomSizeWidth > 1 || prefab.RoomSizeHeight > 1)
        {
            ExpandLargeRoom(prefab, gridPos, isStart);
            return FarEdgeCell(gridPos, prefab.RoomSizeWidth, prefab.RoomSizeHeight, direction);
        }

        Vector3 worldPos = new Vector3(gridPos.x * roomWidth, 0f, gridPos.y * roomHeight);
        Room room = Instantiate(prefab, worldPos, Quaternion.identity);
        room.position = gridPos;
        room.roomType = RoomType.Normal;
        room.SpawnCredits = isStart ? 0 : 3;

        if (!isStart) AlignRoomToNeighborExit(room, gridPos);

        placedRooms[gridPos] = room;
        path.Add(gridPos);
        return gridPos;
    }

    // ─── Large room expansion ─────────────────────────────────────────────────

    /// <summary>
    /// Decomposes a large room prefab into individual 1x1 sub-room GameObjects,
    /// one per grid cell. Each sub-room gets the outer exits from the source prefab's
    /// exit arrays; any edge shared with another sub-room of the same large room gets
    /// a fake exit Transform parented far away so the door/wall system ignores it.
    /// </summary>
    void ExpandLargeRoom(Room source, Vector2Int anchorPos, bool isStart)
    {
        Vector3 anchorWorldPos = new Vector3(anchorPos.x * roomWidth, 0f, anchorPos.y * roomHeight);
        Room actualRoom = Instantiate(source, anchorWorldPos, Quaternion.identity);

        int w = source.RoomSizeWidth;
        int h = source.RoomSizeHeight;

        Debug.Log($"[RoomGenerator] ExpandLargeRoom: placing {source.name} ({w}x{h}) at anchor {anchorPos}. " +
                  $"Cells to claim: {string.Join(", ", System.Linq.Enumerable.Range(0, w).SelectMany(lx => System.Linq.Enumerable.Range(0, h).Select(ly => $"({anchorPos.x + lx},{anchorPos.y + ly})")))}");

        var subRooms = new List<(Vector2Int cell, Room sub)>();
        var cellList = new List<Vector2Int>();

        for (int lx = 0; lx < w; lx++)
        {
            for (int ly = 0; ly < h; ly++)
            {
                Vector2Int cell = anchorPos + new Vector2Int(lx, ly);

                // Hard guard: if this cell is already taken, abort the whole large room.
                if (placedRooms.ContainsKey(cell))
                {
                    Debug.LogError($"[RoomGenerator] ExpandLargeRoom: cell {cell} (lx={lx},ly={ly}) is already " +
                                   $"occupied by '{placedRooms[cell].gameObject.name}' — aborting large room placement at {anchorPos}!");
                    // Clean up any sub-rooms we already created in this loop.
                    foreach (var (c, s) in subRooms)
                    {
                        Destroy(s.gameObject);
                        placedRooms.Remove(c);
                        _largeRoomAnchorOf.Remove(c);
                        path.Remove(c);
                    }
                    Destroy(actualRoom.gameObject);
                    return;
                }

                Vector3 worldPos = new Vector3(cell.x * roomWidth, 0f, cell.y * roomHeight);

                GameObject go = new GameObject($"Room_{cell.x}_{cell.y}");
                go.transform.position = worldPos;

                Room sub = go.AddComponent<Room>();

                sub.position = cell;
                sub.roomType = RoomType.Normal;
                sub.SpawnCredits = isStart ? 0 : actualRoom.SpawnCredits;
                sub.spawnChance = actualRoom.spawnChance;
                sub.doorPrefab = actualRoom.doorPrefab;
                sub.wallPrefab = actualRoom.wallPrefab;
                sub.RoomSizeWidth = 1;
                sub.RoomSizeHeight = 1;

                foreach (Transform srcPt in actualRoom.spawnPoints)
                {
                    if (srcPt == null) continue;
                    GameObject ptGo = new GameObject("SpawnPoint");
                    ptGo.transform.SetParent(go.transform);
                    ptGo.transform.position = srcPt.position;
                    sub.spawnPoints.Add(ptGo.transform);
                }
                if (sub.spawnPoints.Count == 0)
                    sub.spawnPoints.Add(go.transform);

                sub.exitLeft = AssignSubExit(sub, actualRoom, Vector2Int.left, lx, ly, w, h);
                sub.exitRight = AssignSubExit(sub, actualRoom, Vector2Int.right, lx, ly, w, h);
                sub.exitTop = AssignSubExit(sub, actualRoom, Vector2Int.up, lx, ly, w, h);
                sub.exitBottom = AssignSubExit(sub, actualRoom, Vector2Int.down, lx, ly, w, h);

                placedRooms[cell] = sub;
                path.Add(cell);
                subRooms.Add((cell, sub));
                cellList.Add(cell);

                // Register this cell as belonging to the large room group.
                _largeRoomAnchorOf[cell] = anchorPos;
            }
        }

        // Store the full cell list for the group so we can clean up all sub-rooms at once.
        _largeRoomCells[anchorPos] = cellList;
        // Store the actual geometry so RemoveLargeRoomGroup can destroy it too.
        _largeRoomGeometry[anchorPos] = actualRoom.gameObject;

        if (!isStart)
        {
            foreach (var (cell, sub) in subRooms)
                AlignRoomToNeighborExit(sub, cell);
        }
    }

    /// <summary>
    /// Returns the correct exit Transform for one face of a sub-room inside a large room.
    /// Internal edges get a far-away fake Transform; external edges pull from the
    /// source prefab's exit arrays (left/right indexed by ly, top/bottom by lx).
    /// </summary>
    Transform AssignSubExit(Room sub, Room source, Vector2Int dir, int lx, int ly, int w, int h)
    {
        int nx = lx + dir.x;
        int ny = ly + dir.y;
        bool isInternal = (nx >= 0 && nx < w && ny >= 0 && ny < h);

        if (isInternal)
        {
            GameObject fake = new GameObject("FakeExit_" + dir);
            fake.transform.SetParent(sub.transform);
            fake.transform.position = new Vector3(1000f, 1000f, 1000f);
            return fake.transform;
        }

        if (dir == Vector2Int.left)
            return (source.exitsLeft != null && ly < source.exitsLeft.Length)
                ? source.exitsLeft[ly] : null;

        if (dir == Vector2Int.right)
            return (source.exitsRight != null && ly < source.exitsRight.Length)
                ? source.exitsRight[ly] : null;

        if (dir == Vector2Int.up)
            return (source.exitsTop != null && lx < source.exitsTop.Length)
                ? source.exitsTop[lx] : null;

        if (dir == Vector2Int.down)
            return (source.exitsBottom != null && lx < source.exitsBottom.Length)
                ? source.exitsBottom[lx] : null;

        return null;
    }

    /// <summary>
    /// Returns true when every grid cell a large room (w×h) would occupy starting
    /// at <paramref name="anchor"/> is currently unoccupied.
    /// </summary>
    bool LargeRoomCellsFree(Vector2Int anchor, int w, int h)
    {
        for (int lx = 0; lx < w; lx++)
            for (int ly = 0; ly < h; ly++)
                if (placedRooms.ContainsKey(anchor + new Vector2Int(lx, ly)))
                    return false;
        return true;
    }

    /// <summary>
    /// Removes all sub-rooms that belong to the same large room group as
    /// <paramref name="anyCell"/> from <see cref="placedRooms"/> and destroys
    /// their GameObjects. Returns the anchor position of the removed group.
    /// </summary>
    Vector2Int RemoveLargeRoomGroup(Vector2Int anyCell)
    {
        Vector2Int anchor = _largeRoomAnchorOf[anyCell];

        if (_largeRoomCells.TryGetValue(anchor, out List<Vector2Int> cells))
        {
            foreach (Vector2Int cell in cells)
            {
                if (placedRooms.TryGetValue(cell, out Room sub))
                {
                    Destroy(sub.gameObject);
                    placedRooms.Remove(cell);
                }
                _largeRoomAnchorOf.Remove(cell);
                path.Remove(cell);
            }
            _largeRoomCells.Remove(anchor);
        }

        // Also destroy the actual room geometry.
        if (_largeRoomGeometry.TryGetValue(anchor, out GameObject geometry))
        {
            Destroy(geometry);
            _largeRoomGeometry.Remove(anchor);
        }

        return anchor;
    }

    // ─── Special rooms ────────────────────────────────────────────────────────

    void PlaceSpecialRooms()
    {
        List<Vector2Int> candidates = FindDeadEndCandidates();

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = RogueDifficultyManager.RoomRNG.Next(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        TryPlaceSpecialRoom(ref candidates, treasureRoomPrefab);
        TryPlaceSpecialRoom(ref candidates, shopRoomPrefab);
        TryPlaceSpecialRoom(ref candidates, gamblingRoomPrefab);

        if (RogueDifficultyManager.RoomRNG.NextDouble() <= planetChance && planetariumPrefab != null)
        {
            TryPlaceSpecialRoom(ref candidates, planetariumPrefab); // planetarium spawning
        }

        foreach (Room prefab in specialRoomPrefabs)
        {
            if(RogueDifficultyManager.RoomRNG.NextDouble() <= prefab.spawnChance)
            {
                TryPlaceSpecialRoom(ref candidates, prefab);
            }
        }
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

                if (neighbourCount == 1 && !deadEnds.Contains(candidate))
                    deadEnds.Add(candidate);
            }
        }

        if (deadEnds.Count < 3)
            Debug.LogWarning("[RoomGenerator] Fewer than 3 dead-end slots found for special rooms " +
                             $"({deadEnds.Count} available). Some special rooms may be skipped.");

        return deadEnds;
    }

    void TryPlaceSpecialRoom(ref List<Vector2Int> candidates, Room prefabRoom)
    {
        Vector2Int pos = Vector2Int.zero;
        bool found = false;

        RoomType roomType = prefabRoom.roomType;

        while (candidates.Count > 0)
        {
            pos = candidates[0];
            candidates.RemoveAt(0);

            candidates.RemoveAll(c =>
            {
                foreach (var d in directions)
                    if (c == pos + d) return true;
                return false;
            });

            bool adjacentToSpecial = false;
            foreach (var d in directions)
            {
                if (placedRooms.TryGetValue(pos + d, out Room adj) && adj.roomType != RoomType.Normal)
                {
                    adjacentToSpecial = true;
                    break;
                }
            }

            if (adjacentToSpecial)
            {
                Debug.Log($"[RoomGenerator] Skipping {roomType} candidate {pos} — adjacent to a special room.");
                continue;
            }

            Room specialPrefab = prefabRoom;

            bool exitCompatible = true;

            foreach (var d in directions)
            {
                if (!placedRooms.TryGetValue(pos + d, out Room neighbour)) continue;

                if (!RoomHasExit(neighbour, -d))
                {
                    exitCompatible = false;
                    Debug.Log($"[RoomGenerator] Skipping {roomType} candidate {pos} — " +
                              $"neighbour at {pos + d} has no exit toward it.");
                    break;
                }

                if (specialPrefab != null && !RoomHasExit(specialPrefab, d))
                {
                    exitCompatible = false;
                    Debug.Log($"[RoomGenerator] Skipping {roomType} candidate {pos} — " +
                              $"dedicated prefab has no exit in direction {d}.");
                    break;
                }

                if (specialPrefab == null && CompatiblePrefabs(d).Count == 0)
                {
                    exitCompatible = false;
                    Debug.Log($"[RoomGenerator] Skipping {roomType} candidate {pos} — " +
                              $"no normal prefab has exit in direction {d}.");
                    break;
                }
            }

            if (!exitCompatible) continue;

            found = true;
            break;
        }

        if (!found)
        {
            Debug.LogWarning($"[RoomGenerator] No valid candidate for {roomType} room — skipping.");
            return;
        }

        Room prefab = prefabRoom;

        if (prefab == null)
        {
            Debug.LogWarning($"[RoomGenerator] Could not resolve a prefab for {roomType} at {pos} — skipping.");
            return;
        }

        Vector3 worldPos = new Vector3(pos.x * roomWidth, 0f, pos.y * roomHeight);

        Room room = Instantiate(prefab, worldPos, Quaternion.identity);
        room.position = pos;
        room.roomType = roomType;
        room.SpawnCredits = 0;

        AlignRoomToNeighborExit(room, pos);

        placedRooms[pos] = room;
        Debug.Log($"[RoomGenerator] {roomType} room placed at grid {pos}.");
    }

    Room PickCompatibleNormalPrefab(Vector2Int gridPos)
    {
        List<Room> compatible = roomPrefabs.FindAll(p => PrefabFitsNeighbours(p, gridPos));

        if (compatible.Count > 0)
            return compatible[RogueDifficultyManager.RoomRNG.Next(0, compatible.Count)];

        Debug.LogWarning($"[RoomGenerator] No fully-compatible normal prefab for {gridPos}; using any.");
        return roomPrefabs.Count > 0
            ? roomPrefabs[RogueDifficultyManager.RoomRNG.Next(0, roomPrefabs.Count)]
            : null;
    }

    // ─── Boss room ────────────────────────────────────────────────────────────

    void DesignateBossRoom()
    {
        Vector2Int bossPos = Vector2Int.zero;
        int bestManhattan = -1;

        foreach (var kvp in placedRooms)
        {
            if (kvp.Key == Vector2Int.zero) continue;
            if (kvp.Value.roomType != RoomType.Normal) continue;

            // Skip sub-rooms that aren't the anchor of their large room group —
            // we only want to designate a boss at a position we can cleanly replace.
            if (_largeRoomAnchorOf.TryGetValue(kvp.Key, out Vector2Int anchor) && anchor != kvp.Key)
                continue;

            int neighbourCount = 0;
            foreach (var dir in directions)
                if (placedRooms.ContainsKey(kvp.Key + dir))
                    neighbourCount++;

            if (neighbourCount != 1) continue;

            bool adjacentToSpecial = false;
            foreach (var dir in directions)
            {
                if (placedRooms.TryGetValue(kvp.Key + dir, out Room adj) &&
                    adj.roomType != RoomType.Normal && adj.roomType != RoomType.Boss)
                {
                    adjacentToSpecial = true;
                    break;
                }
            }
            if (adjacentToSpecial) continue;

            if (bossRoomPrefab != null && !BossPrefabFitsPosition(kvp.Key))
            {
                Debug.Log($"[RoomGenerator] Skipping boss candidate {kvp.Key} — " +
                           "boss prefab lacks a required exit toward its neighbour.");
                continue;
            }

            int manhattan = Mathf.Abs(kvp.Key.x) + Mathf.Abs(kvp.Key.y);
            if (manhattan > bestManhattan)
            {
                bestManhattan = manhattan;
                bossPos = kvp.Key;
            }
        }

        // Fallback: farthest normal room (no dead-end constraint).
        if (bestManhattan < 0)
        {
            Debug.LogWarning("[RoomGenerator] No dead-end normal room found for boss — falling back to farthest normal room.");
            foreach (var kvp in placedRooms)
            {
                if (kvp.Key == Vector2Int.zero) continue;
                if (kvp.Value.roomType != RoomType.Normal) continue;

                if (_largeRoomAnchorOf.TryGetValue(kvp.Key, out Vector2Int anchor) && anchor != kvp.Key)
                    continue;

                if (bossRoomPrefab != null && !BossPrefabFitsPosition(kvp.Key)) continue;

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

        // If bossPos is part of a large room group, remove all sibling sub-rooms first.
        Vector3 oldWorldPos;
        if (_largeRoomAnchorOf.ContainsKey(bossPos))
        {
            oldWorldPos = new Vector3(bossPos.x * roomWidth, 0f, bossPos.y * roomHeight);
            RemoveLargeRoomGroup(bossPos);
        }
        else
        {
            Room oldRoom = placedRooms[bossPos];
            oldWorldPos = oldRoom.transform.position;
            Destroy(oldRoom.gameObject);
            placedRooms.Remove(bossPos);
            path.Remove(bossPos);
        }

        Room prefab = bossRoomPrefab != null
            ? bossRoomPrefab
            : PickCompatibleNormalPrefab(bossPos);

        Room bossRoom = Instantiate(prefab, new Vector3(oldWorldPos.x, 0f, oldWorldPos.z), Quaternion.identity);
        bossRoom.position = bossPos;
        bossRoom.roomType = RoomType.Boss;

        AlignRoomToNeighborExit(bossRoom, bossPos);

        placedRooms[bossPos] = bossRoom;
        path.Add(bossPos);
        Debug.Log($"[RoomGenerator] Boss room at grid {bossPos} (Manhattan {bestManhattan}).");
    }

    bool BossPrefabFitsPosition(Vector2Int gridPos)
    {
        if (bossRoomPrefab == null) return true;

        foreach (var dir in directions)
        {
            if (!placedRooms.TryGetValue(gridPos + dir, out Room neighbour)) continue;

            if (RoomHasExit(neighbour, -dir) && !RoomHasExit(bossRoomPrefab, dir))
                return false;
        }
        return true;
    }

    // ─── NavMesh ──────────────────────────────────────────────────────────────

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

    // ─── Connection finalization ──────────────────────────────────────────────

    void FinalizeConnections()
    {
        var validConnections = new HashSet<(Vector2Int pos, Vector2Int dir)>();

        foreach (var kvp in placedRooms)
        {
            Vector2Int pos = kvp.Key;
            Room room = kvp.Value;

            foreach (var dir in new[] { Vector2Int.right, Vector2Int.up })
            {
                Vector2Int neighborPos = pos + dir;
                if (!placedRooms.TryGetValue(neighborPos, out Room neighbor)) continue;

                Transform myExit = GetExitFacing(room, dir);
                Transform neighborExit = GetExitFacing(neighbor, -dir);

                if (myExit == null || neighborExit == null) continue;
                if (Mathf.Abs(myExit.position.y - neighborExit.position.y) <= 0.1f)
                    validConnections.Add((pos, dir));
            }
        }

        foreach (var kvp in placedRooms)
        {
            Vector2Int pos = kvp.Key;
            Room room = kvp.Value;

            HandleExit(room, pos, Vector2Int.up, room.exitTop);
            HandleExit(room, pos, Vector2Int.down, room.exitBottom);
            HandleExit(room, pos, Vector2Int.left, room.exitLeft);
            HandleExit(room, pos, Vector2Int.right, room.exitRight);
        }

        if (MinimapUI.Instance != null)
            MinimapUI.Instance.BuildMinimap(placedRooms, validConnections);
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
                    $"[RoomGenerator] Exit mismatch at {pos} → {neighborPos} (ΔY={yDiff:F2}) → wall placed.");
                return;
            }

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

    // ─── Update / activation ──────────────────────────────────────────────────

    void Update()
    {
        if (!_generationComplete) return;
        if (Time.time < _nextActivationCheck) return;
        _nextActivationCheck = Time.time + ActivationCheckInterval;
        UpdateRoomActivation();
        CheckOutOfBounds();
    }

    void UpdateRoomActivation()
    {
        var player = NewMovement.Instance;
        if (player == null) return;

        Vector2Int playerGrid = WorldToGrid(player.transform.position);
        Vector2Int startGrid = path.Count > 0 ? path[0] : Vector2Int.zero;

        foreach (var kvp in placedRooms)
        {
            if (kvp.Value == null) continue;

            bool isStartRoom = kvp.Key == startGrid;

            int manhattanDist = Mathf.Abs(kvp.Key.x - playerGrid.x)
                              + Mathf.Abs(kvp.Key.y - playerGrid.y);

            bool shouldBeActive = isStartRoom || manhattanDist <= activationRadius;

            if (kvp.Value.gameObject.activeSelf != shouldBeActive)
                kvp.Value.gameObject.SetActive(shouldBeActive);
        }
    }

    // ─── Out-of-bounds recovery ───────────────────────────────────────────────

    private const float ErrorRoomRadius = 100f;
    private bool _isTeleportingToErrorRoom = false;

    void CheckOutOfBounds()
    {
        var player = NewMovement.Instance;
        if (player == null) return;
        if (_isTeleportingToErrorRoom) return;

        Vector2Int playerGrid = WorldToGrid(player.transform.position);

        if (placedRooms.ContainsKey(playerGrid)) return;

        GameObject errorRoom = GameObject.Find("ErrorRoom");
        if (errorRoom != null)
        {
            float dist = Vector3.Distance(player.transform.position, errorRoom.transform.position);
            if (dist <= ErrorRoomRadius) return;
        }
        if (!canDoTheErrorRoom) return;
        Debug.LogWarning($"[RoomGenerator] Player is out of bounds at grid {playerGrid} " +
                         $"(world {player.transform.position}). Teleporting to ErrorRoom.");
        TeleportPlayerToErrorRoom(player, errorRoom);
    }

    void TeleportPlayerToErrorRoom(NewMovement player, GameObject errorRoom = null)
    {
        if (errorRoom == null) errorRoom = GameObject.Find("ErrorRoom");
        if (errorRoom == null)
        {
            Debug.LogError("[RoomGenerator] Could not find a GameObject named 'ErrorRoom' to teleport the player to!");
            return;
        }

        _isTeleportingToErrorRoom = true;

        player.transform.position = errorRoom.transform.position;

        if (player.rb != null)
            player.rb.velocity = Vector3.zero;

        Debug.Log($"[RoomGenerator] Player teleported to ErrorRoom at {errorRoom.transform.position}.");

        foreach (var door in FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (door.TryGetComponent<Lockable>(out var lockable))
            {
                if (lockable.locked)
                    continue;
            }
            door.Unlock();
        }

        _isTeleportingToErrorRoom = false;
    }

    void EnforceEmptyRoomConnectivity()
    {
        foreach (var kvp in placedRooms)
        {
            Room room = kvp.Value;
            if (room.roomType != RoomType.Normal) continue;
            if (room.SpawnCredits != 0) continue;

            int neighbourCount = 0;
            foreach (var dir in directions)
                if (placedRooms.ContainsKey(kvp.Key + dir))
                    neighbourCount++;

            if (neighbourCount < 2)
            {
                room.SpawnCredits = 3;
                Debug.Log($"[RoomGenerator] Empty room at {kvp.Key} was a dead end — credits restored.");
            }
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

    /// <summary>
    /// Returns the cell at the far edge of a large room footprint in the given
    /// travel direction, so the generation loop's next step starts just outside
    /// the large room rather than inside it.
    /// For directions with no component on an axis, we just return the anchor.
    /// </summary>
    Vector2Int FarEdgeCell(Vector2Int anchor, int w, int h, Vector2Int dir)
    {
        int dx = dir.x > 0 ? w - 1 : 0;
        int dy = dir.y > 0 ? h - 1 : 0;
        return anchor + new Vector2Int(dx, dy);
    }
}
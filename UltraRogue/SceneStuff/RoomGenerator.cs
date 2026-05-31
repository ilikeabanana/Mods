using BepInEx.Bootstrap;
using System;
using System.Collections;
using System.Collections.Generic;
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
    public Room startRoomPrefab;
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
    int activationRadius = 50;

    private bool _generationComplete = false;
    private float _nextActivationCheck = 0f;
    private const float ActivationCheckInterval = 0.3f;
    private int guaranteedCombatRoomsWithCredits;
    private const int MinCombatRoomsWithCredits = 4;

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
        StatsManager.Instance.StopTimer();
        MusicManager.Instance.StopMusic();
        foreach (var room in placedRooms.Values)
        {
            if (room != null)
                Destroy(room.gameObject);
        }

        placedRooms.Clear();
        path.Clear();

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
            5f + Mathf.Log(floor + 1f, 2f) * 2f
            + RogueDifficultyManager.RoomRNG.Next(-1, 2)
        );

        Vector2Int current = Vector2Int.zero;
        PlaceRoom(current, isStart: true, direction: Vector2Int.zero);

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

                PlaceRoom(next, prefabPool: compatible, direction: dir);
                current = next;
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

            //StartCoroutine(StartThingggg(quad1, quad2));
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

            // The placed neighbour must have a return exit toward gridPos.
            Room neighbour = placedRooms[gridPos + dir];
            bool neighbourFacesUs = RoomHasExit(neighbour, -dir);

            // The new prefab must also face the neighbour.
            bool weFaceNeighbour = RoomHasExit(prefab, dir);

            // Only block placement when BOTH sides want a connection but one is
            // missing. If the neighbour has no exit toward us, a wall will be
            // placed there regardless — no constraint on the new prefab.
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

    /// <summary>
    /// Places a room at <paramref name="gridPos"/>.
    /// </summary>
    /// <param name="gridPos">Target grid cell.</param>
    /// <param name="isStart">If true, always uses the first prefab and gives 0 spawn credits.</param>
    /// <param name="prefabPool">
    ///   Optional filtered list of prefabs to draw from (e.g. those with a specific
    ///   required exit). Falls back to <see cref="roomPrefabs"/> when null.
    /// </param>
    void PlaceRoom(Vector2Int gridPos, Vector2Int direction, bool isStart = false, List<Room> prefabPool = null)
    {
        Room prefab;

        if (isStart)
        {
            prefab = startRoomPrefab;
        }
        else
        {
            // Use the supplied pool (already filtered for exit compatibility).
            // Apply a secondary filter: the chosen prefab must also face every
            // other already-placed neighbour it will touch.
            List<Room> pool = prefabPool ?? roomPrefabs;

            // Give large room prefabs a chance to be picked if any are defined
            // and all cells they would occupy are free.
            if (largeRoomPrefabs != null && largeRoomPrefabs.Count > 0)
            {
                List<Room> largeCandidates = largeRoomPrefabs.FindAll(p =>
                    LargeRoomCellsFree(gridPos, p.RoomSizeWidth, p.RoomSizeHeight));

                if (largeCandidates.Count > 0 && RogueDifficultyManager.RoomRNG.Next(0, 4) == 0)
                {
                    prefab = largeCandidates[RogueDifficultyManager.RoomRNG.Next(0, largeCandidates.Count)];

                    // Large rooms are expanded separately — skip the normal flow.
                    ExpandLargeRoom(prefab, gridPos, isStart);
                    return;
                }
            }

            // Further narrow to prefabs that satisfy all existing neighbours.
            List<Room> fullyCompatible = pool.FindAll(p => PrefabFitsNeighbours(p, gridPos));

            if (fullyCompatible.Count > 0)
            {
                prefab = fullyCompatible[RogueDifficultyManager.RoomRNG.Next(0, fullyCompatible.Count)];
            }
            else
            {
                // Fallback: use the broader pool (HandleExit will wall off mismatches).
                Debug.LogWarning($"[RoomGenerator] No fully-compatible prefab found at {gridPos}; " +
                                  "falling back to partially-compatible pool.");
                prefab = pool[RogueDifficultyManager.RoomRNG.Next(0, pool.Count)];
            }
        }

        // Large rooms: spawn sub-room GameObjects instead of instantiating the prefab directly.
        if (prefab.RoomSizeWidth > 1 || prefab.RoomSizeHeight > 1)
        {
            ExpandLargeRoom(prefab, gridPos, isStart);
            return;
        }

        Vector3 worldPos = new Vector3(gridPos.x * roomWidth, 0f, gridPos.y * roomHeight);
        Room room = Instantiate(prefab, worldPos, Quaternion.identity);
        room.position = gridPos;
        room.roomType = RoomType.Normal;
        if (isStart)
        {
            room.SpawnCredits = 0;
        }
        else
        {
            room.SpawnCredits = 3;
        }

        if (!isStart) AlignRoomToNeighborExit(room, gridPos);

        placedRooms[gridPos] = room;
        path.Add(gridPos);
    }

    /// <summary>
    /// Decomposes a large room prefab into individual 1x1 sub-room GameObjects,
    /// one per grid cell. Each sub-room gets the outer exits from the source prefab's
    /// exit arrays; any edge shared with another sub-room of the same large room gets
    /// a fake exit Transform parented far away so the door/wall system ignores it.
    /// </summary>
    void ExpandLargeRoom(Room source, Vector2Int anchorPos, bool isStart)
    {
        int w = source.RoomSizeWidth;
        int h = source.RoomSizeHeight;

        // We collect all sub-rooms first so we can run alignment afterwards.
        var subRooms = new List<(Vector2Int cell, Room sub)>();

        for (int lx = 0; lx < w; lx++)
        {
            for (int ly = 0; ly < h; ly++)
            {
                Vector2Int cell = anchorPos + new Vector2Int(lx, ly);
                Vector3 worldPos = new Vector3(cell.x * roomWidth, 0f, cell.y * roomHeight);

                // Create a fresh GameObject for this sub-room.
                GameObject go = new GameObject($"Room_{cell.x}_{cell.y}");
                go.transform.position = worldPos;

                Room sub = go.AddComponent<Room>();

                // Copy shared data from the source prefab.
                sub.position = cell;
                sub.roomType = RoomType.Normal;
                sub.SpawnCredits = isStart ? 0 : source.SpawnCredits;
                sub.spawnChance = source.spawnChance;
                sub.doorPrefab = source.doorPrefab;
                sub.wallPrefab = source.wallPrefab;
                sub.RoomSizeWidth = 1;
                sub.RoomSizeHeight = 1;

                // Copy spawn points: create new child Transforms at the same world positions
                // so SpawnEnemies has valid points to pick from.
                foreach (Transform srcPt in source.spawnPoints)
                {
                    if (srcPt == null) continue;
                    GameObject ptGo = new GameObject("SpawnPoint");
                    ptGo.transform.SetParent(go.transform);
                    ptGo.transform.position = srcPt.position;
                    sub.spawnPoints.Add(ptGo.transform);
                }
                // Fallback: if the source had no spawn points, use the room centre.
                if (sub.spawnPoints.Count == 0)
                    sub.spawnPoints.Add(go.transform);

                // ── Assign exits ──────────────────────────────────────────────
                // Rule: if the neighbor in a given direction is another sub-room
                // of this same large room, put a fake Transform far away.
                // Otherwise pull the real exit from the source's exit arrays,
                // indexed by the sub-room's position along that wall.

                sub.exitLeft = AssignSubExit(sub, source, Vector2Int.left, lx, ly, w, h);
                sub.exitRight = AssignSubExit(sub, source, Vector2Int.right, lx, ly, w, h);
                sub.exitTop = AssignSubExit(sub, source, Vector2Int.up, lx, ly, w, h);
                sub.exitBottom = AssignSubExit(sub, source, Vector2Int.down, lx, ly, w, h);

                placedRooms[cell] = sub;
                path.Add(cell);
                subRooms.Add((cell, sub));
            }
        }

        // Align all sub-rooms to their real outer neighbours (same logic as normal rooms).
        if (!isStart)
        {
            foreach (var (cell, sub) in subRooms)
                AlignRoomToNeighborExit(sub, cell);
        }
    }

    /// <summary>
    /// Returns the correct exit Transform for one face of a sub-room inside a large room.
    /// <para>
    /// If the face is internal (the tile on that side is another sub-room of the same large
    /// room), a dummy Transform is created and placed at (1000, 1000, 1000) so the
    /// door/wall system will never find a matching grid neighbour there.
    /// </para>
    /// <para>
    /// If the face is external, we pick the real exit from the source prefab's exit array.
    /// Left/Right arrays are indexed bottom-to-top (by ly).
    /// Top/Bottom arrays are indexed left-to-right (by lx).
    /// </para>
    /// </summary>
    Transform AssignSubExit(Room sub, Room source, Vector2Int dir, int lx, int ly, int w, int h)
    {
        // Is the neighbour in this direction another tile of the same large room?
        int nx = lx + dir.x;
        int ny = ly + dir.y;
        bool isInternal = (nx >= 0 && nx < w && ny >= 0 && ny < h);

        if (isInternal)
        {
            // Fake exit — park it far away so HandleExit never matches a grid cell there.
            GameObject fake = new GameObject("FakeExit_" + dir);
            fake.transform.SetParent(sub.transform);
            fake.transform.position = new Vector3(1000f, 1000f, 1000f);
            return fake.transform;
        }

        // External face — pull from the source prefab's exit arrays.
        // Left/Right: indexed by ly (bottom = 0, top = h-1).
        // Top/Bottom: indexed by lx (left = 0, right = w-1).
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

    // ─── Special rooms ────────────────────────────────────────────────────────

    void PlaceSpecialRooms()
    {
        List<Vector2Int> candidates = FindDeadEndCandidates();

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = RogueDifficultyManager.RoomRNG.Next(0, i + 1);
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
        Vector2Int pos = Vector2Int.zero;
        bool found = false;

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

            // ── Exit compatibility check ──────────────────────────────────────
            // The single neighbour that owns this dead-end slot must have an
            // exit facing toward pos, AND the special prefab must have a return
            // exit facing back.

            Room specialPrefab = roomType switch
            {
                RoomType.Treasure => treasureRoomPrefab,
                RoomType.Shop => shopRoomPrefab,
                RoomType.Gambling => gamblingRoomPrefab,
                _ => null,
            };

            bool exitCompatible = true;

            foreach (var d in directions)
            {
                if (!placedRooms.TryGetValue(pos + d, out Room neighbour)) continue;

                // Does the neighbour actually open toward us?
                if (!RoomHasExit(neighbour, -d))
                {
                    // Neighbour has no exit facing pos — this slot is blocked.
                    exitCompatible = false;
                    Debug.Log($"[RoomGenerator] Skipping {roomType} candidate {pos} — " +
                              $"neighbour at {pos + d} has no exit toward it.");
                    break;
                }

                // If we have a dedicated prefab, make sure it faces the neighbour.
                if (specialPrefab != null && !RoomHasExit(specialPrefab, d))
                {
                    exitCompatible = false;
                    Debug.Log($"[RoomGenerator] Skipping {roomType} candidate {pos} — " +
                              $"dedicated prefab has no exit in direction {d}.");
                    break;
                }

                // If we're falling back to normal prefabs, make sure at least one works.
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

        // Resolve prefab (dedicated or random compatible normal prefab).
        Room prefab = roomType switch
        {
            RoomType.Treasure => treasureRoomPrefab != null
                                    ? treasureRoomPrefab
                                    : PickCompatibleNormalPrefab(pos),
            RoomType.Shop => shopRoomPrefab != null
                                    ? shopRoomPrefab
                                    : PickCompatibleNormalPrefab(pos),
            RoomType.Gambling => gamblingRoomPrefab != null
                                    ? gamblingRoomPrefab
                                    : PickCompatibleNormalPrefab(pos),
            _ => PickCompatibleNormalPrefab(pos),
        };

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

    /// <summary>
    /// Picks a random normal prefab that is fully compatible with all neighbours
    /// already placed around <paramref name="gridPos"/>.
    /// </summary>
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

            // Ensure the boss prefab has an exit toward its one neighbour.
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

        Room oldRoom = placedRooms[bossPos];
        Vector3 oldWorldPos = oldRoom.transform.position;
        Destroy(oldRoom.gameObject);
        placedRooms.Remove(bossPos);

        Room prefab = bossRoomPrefab != null
            ? bossRoomPrefab
            : PickCompatibleNormalPrefab(bossPos);

        Room bossRoom = Instantiate(prefab, new Vector3(oldWorldPos.x, 0f, oldWorldPos.z), Quaternion.identity);
        bossRoom.position = bossPos;
        bossRoom.roomType = RoomType.Boss;

        AlignRoomToNeighborExit(bossRoom, bossPos);

        placedRooms[bossPos] = bossRoom;
        Debug.Log($"[RoomGenerator] Boss room at grid {bossPos} (Manhattan {bestManhattan}).");
    }

    /// <summary>
    /// Returns true when <see cref="bossRoomPrefab"/> has exits toward every
    /// already-placed neighbour of <paramref name="gridPos"/> that opens toward it.
    /// </summary>
    bool BossPrefabFitsPosition(Vector2Int gridPos)
    {
        if (bossRoomPrefab == null) return true;

        foreach (var dir in directions)
        {
            if (!placedRooms.TryGetValue(gridPos + dir, out Room neighbour)) continue;

            // Only enforce if the neighbour actually has an exit facing us.
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
        // Build the set of connections that are actually open (not walled off by a
        // Y-level mismatch). Only Right/Up directions so each pair is stored once.
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

                // Both exits must exist and be at the same Y level.
                if (myExit == null || neighborExit == null) continue;
                if (Mathf.Abs(myExit.position.y - neighborExit.position.y) <= 0.1f)
                    validConnections.Add((pos, dir));
            }
        }

        // Now do the usual door/wall finalization.
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

            // The start room is always kept active regardless of player distance.
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

        // Player is still inside a valid room — nothing to do.
        if (placedRooms.ContainsKey(playerGrid)) return;

        // Player is OOB. Check if they're already near the ErrorRoom — if so, leave them alone.
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

        // Reset velocity so the player doesn't carry momentum into the error room.
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
            if (room.SpawnCredits != 0) continue;          // only care about empty rooms

            int neighbourCount = 0;
            foreach (var dir in directions)
                if (placedRooms.ContainsKey(kvp.Key + dir))
                    neighbourCount++;

            if (neighbourCount < 2)                        // dead end → give it credits
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
    /// Returns true when every grid cell that a large room (w × h) would occupy
    /// starting at <paramref name="anchor"/> is currently unoccupied.
    /// </summary>
    bool LargeRoomCellsFree(Vector2Int anchor, int w, int h)
    {
        for (int lx = 0; lx < w; lx++)
            for (int ly = 0; ly < h; ly++)
                if (placedRooms.ContainsKey(anchor + new Vector2Int(lx, ly)))
                    return false;
        return true;
    }
}
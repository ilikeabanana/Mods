using BepInEx.Bootstrap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    [Header("Forced Room Spawn")]
    [Tooltip("When >= 0, guarantees roomPrefabs[forceSpawnRoomIndex] is placed as one of the normal rooms. Set to -1 to disable.")]
    public int forceSpawnRoomIndex = -1;
    bool _forcedRoomPlaced = false;

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
    // One grid cell = one standard (1×1) room = 60 wide × 30 deep in world units.
    // RoomSizeWidth=2 means a room that is 120 wide (two standard rooms side by side).
    const float roomWidth = 60f;
    const float roomHeight = 30f;

    // World-space AABB per placed room (centre + half-extents).
    // Used only for overlap rejection and player-grid lookup.
    readonly Dictionary<Vector2Int, (Vector3 centre, float hx, float hz)> roomBounds
        = new Dictionary<Vector2Int, (Vector3, float, float)>();

    [Header("Performance")]
    [Tooltip("How many grid cells away from the player rooms stay active (1 = current + immediate neighbors).")]
    int activationRadius = 2;

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
        roomBounds.Clear();
        _forcedRoomPlaced = false;

        _generationComplete = false;
        if (MinimapUI.Instance != null) MinimapUI.Instance.ClearAndReset();
        RogueDifficultyManager.Instance.MoveStage();
        StartCoroutine(GenerateRooms(false));
    }

    IEnumerator GenerateRooms(bool firstTime = true)
    {
        canDoTheErrorRoom = false;
        guaranteedCombatRoomsWithCredits = 0;
        _forcedRoomPlaced = false;

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
        PlaceRoom(current, isStart: true);

        int placed = 1;
        int safetyBreak = 0;

        while (placed < count && safetyBreak++ < 1000)
        {
            Vector2Int dir = directions[RogueDifficultyManager.RoomRNG.Next(0, directions.Length)];
            int steps = RogueDifficultyManager.RoomRNG.Next(1, 2);

            for (int i = 0; i < steps && placed < count; i++)
            {
                Room currentRoom = placedRooms[current];

                // Step to the first free cell just beyond the current room's footprint in dir.
                // The room's anchor (current) is its leftmost/bottommost cell.
                // Going right: skip over RoomSizeWidth cells → next = current + (RoomSizeWidth, 0)
                // Going up:    skip over RoomSizeHeight cells → next = current + (0, RoomSizeHeight)
                // Going left/down: one step back is always current + dir (anchor IS the left/bottom edge)
                Vector2Int next;
                if (dir == Vector2Int.right)
                    next = current + new Vector2Int(currentRoom.RoomSizeWidth, 0);
                else if (dir == Vector2Int.up)
                    next = current + new Vector2Int(0, currentRoom.RoomSizeHeight);
                else
                    next = current + dir; // left/down: anchor is already the near edge
                if (placedRooms.ContainsKey(next)) break;

                // The current room must have an exit on the side facing dir.
                if (!RoomHasExit(currentRoom, dir)) break;

                // At least one normal prefab must have a return exit.
                List<Room> compatible = CompatiblePrefabs(-dir);
                if (compatible.Count == 0) break;

                PlaceRoomInternal(next, prefabPool: compatible);

                // Advance current to the canonical (anchor) cell of the new room,
                // which is what we stored in room.position = next.
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

    /// <summary>
    /// Returns the exit on <paramref name="room"/> that faces <paramref name="dir"/>
    /// AND is closest to the world position of <paramref name="neighborGridPos"/>.
    /// For 1×1 rooms there is only one exit per direction so it is returned immediately.
    /// For wider/taller rooms the correct exit is whichever one sits nearest the
    /// neighbor's centre — e.g. a 2×1 room picks its left top-exit when connecting
    /// to the room above-left, and its right top-exit for the room above-right.
    /// </summary>
    Transform GetExitFacing(Room room, Vector2Int dir, Vector2Int neighborGridPos)
    {
        List<Transform> exits = room.ExitsForDir(dir);
        if (exits.Count == 0) return null;
        if (exits.Count == 1) return exits[0];

        // Use the neighbor's actual world position (its pivot) if it's already placed,
        // otherwise fall back to the grid formula. This handles large rooms correctly.
        Vector3 neighborCentre;
        if (placedRooms.TryGetValue(neighborGridPos, out Room neighborRoom))
            neighborCentre = new Vector3(neighborRoom.transform.position.x, 0f, neighborRoom.transform.position.z);
        else
            neighborCentre = new Vector3(
                neighborGridPos.x * roomWidth,
                0f,
                neighborGridPos.y * roomHeight
            );

        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (var t in exits)
        {
            if (t == null) continue;
            float d = Vector3.Distance(
                new Vector3(t.position.x, 0f, t.position.z),
                new Vector3(neighborCentre.x, 0f, neighborCentre.z)
            );
            if (d < bestDist) { bestDist = d; best = t; }
        }
        return best;
    }

    // Overload for cases where we don't care which specific neighbour — just need
    // to know if ANY exit exists in that direction (e.g. prefab compatibility checks).
    Transform GetExitFacing(Room room, Vector2Int dir) =>
        room.ExitsForDir(dir).Find(t => t != null);

    /// <summary>Returns true when the room (or prefab) has at least one non-null exit in <paramref name="dir"/>.</summary>
    bool RoomHasExit(Room room, Vector2Int dir) => GetExitFacing(room, dir) != null;

    /// <summary>
    /// Returns the subset of <see cref="roomPrefabs"/> that have a non-null exit
    /// in <paramref name="dir"/>. Used to guarantee a compatible prefab exists
    /// before committing to a grid cell.
    /// </summary>
    List<Room> CompatiblePrefabs(Vector2Int dir) =>
        roomPrefabs.FindAll(p => RoomHasExit(p, dir));

    /// <summary>
    /// Returns true when the world-space AABB of <paramref name="prefab"/> placed at
    /// <paramref name="gridPos"/> does not overlap any already-placed room.
    /// A 1×1 room is exactly 60×30, so it never overlaps its own grid cell neighbours.
    /// Larger rooms extend beyond their cell and can clip — this catches that.
    /// We shrink by 0.5 u on each side so walls that share an edge are not rejected.
    /// </summary>
    bool WorldOverlapsClear(Vector2Int gridPos, Room prefab)
    {
        float hw = prefab.RoomSizeWidth * roomWidth * 0.5f - 0.5f;
        float hd = prefab.RoomSizeHeight * roomHeight * 0.5f - 0.5f;

        float newPivotX = gridPos.x * roomWidth + (prefab.RoomSizeWidth - 1) * roomWidth * 0.5f;
        float newPivotZ = gridPos.y * roomHeight + (prefab.RoomSizeHeight - 1) * roomHeight * 0.5f;
        Vector3 newCentre = new Vector3(newPivotX, 0f, newPivotZ);

        foreach (var kvp in roomBounds)
        {
            var (c, ehx, ehz) = kvp.Value;
            // AABB overlap test (ignore Y entirely).
            bool overlapX = Mathf.Abs(newCentre.x - c.x) < hw + ehx;
            bool overlapZ = Mathf.Abs(newCentre.z - c.z) < hd + ehz;
            if (overlapX && overlapZ) return false;
        }
        return true;
    }

    /// <summary>
    /// Returns true when <paramref name="prefab"/> has exits in every direction
    /// required by its already-placed neighbours at <paramref name="gridPos"/>,
    /// AND its world-space footprint does not overlap any existing room.
    /// </summary>
    bool PrefabFitsNeighbours(Room prefab, Vector2Int gridPos)
    {
        if (!WorldOverlapsClear(gridPos, prefab)) return false;

        foreach (var dir in directions)
        {
            if (!placedRooms.TryGetValue(gridPos + dir, out Room neighbour)) continue;
            // Skip cells that belong to the room we're currently placing (won't happen
            // for new placements, but guard against it for the boss-room swap case).
            // WorldOverlapsClear already blocks true physical overlaps.

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

            // Get the specific exits that will connect (chosen by proximity to each other's centre).
            Transform neighborExit = GetExitFacing(neighbor, -dir, gridPos);
            Transform myExit = GetExitFacing(room, dir, neighborPos);

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
    void PlaceRoom(Vector2Int gridPos, bool isStart = false, List<Room> prefabPool = null)
        => PlaceRoomInternal(gridPos, isStart, prefabPool);

    Room PlaceRoomInternal(Vector2Int gridPos, bool isStart = false, List<Room> prefabPool = null)
    {
        Room prefab;

        if (isStart)
        {
            prefab = startRoomPrefab;
        }
        else
        {
            // If a forced room hasn't been placed yet, try to inject it as the only option.
            // If it doesn't fit here, fall through to the normal pool and try again later.
            Room forcedPrefab = null;
            if (!_forcedRoomPlaced
                && forceSpawnRoomIndex >= 0
                && forceSpawnRoomIndex < roomPrefabs.Count)
            {
                forcedPrefab = roomPrefabs[forceSpawnRoomIndex];
            }

            List<Room> pool = prefabPool ?? roomPrefabs;

            // If we have a forced prefab and it fits, use it; otherwise use the normal pool.
            if (forcedPrefab != null && PrefabFitsNeighbours(forcedPrefab, gridPos))
            {
                prefab = forcedPrefab;
                _forcedRoomPlaced = true;
            }
            else
            {
                List<Room> fullyCompatible = pool.FindAll(p => PrefabFitsNeighbours(p, gridPos));

                if (fullyCompatible.Count > 0)
                {
                    prefab = fullyCompatible[RogueDifficultyManager.RoomRNG.Next(0, fullyCompatible.Count)];
                }
                else
                {
                    Debug.LogWarning($"[RoomGenerator] No fully-compatible prefab found at {gridPos}; " +
                                      "falling back to partially-compatible pool.");
                    prefab = pool[RogueDifficultyManager.RoomRNG.Next(0, pool.Count)];
                }
            }
        }

        // gridPos is the leftmost/bottommost cell this room occupies.
        // The pivot (centre of the room) sits half-a-room to the right/up from that cell's centre.
        // e.g. 2×1 at gridPos (1,0):
        //   pivotX = 1*60 + (2-1)*0.5*60 = 60+30 = 90
        //   left exit at local -30 → world 60 = cell (1,0) centre ✓
        //   right exit at local +30 → world 120 = cell (2,0) centre ✓
        float pivotX = gridPos.x * roomWidth + (prefab.RoomSizeWidth - 1) * roomWidth * 0.5f;
        float pivotZ = gridPos.y * roomHeight + (prefab.RoomSizeHeight - 1) * roomHeight * 0.5f;
        Vector3 worldPos = new Vector3(pivotX, 0f, pivotZ);

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

        float hx = room.RoomSizeWidth * roomWidth * 0.5f;
        float hz = room.RoomSizeHeight * roomHeight * 0.5f;
        var centre = room.transform.position;

        // Register every grid cell this room occupies so other rooms can't overlap it
        // and the generation loop treats all its cells as taken.
        for (int dx = 0; dx < room.RoomSizeWidth; dx++)
            for (int dz = 0; dz < room.RoomSizeHeight; dz++)
            {
                var cell = gridPos + new Vector2Int(dx, dz);
                placedRooms[cell] = room;
                roomBounds[cell] = (centre, hx, hz);
            }

        room.position = gridPos;
        path.Add(gridPos);
        return room;
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

        float pivotX = pos.x * roomWidth + (prefab.RoomSizeWidth - 1) * roomWidth * 0.5f;
        float pivotZ = pos.y * roomHeight + (prefab.RoomSizeHeight - 1) * roomHeight * 0.5f;
        Vector3 worldPos = new Vector3(pivotX, 0f, pivotZ);

        Room room = Instantiate(prefab, worldPos, Quaternion.identity);
        room.position = pos;
        room.roomType = roomType;
        room.SpawnCredits = 0;

        AlignRoomToNeighborExit(room, pos);

        float hx = room.RoomSizeWidth * roomWidth * 0.5f;
        float hz = room.RoomSizeHeight * roomHeight * 0.5f;
        var centre = room.transform.position;
        for (int dx = 0; dx < room.RoomSizeWidth; dx++)
            for (int dz = 0; dz < room.RoomSizeHeight; dz++)
            {
                var cell = pos + new Vector2Int(dx, dz);
                placedRooms[cell] = room;
                roomBounds[cell] = (centre, hx, hz);
            }

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
        // Remove every cell the old room occupied.
        var toRemove = placedRooms.Where(kvp => kvp.Value == oldRoom).Select(kvp => kvp.Key).ToList();
        foreach (var cell in toRemove) { placedRooms.Remove(cell); roomBounds.Remove(cell); }
        Destroy(oldRoom.gameObject);

        Room prefab = bossRoomPrefab != null
            ? bossRoomPrefab
            : PickCompatibleNormalPrefab(bossPos);

        float bossPivotX = bossPos.x * roomWidth + (prefab.RoomSizeWidth - 1) * roomWidth * 0.5f;
        float bossPivotZ = bossPos.y * roomHeight + (prefab.RoomSizeHeight - 1) * roomHeight * 0.5f;
        Room bossRoom = Instantiate(prefab, new Vector3(bossPivotX, 0f, bossPivotZ), Quaternion.identity);
        bossRoom.position = bossPos;
        bossRoom.roomType = RoomType.Boss;

        AlignRoomToNeighborExit(bossRoom, bossPos);

        float hx = bossRoom.RoomSizeWidth * roomWidth * 0.5f;
        float hz = bossRoom.RoomSizeHeight * roomHeight * 0.5f;
        var centre = bossRoom.transform.position;
        for (int dx = 0; dx < bossRoom.RoomSizeWidth; dx++)
            for (int dz = 0; dz < bossRoom.RoomSizeHeight; dz++)
            {
                var cell = bossPos + new Vector2Int(dx, dz);
                placedRooms[cell] = bossRoom;
                roomBounds[cell] = (centre, hx, hz);
            }

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
                if (neighbor == room) continue; // same large room, not a real connection

                Transform myExit = GetExitFacing(room, dir, neighborPos);
                Transform neighborExit = GetExitFacing(neighbor, -dir, pos);

                if (myExit == null || neighborExit == null) continue;
                if (Mathf.Abs(myExit.position.y - neighborExit.position.y) <= 0.1f)
                    validConnections.Add((pos, dir));
            }
        }

        // Now do the usual door/wall finalization.
        // Use UniqueRooms so each room is processed exactly once even when it
        // spans multiple grid cells.
        var seen = new HashSet<Room>();
        foreach (var kvp in placedRooms)
        {
            Room room = kvp.Value;
            if (!seen.Add(room)) continue; // already handled

            Vector2Int pos = room.position; // canonical (leftmost/bottommost) cell

            foreach (var dir in directions)
            {
                Vector2Int neighborPos = pos + dir;
                // For wide/tall rooms, exits on the top/bottom face multiple cells.
                // Iterate all exits for this direction; HandleExit picks the right neighbor cell.
                foreach (var exit in room.ExitsForDir(dir))
                {
                    // Find which neighbor cell this specific exit is closest to.
                    Vector2Int exitNeighborCell = ClosestNeighborCell(exit, pos, dir, room);
                    HandleExit(room, pos, dir, exitNeighborCell, exit);
                }
            }
        }

        if (MinimapUI.Instance != null)
            MinimapUI.Instance.BuildMinimap(placedRooms, validConnections);
    }

    /// <summary>
    /// For a given exit transform, find the grid cell on the other side of the wall
    /// that this exit is physically adjacent to.
    /// For a 1×1 room this is always just pos+dir.
    /// For a 2×1 room with two top exits, each exit points to a different cell.
    /// </summary>
    Vector2Int ClosestNeighborCell(Transform exit, Vector2Int canonicalPos, Vector2Int dir, Room room)
    {
        // Project the exit's world position onto the axis perpendicular to dir.
        // That tells us which "column" or "row" of neighbor cells it belongs to.
        if (dir == Vector2Int.right || dir == Vector2Int.left)
        {
            // Moving horizontally: exit's Z tells us which row.
            int dz = Mathf.RoundToInt((exit.position.z - canonicalPos.y * roomHeight) / roomHeight);
            dz = Mathf.Clamp(dz, 0, room.RoomSizeHeight - 1);
            return new Vector2Int(
                dir == Vector2Int.right ? canonicalPos.x + room.RoomSizeWidth : canonicalPos.x - 1,
                canonicalPos.y + dz
            );
        }
        else
        {
            // Moving vertically: exit's X tells us which column.
            int dx = Mathf.RoundToInt((exit.position.x - canonicalPos.x * roomWidth) / roomWidth);
            dx = Mathf.Clamp(dx, 0, room.RoomSizeWidth - 1);
            return new Vector2Int(
                canonicalPos.x + dx,
                dir == Vector2Int.up ? canonicalPos.y + room.RoomSizeHeight : canonicalPos.y - 1
            );
        }
    }


    void HandleExit(Room room, Vector2Int pos, Vector2Int dir, Vector2Int neighborPos, Transform exit)
    {
        if (exit == null) return;

        if (placedRooms.TryGetValue(neighborPos, out Room neighbor))
        {
            if (neighbor == room) { room.DisableExit(exit); return; } // interior cell of same room

            // Pick the neighbor's exit that is closest to this specific exit.
            Transform neighborExit = GetExitFacing(neighbor, -dir, pos);

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
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // First check actual room bounds — handles large rooms correctly.
        foreach (var kvp in roomBounds)
        {
            var (centre, hx, hz) = kvp.Value;
            if (Mathf.Abs(worldPos.x - centre.x) <= hx &&
                Mathf.Abs(worldPos.z - centre.z) <= hz)
                return kvp.Key;
        }
        // Fallback: standard grid snap for positions between rooms.
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / roomWidth),
            Mathf.RoundToInt(worldPos.z / roomHeight)
        );
    }

    bool IsSpecialRoomPriority(RoomType mine, RoomType theirs) =>
        mine != RoomType.Normal && theirs == RoomType.Normal;

    bool IsPrimary(Vector2Int a, Vector2Int b) =>
        a.x != b.x ? a.x < b.x : a.y < b.y;
}
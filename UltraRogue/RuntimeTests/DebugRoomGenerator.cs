#if RUNTIME_ROOMS
using System;
using System.Collections;
using System.Collections.Generic;
using Ultrarogue;
using Ultrarogue.SceneStuff;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using static MonoMod.RuntimeDetour.DynamicHookGen;
using Random = UnityEngine.Random;

/// <summary>
/// Drop-in replacement for RoomGenerator that creates all rooms at runtime —
/// no prefabs, no Unity editor setup required.
///
/// Layout order (Isaac-style):
///   1. Main path of Normal combat rooms.
///   2. One Treasure room, one Shop, one Gambling room — each placed as a
///      dead-end branch off the main path so the player must choose to detour.
///   3. The farthest Normal room from the origin becomes the Boss room.
///
/// Compile this file out by removing the RUNTIME_ROOMS scripting define symbol
/// (Project Settings → Player → Other Settings → Scripting Define Symbols).
/// </summary>
public class DebugRoomGenerator : MonoBehaviour
{
    // ── Config ────────────────────────────────────────────────────────────────

    [Header("Generation Settings")]
    public int minRooms = 5;
    public int maxRooms = 12;
    public int baseSpawnCredits = 30;

    [Header("Boss Room Settings")]
    [Tooltip("EnemyType spawned in the boss room.")]
    public EnemyType bossEnemyType = EnemyType.MinosPrime;

    // ── State ─────────────────────────────────────────────────────────────────

    Dictionary<Vector2Int, Room> placedRooms = new Dictionary<Vector2Int, Room>();

    /// <summary>Grid positions of Normal rooms only (used for backtracking).</summary>
    List<Vector2Int> path = new List<Vector2Int>();

    GameObject layoutRoot;

    readonly Vector2Int[] directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    // ── Public API ────────────────────────────────────────────────────────────

    public void SpawnLayout()
    {
        ClearLayout();

        layoutRoot = new GameObject("DebugLayout");

        int count = Mathf.RoundToInt((float)Random.Range(minRooms, maxRooms)
                      * (PrefsManager.Instance.GetInt("difficulty") + 1f / 2f));
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

        // ── Special rooms (Isaac-style) ────────────────────────────────────
        // Must happen before DesignateBossRoom so the boss pick ignores them.
        PlaceSpecialRooms();

        DesignateBossRoom();
        FinalizeConnections();
        BuildNavMesh();

        int special = 3; // treasure + shop + gambling
        Debug.Log($"[DebugRoomGenerator] Spawned {placed} combat rooms + {special} special rooms + 1 boss room.");
    }

    public void ClearLayout()
    {
        if (navMeshInstance.valid)
            NavMesh.RemoveNavMeshData(navMeshInstance);

        if (layoutRoot != null)
            Destroy(layoutRoot);

        placedRooms.Clear();
        path.Clear();
    }

    // ── Special room placement ─────────────────────────────────────────────────

    /// <summary>
    /// Places one Treasure room, one Shop, and one Gambling room as dead-end
    /// branches adjacent to existing Normal rooms — mirroring Isaac's approach
    /// of guaranteed special rooms that require a deliberate detour.
    /// </summary>
    void PlaceSpecialRooms()
    {
        List<Vector2Int> candidates = FindDeadEndCandidates();

        // Shuffle so rooms don't always cluster in the same direction
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        TryPlaceSpecialRoom(ref candidates, RoomType.Treasure);
        TryPlaceSpecialRoom(ref candidates, RoomType.Shop);
        TryPlaceSpecialRoom(ref candidates, RoomType.Gambling);
    }

    /// <summary>
    /// Returns all grid positions that are:
    ///   (a) not yet occupied, and
    ///   (b) adjacent to exactly ONE existing room (true dead ends).
    /// Falls back to any free adjacent position if no pure dead ends exist.
    /// </summary>
    List<Vector2Int> FindDeadEndCandidates()
    {
        var deadEnds = new HashSet<Vector2Int>();
        var anyAdjacent = new HashSet<Vector2Int>();

        foreach (var pos in placedRooms.Keys)
        {
            foreach (var dir in directions)
            {
                Vector2Int candidate = pos + dir;
                if (placedRooms.ContainsKey(candidate)) continue;

                anyAdjacent.Add(candidate);

                int neighbourCount = 0;
                foreach (var d in directions)
                    if (placedRooms.ContainsKey(candidate + d))
                        neighbourCount++;

                if (neighbourCount == 1)
                    deadEnds.Add(candidate);
            }
        }

        // Prefer pure dead ends; use all adjacent positions as fallback
        return deadEnds.Count >= 3
            ? new List<Vector2Int>(deadEnds)
            : new List<Vector2Int>(anyAdjacent);
    }

    void TryPlaceSpecialRoom(ref List<Vector2Int> candidates, RoomType roomType)
    {
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[DebugRoomGenerator] No candidate position for {roomType} room — skipping.");
            return;
        }

        Vector2Int pos = candidates[0];
        candidates.RemoveAt(0);

        Vector3 worldPos = new Vector3(
            pos.x * RuntimeRoomFactory.ROOM_SPACING,
            0f,
            pos.y * RuntimeRoomFactory.ROOM_SPACING);

        Room room;
        switch (roomType)
        {
            case RoomType.Treasure: room = RuntimeRoomFactory.CreateTreasureRoom(worldPos); break;
            case RoomType.Shop: room = RuntimeRoomFactory.CreateShopRoom(worldPos); break;
            case RoomType.Gambling: room = RuntimeRoomFactory.CreateGamblingRoom(worldPos); break;
            default:
                Debug.LogWarning($"[DebugRoomGenerator] Unknown special room type {roomType}.");
                return;
        }

        room.position = pos;
        room.transform.SetParent(layoutRoot.transform);
        placedRooms[pos] = room;
        // Do NOT add to path — special rooms aren't backtrack candidates

        Debug.Log($"[DebugRoomGenerator] {roomType} room placed at grid {pos}.");
    }

    // ── Boss room designation ─────────────────────────────────────────────────

    /// <summary>
    /// Among Normal rooms, finds the one farthest from the origin by Manhattan
    /// distance and replaces it with a boss room.
    /// </summary>
    void DesignateBossRoom()
    {
        Vector2Int bossGridPos = Vector2Int.zero;
        int bestManhattan = -1;

        foreach (var kvp in placedRooms)
        {
            // Never make the start room the boss room
            if (kvp.Key == Vector2Int.zero) continue;

            // Only promote Normal rooms — never turn a special room into the boss
            if (kvp.Value.roomType != RoomType.Normal) continue;

            int manhattan = Mathf.Abs(kvp.Key.x) + Mathf.Abs(kvp.Key.y);
            if (manhattan > bestManhattan)
            {
                bestManhattan = manhattan;
                bossGridPos = kvp.Key;
            }
        }

        if (bestManhattan < 0)
        {
            Debug.LogWarning("[DebugRoomGenerator] Could not find a valid boss room candidate.");
            return;
        }

        Room oldRoom = placedRooms[bossGridPos];
        Vector3 worldPos = oldRoom.transform.position;
        Destroy(oldRoom.gameObject);

        Room bossRoom = RuntimeRoomFactory.CreateBossRoom(worldPos, bossEnemyType);
        bossRoom.position = bossGridPos;
        bossRoom.transform.SetParent(layoutRoot.transform);
        placedRooms[bossGridPos] = bossRoom;

        Debug.Log($"[DebugRoomGenerator] Boss room at grid {bossGridPos} (Manhattan {bestManhattan}).");
    }

    // ── NavMesh ───────────────────────────────────────────────────────────────

    NavMeshDataInstance navMeshInstance;

    void BuildNavMesh() => StartCoroutine(buildDaMesh());

    void NavmeshBuilt()
    {
        SandboxNavmesh instance = MonoSingleton<SandboxNavmesh>.Instance;
        instance.navmeshBuilt = (UnityAction)Delegate.Remove(
            instance.navmeshBuilt, new UnityAction(NavmeshBuilt));
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
        surface.BuildNavMesh();
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    void PlaceRoom(Vector2Int gridPos, bool isStart = false)
    {
        float spacing = RuntimeRoomFactory.ROOM_SPACING;
        Vector3 worldPos = new Vector3(gridPos.x * spacing, 0f, gridPos.y * spacing);

        int credits = isStart ? 0 : Random.Range(5, 11);
        Room room = RuntimeRoomFactory.CreateRoom(worldPos, credits);
        room.position = gridPos;
        room.transform.SetParent(layoutRoot.transform);

        placedRooms[gridPos] = room;
        path.Add(gridPos);   // Normal rooms only go into the backtrack path

        if (isStart)
        {
            var player = NewMovement.Instance;
            if (player != null)
                player.transform.position = worldPos + Vector3.up * 2f;
        }
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
    }

    void HandleExit(Room room, Vector2Int pos, Vector2Int dir, Transform exit)
    {
        if (exit == null) return;

        Vector2Int neighborPos = pos + dir;
        var handler = room.GetComponent<RuntimeRoomDoorHandler>();

        if (placedRooms.ContainsKey(neighborPos))
        {
            if (IsPrimary(pos, neighborPos))
                handler?.PlaceDoor(exit);
            else
                room.DisableExit(exit);
        }
        else
        {
            handler?.PlaceWall(exit);
        }
    }

    bool IsPrimary(Vector2Int a, Vector2Int b) =>
        a.x != b.x ? a.x < b.x : a.y < b.y;
}
#endif // RUNTIME_ROOMS
using System.Collections.Generic;
using UnityEngine;

public class RoomGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public int minRooms = 5;
    public int maxRooms = 12;
    public int baseSpawnCredits = 30;

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

    Dictionary<Vector2Int, Room> placedRooms = new Dictionary<Vector2Int, Room>();

    List<Vector2Int> path = new List<Vector2Int>();

    readonly Vector2Int[] directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    void Awake() => GenerateRooms();

    void GenerateRooms()
    {
        if (roomPrefabs == null || roomPrefabs.Count == 0)
        {
            Debug.LogWarning("[RoomGenerator] No room prefabs assigned — skipping generation.");
            return;
        }

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

        PlaceSpecialRooms();

        DesignateBossRoom();
        FinalizeConnections();

        int special = 3;
        Debug.Log($"[RoomGenerator] Spawned {placed} combat rooms + {special} special rooms + 1 boss room.");
    }


    void PlaceRoom(Vector2Int gridPos, bool isStart = false)
    {
        Room prefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];
        Vector3 worldPos = new Vector3(gridPos.x * 10f, 0f, gridPos.y * 10f);

        Room room = Instantiate(prefab, worldPos, Quaternion.identity);
        room.position = gridPos;
        room.roomType = isStart ? RoomType.Start : RoomType.Normal;
        room.SpawnCredits = isStart ? 0 : Random.Range(5, baseSpawnCredits + 1);

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

        return deadEnds.Count >= 3
            ? new List<Vector2Int>(deadEnds)
            : new List<Vector2Int>(anyAdjacent);
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

        Room prefab = roomType switch
        {
            RoomType.Treasure => treasureRoomPrefab != null ? treasureRoomPrefab : roomPrefabs[Random.Range(0, roomPrefabs.Count)],
            RoomType.Shop => shopRoomPrefab != null ? shopRoomPrefab : roomPrefabs[Random.Range(0, roomPrefabs.Count)],
            RoomType.Gambling => gamblingRoomPrefab != null ? gamblingRoomPrefab : roomPrefabs[Random.Range(0, roomPrefabs.Count)],
            _ => roomPrefabs[Random.Range(0, roomPrefabs.Count)],
        };

        Vector3 worldPos = new Vector3(pos.x * 10f, 0f, pos.y * 10f);
        Room room = Instantiate(prefab, worldPos, Quaternion.identity);
        room.position = pos;
        room.roomType = roomType;
        room.SpawnCredits = 0;

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

            int manhattan = Mathf.Abs(kvp.Key.x) + Mathf.Abs(kvp.Key.y);
            if (manhattan > bestManhattan)
            {
                bestManhattan = manhattan;
                bossPos = kvp.Key;
            }
        }

        if (bestManhattan < 0)
        {
            Debug.LogWarning("[RoomGenerator] Could not find a valid boss room candidate.");
            return;
        }

        Room oldRoom = placedRooms[bossPos];
        Vector3 worldPos = oldRoom.transform.position;
        Destroy(oldRoom.gameObject);

        Room prefab = bossRoomPrefab != null
            ? bossRoomPrefab
            : roomPrefabs[Random.Range(0, roomPrefabs.Count)];

        Room bossRoom = Instantiate(prefab, worldPos, Quaternion.identity);
        bossRoom.position = bossPos;
        bossRoom.roomType = RoomType.Boss;
        bossRoom.bossEnemyType = bossEnemyType;
        bossRoom.SpawnCredits = 0;

        placedRooms[bossPos] = bossRoom;

        Debug.Log($"[RoomGenerator] Boss room at grid {bossPos} (Manhattan {bestManhattan}).");
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

        if (placedRooms.ContainsKey(neighborPos))
        {
            if (IsPrimary(pos, neighborPos))
                room.CreateDoor(exit);
            else
                room.DisableExit(exit);
        }
        else
        {
            room.CreateWall(exit);
        }
    }

    bool IsPrimary(Vector2Int a, Vector2Int b) =>
        a.x != b.x ? a.x < b.x : a.y < b.y;
}
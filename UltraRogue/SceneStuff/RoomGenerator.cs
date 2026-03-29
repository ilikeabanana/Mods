using System.Collections.Generic;
using System.Text;
using UnityEngine;


public class RoomGenerator : MonoBehaviour
{
    Dictionary<Vector2Int, Room> placedRooms = new Dictionary<Vector2Int, Room>();
    List<Room> rooms = new List<Room>();
    List<Vector2Int> path = new List<Vector2Int>();
    Vector2Int[] directions = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public List<Room> roomObjects = new List<Room>();
    void Awake()
    {
        GenerateRooms();
    }

    int roomCount;

    void GenerateRooms()
    {
        roomCount = Random.Range(5, 15);

        Vector2Int currentPos = Vector2Int.zero;

        Room startRoom = Instantiate(
            roomObjects[Random.Range(0, roomObjects.Count)],
            Vector3.zero,
            Quaternion.identity
        );

        startRoom.position = currentPos;

        rooms.Add(startRoom);
        placedRooms[currentPos] = startRoom;
        path.Add(currentPos);

        int placed = 1;

        while (placed < roomCount)
        {
            Vector2Int dir = directions[Random.Range(0, directions.Length)];
            int steps = Random.Range(1, 4);

            for (int i = 0; i < steps; i++)
            {
                if (placed >= roomCount)
                    break;

                Vector2Int newPos = currentPos + dir;

                if (RoomAlreadyAtSpot(newPos))
                    break;

                Room newRoom = Instantiate(
                    roomObjects[Random.Range(0, roomObjects.Count)],
                    new Vector3(newPos.x * 10, 0, newPos.y * 10),
                    Quaternion.identity
                );

                newRoom.position = newPos;

                rooms.Add(newRoom);
                placedRooms[newPos] = newRoom;
                path.Add(newPos);

                currentPos = newPos;
                placed++;
            }

            int backSteps = Random.Range(1, path.Count);
            currentPos = path[path.Count - 1 - backSteps];
        }

        FinalizeRooms();
    }

    void FinalizeRooms()
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
        Vector2Int neighborPos = pos + dir;

        if (placedRooms.TryGetValue(neighborPos, out Room neighbor))
        {
            // Prevent double doors:
            // Only place door if this room is "smaller" than neighbor
            if (IsPrimaryRoom(pos, neighborPos))
            {
                room.CreateDoor(exit);
            }
            else
            {
                room.DisableExit(exit); // neighbor will handle it
            }
        }
        else
        {
            room.CreateWall(exit);
        }
    }

    bool IsPrimaryRoom(Vector2Int a, Vector2Int b)
    {
        if (a.x != b.x)
            return a.x < b.x;

        return a.y < b.y;
    }




    bool RoomAlreadyAtSpot(Vector2Int spot)
    {
        return placedRooms.ContainsKey(spot);
    }

}

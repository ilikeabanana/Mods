#if RUNTIME_ROOMS
using UnityEngine;

/// <summary>
/// Attached to each procedural room. Uses a trigger collider spanning the room
/// floor area to detect when the player enters, then fires OnRoomEnter() once.
///
/// Compile this file out by removing the RUNTIME_ROOMS scripting define symbol
/// (Project Settings → Player → Other Settings → Scripting Define Symbols).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class RoomTrigger : MonoBehaviour
{
    Room room;
    bool triggered = false;

    void Awake()
    {
        room = GetComponent<Room>();

        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size   = new Vector3(RuntimeRoomFactory.ROOM_SIZE, RuntimeRoomFactory.WALL_HEIGHT * 2f, RuntimeRoomFactory.ROOM_SIZE);
        col.center = new Vector3(0, RuntimeRoomFactory.WALL_HEIGHT / 2f, 0);
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        room.OnRoomEnter();
    }
}
#endif // RUNTIME_ROOMS
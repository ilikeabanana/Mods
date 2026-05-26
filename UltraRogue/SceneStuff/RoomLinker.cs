using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomLinker : MonoBehaviour
{
    [Tooltip("The room script this trigger belongs to.")]
    public Room linkedRoom;

    void Awake()
    {
        if (linkedRoom == null)
            linkedRoom = transform.parent.GetComponent<Room>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (MinimapUI.Instance != null && linkedRoom != null)
            {
                MinimapUI.Instance.SetRoomOverride(linkedRoom.position);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (MinimapUI.Instance != null)
            {
                // Return control to the coordinate-based system
                MinimapUI.Instance.SetRoomOverride(null);
            }
        }
    }
}
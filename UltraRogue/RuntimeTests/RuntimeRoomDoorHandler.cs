#if RUNTIME_ROOMS
using UnityEngine;

/// <summary>
/// Replaces the prefab-based door/wall system in Room.cs for runtime testing.
/// Attach to every procedurally created Room object.
///
/// Compile this file out by removing the RUNTIME_ROOMS scripting define symbol
/// (Project Settings → Player → Other Settings → Scripting Define Symbols).
/// </summary>
public class RuntimeRoomDoorHandler : MonoBehaviour
{
    static readonly Color DoorColor = new Color(0.6f, 0.4f, 0.1f);   // Brown
    static readonly Color WallColor = new Color(0.35f, 0.35f, 0.4f); // Grey

    /// <summary>Places a coloured lintel at the exit — visually distinct from walls.</summary>
    public void PlaceDoor(Transform exit)
    {
        GameObject lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lintel.name = "Door_Lintel";
        lintel.GetComponent<Collider>().enabled = false;
        lintel.transform.SetParent(transform);
        lintel.transform.position   = exit.position + Vector3.up * RuntimeRoomFactory.WALL_HEIGHT;
        lintel.transform.rotation   = exit.rotation;
        lintel.transform.localScale = new Vector3(4f, 0.5f, 0.5f);
        SetColor(lintel, DoorColor);
    }

    /// <summary>Fills the exit gap with a solid wall cube.</summary>
    public void PlaceWall(Transform exit)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall_Filler";
        wall.transform.SetParent(transform);
        wall.transform.position   = exit.position + Vector3.up * (RuntimeRoomFactory.WALL_HEIGHT / 2f);
        wall.transform.rotation   = exit.rotation;
        wall.transform.localScale = new Vector3(4f, RuntimeRoomFactory.WALL_HEIGHT, 0.5f);
        SetColor(wall, WallColor);
    }

    static void SetColor(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (!r) return;

        Shader shader = DefaultReferenceManager.Instance != null
            ? DefaultReferenceManager.Instance.masterShader
            : Shader.Find("Standard");

        r.material = new Material(shader) { color = color };
    }
}
#endif // RUNTIME_ROOMS
using Ultrarogue.Items;
using UnityEngine;

public class GoldPickup : MonoBehaviour
{
    bool pickedUp = false;
    void Update()
    {
        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f)
        {
            if (pickedUp) return;
            pickedUp = true;
            RogueDifficultyManager.Instance.Gold++;
            Destroy(gameObject);
        }
    }

    public static void CreatePickup(Vector3 position)
    {
        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pickup.GetComponent<Collider>().enabled = false;
        pickup.AddComponent<GoldPickup>();
        pickup.transform.position = position + Vector3.up * 2;
    }
}

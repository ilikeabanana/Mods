using System;
using System.Collections.Generic;
using System.Text;
using Ultrarogue;
using Ultrarogue.Items;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public BaseItem item;
    bool pickedUp = false;
    void Update()
    {
        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f)
        {
            if (pickedUp) return;
            pickedUp = true;
            HudMessageReceiver.Instance?.SendHudMessage(item.ToString());

            Plugin.GiveItem(item);
            Destroy(gameObject);
        }
    }
    public static void CreatePickup(BaseItem item, Vector3 position)
    {
        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pickup.GetComponent<Collider>().enabled = false;
        pickup.AddComponent<ItemPickup>().item = item;
        pickup.transform.position = position + Vector3.up * 2;
    }
}

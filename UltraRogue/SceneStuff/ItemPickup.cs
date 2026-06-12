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
    Func<bool> canPickup;
    void Update()
    {
        transform.transform.LookAt(Camera.main.transform);
        transform.transform.Rotate(0, 180f, 0); // Quads face backwards

        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f)
        {
            if (pickedUp) return;
            if (canPickup != null){
                if (!canPickup.Invoke()) return;
            }
            pickedUp = true;
            HudMessageReceiver.Instance?.SendHudMessage(item.ToString());

            Plugin.GiveItem(item);
            Destroy(gameObject);
        }
    }
    public static void CreatePickup(BaseItem item, Transform position, float offset = 3)
    {
        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pickup.GetComponent<Collider>().enabled = false;
        pickup.AddComponent<ItemPickup>().item = item;
        Material mat = new Material(item.materialOverride ? item.materialOverride : AssetsManager.weaponMat);
        mat.mainTexture = item.ItemTexture;
        item.OnMaterialApply(mat);
        pickup.GetComponent<MeshRenderer>().material = mat;
        pickup.transform.position = position.position + Vector3.up * offset;
        pickup.transform.parent = position;
        pickup.transform.localScale *= 3;
    }
    public static void CreatePickupConditional(BaseItem item, Transform position, Func<bool> pickupCon, float offset = 3)
    {
        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pickup.GetComponent<Collider>().enabled = false;
        pickup.AddComponent<ItemPickup>().item = item;
        pickup.GetComponent<ItemPickup>().canPickup = pickupCon;
        Material mat = new Material(AssetsManager.weaponMat);
        mat.mainTexture = item.ItemTexture;
        pickup.GetComponent<MeshRenderer>().material = mat;
        pickup.transform.position = position.position + Vector3.up * offset;
        pickup.transform.localScale *= 3;
        pickup.transform.parent = position;
    }
}
public enum DroptableType
{
    Shop,
    RationShop,
    Planetarium,
    RandomDrop,
    Boss,
    LegendaryOnly,
    UncommonOnly,
    CommonOnly,
    BloodMachine
}
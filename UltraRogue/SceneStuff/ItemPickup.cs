using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Ultrarogue;
using Ultrarogue.Characters;
using Ultrarogue.Items;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ItemPickup : MonoBehaviour
{
    public BaseItem item;
    bool pickedUp = false;
    Func<bool> canPickup;

    float t = 0;
    float messageCooldown = 0f; // for shop "not enough gold" spam prevention

    void Update()
    {
        transform.transform.LookAt(Camera.main.transform);
        transform.transform.Rotate(0, 180f, 0); // Quads face backwards

        if (messageCooldown > 0f)
            messageCooldown -= Time.deltaTime;

        if (t > 0)
        {
            t -= Time.deltaTime;
            return;
        }

        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f)
        {
            if (pickedUp) return;
            if (canPickup != null)
            {
                if (!canPickup.Invoke()) return;
            }
            pickedUp = true;
            HudMessageReceiver.Instance?.SendHudMessage(item.ToString());
            if (Plugin.SelectedChar.GetType() == typeof(Filth))
            {
                NewMovement.Instance.FullHeal();
            }

            bool Remove = Plugin.holder.CurrentActive == null;

            Plugin.GiveItem(item, this);
            if (item is not ActiveItem || Remove)
                Destroy(gameObject);
        }
    }

    // Returns true if the current character has the passive that makes all shop items purchasable.
    static bool HasShoppingPassive()
    {
        if (Plugin.SelectedChar != null && Plugin.SelectedChar.HasPassive(Passive.Greedy))
            Plugin.Logger.LogInfo($"[ItemPickup] Yup, its a greedy bitch");
        return Plugin.SelectedChar != null && Plugin.SelectedChar.HasPassive(Passive.Greedy);
    }

    public void SwitchItem(BaseItem item, bool RemoveCondition = true, float delay = 3)
    {
        Material mat = new Material(item.materialOverride ? item.materialOverride : AssetsManager.weaponMat);
        mat.mainTexture = item.ItemTexture;
        item.OnMaterialApply(mat);
        gameObject.GetComponent<MeshRenderer>().material = mat;

        this.item = item;
        if(RemoveCondition)
            canPickup = () => true;

        t = delay; // 3 second delay before another pickup
        pickedUp = false;
    }

    public static GameObject ShopItemPrefab;

    static void AddShopPrefab(ItemPickup pickup, float offset)
    {
        if (ShopItemPrefab == null)
        {
            Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/ShopItemPrefab.prefab").Completed += handle =>
            {
                ShopItemPrefab = handle.Result;
            };
            return; // prefab not ready yet this call, bail out
        }

        BaseItem chosenItem = pickup.item;
        int price = ShopItem.getCost(chosenItem.Rarity);

        GameObject priceThingy = Instantiate(ShopItemPrefab, null);
        TMP_Text priceText = priceThingy.GetComponentInChildren<TMP_Text>();
        priceText.text = $"${price}";
        priceText.transform.position = pickup.transform.position + new Vector3(0, offset, 0);
        priceThingy.transform.parent = pickup.transform.parent;
        priceThingy.SetActive(true);
        Func<bool> existingCondition = pickup.canPickup;

        pickup.canPickup = () =>
        {
            if (existingCondition != null && !existingCondition.Invoke())
                return false;

            var mgr = RogueDifficultyManager.Instance;
            if (mgr == null) return false;

            if (mgr.Gold >= price)
            {
                mgr.Gold -= price;
                HudMessageReceiver.Instance?.SendHudMessage($"Bought: {chosenItem}  (-{price} gold)");
                Destroy(priceThingy);
                return true;
            }
            else if (pickup.messageCooldown <= 0f)
            {
                HudMessageReceiver.Instance?.SendHudMessage(
                    $"Need {price} gold  (you have {mgr.Gold})");
                pickup.messageCooldown = 2f;
            }
            return false;
        };
    }

    public static void CreatePickup(BaseItem item, Transform position, float offset = 3, float delay = 0)
    {
        int c = Plugin.GetItemCount(Null.I);
        if (c > 0)
        {
            if (UnityEngine.Random.value <= 0.5f)
                item = Plugin.GiveRandomItem(table: DroptableType.Null);
        }
        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pickup.GetComponent<Collider>().enabled = false;
        ItemPickup p =pickup.AddComponent<ItemPickup>();
        p.item = item;
        Material mat = new Material(item.materialOverride ? item.materialOverride : AssetsManager.weaponMat);
        mat.mainTexture = item.ItemTexture;
        item.OnMaterialApply(mat);
        pickup.GetComponent<MeshRenderer>().material = mat;
        pickup.transform.position = position.position + Vector3.up * offset;
        pickup.transform.parent = position;
        pickup.transform.localScale *= 3;
        p.t = delay;
        if (HasShoppingPassive())
            AddShopPrefab(p, offset);
    }
    public static void CreatePickupConditional(BaseItem item, Transform position, Func<bool> pickupCon, float offset = 3, bool isShop = false, float delay = 0)
    {
        int c = Plugin.GetItemCount(Null.I);
        if(c > 0)
        {
            if (UnityEngine.Random.value <= 0.5f)
                item = Plugin.GiveRandomItem(table: DroptableType.Null);
        }

        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pickup.GetComponent<Collider>().enabled = false;
        pickup.AddComponent<ItemPickup>().item = item;
        pickup.GetComponent<ItemPickup>().canPickup = pickupCon;
        Material mat = new Material(item.materialOverride ? item.materialOverride : AssetsManager.weaponMat);
        mat.mainTexture = item.ItemTexture;
        pickup.GetComponent<MeshRenderer>().material = mat;
        pickup.transform.position = position.position + Vector3.up * offset;
        pickup.transform.localScale *= 3;
        pickup.transform.parent = position;
        pickup.GetComponent<ItemPickup>().t = delay;
        if (HasShoppingPassive() && !isShop)
            AddShopPrefab(pickup.GetComponent<ItemPickup>(), offset);
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
    BloodMachine,
    Challenge,
    Null
}
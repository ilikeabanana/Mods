using System.Collections.Generic;
using TMPro;
using Ultrarogue;
using Ultrarogue.Items;
using Ultrarogue.SceneStuff;
using UnityEngine;

public class ShopItem : MonoBehaviour
{
    public BaseItem item;
    public int cost = 3;

    bool purchased = false;
    float messageCooldown = 0f;

    TMP_Text price;

    void Awake()
    {
        price = GetComponentInChildren<TMP_Text>();
    }

    int getCost(Rarity rar)
    {
        switch (rar)
        {
            case Rarity.Common: return 3;
            case Rarity.Uncommon: return 5;
            case Rarity.Legendary: return 8;
        }
        return 2;
    }
    public static DropTable rationTable = new DropTable(new Dictionary<Rarity, float>()
        {
            {Rarity.Common, 0.25f },
            {Rarity.Uncommon, 0.65f },
            {Rarity.Legendary, 0.1f }
        });
    void Start()
    {
        if((float)RogueDifficultyManager.ItemRNG.NextDouble() >= 0.5f)
        {

            BaseItem item = Plugin.GiveRandomItem(table: gameObject.name.Contains("Ration") ? rationTable : null);
            cost = getCost(item.Rarity);
            price.text = $"${cost}";
            ItemPickup.CreatePickupConditional(item, transform, () =>
            {
                var mgr = RogueDifficultyManager.Instance;
                if (mgr == null) return false;

                if (mgr.Gold >= cost)
                {
                    purchased = true;
                    mgr.Gold -= cost;
                    HudMessageReceiver.Instance?.SendHudMessage($"Bought: {item}  (-{cost} gold)");
                    return true;
                }
                else if (messageCooldown <= 0f)
                {
                    HudMessageReceiver.Instance?.SendHudMessage(
                        $"Need {cost} gold  (you have {mgr.Gold})");
                    messageCooldown = 2f;
                    return false;
                }
                return false;
            });
        }
        else
        {
            WeaponPickupRogue.CreatePickupConditional(transform, () =>
            {
                var mgr = RogueDifficultyManager.Instance;
                if (mgr == null) return false;

                if (mgr.Gold >= cost)
                {
                    purchased = true;
                    mgr.Gold -= cost;
                    HudMessageReceiver.Instance?.SendHudMessage($"Bought: {item}  (-{cost} gold)");
                    return true;
                }
                else if (messageCooldown <= 0f)
                {
                    HudMessageReceiver.Instance?.SendHudMessage(
                        $"Need {cost} gold  (you have {mgr.Gold})");
                    messageCooldown = 2f;
                    return false;
                }
                return false;
            });
        }

    }

    void Update()
    {
        if (purchased) return;
        if (messageCooldown > 0f) messageCooldown -= Time.deltaTime;

        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) > 2f)
            return;

        
    }

    public static ShopItem CreateShopItem(BaseItem item, Vector3 position, int cost = 3)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ShopItem";
        go.GetComponent<Collider>().enabled = false;
        go.transform.position = position + Vector3.up * 2f;
        go.transform.localScale = Vector3.one * 0.55f;
        ApplyMaterial(go, new Color(1f, 0.82f, 0.1f));   // Gold

        var si = go.AddComponent<ShopItem>();
        si.item = item;
        si.cost = cost;

        int pips = Mathf.Min(cost, 5);
        for (int i = 0; i < pips; i++)
        {
            GameObject pip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pip.name = $"PricePip_{i}";
            pip.transform.SetParent(go.transform);
            pip.transform.localPosition = new Vector3(0f, 1.4f + i * 0.35f, 0f);
            pip.transform.localScale    = new Vector3(0.25f, 0.2f, 0.25f);
            pip.GetComponent<Collider>().enabled = false;
            ApplyMaterial(pip, new Color(0.9f, 0.15f, 0.1f));
        }

        return si;
    }

    static void ApplyMaterial(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;

        Shader shader = DefaultReferenceManager.Instance != null
            ? DefaultReferenceManager.Instance.masterShader
            : Shader.Find("Standard");

        r.material = new Material(shader) { color = color };
    }
}

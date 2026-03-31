using Ultrarogue;
using Ultrarogue.Items;
using UnityEngine;

public class ShopItem : MonoBehaviour
{
    public BaseItem item;
    public int cost = 3;

    bool purchased = false;
    float messageCooldown = 0f;

    void Update()
    {
        if (purchased) return;
        if (messageCooldown > 0f) messageCooldown -= Time.deltaTime;

        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) > 2f)
            return;

        var mgr = RogueDifficultyManager.Instance;
        if (mgr == null) return;

        if (mgr.Gold >= cost)
        {
            purchased = true;
            mgr.Gold -= cost;
            HudMessageReceiver.Instance?.SendHudMessage($"Bought: {item}  (-{cost} gold)");
            Plugin.GiveItem(item);
            Destroy(gameObject);
        }
        else if (messageCooldown <= 0f)
        {
            HudMessageReceiver.Instance?.SendHudMessage(
                $"Need {cost} gold  (you have {mgr.Gold})");
            messageCooldown = 2f;
        }
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

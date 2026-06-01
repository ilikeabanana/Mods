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
    public Plugin.DroptableType tableType;

    bool purchased = false;
    float messageCooldown = 0f;

    TMP_Text price;

    // ── Per-floor reservation tracking ──────────────────────────────────────
    static readonly HashSet<BaseItem> s_reservedItems = new HashSet<BaseItem>();
    static readonly HashSet<string> s_reservedWeapons = new HashSet<string>();
    static int s_trackedFloor = -1;

    /// <summary>
    /// Called once per floor (lazily, from Start) to wipe stale reservations.
    /// </summary>
    static void TryResetForFloor()
    {
        int currentFloor = RogueDifficultyManager.Instance != null
            ? RogueDifficultyManager.Instance.floor
            : 0;

        if (currentFloor != s_trackedFloor)
        {
            s_reservedItems.Clear();
            s_reservedWeapons.Clear();
            s_trackedFloor = currentFloor;
        }
    }

    // ── Item helper ─────────────────────────────────────────────────────────
    /// <summary>
    /// Picks a random item that no other shop slot on this floor has already reserved.
    /// Falls back to any item after 30 failed attempts (e.g. tiny item pool).
    /// </summary>
    static BaseItem PickUniqueItem(Plugin.DroptableType table)
    {
        const int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            BaseItem candidate = Plugin.GiveRandomItem(table: table);
            if (!s_reservedItems.Contains(candidate))
            {
                s_reservedItems.Add(candidate);
                return candidate;
            }
        }

        // Pool exhausted or very small — just return whatever
        BaseItem fallback = Plugin.GiveRandomItem(table: table);
        s_reservedItems.Add(fallback);
        return fallback;
    }

    // ── Weapon helper ────────────────────────────────────────────────────────
    /// <summary>
    /// Generates a weapon that no other shop slot on this floor has already reserved.
    /// </summary>
    static AWeapon PickUniqueWeapon()
    {
        const int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            AWeapon candidate = AWeapon.GenerateWeapon();
            string key = $"{candidate.weapon}_{candidate.variant}_{candidate.Alternate}";
            if (!s_reservedWeapons.Contains(key))
            {
                s_reservedWeapons.Add(key);
                return candidate;
            }
        }

        // Fallback
        AWeapon fallback = AWeapon.GenerateWeapon();
        s_reservedWeapons.Add($"{fallback.weapon}_{fallback.variant}_{fallback.Alternate}");
        return fallback;
    }

    // ────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        price = GetComponentInChildren<TMP_Text>();
    }

    int GetScaledCost(int baseCost)
    {
        int floor = RogueDifficultyManager.Instance != null
            ? RogueDifficultyManager.Instance.floor
            : 0;

        // +53% every 5 floors
        int scalingSteps = floor / 5;

        float multiplier = Mathf.Pow(1.53f, scalingSteps);

        return Mathf.CeilToInt(baseCost * multiplier);
    }

    int getCost(Rarity rar)
    {
        int baseCost = 2;

        switch (rar)
        {
            case Rarity.Common:
                baseCost = 4;
                break;

            case Rarity.Uncommon:
                baseCost = 7;
                break;

            case Rarity.Legendary:
                baseCost = 12;
                break;
        }

        return GetScaledCost(baseCost);
    }


    void Start()
    {
        TryResetForFloor();

        if ((float)RogueDifficultyManager.ItemRNG.NextDouble() >= 0.5f)
        {
            BaseItem chosenItem = PickUniqueItem(tableType);

            cost = getCost(chosenItem.Rarity);
            price.text = $"${cost}";

            ItemPickup.CreatePickupConditional(chosenItem, transform, () =>
            {
                var mgr = RogueDifficultyManager.Instance;
                if (mgr == null) return false;

                if (mgr.Gold >= cost)
                {
                    purchased = true;
                    mgr.Gold -= cost;
                    HudMessageReceiver.Instance?.SendHudMessage($"Bought: {chosenItem}  (-{cost} gold)");
                    return true;
                }
                else if (messageCooldown <= 0f)
                {
                    HudMessageReceiver.Instance?.SendHudMessage(
                        $"Need {cost} gold  (you have {mgr.Gold})");
                    messageCooldown = 2f;
                }
                return false;
            });
        }
        else
        {
            AWeapon chosenWeapon = PickUniqueWeapon();

            int weaponBaseCost = 5;

            weaponBaseCost += RogueDifficultyManager.ItemRNG.Next(-1, 3);

            weaponBaseCost = Mathf.Max(4, weaponBaseCost);

            cost = GetScaledCost(weaponBaseCost);

            price.text = $"${cost}";

            WeaponPickupRogue.CreatePickupConditional(transform, () =>
            {
                var mgr = RogueDifficultyManager.Instance;
                if (mgr == null) return false;

                if (mgr.Gold >= cost)
                {
                    purchased = true;
                    mgr.Gold -= cost;
                    HudMessageReceiver.Instance?.SendHudMessage($"Bought: {chosenWeapon}  (-{cost} gold)");
                    return true;
                }
                else if (messageCooldown <= 0f)
                {
                    HudMessageReceiver.Instance?.SendHudMessage(
                        $"Need {cost} gold  (you have {mgr.Gold})");
                    messageCooldown = 2f;
                }

                return false;
            }, weapon: chosenWeapon);
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
        ApplyMaterial(go, new Color(1f, 0.82f, 0.1f));

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
            pip.transform.localScale = new Vector3(0.25f, 0.2f, 0.25f);
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
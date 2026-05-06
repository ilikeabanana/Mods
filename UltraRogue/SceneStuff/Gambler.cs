using Ultrarogue;
using UnityEngine;

public class Gambler : MonoBehaviour
{
    const float GAMBLE_COOLDOWN = 1.5f;
    const float EXPLOSION_BASE_CHANCE = 0.08f;   // 8% on first use
    const float EXPLOSION_CHANCE_RAMP = 0.07f;   // +7% each subsequent use
    const float EXPLOSION_RADIUS = 5f;
    const float EXPLOSION_DAMAGE = 40f;

    float cooldown = 0f;
    int useCount = 0;
    bool exploded = false;

    Transform itemPlacementThing;

    void Update()
    {
        if (exploded) return;

        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
            return;
        }

        if (NewMovement.Instance == null) return;

        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f)
        {
            Activate();
            cooldown = GAMBLE_COOLDOWN;
        }
    }

    void Awake()
    {
        if (itemPlacementThing == null)
        {
            itemPlacementThing = new GameObject("ItemPar").transform;
            itemPlacementThing.transform.parent = transform.parent;
            itemPlacementThing.position = transform.position;
        }
    }

    public void Activate()
    {
        var mgr = RogueDifficultyManager.Instance;
        if (mgr == null) return;

        if (mgr.Gold <= 0)
        {
            HudMessageReceiver.Instance?.SendHudMessage("No gold to gamble!");
            return;
        }

        mgr.Gold--;
        useCount++;

        // Check for explosion before resolving the gamble
        float explosionChance = EXPLOSION_BASE_CHANCE + EXPLOSION_CHANCE_RAMP * (useCount - 1);
        if (RogueDifficultyManager.GambleItemRNG.NextDouble() <= explosionChance)
        {
            Explode();
            return;
        }

        if (RogueDifficultyManager.GambleItemRNG.NextDouble() <= 0.35f)
        {
            HudMessageReceiver.Instance?.SendHudMessage("You won!");
            ItemPickup.CreatePickup(Plugin.GiveRandomItem(RogueDifficultyManager.GambleItemRNG), itemPlacementThing);
        }
        else
        {
            HudMessageReceiver.Instance?.SendHudMessage("You lost... try again?");
        }
    }

    void Explode()
    {
        exploded = true;

        var explosionPrefab = DefaultReferenceManager.Instance.explosion;
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
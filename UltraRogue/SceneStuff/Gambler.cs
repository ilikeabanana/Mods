using System.Collections;
using Ultrarogue;
using UnityEngine;

public class Gambler : MonoBehaviour
{
    const float GAMBLE_COOLDOWN = 1.5f;
    const float EXPLOSION_BASE_CHANCE = 0.08f;   // 8% on first use
    const float EXPLOSION_CHANCE_RAMP = 0.07f;   // +7% each subsequent use
    public GameObject ExplosionWarningThing;
    public ShopZone zone;
    float cooldown = 0f;
    int useCount = 0;
    bool exploded = false;

    Transform itemPlacementThing;
    public void Gamble()
    {
        if (exploded) return;
        Activate();
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
            ItemPickup.CreatePickup(Plugin.GiveRandomItem(RogueDifficultyManager.GambleItemRNG), itemPlacementThing, 5);
        }
        else
        {
            HudMessageReceiver.Instance?.SendHudMessage("You lost... try again?");
        }
    }

    void Explode()
    {
        exploded = true;
        StartCoroutine(explosionNumerator());
    }

    IEnumerator explosionNumerator()
    {
        ExplosionWarningThing.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        var explosionPrefab = DefaultReferenceManager.Instance.explosion;
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            zone.ForceOff();
            MonoSingleton<AudioMixerController>.Instance.SetMusicVolume(zone.originalMusicVolume);
        }

        Destroy(gameObject);
    }
}
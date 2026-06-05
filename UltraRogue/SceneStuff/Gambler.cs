using System.Collections;
using System.Collections.Generic;
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

    public GameObject Slot1;
    public GameObject Slot2;
    public GameObject Slot3;

    List<GameObject> slots = new List<GameObject>();
    public void Gamble()
    {
        if (exploded) return;
        //Activate();
        Spin();
    }
        

    void Awake()
    {
        if (itemPlacementThing == null)
        {
            itemPlacementThing = new GameObject("ItemPar").transform;
            itemPlacementThing.transform.parent = transform.parent;
            itemPlacementThing.position = transform.position;
        }

        if (Slot1 == null)
            Slot1 = GetSlot("Slot (1)");

        if (Slot2 == null)
            Slot2 = GetSlot("Slot (2)");

        if (Slot3 == null)
            Slot3 = GetSlot("Slot (3)");

        slots.Add(Slot1);
        slots.Add(Slot2);
        slots.Add(Slot3);
    }

    GameObject GetSlot(string name)
    {
        return transform.Find("Canvas/Background/Text Inset/" + name).gameObject;
    }

    void Spin() => StartCoroutine(SpinRoutine());

    IEnumerator SpinRoutine()
    {
        float t = 0;

        while (t <= 10)
        {
            t += Time.deltaTime;

            foreach (GameObject slot in slots)
            {
                Transform s = slot.transform.Find("Slots");

                s.position -= (Vector3.up * Time.deltaTime * 10f) / t;
                // (3) is the thing that is at 0 0 0
            }

            yield return null;
        }
    }
    /* Old Gamble Code
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
    }*/

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
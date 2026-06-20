using Steamworks.Ugc;
using System.Collections;
using System.Collections.Generic;
using Ultrarogue;
using Ultrarogue.Items;
using UnityEngine;
using UnityEngine.UI;

public class Gambler : MonoBehaviour
{
    const float EXPLOSION_BASE_CHANCE = 0.02f;   // 2% on first use
    const float EXPLOSION_CHANCE_RAMP = 0.02f;   // +2% each subsequent use
    public GameObject ExplosionWarningThing;
    public ShopZone zone;
    int useCount = 0;
    bool exploded = false;

    Coroutine activeRoutine = null;

    Transform itemPlacementThing;

    public GameObject Slot1;
    public GameObject Slot2;
    public GameObject Slot3;
    public Texture2D coinText;
    public Texture2D keyText;
    List<GameObject> slots = new List<GameObject>();
    public void Gamble()
    {
        if (exploded) return;
        if (activeRoutine != null) return; // respect cooldown
        var mgr = RogueDifficultyManager.Instance;
        if (mgr == null) return;

        if (mgr.Gold <= 0)
        {
            HudMessageReceiver.Instance?.SendHudMessage("No gold to gamble!");
            return;
        }

        mgr.Gold--;
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

        foreach (var item in Plugin.possibleItems)
        {
            texts.Add(item.ItemTexture);
        }
        skullText = Slot1.transform.Find("Slots").GetComponentInChildren<RawImage>().mainTexture;
        texts.Add((Texture2D)skullText);


        foreach (var s in slots)
        {
            Transform ss = s.transform.Find("Slots");
            AssignImage(2, ss, texts[Random.Range(0, texts.Count)]);
            AssignImage(4, ss, texts[Random.Range(0, texts.Count)]);
            AssignImage(3, ss, texts[Random.Range(0, texts.Count)]);

        }
    }

    GameObject GetSlot(string name)
    {
        return transform.Find("Canvas/Background/Text Inset/" + name).gameObject;
    }

    void Spin() => activeRoutine = StartCoroutine(SpinRoutine());

    void AssignImage(int num, Transform parent, Texture2D text)
    {
        Transform r = parent.Find($"RawImage ({num})");
        Transform r2 = parent.Find($"RawImage ({num + 3})");

        r.GetComponent<RawImage>().texture = text;
        r2.GetComponent<RawImage>().texture = text;
    }
    Texture skullText;
    List<Texture2D> texts = new List<Texture2D>();
    float Threshold = 240; // THIS TOOK SO LONG TO FIND THIS ONE FUCKING NUMBER

    IEnumerator SpinRoutine()
    {
        BaseItem item = null;
        bool willExplode = false;

        // Check explosion first
        useCount++;
        float explosionChance = EXPLOSION_BASE_CHANCE + EXPLOSION_CHANCE_RAMP * (useCount - 1);

        // For explosion: we want luck to make it LESS likely, so we invert luck effect
        // by passing luckaffected with a negated context — easiest: pass luckaffected=false
        // and manually do the "bad luck" roll (picks highest value = harder to trigger)
        float explosionRoll = Plugin.getChanceVal(RogueDifficultyManager.GambleItemRNG, luckaffected: false);
        int luck = Plugin.luck; // adjust to however you access luck
        if (luck >= 0)
        {
            for (int i = 0; i < luck; i++)
            {
                float candidate = (float)RogueDifficultyManager.GambleItemRNG.NextDouble();
                if (candidate > explosionRoll) explosionRoll = candidate; // take HIGHEST = harder to explode
            }
        }
        else
        {
            for (int i = 0; i < -luck; i++)
            {
                float candidate = (float)RogueDifficultyManager.GambleItemRNG.NextDouble();
                if (candidate < explosionRoll) explosionRoll = candidate; // negative luck = easier to explode
            }
        }

        if (explosionRoll <= explosionChance)
        {
            willExplode = true;
        }
        else
        {
            // Item win: luck-affected normally (lower roll = more likely to win)
            float itemRoll = Plugin.getChanceVal(luckaffected: true);
            if (itemRoll <= 0.35f)
            {
                item = Plugin.GiveRandomItem(RogueDifficultyManager.GambleItemRNG);
            }
        }

        foreach (var s in slots)
        {
            Transform ss = s.transform.Find("Slots");
            AssignImage(2, ss, texts[Random.Range(0, texts.Count)]);
            AssignImage(4, ss, texts[Random.Range(0, texts.Count)]);

            if (willExplode)
                AssignImage(3, ss, (Texture2D)skullText); // all skulls = boom
            else if (item != null)
                AssignImage(3, ss, item.ItemTexture);     // item won
            else
                AssignImage(3, ss, texts[Random.Range(0, texts.Count)]); // loss
        }

        float snapTime = 0.35f;
        float duration = 1f;
        float t = 0;

        Dictionary<GameObject, float> slotSpeeds = new Dictionary<GameObject, float>();
        foreach (GameObject slot in slots)
            slotSpeeds[slot] = Random.Range(0.7f, 1.3f);

        while (t <= duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            float baseSpeed = Mathf.Lerp(10f, 0f, progress);

            foreach (GameObject slot in slots)
            {
                Transform s = slot.transform.Find("Slots");
                float speed = baseSpeed * slotSpeeds[slot];
                float maxStep = 74f;
                float step = Mathf.Min(speed * Time.deltaTime, maxStep);

                s.position -= Vector3.up * step;

                if (s.localPosition.y <= -Threshold)
                {
                    float offset = s.localPosition.y + Threshold;
                    s.localPosition = new Vector3(s.localPosition.x, offset, s.localPosition.z);
                }
            }

            yield return null;
        }

        // Snap to 0
        t = 0;
        while (t <= 1)
        {
            t += Time.deltaTime / snapTime;

            foreach (GameObject slot in slots)
            {
                Transform s = slot.transform.Find("Slots");
                float position = Mathf.Lerp(s.localPosition.y, 0, t);
                s.localPosition = new Vector3(s.localPosition.x, position, s.localPosition.z);
            }

            yield return null;
        }

        foreach (GameObject slot in slots)
        {
            Transform s = slot.transform.Find("Slots");
            s.localPosition = new Vector3(s.localPosition.x, 0, s.localPosition.z);
        }

        // Resolve outcome after spin finishes
        if (willExplode)
        {
            Explode();
        }
        else if (item != null)
        {
            ItemPickup.CreatePickup(item, itemPlacementThing, 8);
            HudMessageReceiver.Instance.SendHudMessage("YOU WIN!");
        }
        else
        {
            HudMessageReceiver.Instance.SendHudMessage("You lost... try again?");
        }
        activeRoutine = null; // allow user to gamble again
    }
    void Explode()
    {
        exploded = true;
        StartCoroutine(explosionNumerator());
    }

    IEnumerator explosionNumerator()
    {
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
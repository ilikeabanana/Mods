using System;
using System.Collections;
using System.Collections.Generic;
using Ultrarogue;
using Ultrarogue.SceneStuff;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class Chest : MonoBehaviour
{
    List<ChestLootpool> pools;

    bool pickedUp = false;
    Animator anim;

    public float openDelay = 0f;
    float openTimer = 0f;

    void Awake()
    {
        anim = GetComponent<Animator>();
        pools = new List<ChestLootpool>()
        { 
            new ChestLootpool(1, 4),                          // Gold only
            new ChestLootpool(0, 3, 0, 2),                    // Key AND Gold
            new ChestLootpool(MinKeys: 1, MaxKeys: 3),        // Key only
            new ChestLootpool(open: () =>
            {
                SpawnAtChest(anchor => ItemPickup.CreatePickup(Plugin.GiveRandomItem(), anchor, 4, 1));
            }, weight: 0.3f),                                 // Item spawning 
            new ChestLootpool(open: () =>
            {
                SpawnableObject filth = AssetsManager.GetEnemiesOfType(EnemyType.Filth)[0];

                Instantiate(filth.gameObject, transform.position, Quaternion.identity).transform.parent = transform.parent;
                Instantiate(filth.gameObject, transform.position, Quaternion.identity).transform.parent = transform.parent;
            }),                                               // Two filth
        };
    }
    void OnEnable()
    {

        if (pickedUp)
        {
            anim.Play("Open", 0, 1);
            anim.Update(0);
        }
    }

    void Update()
    { 
        if (openTimer < openDelay)
        {
            openTimer += Time.deltaTime;
            return;
        }

            

        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f)
        {
            if (pickedUp) return;

            pickedUp = true;

            anim.SetTrigger("Open");

            OpenChest();
        }
    }

    void OpenChest()
    {
        ChestLootpool pool = PickPool();
        pool.open?.Invoke();

        int goldAmount = Random.Range(pool.MinGold, pool.MaxGold + 1);
        int keyAmount = Random.Range(pool.MinKeys, pool.MaxKeys + 1);

        for (int i = 0; i < goldAmount; i++)
        {
            SpawnAt(anchor =>
            {
                GameObject g = GoldPickup.CreatePickup(anchor, 1);
                g.GetComponent<Rigidbody>().AddForce(transform.right * 3, ForceMode.VelocityChange);
            });
        }

        for (int i = 0; i < keyAmount; i++)
        {
            SpawnAt(anchor => KeyPickup.CreatePickup(anchor));
        }
    }

    ChestLootpool PickPool()
    {
        float totalWeight = 0f;

        foreach (var p in pools)
            totalWeight += p.weight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var p in pools)
        {
            cumulative += p.weight;

            if (roll <= cumulative)
                return p;
        }

        return pools[pools.Count - 1];
    }

    void SpawnAt(Action<Transform> createPickup)
    {
        Vector3 itemPos = transform.position + (-transform.right) * 4;
        itemPos += new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));

        GameObject anchor = new GameObject("ItemDropAnchor");
        anchor.transform.position = itemPos;
        anchor.transform.parent = transform;

        createPickup(anchor.transform);
    }

    void SpawnAtChest(Action<Transform> createPickup)
    {
        Vector3 itemPos = transform.position;

        GameObject anchor = new GameObject("ItemDropAnchor");
        anchor.transform.position = itemPos;
        anchor.transform.parent = transform;

        createPickup(anchor.transform);
    }

    public static GameObject prefab;

    public static GameObject CreateChest(Transform position, float openDelay = 0f)
    {
        if (prefab == null)
            prefab = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/Chest.prefab").WaitForCompletion();

        GameObject pickup = Instantiate(prefab);

        Chest chest = pickup.AddComponent<Chest>();
        chest.openDelay = openDelay;

        pickup.transform.position = position.position + (Vector3.up / 2);
        pickup.transform.parent = position;

        return pickup;
    }
}


public class ChestLootpool
{
    public int MinGold;
    public int MaxGold;

    public int MinKeys;
    public int MaxKeys;

    public float weight;

    public Action open;

    public ChestLootpool(
        int MinGold = 0,
        int MaxGold = 0,
        int MinKeys = 0,
        int MaxKeys = 0,
        Action open = null,
        float weight = 1)
    {
        this.MinGold = MinGold;
        this.MaxGold = MaxGold;
        this.MinKeys = MinKeys;
        this.MaxKeys = MaxKeys;
        this.open = open;
        this.weight = weight;
    }
}
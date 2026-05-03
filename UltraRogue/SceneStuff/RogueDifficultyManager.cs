using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Ultrarogue;
using Ultrarogue.Items;
using System;


// This is actually just the rogue manager... but eh, too lazy to change lol
public class RogueDifficultyManager : MonoBehaviour
{
    public static RogueDifficultyManager Instance { get; private set; }

    [SerializeField] TMP_Text goldText;
    [SerializeField] TMP_Text keyText;
    [SerializeField] GridLayoutGroup itemParent;

    public float Difficulty;

    public int Gold;
    public int Keys;

    float difficultyScaleMult = 1f;
    public int floor = 1;
    void Awake()
    {
        Instance = this;
        Difficulty = 1;

        itemsUI = GameObject.Find("Items").transform.Find("Panel").gameObject;
        itemsUI.SetActive(false);
    }

    GameObject itemsUI;
    
    public void AddItem(BaseItem item)
    {
        GameObject img = new GameObject();
        Image imgg = img.AddComponent<Image>();
        imgg.sprite = item.ItemIcon;

        img.transform.parent = itemsUI.transform;
    }

    void Update()
    {
        if (InputManager.Instance.InputSource.Stats.IsPressed)
        {
            itemsUI.SetActive(true);
        }
        else
        {
            itemsUI.SetActive(false);
        }

        Difficulty += (Time.deltaTime / 180) * difficultyScaleMult;
        goldText.text = "Gold: " + Gold;
        keyText.text = "Keys: " + Keys;
    }

    public void MoveStage()
    {
        Difficulty *= 1.2f;
        Plugin.Logger.LogInfo("Before: " + difficultyScaleMult);
        int diff = Plugin.CurrentDifficulty;
        Plugin.Logger.LogInfo("Difficulty pref: " + diff);
        difficultyScaleMult *= 1.34f * ((diff + 1) / 3);
        Plugin.Logger.LogInfo("After: " + difficultyScaleMult);
        // Harmless = 0,3333333333333333 = 0,4466666666666666 per stage
        // Lenient = 0,6666666666666667 = 0,8933333333333334 per stage
        // Standard = 1 = 1.34 per stage
        // Violent = 1,333333333333333 = 1,786666666666666 per stage
        // Brutal = 1,666666666666667 = 2,233333333333334 per stage
        // UKMD = 2 = 2.68 per stage
        floor++;
    }

    public int GetCountBeforeRadiance(EnemyType enemyType)
    {
        switch (enemyType)
        {
            case EnemyType.Filth:
                return 10;
            case EnemyType.Stray:
            case EnemyType.Schism:
            case EnemyType.Streetcleaner:
            case EnemyType.Drone:
                return 5;
            case EnemyType.Idol:
            case EnemyType.Deathcatcher:
                return 3;
            case EnemyType.Turret:
            case EnemyType.Cerberus:
            case EnemyType.Virtue:
            case EnemyType.Gutterman:
            case EnemyType.Guttertank:
                return 4;
        }
        return 2;
    }

    public BossPick GetBoss()
    {
        List<BossPick> options = new List<BossPick>();

        switch (floor)
        {
            case 1:
            case 2:
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Cerberus)[0].gameObject, 0),
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Cerberus)[0].gameObject, 0),
                }));

                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MaliciousFace)[0].gameObject, 30),
                }));

                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.VeryCancerousRodent)[0].gameObject, 0),
                }));

                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Swordsmachine)[0].gameObject, 0),
                }));

                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Swordsmachine)[0].gameObject, 0),
                }));

                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Power)[0].gameObject, 20),
                }));

                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Mindflayer)[0].gameObject, 0),
                }));
                break;
            case 3:
            case 4:
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.V2)[0].gameObject, 0),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Gabriel)[0].gameObject, 50),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.HideousMass)[0].gameObject, 50),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Ferryman)[0].gameObject, 50),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Sisyphus)[0].gameObject, 50),
                }));
                break;
            case 5:
            case 6:
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Ferryman)[0].gameObject, 0),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Minotaur)[0].gameObject, 0),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.GabrielSecond)[0].gameObject, 0),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Mandalore)[0].gameObject, 0),
                }));

                Enemy A = null;
                Enemy T = null;

                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.Agony, 0),
                    new BossEntry(AssetsManager.Tundra, 0),
                }, (eid) =>
                {
                    if (A == null)
                    {
                        A = Room.FindEnemyComponent(eid.gameObject);
                        return;
                    }
                    if (T == null)
                    {
                        T = Room.FindEnemyComponent(eid.gameObject);
                    }

                    A.symbiote = T;
                    T.symbiote = A;

                }));
                options.Add(new BossPick(new List<List<BossEntry>>()
                {
                    new List<BossEntry> { new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Power)[0].gameObject, 0) }, // Wave 1
                    new List<BossEntry> { new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Power)[0].gameObject, 0), 
                        new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Power)[0].gameObject, 0) } // Wave 2
                }));
                break;
            default:
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MinosPrime)[0].gameObject, 0),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.SisyphusPrime)[0].gameObject, 130),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MirrorReaper)[0].gameObject, 0),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.BigJohnator)[0].gameObject, 100),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MaliciousFace)[0].gameObject, 60),
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MaliciousFace)[0].gameObject, 60),
                }));
                break;
        }

        if (options.Count == 0)
            return null;

        return options[UnityEngine.Random.Range(0, options.Count)];
    }



    public int GetCost(EnemyType enemyType)
    {
        switch(enemyType)
        {
            case EnemyType.Filth:
                return 1;
            case EnemyType.Stray:
                return 2;
            case EnemyType.Schism:
                return 5;
            case EnemyType.Streetcleaner:
                return 5;
            case EnemyType.Cerberus:
                return 16;
            case EnemyType.Swordsmachine:
                return 20;
            case EnemyType.Drone:
                return 4;
            case EnemyType.HideousMass:
                return 40;
            case EnemyType.V2:
                return 175;
            case EnemyType.V2Second:
                return 200;
            case EnemyType.SisyphusPrime:
                return 400;
            case EnemyType.MinosPrime:
                return 350;
            case EnemyType.Minotaur:
                return 250;
            case EnemyType.Gabriel:
                return 325;
            case EnemyType.GabrielSecond:
                return 335;
            case EnemyType.Soldier:
                return 6;
            case EnemyType.Mindflayer:
                return 55;
            case EnemyType.Sisyphus:
                return 75;
            case EnemyType.Providence:
                return 30;
            case EnemyType.Turret:
                return 30;
            case EnemyType.Stalker:
                return 20;
            case EnemyType.Gutterman:
                return 45;
            case EnemyType.Virtue:
                return 20;
            case EnemyType.Idol:
                return 30;
            case EnemyType.Deathcatcher:
                return 50;
            case EnemyType.Guttertank:
                return 40;
            case EnemyType.Mannequin:
                return 25;
            case EnemyType.MirrorReaper:
                return 125;
            case EnemyType.Ferryman:
                return 55;
            case EnemyType.MaliciousFace:
                return 13;
            case EnemyType.Power:
                return 100;
            
        }
        return int.MaxValue - 1;
    }

}

public class BossPick
{
    // Each entry in this list is a "Wave" (which is itself a list of BossEntries)
    public List<List<BossEntry>> waves = new List<List<BossEntry>>();
    public Action<EnemyIdentifier> onSpawn;

    public BossPick(List<List<BossEntry>> waves, Action<EnemyIdentifier> onSpawn = null)
    {
        this.waves = waves;
        this.onSpawn = onSpawn;
    }

    // Constructor for single-wave bosses (backward compatibility)
    public BossPick(List<BossEntry> singleWave, Action<EnemyIdentifier> onSpawn = null)
    {
        this.waves = new List<List<BossEntry>> { singleWave };
        this.onSpawn = onSpawn;
    }
}

public class BossEntry
{
    public GameObject prefab;
    public float healthMod;


    public BossEntry(GameObject prefab, float healthMod = 0)
    {
        this.prefab = prefab;
        this.healthMod = healthMod;
    }
}
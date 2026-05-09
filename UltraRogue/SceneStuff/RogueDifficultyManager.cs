using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Ultrarogue;
using Ultrarogue.Items;
using System;


// This is actually just the rogue manager... but eh, too lazy to change lol
[DefaultExecutionOrder(-1)]
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
    Dictionary<string, int> itemCounts;
    Dictionary<string, GameObject> itemUIObjects;

    public static System.Random ItemRNG;
    public static System.Random GambleItemRNG;
    public static System.Random RoomRNG;
    public static System.Random BossRNG;

    void Awake()
    {
        ItemRNG = new System.Random(Plugin.GameSeed.GetHashCode());
        GambleItemRNG = new System.Random(Plugin.GameSeed.GetHashCode() * 2); // Considering you can infinitely gamble, it might cause rng issues.
        RoomRNG = new System.Random(Plugin.GameSeed.GetHashCode() / 2);
        BossRNG = new System.Random(Plugin.GameSeed.GetHashCode() ^ 2);
        Instance = this;
        Difficulty = Plugin.CurrentDifficulty;

        itemCounts = new Dictionary<string, int>();
        itemUIObjects = new Dictionary<string, GameObject>();

        itemsUI = GameObject.Find("Items").transform.Find("Panel").gameObject;
        itemsUI.SetActive(false);
        itemParent = itemsUI.GetComponent<GridLayoutGroup>();

        Transform stats = GameObject.Find("Items").transform.Find("Stats");

        statSpeedText = stats.Find("StatSpeed/Stt").GetComponent<TMP_Text>();
        statDamageText = stats.Find("StatDamage/Stt").GetComponent<TMP_Text>();
        statAtkSpeedText = stats.Find("StatAtkSpeed/Stt").GetComponent<TMP_Text>();
        statCooldownText = stats.Find("StatCooldown/Stt").GetComponent<TMP_Text>();
    }

    void UpdateStatsUI()
    {
        if (NewMovement.Instance == null) return;

        // Movement speed
        float speed = NewMovement.Instance.walkSpeed;
        float baseSpeed = Plugin.Instance.normalMoveSpeed;

        float speedMult = speed / baseSpeed;

        // Attack speed
        float atkSpeed = Plugin.AttackSpeed.CalculateChanges(1f);

        // Damage
        float dmg = Plugin.globalDamageMult.CalculateChanges(1f);

        // Cooldown
        float cd = Plugin.cooldownReduction.CalculateChanges(1f);

        // Apply text
        statSpeedText.text = $"x{speedMult:F2}";
        statDamageText.text = $"x{dmg:F2}";
        statAtkSpeedText.text = $"x{atkSpeed:F2}";
        statCooldownText.text = $"x{cd:F2}";
    }

    public void AddItem(BaseItem item)
    {
        if (item == null)
        {
            Plugin.Logger.LogError("AddItem called with null item!");
            return;
        }

        if (itemParent == null)
        {
            Plugin.Logger.LogError("itemParent is not assigned in the Inspector!");
            
            return;
        }

        string itemKey = item.ItemName;

        if (itemCounts.ContainsKey(itemKey))
        {
            itemCounts[itemKey]++;

            if (itemUIObjects.TryGetValue(itemKey, out GameObject existingUI) && existingUI != null)
            {
                TMP_Text countLabel = existingUI.GetComponentInChildren<TMP_Text>();
                if (countLabel != null)
                    countLabel.text = $"(x{itemCounts[itemKey]})";
            }
        }
        else
        {
            itemCounts[itemKey] = 1;

            GameObject container = new GameObject(itemKey);
            container.transform.SetParent(itemParent.transform, false);

            Image icon = container.AddComponent<Image>();
            if (item.ItemIcon != null)
                icon.sprite = item.ItemIcon;
            else
                Plugin.Logger.LogWarning($"Item '{itemKey}' has no icon!");

            GameObject labelObj = new GameObject("CountLabel");
            labelObj.transform.SetParent(container.transform, false);

            TMP_Text countText = labelObj.AddComponent<TextMeshProUGUI>();
            countText.text = "";
            countText.fontSize = 14;
            countText.alignment = TextAlignmentOptions.BottomRight;
            countText.color = Color.white;

            RectTransform rt = labelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            itemUIObjects[itemKey] = container;
        }
    }

    GameObject itemsUI;
    private TMP_Text statSpeedText;
    private TMP_Text statDamageText;
    private TMP_Text statAtkSpeedText;
    private TMP_Text statCooldownText;

    void Update()
    {
        if (InputManager.Instance.InputSource.Stats.IsPressed)
        {
            itemsUI.SetActive(true);
            statDamageText.transform.parent.parent.gameObject.SetActive(true);
        }
        else
        {
            itemsUI.SetActive(false);
            statDamageText.transform.parent.parent.gameObject.SetActive(false);
        }

        Difficulty += (Time.deltaTime / 180) * difficultyScaleMult;
        goldText.text = "Gold: " + Gold;
        keyText.text = "Keys: " + Keys;
        UpdateStatsUI();
    }

    public void MoveStage()
    {
        Difficulty *= 1.2f;
        Plugin.Logger.LogInfo("Before: " + difficultyScaleMult);
        int diff = Plugin.CurrentDifficulty;
        Plugin.Logger.LogInfo("Difficulty pref: " + diff);
        difficultyScaleMult *= 1.34f * ((diff + 2) / 3);
        Plugin.Logger.LogInfo("After: " + difficultyScaleMult);
        // Easy mode = 0,6666666666666667 = 0,8933333333333334 per floor.
        // Hard mode = 1 = 1.34 per floor.
        floor++;
        GambleItemRNG = new System.Random(Plugin.GameSeed.GetHashCode() * (floor + 2));
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
                    new BossEntry(AssetsManager.funnyPowerIntroSpawn, 20),
                }));

                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Mindflayer)[0].gameObject, 0),
                }));
                break;
            case 3:
            case 4:
            case 5:
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
            case 6:
            case 7:
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
                    new List<BossEntry> { new BossEntry(AssetsManager.funnyPowerIntroSpawn, 0) }, // Wave 1
                    new List<BossEntry> { new BossEntry(AssetsManager.funnyPowerIntroSpawn, 0), 
                        new BossEntry(AssetsManager.funnyPowerIntroSpawn, 0) } // Wave 2
                }));
                break;
            default:
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MinosPrime)[0].gameObject, 0, 25, 8),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.SisyphusPrime)[0].gameObject, 130, 20, 8),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MirrorReaper)[0].gameObject, 0, 23, 8),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.BigJohnator)[0].gameObject, 100, 12, 8),
                }));
                options.Add(new BossPick(new List<BossEntry>()
                {
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MaliciousFace)[0].gameObject, 60, 45, 8),
                    new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MaliciousFace)[0].gameObject, 60, 45, 8),
                }));
                break;
        }

        if (options.Count == 0)
            return null;

        return options[UnityEngine.Random.Range(0, options.Count)];
    }

    public bool CanSpawn(EnemyType enemy)
    {
        switch (enemy)
        {
            case EnemyType.Filth:
            case EnemyType.Stray:
            case EnemyType.Schism:
            case EnemyType.Drone:
            case EnemyType.Soldier:
                return true;
            case EnemyType.Streetcleaner:
                return Plugin.SelectedChar.GetType() != typeof(Ultrarogue.Characters.Streetcleaner);
            case EnemyType.MaliciousFace:
            case EnemyType.Cerberus:
            case EnemyType.Swordsmachine:
            case EnemyType.Mindflayer:
                return floor >= 3;
            case EnemyType.Power:
            case EnemyType.Ferryman:
            case EnemyType.Sisyphus:
                return floor >= 5;
            case EnemyType.Gabriel:
                return floor >= 6;
            case EnemyType.V2:
            case EnemyType.V2Second:
            case EnemyType.HideousMass:
                return false;
            default:
                return floor >= 9;

        }
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
                return 20;
            case EnemyType.Swordsmachine:
                return 25;
            case EnemyType.Drone:
                return 4;
            case EnemyType.HideousMass:
                return 48;
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
                return 4;
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
                return 14;
            case EnemyType.MirrorReaper:
                return 125;
            case EnemyType.Mandalore:
                return 145;
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
    public List<List<BossEntry>> waves = new List<List<BossEntry>>();
    public Action<EnemyIdentifier> onSpawn;

    public BossPick(List<List<BossEntry>> waves, Action<EnemyIdentifier> onSpawn = null)
    {
        this.waves = waves;
        this.onSpawn = onSpawn;
    }

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
    public float healthPerFloorMod;
    public int startFloor;


    public BossEntry(GameObject prefab, float healthMod = 0, float healthPerFloorMod = 0, int startFloor = 0)
    {
        this.prefab = prefab;
        this.healthMod = healthMod;
        this.healthPerFloorMod = healthPerFloorMod;
        this.startFloor = startFloor;
    }
}
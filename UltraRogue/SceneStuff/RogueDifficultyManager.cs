using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Ultrarogue;
using Ultrarogue.Characters;
using Ultrarogue.Items;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;


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
    public static System.Random BloodRNG;
    public static System.Random ChestRNG;
    bool keepOpen;
    float doubleTap;
    void Awake()
    {

        ItemRNG = new System.Random(Plugin.GameSeed.GetHashCode());
        GambleItemRNG = new System.Random(Plugin.GameSeed.GetHashCode() * 2); // Considering you can infinitely gamble, it might cause rng issues.
        RoomRNG = new System.Random(Plugin.GameSeed.GetHashCode() / 2);
        BossRNG = new System.Random(Plugin.GameSeed.GetHashCode() ^ 2);
        BloodRNG = new System.Random((int)Mathf.Log(Plugin.GameSeed.GetHashCode(), 2));
        ChestRNG = new System.Random((int)Mathf.PingPong(Plugin.GameSeed.GetHashCode(), 2)); // Im using random calculations
        Instance = this;
        Difficulty = Plugin.CurrentDifficulty;

        itemCounts = new Dictionary<string, int>();
        itemUIObjects = new Dictionary<string, GameObject>();

        itemsUI = GameObject.Find("Items").transform.Find("Panel").gameObject;
        itemsUI.SetActive(true);
        itemParent = itemsUI.GetComponent<GridLayoutGroup>();

        Transform stats = GameObject.Find("Items").transform.Find("Stats");

        statSpeedText = stats.Find("StatSpeed/Stt").GetComponent<TMP_Text>();
        statDamageText = stats.Find("StatDamage/Stt").GetComponent<TMP_Text>();
        statAtkSpeedText = stats.Find("StatAtkSpeed/Stt").GetComponent<TMP_Text>();

        if(Plugin.SelectedChar.GetType() == typeof(Filth))
        {
            stats.Find("StatAtkSpeed").gameObject.SetActive(false);
        }

        statCooldownText = stats.Find("StatCooldown/Stt").GetComponent<TMP_Text>();
        statFloorText = stats.Find("StatFloor/Stt").GetComponent<TMP_Text>();

        // Show the starting items
        foreach (var item in Plugin.items)
        {
            AddItem(item.Key);
        }

        GameObject tooltipHost = new GameObject("ItemTooltip");
        tooltipHost.transform.SetParent(GameObject.Find("Items").transform.root, false); // top of canvas
        tooltipHost.AddComponent<ItemTooltip>();

       
    }
    void Start()
    {
        activeHolder = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/ActiveHolder.prefab").WaitForCompletion();


        Transform ChargeParent = NewMovement.Instance.transform.Find("Main Camera/HUD Camera/HUD/GunCanvas/StatsPanel/Filler/Panel (3)");

        charge = Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/ActiveCharge.prefab").WaitForCompletion(), ChargeParent);
        charge.GetComponent<Slider>().value = 0;
    }

    GameObject charge;

    GameObject activeHolder;
    GameObject gun;

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
        statFloorText.text = $"{floor}";
    }
    // Paste this method into RogueDifficultyManager, alongside AddItem().

    public void RemoveItem(BaseItem item, int stacksRemoved = 1)
    {
        if (item == null) return;

        string itemKey = item.ItemName;

        if (!itemCounts.ContainsKey(itemKey)) return;

        itemCounts[itemKey] -= stacksRemoved;

        if (itemCounts[itemKey] <= 0)
        {
            // Remove the UI object entirely
            itemCounts.Remove(itemKey);

            if (itemUIObjects.TryGetValue(itemKey, out GameObject uiObj) && uiObj != null)
                Destroy(uiObj);

            itemUIObjects.Remove(itemKey);
        }
        else
        {
            // Update the stack count label
            if (itemUIObjects.TryGetValue(itemKey, out GameObject uiObj) && uiObj != null)
            {
                TMP_Text countLabel = uiObj.GetComponentInChildren<TMP_Text>();
                if (countLabel != null)
                {
                    int remaining = itemCounts[itemKey];
                    countLabel.text = remaining > 1 ? $"(x{remaining})" : "";
                }
            }
        }
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

            if(item.materialOverride != null)
                icon.material = item.materialOverride;

            ItemHoverHandler hover = container.AddComponent<ItemHoverHandler>();
            hover.Item = item;
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
    private TMP_Text statFloorText;

    void Update()
    {
        if (gun == null)
        {
            if (activeHolder != null)
            {
                gun = Plugin.MakeGun(5, activeHolder);
                gun.SetActive(false);
                Plugin.holder = gun.GetComponent<ActiveHolder>();

                Plugin.holder.chargeUI = charge.GetComponent<Slider>();
            }

        }
        Gold = Mathf.Clamp(Gold, 0, 99);
         
        if (!this.keepOpen)
        {
            if (MonoSingleton<InputManager>.Instance.InputSource.Stats.WasPerformedThisFrame)
            {
                if (!this.keepOpen)
                {
                    if (this.doubleTap > 0f)
                    {
                        this.keepOpen = true;
                    }
                    else
                    {
                        this.doubleTap = 0.5f;
                    }
                }
                itemsUI.SetActive(true);
                statDamageText.transform.parent.parent.gameObject.SetActive(true);
                if(MinimapUI.Instance != null)
                    MinimapUI.Instance.minimapPanel.gameObject.SetActive(true);
            }
            else if (MonoSingleton<InputManager>.Instance.InputSource.Stats.WasCanceledThisFrame)
            {
                itemsUI.SetActive(false);
                statDamageText.transform.parent.parent.gameObject.SetActive(false);
                if (MinimapUI.Instance != null)
                    MinimapUI.Instance.minimapPanel.gameObject.SetActive(false);
            }
        }
        else if (MonoSingleton<InputManager>.Instance.InputSource.Stats.WasPerformedThisFrame)
        {
            this.keepOpen = false;
            itemsUI.SetActive(false);
            statDamageText.transform.parent.parent.gameObject.SetActive(false);
            if (MinimapUI.Instance != null)
                MinimapUI.Instance.minimapPanel.gameObject.SetActive(false);
        }
        if (this.doubleTap > 0f)
        {
            this.doubleTap = Mathf.MoveTowards(this.doubleTap, 0f, Time.deltaTime);
        }

        Difficulty += (Time.deltaTime / 180) * difficultyScaleMult;
        goldText.text = "Gold: " + Gold;
        keyText.text = "Keys: " + Keys;
        UpdateStatsUI();
    }

    public void MoveStage()
    {
        int diff = Plugin.CurrentDifficulty;
        Difficulty *= (1f + (0.1f * diff));
        Plugin.Logger.LogInfo("Before: " + difficultyScaleMult);

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
            case EnemyType.Soldier:
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
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Cerberus)[0].gameObject, healthAddition: 8),
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Cerberus)[0].gameObject, healthAddition: 8),
            }));

                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MaliciousFace)[0].gameObject,
                    healthMod: 42),
            }));

                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.VeryCancerousRodent)[0].gameObject,
                    healthAddition: 18),
            }));

                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Swordsmachine)[0].gameObject,
                    healthAddition: 20),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.funnyPowerIntroSpawn, healthMod: 40),
            }));

                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Mindflayer)[0].gameObject,
                    healthAddition: 28),
            }));
                break;

            case 3:
            case 4:
            case 5:
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.V2)[0].gameObject,
                    healthAddition: 30),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Gabriel)[0].gameObject,
                    healthMod: 65),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Ferryman)[0].gameObject,
                    healthMod: 65),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Sisyphus)[0].gameObject,
                    healthMod: 70),
            }));
                break;

            case 6:
            case 7:
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Ferryman)[0].gameObject,
                    healthAddition: 30),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Minotaur)[0].gameObject,
                    healthAddition: 25),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.GabrielSecond)[0].gameObject,
                    healthAddition: 35),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.Mandalore)[0].gameObject,
                    healthAddition: 30),
            }));
                Enemy A = null;
                Enemy T = null;
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.Agony, healthAddition: 45),
                new BossEntry(AssetsManager.Tundra, healthAddition: 45),
            }, (eid) =>
            {
                if (A == null) { A = Room.FindEnemyComponent(eid.gameObject); return; }
                if (T == null) { T = Room.FindEnemyComponent(eid.gameObject); }
                A.symbiote = T;
                T.symbiote = A;
            }));
                options.Add(new BossPick(new List<List<BossEntry>>()
            {
                new List<BossEntry> {
                    new BossEntry(AssetsManager.funnyPowerIntroSpawn)
                },
                new List<BossEntry> {
                    new BossEntry(AssetsManager.funnyPowerIntroSpawn, healthAddition: 10),
                    new BossEntry(AssetsManager.funnyPowerIntroSpawn, healthAddition: 5)
                }
            }));
                break;

            case 8:
            case 9:
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MinosPrime)[0].gameObject,
                    healthMod: 40, healthPerFloorMod: 12, startFloor: 8),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MirrorReaper)[0].gameObject,
                    healthPerFloorMod: 12, startFloor: 8),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MaliciousFace)[0].gameObject,
                    healthMod: 80, healthPerFloorMod: 50, startFloor: 8,
                    radianceBuffs: 2, radianceBuffsPerFloor: 0.5f),
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MaliciousFace)[0].gameObject,
                    healthMod: 80, healthPerFloorMod: 50, startFloor: 8,
                    radianceBuffs: 2, radianceBuffsPerFloor: 0.5f),
            }));
                break;

            default:
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MinosPrime)[0].gameObject,
                    healthMod: 55, healthPerFloorMod: 18, startFloor: 8,
                    radianceBuffs: 0, radianceBuffsPerFloor: 0.35f),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.SisyphusPrime)[0].gameObject,
                    healthMod: 170, healthPerFloorMod: 28, startFloor: 8,
                    radianceBuffs: 0, radianceBuffsPerFloor: 0.35f),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MirrorReaper)[0].gameObject,
                    healthAddition: 45, healthPerFloorMod: 18, startFloor: 8,
                    radianceBuffs: 0, radianceBuffsPerFloor: 1f),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.BigJohnator)[0].gameObject,
                    healthMod: 130, healthPerFloorMod: 18, startFloor: 8,
                    radianceBuffs: 0, radianceBuffsPerFloor: 0.5f),
            }));
                options.Add(new BossPick(new List<BossEntry>()
            {
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MaliciousFace)[0].gameObject,
                    healthMod: 95, healthPerFloorMod: 60, startFloor: 8,
                    radianceBuffs: 0, radianceBuffsPerFloor: 1f),
                new BossEntry(AssetsManager.GetEnemiesOfType(EnemyType.MaliciousFace)[0].gameObject,
                    healthMod: 95, healthPerFloorMod: 60, startFloor: 8,
                    radianceBuffs: 0, radianceBuffsPerFloor: 1f),
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
            case EnemyType.Mannequin:
            case EnemyType.Soldier:
                return true;
            case EnemyType.Streetcleaner:
                return Plugin.SelectedChar.GetType() != typeof(Ultrarogue.Characters.Streetcleaner);
            case EnemyType.MaliciousFace:
            case EnemyType.Cerberus:
            case EnemyType.Swordsmachine:
            case EnemyType.Mindflayer:
            case EnemyType.Stalker:
                return floor >= 3;
            case EnemyType.Idol:
            case EnemyType.Ferryman:
                return floor >= 5;
            case EnemyType.Deathcatcher:
                return floor >= 5 && floor <= 12;
            case EnemyType.Gabriel:
            case EnemyType.Power:
                return floor >= 6;
            case EnemyType.V2:
            case EnemyType.V2Second:
            case EnemyType.HideousMass:
            case EnemyType.Sisyphus:
                return false;
            case EnemyType.MinosPrime:
                return floor >= 8;
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
    public float healthAddition;
    public float healthPerFloorMod;
    public int startFloor;
    public int radianceBuffs;
    public float radianceBuffsPerFloor; // fractional — floors past startFloor accumulate this

    public BossEntry(GameObject prefab, float healthMod = 0, float healthAddition = 0, float healthPerFloorMod = 0,
                     int startFloor = 0, int radianceBuffs = 0, float radianceBuffsPerFloor = 0f)
    {
        this.prefab = prefab;
        this.healthMod = healthMod;
        this.healthAddition = healthAddition;
        this.healthPerFloorMod = healthPerFloorMod;
        this.startFloor = startFloor;
        this.radianceBuffs = radianceBuffs;
        this.radianceBuffsPerFloor = radianceBuffsPerFloor;
    }
}

public class ItemTooltip : MonoBehaviour
{
    public static ItemTooltip Instance { get; private set; }

    private GameObject panel;
    private TMP_Text nameText;
    private TMP_Text descText;
    private RectTransform rectTransform;

    void Awake()
    {
        Instance = this;

        // Build the tooltip panel dynamically
        panel = new GameObject("TooltipPanel");
        panel.transform.SetParent(transform, false);

        // Background image
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        // Layout
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 4f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0f, 1f); // anchor top-left to cursor

        // Item name label
        GameObject nameObj = new GameObject("TooltipName");
        nameObj.transform.SetParent(panel.transform, false);
        nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 16;
        nameText.fontStyle = TMPro.FontStyles.Bold;
        nameText.color = Color.yellow;

        // Item description label
        GameObject descObj = new GameObject("TooltipDesc");
        descObj.transform.SetParent(panel.transform, false);
        descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.fontSize = 13;
        descText.color = Color.white;

        // Clamp width so long descriptions wrap nicely
        LayoutElement le = descObj.AddComponent<LayoutElement>();
        le.preferredWidth = 220f;

        Hide();
    }

    public void Show(string itemName, string description, Vector2 screenPos)
    {
        nameText.text = itemName;
        descText.text = description;
        panel.SetActive(true);
        UpdatePosition(screenPos);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    void Update()
    {
        if (panel.activeSelf)
            UpdatePosition(Input.mousePosition);
    }

    void UpdatePosition(Vector2 screenPos)
    {
        // Convert screen pos to canvas local pos
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent.GetComponent<RectTransform>(), // assumes tooltip is on the HUD canvas
            screenPos,
            null,
            out Vector2 localPoint
        );

        // Nudge so it doesn't sit right under the cursor
        localPoint += new Vector2(12f, -8f);

        rectTransform.anchoredPosition = localPoint;
    }
}

public class ItemHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public BaseItem Item { get; set; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Item == null || ItemTooltip.Instance == null) return;

        string desc = string.IsNullOrEmpty(Item.itemDescription)
            ? "No description available."
            : Item.itemDescription;

        ItemTooltip.Instance.Show(Item.ItemName, desc, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.Instance?.Hide();
    }
}
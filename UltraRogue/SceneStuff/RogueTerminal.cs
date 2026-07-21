using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Ultrarogue;
using Ultrarogue.Characters;
using Ultrarogue.Items;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputRemoting;

public class RogueTerminal : MonoBehaviour
{
    public GameObject ItemPrefab;
    public TMP_Text CharacterInfo;
    public Image ItemInfoImage;
    public TMP_Text ItemInfoName;
    public TMP_Text ItemInfoNameName;
    public TMP_Text ItemInfoInfo;
    public TMP_Text TipDay;

    private Dictionary<BaseItem, GameObject> itemButtons = new Dictionary<BaseItem, GameObject>();

    void Awake()
    {
        FillItems();
        CharacterInfo.text = GetCharacterInfo();

        string randomMessage = SceneLoader.messages[Random.Range(0, SceneLoader.messages.Length)];
        TipDay.text = randomMessage;
    }

    string GetCharacterInfo()
    {
        BaseCharacter c = Plugin.SelectedChar;
        if (c == null) return "NO CLASS SELECTED WHAT?";

        StringBuilder sb = new StringBuilder();

        sb.Append($"NAME: {c.Name}\n\n");
        sb.Append($"DESCRIPTION: {c.Description}\n\n");
        sb.Append($"DETAILS: {c.Detail}\n\n");
        return sb.ToString();
    }

    void FillItem(BaseItem item)
    {
        GameObject itemButC = Instantiate(ItemPrefab);
        itemButC.transform.SetParent(ItemPrefab.transform.parent, false);

        itemButC.transform.Find("Background/Enemy").GetComponent<Image>().sprite = item.ItemIcon;
        itemButC.transform.Find("Background").GetComponent<Image>().color = rarityToColor(item.Rarity);

        itemButtons[item] = itemButC;

        RefreshItem(item);
    }

    public void RefreshItem(BaseItem item)
    {
        if (!itemButtons.TryGetValue(item, out GameObject button))
            return;

        Image icon = button.transform.Find("Background/Enemy").GetComponent<Image>();
        Button btn = button.GetComponent<Button>();

        btn.onClick.RemoveAllListeners();

        if (Plugin.HasGottenItem(item))
        {
            icon.color = Color.white;
            btn.onClick.AddListener(() => FillInfo(item));
        }
        else
        {
            icon.color = Color.black;
            btn.onClick.AddListener(() => FillInfoButEvil(item));
        }
    }
    Color rarityToColor(Rarity rar)
    {
        switch (rar)
        {
            case Rarity.Alchemy:
                return new Color(0.6f, 0.2f, 0.8f);
            case Rarity.Common:
                return Color.white;
            case Rarity.Uncommon:
                return Color.green;
            case Rarity.Legendary:
                return Color.yellow;
            default:
                return Color.white;
        }
    }
    void FillInfo(BaseItem item)
    {
        ItemInfoImage.sprite = item.ItemIcon;
        ItemInfoImage.color = Color.white;
        ItemInfoName.text = item.ItemName;
        ItemInfoNameName.text = item.ItemName;

        StringBuilder sb = new StringBuilder();

        sb.Append($"NAME: {item.ItemName}\n\n");
        sb.Append($"DESCRIPTION: {item.itemDescription}\n\n");
        //sb.Append($"LORE:\n{item.itemDescription}"); // Ill leave the lore for later

        ItemInfoInfo.text = sb.ToString();
    }

    void FillInfoButEvil(BaseItem item)
    {
        ItemInfoImage.sprite = item.ItemIcon;
        ItemInfoImage.color = Color.black;
        ItemInfoName.text = item.ItemName;
        ItemInfoNameName.text = item.ItemName;

        StringBuilder sb = new StringBuilder();

        sb.Append($"<color=red>NO DATA AVAILABLE");
        //sb.Append($"LORE:\n{item.itemDescription}"); // Ill leave the lore for later

        ItemInfoInfo.text = sb.ToString();
    }

    void FillItems()
    {
        var rarityOrder = new Dictionary<Rarity, int>
        {
            { Rarity.Common, 0 },
            { Rarity.Uncommon, 1 },
            { Rarity.Legendary, 2 },
            { Rarity.Alchemy, 3 }
        };

        foreach (var item in Plugin.possibleItems.OrderBy(i => rarityOrder[i.Rarity]))
        {
            FillItem(item);
        }

        Destroy(ItemPrefab);
    }
}

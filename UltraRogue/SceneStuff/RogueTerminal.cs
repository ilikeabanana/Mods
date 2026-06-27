using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Ultrarogue;
using Ultrarogue.Characters;
using Ultrarogue.Items;
using UnityEngine;
using UnityEngine.UI;

public class RogueTerminal : MonoBehaviour
{
    public GameObject ItemPrefab;
    public TMP_Text CharacterInfo;
    public Image ItemInfoImage;
    public TMP_Text ItemInfoName;
    public TMP_Text ItemInfoNameName;
    public TMP_Text ItemInfoInfo;

    void Awake()
    {
        FillItems();
        CharacterInfo.text = GetCharacterInfo();
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
        itemButC.transform.Find("Background/Enemy").GetComponent<Image>().sprite = item.ItemIcon;

        //if(item.materialOverride != null)
        //    itemButC.transform.Find("Background/Enemy").GetComponent<Image>().material = item.materialOverride;
        itemButC.transform.Find("Background").GetComponent<Image>().color = rarityToColor(item.Rarity);
        itemButC.transform.SetParent(ItemPrefab.transform.parent, false);

        itemButC.GetComponent<Button>().onClick.AddListener(() => FillInfo(item));
    }
    Color rarityToColor(Rarity rar)
    {
        switch (rar)
        {
            case Rarity.Alchemy:
                return Color.white;
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
        ItemInfoName.text = item.ItemName;
        ItemInfoNameName.text = item.ItemName;

        StringBuilder sb = new StringBuilder();

        sb.Append($"NAME: {item.ItemName}\n\n");
        sb.Append($"DESCRIPTION: {item.itemDescription}\n\n");
        //sb.Append($"LORE:\n{item.itemDescription}"); // Ill leave the lore for later

        ItemInfoInfo.text = sb.ToString();
    }

    void FillItems()
    {
        foreach (var item in Plugin.possibleItems)
        {
            FillItem(item);
        }

        Destroy(ItemPrefab);
    }
}

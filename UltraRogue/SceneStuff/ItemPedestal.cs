using System;
using System.Collections.Generic;
using System.Text;
using Ultrarogue;
using Ultrarogue.Items;
using UnityEngine;

public class ItemPedestal : MonoBehaviour
{
    BaseItem chosenItem;
    void Awake()
    {
        chosenItem = Plugin.GiveRandomItem();
    }
    bool collected = false;
    public void Collect()
    {
        if (collected) return;
        collected = true;
        Plugin.GiveItem(chosenItem);
    }

}

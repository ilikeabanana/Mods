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
        ItemPickup.CreatePickup(chosenItem, transform.position);
    }

}
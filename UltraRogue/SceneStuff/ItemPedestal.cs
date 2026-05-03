using System.Collections.Generic;
using System.Text;
using Ultrarogue;
using Ultrarogue.Items;
using Ultrarogue.SceneStuff;
using UnityEngine;

public class ItemPedestal : MonoBehaviour
{
    BaseItem chosenItem;

    void Start()
    {
        if(Random.value <= 0.5f)
        {
            chosenItem = Plugin.GiveRandomItem();
            ItemPickup.CreatePickup(chosenItem, transform);
        }
        else
        {
            WeaponPickupRogue.CreatePickup(transform);
        }
        
    }

}
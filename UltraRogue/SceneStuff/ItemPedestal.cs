using System.Collections.Generic;
using System.Text;
using Ultrarogue;
using Ultrarogue.Items;
using Ultrarogue.SceneStuff;
using UnityEngine;

public class ItemPedestal : MonoBehaviour
{
    public UltrakillEvent onPickup;
    public float offset = 3;
    public bool forceItem;
    public bool forceWeapon;
    public DroptableType tableType;
    BaseItem chosenItem;

    void Start()
    {
        if (forceItem)
        {
            SpawnItem();
            return;
        }
        else if(forceWeapon)
        {
            SpawnWeapon();
            return;
        }

        if((float)RogueDifficultyManager.ItemRNG.NextDouble() <= 0.5f)
        {
            SpawnItem();
        }
        else
        {
            SpawnWeapon();
        }
        
    }

    public void SpawnItem()
    {
        chosenItem = Plugin.GiveRandomItem(table: tableType);
        ItemPickup.CreatePickupConditional(chosenItem, transform, () =>
        {
            onPickup.Invoke();
            return true;
        }, offset);
    }

    public void SpawnWeapon()
    {
        WeaponPickupRogue.CreatePickupConditional(transform, () =>
        {
            onPickup.Invoke();
            return true;
        }, offset);
    }

}
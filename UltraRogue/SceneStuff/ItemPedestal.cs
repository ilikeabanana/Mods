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

    void Update()
    {
        if (collected) return;

        if (NewMovement.Instance == null) return;

        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f)
        {
            Collect();
        }
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;

        HudMessageReceiver.Instance?.SendHudMessage(chosenItem.ToString());
        Plugin.GiveItem(chosenItem);
        Destroy(gameObject);
    }
}
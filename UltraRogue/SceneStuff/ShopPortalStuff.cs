using System;
using System.Collections.Generic;
using System.Text;
using ULTRAKILL.Portal;
using Ultrarogue;
using UnityEngine;

public class ShopPortalStuff : MonoBehaviour
{
    [SerializeField] Door DoorToRunSimpleOpenOverrideOn;
    [SerializeField] string PortalExitFinder = "ShopPortalExit";
    [Tooltip("If null, will just do GetComponent to get it")]
    [SerializeField] Portal PortalScript;

    bool isOpen = false;

    void Awake()
    {
        if (PortalScript == null) PortalScript = GetComponent<Portal>();


        if (GameObject.Find(PortalExitFinder))
        {
            PortalScript.exit = GameObject.Find(PortalExitFinder).transform;
        }

        if (Plugin.GetItemCount("Ration Card") > 0)
            Open();


    }

    void Open()
    {
        if (!isOpen)
        {
            isOpen = true;
            DoorToRunSimpleOpenOverrideOn.SimpleOpenOverride();
        }
    }

    void Update()
    {
        if (Plugin.GetItemCount("Ration Card") > 0)
            Open();
    }

}

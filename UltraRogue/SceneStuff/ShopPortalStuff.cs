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
    [SerializeField] string PortalExitSlot = "ShopHolders";
    [Tooltip("If null, will just do GetComponent to get it")]
    [SerializeField] Portal PortalScript;
    GameObject Exit;
    private Transform slot;
    GameObject[] ForeignExit = { null, null };
    bool isOpen = false;

    void Awake()
    {

        isOpen = false;
        if (PortalScript == null) PortalScript = GetComponent<Portal>();


        if (GameObject.Find(PortalExitFinder))
        {
            Exit = Instantiate(GameObject.Find(PortalExitFinder));
            slot = GameObject.Find(PortalExitSlot).transform;
            Exit.transform.parent = slot;
            Exit.transform.localPosition = Vector3.zero;
            PortalScript.exit = Exit.transform;
        }

    }

    public void Fake()
    {
        Exit.transform.localPosition = Vector3.zero;
        if (ForeignExit[0] == null)
        {
            int i = 0;
            foreach (Transform child in slot)
            {
                {
                    if (child != Exit.transform)
                    {
                        ForeignExit[i++] = child.gameObject;
                    }
                }
            }
        }

        foreach (GameObject go in ForeignExit)
{
    if (go == null) continue; // add this
    go.transform.localPosition = new Vector3(0, -50, 0);
}

    }
    void Open()
    {
        if (!DoorToRunSimpleOpenOverrideOn.open)
        {
            DoorToRunSimpleOpenOverrideOn.SimpleOpenOverride();
        }
    }

    void Update()
    {
        if (Plugin.GetItemCount("Ration Card") > 0)
            Open();
    }

}
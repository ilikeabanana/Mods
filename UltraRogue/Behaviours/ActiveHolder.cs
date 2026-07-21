using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Text;
using Ultrarogue.Items;
using UnityEngine;
using UnityEngine.UI;


public class ActiveHolder : MonoBehaviour
{
    Dictionary<ActiveItem, int> charges = new Dictionary<ActiveItem, int>();
    public Slider chargeUI;
    public void Charge()
    {
        int charge = charges[CurrentActive];
        if (charge >= CurrentActive.ChargeRequired) return;
        charges[CurrentActive]++;
        if (chargeUI != null)
        {
            chargeUI.value = charge + 1;
        }
    }

    ActiveItem _current;

    public ActiveItem CurrentActive
    {
        get
        {
            return _current;
        }
        set
        {
            if (!charges.ContainsKey(value))
            {
                charges.Add(value, value.ChargeRequired);
            }
            _current = value;

            if(chargeUI != null)
            {
                chargeUI.minValue = 0;
                chargeUI.maxValue = _current.ChargeRequired;
            }

            if(Holder != null)
            {
                Material mat = new Material(value.materialOverride ? value.materialOverride : AssetsManager.weaponMat);
                mat.mainTexture = value.ItemTexture;
                value.OnMaterialApply(mat);
                Holder.material = mat;
            }
        }
    }
    public Renderer Holder;
    public GameObject UseableThing;

    Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();

    }

    void Update()
    {
        
        if (anim != null)
            anim.SetBool("Holding", CurrentActive != null);
        if (CurrentActive == null) return;
        int charge = charges[CurrentActive];
        if (CurrentActive.ChargeRequired != charge)
        {
            UseableThing.SetActive(false);
            return;
        }

        UseableThing.SetActive(true);

        if (InputManager.Instance.InputSource.Fire1.WasPerformedThisFrame && CurrentActive != null)
        {
            
            charges[CurrentActive] = 0;
            anim.SetTrigger("Punch");
            CurrentActive?.OnUse();
            if (chargeUI != null)
            {
                chargeUI.value = 0;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Ultrarogue.Items;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Ultrarogue.SceneStuff
{
    [ConfigureSingleton(SingletonFlags.DestroyDuplicates)]
    public class ActiveManager : MonoSingleton<ActiveManager>
    {
        public Slider ChargeMeter;
        public Image CurrentActiveItemImage;

        Dictionary<ActiveItem, int> charges = new Dictionary<ActiveItem, int>();
        public void Charge()
        {
            int charge = charges[CurrentActive];
            if (charge >= CurrentActive.ChargeRequired) return;
            charges[CurrentActive]++;
            if (ChargeMeter != null)
            {
                ChargeMeter.value = charge + 1;
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

                if (ChargeMeter != null)
                {
                    ChargeMeter.minValue = 0;
                    ChargeMeter.maxValue = _current.ChargeRequired;
                    ChargeMeter.value = charges[value];
                }

                if(CurrentActiveItemImage != null)
                {
                    CurrentActiveItemImage.sprite = value.ItemIcon;
                }

                ChargeMeter.gameObject.SetActive(true);
                CurrentActiveItemImage.gameObject.SetActive(true);
            }
        }
        void Start()
        {
            CurrentActiveItemImage.GetComponentInChildren<TMP_Text>().text =
                AssetsManager.UseActiveKey.GetBindingDisplayString();
        }
        void Update()
        {

            if (CurrentActive == null) return;
            int charge = charges[CurrentActive];
            if (CurrentActive.ChargeRequired != charge)
            {
                CurrentActiveItemImage.color = Color.grey;
                return;
            }
            CurrentActiveItemImage.color = Color.white;
            if (CurrentActive == null) return;
            if ((AssetsManager.UseActiveKey.WasPerformedThisFrame() || (CurrentActive.CanAutoActivate() && SettingsManager.AutoActive)) && GunControl.Instance.activated)
            {

                charges[CurrentActive] = 0;
                CurrentActive?.OnUse();
                if (ChargeMeter != null)
                {
                    ChargeMeter.value = 0;
                }
            }
        }
    }
}

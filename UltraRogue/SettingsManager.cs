using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ultrarogue
{
    public class SettingsManager
    {
        public static bool DestroyChestsOnOpen
        {
            get
            {
                return PlayerPrefs.GetInt("DESTROY_CHEST_ON_OPEN", 0) == 1;
            }
            set
            {
                PlayerPrefs.SetInt("DESTROY_CHEST_ON_OPEN", value ? 1 : 0);
            }
        }
        public static bool CoinPickups
        {
            get
            {
                return PlayerPrefs.GetInt("COIN_PICK_UPS", 0) == 1;
            }
            set
            {
                PlayerPrefs.SetInt("COIN_PICK_UPS", value ? 1 : 0);
            }
        }
        public static bool AutoActive
        {
            get
            {
                return PlayerPrefs.GetInt("AUTOACTIVATE_ACTIVES", 0) == 1;
            }
            set
            {
                PlayerPrefs.SetInt("AUTOACTIVATE_ACTIVES", value ? 1 : 0);
            }
        }
    }
}

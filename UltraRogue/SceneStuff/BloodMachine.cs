using System;
using System.Collections.Generic;
using System.Text;
using Ultrarogue;
using UnityEngine;

public class BloodMachine : MonoBehaviour
{
    
    public void BLOOD()
    {
        int damage = Mathf.RoundToInt(Plugin.MaxHealth * 0.10f);
        if (Plugin.SelectedChar.HasPassive(Ultrarogue.Characters.Passive.HealFromBlood))
        {
            damage = Mathf.RoundToInt(Plugin.MaxHealth * 0.55f);
        }

        if(NewMovement.Instance.hp - damage <= 0)
        {
            HudMessageReceiver.Instance.SendHudMessage("<color=red>NOT ENOUGH BLOOD</color>");
            return;
        }

        NewMovement.Instance.GetHurt(damage, false, ignoreInvincibility: true);
        RogueDifficultyManager.Instance.Gold++;
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Ultrarogue;
using Ultrarogue.Characters;
using UnityEngine;
using UnityEngine.Events;

public class BloodMachine : MonoBehaviour
{
    public GameObject BloodObject;
    public int MinFullThreshold = 3;
    public int MaxFullThreshold = 10;
    public int FullThreshold = 10;
    public Vector3 localPositionPartial;
    public Vector3 localPositionFull;
    public UnityEvent onUse;
    public UnityEvent onFill;

    int bloodDonated;

    public static bool BloodMachined;

    void Awake()
    {
        FullThreshold = RogueDifficultyManager.BloodRNG.Next(MinFullThreshold, MaxFullThreshold);
    }


    public void BLOOD()
    {
        if (Plugin.SelectedChar.HasPassive(Ultrarogue.Characters.Passive.Greedy))
        {
            HudMessageReceiver.Instance.SendHudMessage("<color=red>INCOMPATIBLE BLOOD</color>");
            return;
        }

        Debug.Log($"bloodDonated: {bloodDonated}, FullThreshold: {FullThreshold}");
        if (bloodDonated == FullThreshold) return;
        int damage = 50;

        if (Plugin.SelectedChar.GetType() == typeof(Filth))
        {
            damage = 5;
        }

        if (NewMovement.Instance.hp - damage <= 0)
        {
            HudMessageReceiver.Instance.SendHudMessage("<color=red>NOT ENOUGH BLOOD</color>");
            return;
        }

        

        bloodDonated++;
        onUse.Invoke();

        if(bloodDonated == 1)
        {
            BloodObject.transform.localPosition = localPositionPartial;
        } else if(bloodDonated == FullThreshold)
        {
            BloodObject.transform.localPosition = localPositionFull;
            onFill.Invoke();
            if (Plugin.SelectedChar.HasPassive(Ultrarogue.Characters.Passive.HealFromBlood))
            {
                ItemPickup.CreatePickup(Plugin.GiveRandomItem(RogueDifficultyManager.BloodRNG, DroptableType.CommonOnly), transform, 5);
            }
            else
            {
                ItemPickup.CreatePickup(Plugin.GiveRandomItem(RogueDifficultyManager.BloodRNG, DroptableType.BloodMachine), transform, 5);
            }
            
        }

        if(Plugin.SelectedChar.GetType() != typeof(Filth))
        {
            BloodMachined = true;
            NewMovement.Instance.GetHurt(damage, false, ignoreInvincibility: true);
        }
        else
        {

            for (int i = 0; i < damage; i++) // 5 damage
            {
                BloodMachined = true;
                NewMovement.Instance.GetHurt(1, false, ignoreInvincibility: true);
            }
        }
        
        
        RogueDifficultyManager.Instance.Gold++;
    }
}

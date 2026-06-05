using System;
using System.Collections.Generic;
using System.Text;
using Ultrarogue;
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

    void Awake()
    {
        FullThreshold = RogueDifficultyManager.BloodRNG.Next(MinFullThreshold, MaxFullThreshold);
    }

    public void BLOOD()
    {
        if (bloodDonated == FullThreshold) return;
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
        bloodDonated++;
        onUse.Invoke();

        if(bloodDonated == 1)
        {
            BloodObject.transform.localPosition = localPositionPartial;
        } else if(bloodDonated == FullThreshold)
        {
            BloodObject.transform.localPosition = localPositionFull;
            onFill.Invoke();
        }

        NewMovement.Instance.GetHurt(damage, false, ignoreInvincibility: true);
        RogueDifficultyManager.Instance.Gold++;
    }
}

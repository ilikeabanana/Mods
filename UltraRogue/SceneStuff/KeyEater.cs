using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class KeyEater : MonoBehaviour // Yum
{
    public UnityEvent OnKeyGotten;
    public UnityEvent OnNoKey;

    public float DistanceForActivation = 2f;

    float cooldown = 5;
    float c = 0;

    void Update()
    {
        c -= Time.deltaTime;
        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= DistanceForActivation &&
            c <= 0)
        {
            c = 5;
            if(RogueDifficultyManager.Instance.Keys <= 0)
            {
                OnNoKey?.Invoke();
                HudMessageReceiver.Instance.SendHudMessage("NO KEYS TO GIVE");
                return;
            }
            RogueDifficultyManager.Instance.Gold++;
            RogueDifficultyManager.Instance.Keys--;
            OnKeyGotten?.Invoke();

        }
    }
}

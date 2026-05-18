using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public class MuffleTheMusic : MonoBehaviour
{
    void Awake()
    {
        AudioMuffleZone muffleZone = GetComponent<AudioMuffleZone>();

        muffleZone.muffleTargets = new List<AudioLowPassFilter>()
        {
            MusicManager.Instance.cleanTheme.GetComponent<AudioLowPassFilter>(),
            MusicManager.Instance.battleTheme.GetComponent<AudioLowPassFilter>(),
            MusicManager.Instance.bossTheme.GetComponent<AudioLowPassFilter>(),
        };
    }
}


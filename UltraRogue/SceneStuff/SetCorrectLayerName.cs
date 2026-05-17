using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public class SetCorrectLayerName : MonoBehaviour
{
    
    void Awake()
    {
        GetComponent<LevelNameActivator>().levelName = $"FLOOR {RogueDifficultyManager.Instance.floor}";
    }
}


using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ultrachaos.Randomizers
{
    public class ScaleRandomizer
    {
        public static MinMaxConfig ScaleMult;

        public static void GenerateConfigs()
        {
            ScaleMult = new MinMaxConfig("Scale Mult", 1f, Plugin.ChaosPanel);
        }

        public static void Init()
        {
            Transform[] allTransforms = GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var t in allTransforms)
            {
                if (!Plugin.CanChangeObj(t.gameObject)) continue;
                if (t is RectTransform) continue;
                if (t.GetComponent<Canvas>()) continue;
                if (Plugin.isPlayerChild(t)) continue;
                if (Plugin.isFirstRoomChild(t)) continue;
                if (Plugin.isFinalRoomChild(t)) continue;
                if (Plugin.isEnemyChild(t)) continue;
                if (t.gameObject.GetComponent<PlayerActivator>()) continue;
                t.localScale *= ScaleMult.GetRand;
            }
        }


    }


}

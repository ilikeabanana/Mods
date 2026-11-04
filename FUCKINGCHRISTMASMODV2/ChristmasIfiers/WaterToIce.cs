using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FUCKINGCHRISTMASMOD.ChristmasIfiers
{
    public class WaterToIce
    {
        public static void Init()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private static void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            foreach (var item in GameObject.FindObjectsByType<Water>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            {
                if (item.TryGetComponent<Collider>(out var col))
                {
                    col.isTrigger = false;
                    item.gameObject.tag = "Floor";
                    item.gameObject.layer = 8;
                }

                foreach (var item1 in item.GetComponentsInChildren<Collider>(true))
                {
                    item1.isTrigger = false;
                    item1.gameObject.tag = "Floor";
                    item1.gameObject.layer = 8;
                }

                item.gameObject.AddComponent<CustomGroundProperties>().friction = 0;
            }

        }

        
    }
}

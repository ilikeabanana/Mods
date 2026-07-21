using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Ultrarogue.SceneStuff
{
    public class KeyPickup : MonoBehaviour
    {
        bool pickedUp = false;
        void Update()
        {
            if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f && !pickedUp)
            {
                pickedUp = true;
                RogueDifficultyManager.Instance.Keys++;
                Instantiate(AssetsManager.CoinFlash, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }

        public static void CreatePickup(Transform position)
        {
            if (AssetsManager.KeyPrefab == null)
                AssetsManager.KeyPrefab = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/Key.prefab").WaitForCompletion();
            GameObject pickup = Instantiate(AssetsManager.KeyPrefab);
            pickup.AddComponent<KeyPickup>();
            pickup.transform.position = position.position + Vector3.up;
            pickup.transform.parent = position;
        }
    }
}

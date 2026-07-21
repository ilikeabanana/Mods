using Ultrarogue.Items;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class GoldPickup : MonoBehaviour
{
    bool pickedUp = false;
    float t = 0;
    void Update()
    {
        if(t > 0)
        {
            t -= Time.deltaTime;
            return;
        }
        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f)
        {
            if (pickedUp) return;
            pickedUp = true;
            RogueDifficultyManager.Instance.Gold++;
            Instantiate(AssetsManager.CoinGet, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
    public static GameObject CreatePickup(Transform position, float pickupDelay = 0)
    {
        if(AssetsManager.CoinPrefab == null)
            AssetsManager.CoinPrefab = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/Gold.prefab").WaitForCompletion();
        GameObject pickup = Instantiate(AssetsManager.CoinPrefab);
        GoldPickup g = pickup.AddComponent<GoldPickup>();
        pickup.transform.position = position.position + Vector3.up;
        pickup.transform.parent = position;
        g.t = pickupDelay;
        return pickup;
    }
}

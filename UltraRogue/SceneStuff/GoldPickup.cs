using UnityEngine;

public class GoldPickup : MonoBehaviour
{
    bool pickedUp = false;
    void Update()
    {
        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 0.05f)
        {
            if (pickedUp) return;
            pickedUp = true;
            RogueDifficultyManager.Instance.Gold++;
        }
    }
}

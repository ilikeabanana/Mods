using Ultrarogue;
using UnityEngine;

public class Gambler : MonoBehaviour
{
    public void Activate()
    {
        if (RogueDifficultyManager.Instance.Gold <= 0) return;
        RogueDifficultyManager.Instance.Gold--;
        // LETS GO GAMBLING!!!!
        
        if(Random.value <= 0.35f)
        {
            // YOU WON!!!!!!!!
            ItemPickup.CreatePickup(Plugin.GiveRandomItem(), transform.position);
        }
        else
        {
            // You lost haha

        }
    }
}

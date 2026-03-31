using Ultrarogue;
using UnityEngine;

public class Gambler : MonoBehaviour
{
    const float GAMBLE_COOLDOWN = 1.5f;

    float cooldown = 0f;


    void Update()
    {
        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
            return;
        }

        if (NewMovement.Instance == null) return;

        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f)
        {
            Activate();
            cooldown = GAMBLE_COOLDOWN;
        }
    }

    public void Activate()
    {
        var mgr = RogueDifficultyManager.Instance;
        if (mgr == null) return;

        if (mgr.Gold <= 0)
        {
            HudMessageReceiver.Instance?.SendHudMessage("No gold to gamble!");
            return;
        }

        mgr.Gold--;

        if (Random.value <= 0.35f)
        {
            HudMessageReceiver.Instance?.SendHudMessage("You won!");
            ItemPickup.CreatePickup(Plugin.GiveRandomItem(), transform.position);
        }
        else
        {
            HudMessageReceiver.Instance?.SendHudMessage("You lost... try again?");
        }
    }
}
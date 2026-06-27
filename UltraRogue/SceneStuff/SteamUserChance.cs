using Steamworks;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SteamUserChance : MonoBehaviour
{
    public enum SteamUser
    {
        Banana,
        Linguini,
        Vinny,
        Gronf,
        Tondar,
        Ineo,
        Anyone
    }

    public SteamUser User = SteamUser.Anyone;

    public float chance = 1f;
    public UltrakillEvent UKEvent;


    void Awake()
    {
        if (User == SteamUser.Anyone) Activate();
        if (!SteamClient.IsLoggedOn) return;
        if (UserToUint(User) == SteamClient.SteamId)
        {
            Activate();
        }
    }

    void Activate()
    {
        if(Random.value <= chance)
        {
            UKEvent.Invoke();
        }
    }

    SteamId UserToUint(SteamUser user)
    {
        switch (user)
        {
            case SteamUser.Banana:
                return 76561198300312593L;
            case SteamUser.Linguini:
                return 76561199195414858L;
            case SteamUser.Tondar:
                return 76561199124864632L;
            case SteamUser.Gronf:
                return 76561199354650051L;
            case SteamUser.Ineo:
                return 76561198377209797L;
            case SteamUser.Vinny:
                return 76561199122255407L;
        }

        return 0;
    }

}

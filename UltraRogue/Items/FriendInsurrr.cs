using UnityEngine;

namespace Ultrarogue.Items
{
    public class FriendInsurrr : BaseItem
    {
        public override string ItemName => "Friend Insurr";
        public override string itemDescription => "Gives friend";

        public override void OnGotten(int count, bool firstPickup)
        {
            if (!firstPickup) return;
            GameObject newInsurr = Object.Instantiate(DefaultReferenceManager.Instance.GetEnemyPrefab(EnemyType.Sisyphus),
                NewMovement.Instance.transform.position, Quaternion.identity);

            newInsurr.AddComponent<TeamComponent>().teamId = Team.Player;
        }
    }
}

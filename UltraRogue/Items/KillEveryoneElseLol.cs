using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Items
{
    public class KillEveryoneElseLol : BaseItem
    {
        public override string ItemName => "Kill all";
        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                if (eid.hitter == "deaht") return;

                List<EnemyIdentifier>? allEnemies = EnemyTracker.Instance?.GetCurrentEnemies();

                foreach (var enemy in allEnemies)
                {
                    enemy.hitter = "deaht";
                    enemy.InstaKill();
                }
            });
        }
    }
}

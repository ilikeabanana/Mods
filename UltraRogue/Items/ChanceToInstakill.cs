using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Items
{
    public class ChanceToInstakill : BaseItem
    {
        public override string ItemName => "Chance to Instakill";
        public override Rarity Rarity => Rarity.Uncommon;

        public override void OnStart()
        {
            new HitEffect(ItemName, (eid) =>
            {
                if (Plugin.canExecute(25, eid.hitter))
                {
                    eid.InstaKill();
                }
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Items
{
    public class Explosionsssssss : BaseItem
    {
        public override string ItemName => "Kabooms";
        public override Rarity Rarity => Rarity.Legendary;

        Change change;

        public override void OnStart()
        {
            change = new Change();

            new PlayerChange(change);
        }

        public override void OnUpdate(int count)
        {
            change.addition = 8 * count;
        }

    }
}

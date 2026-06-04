using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Characters
{
    public class Filth : BaseCharacter
    {
        public override string Name => "Filth";
        public override string Description => "Start with NOTHING, have NO PASSIVES, and have 1 HP";

        public override List<AWeapon> StartingWeapons => new List<AWeapon>();
        public override List<Passive> Passives => new List<Passive>();
        public override List<string> StartingItems => new List<string>();

        Change HPChange = new Change(addition: -99);

        PlayerChange change;

        public override void Update(bool selected)
        {
            if (change == null)
                change = new PlayerChange(maxHealth: HPChange);

            if (selected)
            {
                HPChange.addition = -99;
            }
            else
            {
                HPChange.addition = 0;
            }
        }
    }
}

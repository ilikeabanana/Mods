using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Characters
{
    public class Streetcleaner : BaseCharacter
    {
        public override string Name => "Streetcleaner";
        public override string Description => "Start with the fire starter and gasoline. All gasoline will immediately ignite. <color=red>FIRE DOES NOT HURT YOU ANYMORE</color>";
        public override List<AWeapon> StartingWeapons => new List<AWeapon>()
        {
            new AWeapon(Plugin.Weapon.RocketLauncher, Plugin.Variant.Red)
        };
        public override List<string> StartingItems => new List<string>() { "Gasoline" };

        Change HPChange = new Change(addition: 150);

        PlayerChange change;

        public override void Update(bool selected)
        {
            if (change == null)
                change = new PlayerChange(maxHealth: HPChange);

            if (selected)
            {
                HPChange.addition = -45;
            }
            else
            {
                HPChange.addition = 0;
            }
        }
    }
}

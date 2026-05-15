using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Characters
{
    public class V2 : BaseCharacter
    {
        public override string Name => "V2";

        public override string Description => "Start with the piercer, shotgun and knuckle blaster. You <color=red>DONT HEAL FROM BLOOD</color> anymore";
        public override List<AWeapon> StartingWeapons => new List<AWeapon>()
        {
            new AWeapon(Plugin.Weapon.Revolver, Plugin.Variant.Blue, false),
            new AWeapon(Plugin.Weapon.Shotgun, Plugin.Variant.Blue, false),
            new AWeapon(Plugin.Weapon.Arm, Plugin.Variant.Green, false)
        };

        public override List<Passive> Passives => new List<Passive>() { Passive.TripleShot };

        Change HPChange = new Change(addition: 150);

        PlayerChange change;

        public override void Update(bool selected)
        {
            if (change == null)
                change = new PlayerChange(maxHealth: HPChange);

            if (selected)
            {
                HPChange.addition = 150;
            }
            else
            {
                HPChange.addition = 0;
            }
        }
    }
}

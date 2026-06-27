using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Characters
{
    public class V2 : BaseCharacter
    {
        public override string Name => "V2";

        public override string Description => "Start with the piercer, shotgun and knuckle blaster. <color=red>YOUR REVOLVER DOES TRIPLE SHOTS</color>";
        public override List<AWeapon> StartingWeapons => new List<AWeapon>()
        {
            new AWeapon(Plugin.Weapon.Revolver, Plugin.Variant.Blue, false),
            new AWeapon(Plugin.Weapon.Shotgun, Plugin.Variant.Blue, false),
            new AWeapon(Plugin.Weapon.Arm, Plugin.Variant.Green, false)
        };
        public override string Detail => "Start with the piercer, shotgun and knuckle blaster. Using the revolver, holding down your primary fire will allow you to fire " +
            "triple shots. The time between said shots reduces with attackspeed." +
            " You can heal by either gaining healing items OR clearing rooms.";
        public override List<Passive> Passives => new List<Passive>() { Passive.TripleShot };

        Change HPChange = new Change(addition: 300);

        PlayerChange change;

        public override void Update(bool selected)
        {
            if (change == null)
                change = new PlayerChange(maxHealth: HPChange);

            if (selected)
            {
                HPChange.addition = 200;
            }
            else
            {
                HPChange.addition = 0;
            }
        }
    }
}

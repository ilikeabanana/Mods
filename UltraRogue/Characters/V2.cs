using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Characters
{
    public class V2 : BaseCharacter
    {
        public override string Name => "V2";

        public override string Description => "Start with the alternate piercer and knuckle blaster. You <color=red>DONT HEAL FROM BLOOD</color> anymore";
        public override List<AWeapon> StartingWeapons => new List<AWeapon>()
        {
            new AWeapon(Plugin.Weapon.Revolver, Plugin.Variant.Blue, true),
            new AWeapon(Plugin.Weapon.Arm, Plugin.Variant.Green, false)
        };

        Change HPChange = new Change(addition: 150);
        Change DamageChange = new Change(percentage: 35);

        PlayerChange change;

        public override void Update(bool selected)
        {
            if (change == null)
                change = new PlayerChange(maxHealth: HPChange, globalDamageMult: DamageChange);

            if (selected)
            {
                HPChange.addition = 150;
                DamageChange.percentage = 0.35f;
            }
            else
            {
                HPChange.addition = 0;
                DamageChange.percentage = 0;
            }
        }
    }
}

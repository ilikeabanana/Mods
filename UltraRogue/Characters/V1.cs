using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Characters
{
    public class V1 : BaseCharacter
    {
        public override string Name => "V1";
        public override string Description => "Start with the piercer and feedbacker, health items will <color=red>NOT</color> spawn.";
        public override List<AWeapon> StartingWeapons => new List<AWeapon>()
        {
            new AWeapon(Plugin.Weapon.Revolver, Plugin.Variant.Blue),
            new AWeapon(Plugin.Weapon.Arm, Plugin.Variant.Blue),
        };

        public override List<Passive> Passives => new List<Passive>() { Passive.HealFromBlood };
    }
}

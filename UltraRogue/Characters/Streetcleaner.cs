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

        public override string Detail => "Start with the fire starter and an item called 'gasoline' which spawns 10 gasoline projectiles on kill. In addition, every and all gasoline will" +
            " immediately ignite the moment it spawns, any source of fire doesn't hurt you aswell." +
            " Streetcleaners do not spawn with this class, and you can heal by either gaining healing items, setting enemies on fire OR clearing rooms.";
        public override List<string> StartingItems => new List<string>() { "Gasoline" };

        public override List<Passive> Passives => new List<Passive>() 
        { 
            Passive.Street
        };

        Change CChange = new Change(percentage: 1);
        Change AChange = new Change(percentage: 0.5f);

        PlayerChange change;

        public override void Update(bool selected)
        {
            if (change == null)
                change = new PlayerChange(cooldownReduction: CChange, attackSpeed: AChange);

            if (selected)
            {
                CChange.percentage = 1;
                AChange.percentage = 0.5f;
            }
            else
            {
                CChange.percentage = 0;
                AChange.percentage = 0;
            }
        }
    }
}

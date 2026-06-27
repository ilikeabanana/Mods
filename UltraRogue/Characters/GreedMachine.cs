using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Text;
using static Ultrarogue.Plugin;

namespace Ultrarogue.Characters
{
    public class GreedMachine : BaseCharacter
    {
        public override string Name => "Greed Machine";
        public override string Description => "Start with the marksman and Feedbacker. <color=yellow>YOUR GOLD IS YOUR HEALTH, YOUR MARKSMAN HAS INFINITE COINS BUT COSTS GOLD</color>";

        public override string Detail => "Start with the marksman and feedbacker. You can use the marksman infinitely, this however costs gold. " +
            "Gold is related to HP now, damage won't take any health away, but will take away gold. Falling below 0 gold kills you <color=red>INSTANTLY</color>";
        
        public override List<AWeapon> StartingWeapons => new List<AWeapon>()
        {
            new AWeapon(Plugin.Weapon.Revolver, Plugin.Variant.Red),
            new AWeapon(Plugin.Weapon.Arm, Plugin.Variant.Blue),
        };
        public override List<string> StartingItems => new List<string>() { "Ration Card", "Monocle" };

        public override List<Passive> Passives => new List<Passive>()
        {
            Passive.Greedy
        };
    }
}

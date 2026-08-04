using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Characters
{
    public class testcharacterthattestsoutwhichitemsandorweaponsworkinultrarogueasstartingitems : BaseCharacter
    {
        public string ItemToTest = "Wild Card";

        public override string Name => "testcharacterthattestsoutwhichitemsandorweaponsworkinultrarogueasstartingitems";
        public override string Description => "This is a test character, if you see this; uhhhhhhh why?";
        public override string Detail => "I do some testinghgghhgohfg";
        public override List<string> StartingItems => new List<string>() { ItemToTest }; // Todays subject
        public override List<AWeapon> StartingWeapons => new List<AWeapon>() { new AWeapon(Plugin.Weapon.Revolver, Plugin.Variant.Blue) };
    }
}

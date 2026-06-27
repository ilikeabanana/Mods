using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ultrarogue.Characters
{
    public class Filth : BaseCharacter
    {
        public override string Name => "Filth";
        public override string Description => "Start with NOTHING, have 5 HP, take only 1 damage per hit. Dashing damages enemies. Heal when picking up items.";
        public override string Detail => "You start with NOTHING, and can gain NO weapon. Only items spawn. You start with 5 hp and only take 1 damage per hit." +
            " You can only heal by picking up items, any other healing method is not possible. Dashing through enemies damages them by 2, damage increases " +
            "with movement speed. Sliding damages enemies by 1. Attack speed gets converted into movement speed, cooldown reduction gets used on your stamina recharge speed.";
        public override List<AWeapon> StartingWeapons => new List<AWeapon>();
        public override List<Passive> Passives => new List<Passive>() { Passive.HeadBonk };
        public override List<string> StartingItems => new List<string>();

        Change HPChange = new Change();
        Change DChange = new Change();

        PlayerChange change;

        public override void Update(bool selected)
        {
            if (change == null)
                change = new PlayerChange(maxHealth: HPChange, damageReduction: DChange);

            if (selected)
            {
                HPChange.postMultiplier = 0.05f;
                DChange.postMultiplier = -1;
            }
            else
            {
                HPChange.postMultiplier = 1;
                DChange.postMultiplier = 1;
            }

            
        }
    }
}

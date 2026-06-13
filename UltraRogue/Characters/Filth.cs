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

using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Characters
{
    public abstract class BaseCharacter
    {
        public virtual List<AWeapon> StartingWeapons => new List<AWeapon>();
        public virtual List<string> StartingItems => new List<string>();
        public virtual List<Passive> Passives => new List<Passive>();
        public virtual string Name => "idk";

        public virtual string Description => "idk";

        public virtual void Update(bool selected)
        {

        }
        public virtual void OnRunStart() { }
        public bool HasPassive(Passive p) => Passives.Contains(p);
    }

    public enum Passive
    {
        HealFromBlood,
        NoFireDamage,
        TripleShot,
        GasolineFire,
        HealSetOnFire,
        Greedy,
        HeadBonk
    }

}

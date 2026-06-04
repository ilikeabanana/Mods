using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Curses
{
    public class CurseOfManadel : BaseCurse
    {
        public override string CurseName => "Curse of Manadel";

        public override bool OverrideEnemySpawning => true;
        public override EnemyType EnemyToSpawnInstead()
        {
            return EnemyType.Power;
        }
        public override bool CanApply()
        {
            return RogueDifficultyManager.Instance.CanSpawn(EnemyType.Power) && 
                ((3 * RogueDifficultyManager.Instance.Difficulty) >= RogueDifficultyManager.Instance.GetCost(EnemyType.Power));
        }
    }
}

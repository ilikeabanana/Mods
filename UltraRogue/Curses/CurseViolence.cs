using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Curses
{
    public class CurseViolence : BaseCurse
    {
        public override string CurseName => "Curse of Violence";
        public override void OnSpawnEnemy(EnemyIdentifier eid)
        {
            eid.attackEnemies = true;
        }
    }
}

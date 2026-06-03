using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Curses
{
    public class CurseRa : BaseCurse
    {
        public override string CurseName => "Curse of Ra";
        public override void OnSpawnEnemy(EnemyIdentifier eid)
        {
            eid.Sandify();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Curses
{
    public class BaseCurse
    {
        public virtual string CurseName => "Curse Template Name";

        /// <summary>
        /// Invoked when the player gets activated on a new floor.
        /// </summary>
        public virtual void OnApply()
        {

        }
        /// <summary>
        /// Called when the player leaves the floor (thus enters the portal to exit)
        /// </summary>
        public virtual void OnRemove()
        {

        }
        public virtual void Update()
        {

        }
        public virtual bool CanApply()
        {
            return true;
        }
        public virtual bool OverrideEnemySpawning => false;
        public virtual EnemyType EnemyToSpawnInstead()
        {
            return EnemyType.Wicked;
        }
        public virtual void OnSpawnEnemy(EnemyIdentifier eid)
        {

        }
    }
}

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ultrarogue.Curses.ClassCurses
{
    public class CurseOfHusk : BaseCurse
    {
        public override string CurseName => "Curse of Husks";
        public override bool OverrideEnemySpawning => true;

        public override bool CanApply()
        {
            foreach (SpawnableObject obj in AssetsManager.enemiesByClass["Husk"])
            {
                if (RogueDifficultyManager.Instance.CanSpawn(obj.enemyType) &&
                    ((3 * RogueDifficultyManager.Instance.Difficulty) >= RogueDifficultyManager.Instance.GetCost(obj.enemyType)))
                    return true;
            }
            return false;
        }

        public override EnemyType EnemyToSpawnInstead()
        {
            SpawnableObject[] objects = AssetsManager.enemiesByClass["Husk"].ToArray();

            return objects[Random.Range(0, objects.Length)].enemyType;
        }
    }
}
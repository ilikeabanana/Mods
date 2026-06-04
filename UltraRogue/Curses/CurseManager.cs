using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ultrarogue.Curses
{
    public class CurseManager
    {
        public static Dictionary<string, BaseCurse> CurseList = new Dictionary<string, BaseCurse>();
        public static BaseCurse ActiveCurse;
        public static List<BaseCurse> possibleCurses = new List<BaseCurse>();

        public static void GiveRandomCurse(System.Random rng)
        {
            List<BaseCurse> options = possibleCurses.Where(x => x.CanApply()).ToList();

            if (options.Count == 0)
                return; // or handle this however you want

            BaseCurse curse = options[rng.Next(options.Count)];

            ActiveCurse = curse;
            HudMessageReceiver.Instance.SendHudMessage($"You've gotten the {curse.CurseName}");
        }
        public static bool HasCurse(BaseCurse curseToCheck)
        {
            return ActiveCurse == curseToCheck;
        }

        public static bool HasCurse(string curseName)
        {
            if (!CurseList.ContainsKey(curseName)) return false;
            return HasCurse(CurseList[curseName]);
        }

        public static void FloorEnter()
        {
            if (ActiveCurse == null) return;
            ActiveCurse.OnApply();
        }
        public static void FloorExit()
        {
            if (ActiveCurse == null) return;
            ActiveCurse.OnRemove();
            ActiveCurse = null;
        }
        public static void Update()
        {
            if(ActiveCurse == null) return;
            ActiveCurse.Update();
        }
        public static EnemyType getCursedEnemy(EnemyType previousEnemy)
        {
            if (ActiveCurse == null) return previousEnemy;

            if (!ActiveCurse.OverrideEnemySpawning) return previousEnemy;
            return ActiveCurse.EnemyToSpawnInstead();
        }
        public static void OnEnemySpawn(EnemyIdentifier eid)
        {
            if (ActiveCurse == null) return;
            ActiveCurse.OnSpawnEnemy(eid);
        }
        public static void LoadCurses()
        {
            possibleCurses = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                typeof(BaseCurse).IsAssignableFrom(t) && // inherits from base
                t != typeof(BaseCurse) &&                // not the base class itself
                !t.IsAbstract)                  // not abstract
            .Select(t => (BaseCurse)Activator.CreateInstance(t))
            .ToList();

            foreach (var curse in possibleCurses)
            {
                Plugin.Logger.LogInfo($"Adding curse {curse.CurseName}");
                CurseList.Add(curse.CurseName, curse);
            }
        }
    }
}

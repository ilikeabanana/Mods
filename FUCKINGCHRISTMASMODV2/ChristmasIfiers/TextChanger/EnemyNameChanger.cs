using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace FUCKINGCHRISTMASMOD.ChristmasIfiers.TextChanger
{
    [HarmonyPatch(typeof(BossBarManager))]
    public class EnemyNameChanger : BaseStringChanger
    {
        private static readonly EnemyNameChanger Instance = new EnemyNameChanger();
        protected override string JsonFileName => "enemynames.json";

        static Dictionary<string, string> table;
        public static void Init()
        {
            var tables = Instance.LoadTables();
            table = GetTable(tables, "enemynames");
        }

        [HarmonyPatch("CreateBossBar")]
        [HarmonyPrefix]
        public static void Awake_Postfix(ref BossHealthBar bossBar, BossBarManager __instance)
        {
            if (table == null) Init();
            if (table.ContainsKey(bossBar.bossName.ToUpper()))
            {
                bossBar.bossName = table[bossBar.bossName.ToUpper()];
            }
        }
    }
}

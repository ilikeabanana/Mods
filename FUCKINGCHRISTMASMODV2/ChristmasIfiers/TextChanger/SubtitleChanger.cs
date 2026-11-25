using System;
using System.Collections.Generic;
using System.Text;

namespace FUCKINGCHRISTMASMOD.ChristmasIfiers.TextChanger
{
    public class SubtitleChanger : BaseStringChanger
    {
        private static readonly SubtitleChanger Instance = new SubtitleChanger();
        protected override string JsonFileName => "subtitles.json";

        static Dictionary<string, string> table = new Dictionary<string, string>(); 
        public static void Init()
        {
            var tables = Instance.LoadTables();
            table = GetTable(tables, "subtitles");
        }
    }
}

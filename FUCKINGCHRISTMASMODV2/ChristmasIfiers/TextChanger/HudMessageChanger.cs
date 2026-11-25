using HarmonyLib;
using System.Collections.Generic;

namespace FUCKINGCHRISTMASMOD.ChristmasIfiers.TextChanger
{
    [HarmonyPatch(typeof(HudMessage))]
    public class HudMessageChanger : BaseStringChanger
    {
        private static readonly HudMessageChanger Instance = new HudMessageChanger();

        protected override string JsonFileName => "hud_messages.json";

        private static Dictionary<string, string> replacingDicMsg1;
        private static Dictionary<string, string> replacingDicMsg2;

        public static void Init()
        {
            var tables = Instance.LoadTables();
            replacingDicMsg1 = GetTable(tables, "replacingDicMsg1");
            replacingDicMsg2 = GetTable(tables, "replacingDicMsg2");
        }

        private static void ApplyReplacements(HudMessage hudMessage)
        {
            // Safety net in case Init() was never called.
            if (replacingDicMsg1 == null || replacingDicMsg2 == null)
                Init();

            if (hudMessage.message != null && replacingDicMsg1.TryGetValue(hudMessage.message, out string newMessage1))
            {
                hudMessage.message = newMessage1;
            }
            if (hudMessage.message2 != null && replacingDicMsg2.TryGetValue(hudMessage.message2, out string newMessage2))
            {
                hudMessage.message2 = newMessage2;
            }
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void Start_Prefix(HudMessage __instance)
        {
            ApplyReplacements(__instance);
        }

        [HarmonyPatch("PlayMessage")]
        [HarmonyPrefix]
        public static void PlayMessage_Prefix(HudMessage __instance)
        {
            ApplyReplacements(__instance);
        }
    }
}

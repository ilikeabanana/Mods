using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Remove_Slowdowns
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger { get; private set; } = null!;
        Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_VERSION);
        private void Awake()
        {
            Harmony.PatchAll();
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            gameObject.hideFlags = HideFlags.DontSaveInEditor;
        }
    }

    [HarmonyPatch]
    public class RemoveDem
    {
        static IEnumerable<CodeInstruction> ReplaceSlowdown(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo methodToFind = AccessTools.Method(typeof(TimeController), nameof(TimeController.SlowDown));
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction instr = codes[i];
                if (instr.opcode == OpCodes.Callvirt)
                {
                    if (instr.operand is MethodInfo method && method == methodToFind)
                    {
                        codes[i - 2].opcode = OpCodes.Nop;
                        codes[i - 1].opcode = OpCodes.Nop;
                        codes[i].opcode = OpCodes.Nop; // replace it with nuthin
                    }
                }
            }

            return codes;
        }


        [HarmonyPatch(typeof(Minotaur), nameof(Minotaur.Death)), HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MinotaurDeath(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceSlowdown(instructions);
        }


        [HarmonyPatch(typeof(Minotaur), "BloodExplosion"), HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MinotaurExplosion(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceSlowdown(instructions);
        }
        [HarmonyPatch(typeof(MinosPrime), nameof(MinosPrime.Death)), HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MinosDeath(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceSlowdown(instructions);
        }
        [HarmonyPatch(typeof(MinosPrime), nameof(MinosPrime.OutroEnd)), HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MinosOutro(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceSlowdown(instructions);
        }
        [HarmonyPatch(typeof(SisyphusPrime), nameof(SisyphusPrime.Death)), HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SisyphusDeath(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceSlowdown(instructions);
        }
        [HarmonyPatch(typeof(SisyphusPrime), nameof(SisyphusPrime.OutroEnd)), HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SisyphusOutro(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceSlowdown(instructions);
        }
        [HarmonyPatch(typeof(MinotaurChase), nameof(MinotaurChase.Death)), HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ChaseTaurDead(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceSlowdown(instructions);
        }
        [HarmonyPatch(typeof(LeviathanController), nameof(LeviathanController.FinalExplosion)), HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> LeviBoom(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceSlowdown(instructions);
        }
        [HarmonyPatch(typeof(GabrielBase), "JuggleStart"), HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> GabrielJuggle(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceSlowdown(instructions);
        }
        [HarmonyPatch(typeof(SwordsMachine), "EndFirstPhase"), HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SwordPhase(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceSlowdown(instructions);
        }

        [HarmonyPatch(typeof(SlowMo), "OnEnable"), HarmonyPrefix]
        public static bool DontSlowmo(SlowMo __instance)
        {
            if (__instance.gameObject.name.Contains("FakeGabriel")) return false;

            return true;
        }
    }
}

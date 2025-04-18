using HarmonyLib;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(Turret))]
    internal class WorseSentries
    {
        [HarmonyPatch(nameof(Turret.Start))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Turret __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            __instance.maxAimTime = 0;
        }
    }
}
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch]
    public class HudCorrector
    {
        [HarmonyPatch(typeof(HudMessage), nameof(HudMessage.Start))]
        public static void Prefix(HudMessage __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(MonoSingleton<PrefsManager>.Instance.GetInt("difficulty", 0))) return;

            if (__instance.message == "The <color=orange>GUTTERMAN SHIELD</color> can be <color=orange>BROKEN</color> with the <color=red>KNUCKLEBLASTER</color>. Swap arms with '<color=orange>")
            {
                __instance.message =
                    "The <color=orange>GUTTERMAN SHIELD</color> can be <color=orange>BROKEN</color> after hitting it <color=red>3 times</color> with the <color=red>KNUCKLEBLASTER</color>. Swap arms with '<color=orange>";
            }
        }
    }
}

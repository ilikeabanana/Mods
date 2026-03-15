using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(Power), nameof(Power.Update))]
    public class WorsePower
    {
        public static void Postfix(Power __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            //__instance.inAction = true;
            __instance.attackCooldown -= Time.deltaTime;

        }
    }

    [HarmonyPatch(typeof(Power), nameof(Power.UpdateSpeed))]
    public class WorsePower_Awake
    {
        public static void Postfix(Power __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __instance.anim.speed *= 2;

        }
    }
}

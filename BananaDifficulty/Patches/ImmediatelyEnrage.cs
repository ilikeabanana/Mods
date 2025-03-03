using HarmonyLib;

namespace BananaDifficulty.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch("Start")]
    internal class ImmediatelyEnrage
    {
        [HarmonyPatch(typeof(SwordsMachine))]
        [HarmonyPostfix]
        public static void SwordAngry(SwordsMachine __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return;
            __instance.eternalRage = true;
            __instance.Enrage();
        }
        [HarmonyPatch(typeof(SpiderBody))]
        [HarmonyPostfix]
        public static void SpiderAngry(SpiderBody __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return;
            __instance.Enrage();
        }
        [HarmonyPatch(typeof(Gutterman))]
        [HarmonyPostfix]
        public static void GuttermanAngry(Gutterman __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return;
            __instance.eternalRage = true;
            __instance.Enrage();
        }
        [HarmonyPatch(typeof(Mindflayer))]
        [HarmonyPostfix]
        public static void MindAngry(Mindflayer __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return;
            __instance.Enrage();
        }
        [HarmonyPatch(typeof(V2))]
        [HarmonyPostfix]
        public static void V2Angry(V2 __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return;
            __instance.Enrage();
        }
        [HarmonyPatch(typeof(StatueBoss))]
        [HarmonyPostfix]
        public static void StatueAngry(StatueBoss __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return;
            __instance.Enrage();
        }
        [HarmonyPatch(typeof(Drone))]
        [HarmonyPostfix]
        public static void VirtueAngry(Drone __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt()) return;
            if (__instance.eid.enemyType != EnemyType.Virtue)
            {
                __instance.projectile = new UnityEngine.AddressableAssets.AssetReference("6be53089211b2eb4ab93a26541e4e65b");
                return;
            }
            __instance.Enrage();
        }
    }
}
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using System.Collections;
namespace Permawield
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger { get; private set; } = null!;
        Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        public static int lastDualWieldAmount = 0;

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            gameObject.hideFlags = HideFlags.DontSaveInEditor;

            Harmony.PatchAll();
        }

        public static void AddDualWield()
        {
            if (MonoSingleton<GunControl>.Instance)
            {
                MonoSingleton<CameraController>.Instance.CameraShake(0.35f);
                if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.Platformer)
                {
                    MonoSingleton<PlatformerMovement>.Instance.AddExtraHit(3);
                    return;
                }
                GameObject gameObject = new GameObject();
                gameObject.transform.SetParent(MonoSingleton<GunControl>.Instance.transform, true);
                gameObject.transform.localRotation = Quaternion.identity;
                DualWield[] componentsInChildren = MonoSingleton<GunControl>.Instance.GetComponentsInChildren<DualWield>();
                if (componentsInChildren != null && componentsInChildren.Length % 2 == 0)
                {
                    gameObject.transform.localScale = new Vector3(-1f, 1f, 1f);
                }
                else
                {
                    gameObject.transform.localScale = Vector3.one;
                }
                if (componentsInChildren == null || componentsInChildren.Length == 0)
                {
                    gameObject.transform.localPosition = Vector3.zero;
                }
                else if (componentsInChildren.Length % 2 == 0)
                {
                    gameObject.transform.localPosition = new Vector3((float)(componentsInChildren.Length / 2) * -1.5f, 0f, 0f);
                }
                else
                {
                    gameObject.transform.localPosition = new Vector3((float)((componentsInChildren.Length + 1) / 2) * 1.5f, 0f, 0f);
                }
                DualWield dualWield = gameObject.AddComponent<DualWield>();
                dualWield.delay = 0.05f;
                dualWield.juiceAmount = 30f;
                if (componentsInChildren != null && componentsInChildren.Length != 0)
                {
                    dualWield.delay += (float)componentsInChildren.Length / 20f;
                }
            }
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        [HarmonyPatch(typeof(PowerUpMeter), nameof(PowerUpMeter.UpdateMeter))]
        [HarmonyPrefix]
        public static void KeepActive(PowerUpMeter __instance)
        {
            __instance.juice = 30f;
            __instance.latestMaxJuice = 30f;
        }
        [HarmonyPatch(typeof(GunControl), "Update")]
        [HarmonyPrefix]
        public static void KeepDualWields(GunControl __instance)
        {
            if(Plugin.lastDualWieldAmount < __instance.dualWieldCount)
                Plugin.lastDualWieldAmount = __instance.dualWieldCount;
        }
        [HarmonyPatch(typeof(PowerUpMeter), "Start")]
        [HarmonyPostfix]
        public static void KeepDualWields(NewMovement __instance)
        {
            Debug.Log(Plugin.lastDualWieldAmount);
            for (int i = 0; i < Plugin.lastDualWieldAmount; i++)
            {
                Plugin.AddDualWield();
            }
        }

    }
}

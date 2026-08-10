using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using MultiplayerUtil;
using HarmonyLib;

namespace Multiplayer_Chess
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger { get; private set; } = null!;
        public static Plugin Instance { get; private set; }
        Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        private void Awake()
        {
            Instance = this;
            Harmony.PatchAll();
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            gameObject.hideFlags = HideFlags.DontSaveInEditor;
            Callbacks.StartupComplete.AddListener(() =>
            {
                NetworkManager.Setup();
            });

            Callbacks.OnLobbyMemberJoined.AddListener((lobby, friend) =>
            {
                HudMessageReceiver.Instance.SendHudMessage($"{friend.Name} Joined the lobby!");
            });

            Application.quitting += Application_quitting;
        }

        private void Application_quitting()
        {
            throw new System.NotImplementedException();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                NetworkManager.Host();
            }
            if (Input.GetKeyDown(KeyCode.J))
            {
                NetworkManager.Join();
            }
        }
    }
}
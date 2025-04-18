using System;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using plog;
using Logger = plog.Logger;

namespace GameConsole.Commands
{
    public class UnloadModCommand : ICommand, IConsoleLogger
    {
        public Logger Log { get; } = new Logger("UnloadMod");

        public string Name => "UnloadMod";
        public string Description => "Unloads a specified mod.";
        public string Command => "unloadmod";

        plog.Logger IConsoleLogger.Log => throw new NotImplementedException();

        public void Execute(Console con, string[] args)
        {
            if (args.Length == 0)
            {
                Log.Info("Usage: unloadmod <modGUID>", null, null, null);
                return;
            }

            string modGUID = args[0];

            if (!Chainloader.PluginInfos.TryGetValue(modGUID, out var pluginInfo))
            {
                Log.Info($"Mod with GUID '{modGUID}' not found.", null, null, null);
                return;
            }

            var modAssembly = pluginInfo.Instance.GetType().Assembly;
            var harmonyID = pluginInfo.Metadata.GUID;

            try
            {
                Log.Info($"Unpatching Harmony patches for {harmonyID}...");
                var harmony = new Harmony(harmonyID);
                harmony.UnpatchAll(harmonyID);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to unpatch Harmony: {e}", null, null, null);
            }

            try
            {
                Log.Info($"Unloading AssetBundles loaded by {modGUID}...");
                var assetBundles = Resources.FindObjectsOfTypeAll<AssetBundle>();
                foreach (var bundle in assetBundles)
                {
                    if (bundle != null)
                    {
                        bundle.Unload(true);
                        Log.Info($"Unloaded AssetBundle: {bundle.name}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to unload AssetBundles: {e}", null, null, null);
            }

            try
            {
                Log.Info($"Destroying MonoBehaviours from {modGUID}...");
                var allObjects = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                foreach (var obj in allObjects)
                {
                    if (obj.GetType().Assembly == modAssembly)
                    {
                        UnityEngine.Object.Destroy(obj);
                        Log.Info($"Destroyed {obj.name}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to destroy MonoBehaviours: {e}", null, null, null);
            }

            try
            {
                Log.Info($"Unloading mod assembly for {modGUID}...");
                AppDomain.CurrentDomain.LoadedAssemblies.Remove(modAssembly);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to unload assembly: {e}", null, null, null);
            }

            Log.Info("Reloading the current scene...");
            MonoSingleton<OptionsManager>.Instance.RestartMission();
        }
    }
}

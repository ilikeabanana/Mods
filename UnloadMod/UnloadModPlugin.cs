using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameConsole.Commands;
using plog;

using Scene = UnityEngine.SceneManagement.Scene;
using GameConsole;

[BepInPlugin("dopmahreal.ultrakill.unloader", "UnloadModPlugin", "1.0.0")]
public class UnloadModPlugin : BaseUnityPlugin
{
    private static bool _isCommandRegistered = false;
    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneHelper.CurrentScene == "Main Menu" && !_isCommandRegistered)
        {
            MonoSingleton<Console>.Instance.RegisterCommand(new UnloadModCommand());
            _isCommandRegistered = true;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
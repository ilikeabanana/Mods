namespace UltraEditor.Classes;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ultrarogue;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

/// <summary> Handles loading and accessing the empty scene. </summary>
public static class EmptySceneLoader
{
    /// <summary> Whether its already loaded. </summary>
    private static bool _loaded = false;

    /// <summary> Forces the editor to open as soon as the scene loads. </summary>
    public static bool forceEditor = false;

    /// <summary> Forces the editor to load a save as soon as the scene loads. </summary>
    public static string forceSave = "";

    /// <summary> Forces the editor to load a data string as soon as the scene loads, only happens if forceSave is "?". </summary>
    public static string forceSaveData = "";

    /// <summary> Forces the editor to set a scene name, only happens if forceSave is "?". </summary>
    public static string forceLevelName = "";

    /// <summary> Forces the editor to set a scene name to SceneHelper </summary>
    public static string forceLevelGUID = "";

    /// <summary> Load the assetbundle containing the scene. </summary>
    public static void Load()
    {
        // istg why does this crash the game when u dont do this
        Addressables.LoadAssetAsync<GameObject>("FirstRoom").WaitForCompletion();

        // load asset bundle :3 meow rawr
        Stream bundleStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ultrarogue.ultrarogue");

        AssetBundleCreateRequest assetRequest = AssetBundle.LoadFromStreamAsync(bundleStream);
        assetRequest.completed += (_) =>
        {
            Plugin.Logger.LogInfo("Loaded Empty Scene bundle.");
            _loaded = true;
        };
    }

    /// <summary> Loads the Empty level. </summary>
    public static void LoadLevel() => Plugin.Instance.StartCoroutine(LoadLevelAsync());

    /// <summary> Asynchronously loads the Empty level. </summary>
    public static IEnumerator LoadLevelAsync()
    {
        Plugin.Logger.LogInfo("Loading Empty Scene.");
        SceneHelper.PendingScene = "EpicLevel";
        SceneHelper.Instance.loadingBlocker.SetActive(true);
        if (!forceEditor && forceSave != "") SceneHelper.SetLoadingSubtext("Loading level...");
        else SceneHelper.SetLoadingSubtext("Loading editor...");
        yield return null;

        if (!_loaded)
        {
            Plugin.Logger.LogError("Empty Scene Bundle wasn't loaded before trying to enter the scene.");
            Load();

            // wait til its loaded
            while (!_loaded) yield return null;
        }

        if (SceneHelper.CurrentScene.StartsWith("EpicLevel")) 
            SceneHelper.LastScene = SceneHelper.CurrentScene;
        
        SceneHelper.CurrentScene = "EpicLevel";
        if (forceLevelGUID != "" && forceSave == "?" && !forceEditor)
            SceneHelper.CurrentScene = "EpicLevel" + "."+forceLevelGUID;

        AsyncOperation sceneload = SceneManager.LoadSceneAsync("Assets/Maps/RogueMode/EpicLevel.unity");

        // wait til its loaded 
        while (!sceneload.isDone) yield return null;

        Plugin.Logger.LogInfo("Scene loaded!");
        SceneHelper.SetLoadingSubtext("");
        SceneHelper.Instance.loadingBlocker.SetActive(false);
        SceneHelper.PendingScene = null;
    }
}

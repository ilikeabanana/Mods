namespace Ultrarogue;

using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using HarmonyLib;

using BepLogSource = BepInEx.Logging.ManualLogSource;
using BepLogger = BepInEx.Logging.Logger;
using Unity.AI.Navigation;

/// <summary> Handles loading and accessing the empty scene. </summary>
[HarmonyPatch]
public static class SceneLoader
{
    /// <summary> BepInEx log source created in <see cref="Load"/>. </summary>
    public static BepLogSource logger = BepLogger.CreateLogSource("Ultrarogue:: SceneLoader");

    /// <summary> What SceneHelper.CurrentScene will be set to :3 </summary>
    public const string SceneName = "Ultrarogue";

    /// <summary> Whether the asset bundle containing the scene has been loaded yet. </summary>
    public static bool Loaded = false;

    /// <summary> Load the assetbundle containing the scene, and return the bundle load async operation if u wanna wait for it :3 </summary>
    public static AsyncOperation Load()
    {
        // istg why does this crash the game when u dont do this
        Addressables.LoadAssetAsync<GameObject>("FirstRoom").WaitForCompletion();

        // load embedded asset bundle :3 meow rawr
        Stream bundleStream = Assembly.GetExecutingAssembly()
        .GetManifestResourceStream("Ultrarogue.ultrarogue");

        var asyncOp = AssetBundle.LoadFromStreamAsync(bundleStream);

        asyncOp.completed += (op) =>
        {
            AssetBundle bundle = ((AssetBundleCreateRequest)op).assetBundle;

            foreach (string scenePath in bundle.GetAllScenePaths())
                logger.LogInfo("Scene in bundle: " + scenePath);

            Loaded = true;
        };



        return asyncOp;
    }


    /// <summary> Asynchronously loads the Empty level. </summary>
    public static IEnumerator LoadLevelAsync(bool noSplash)
    {
        // show loading subtext and loading blocker
        logger.LogInfo("Loading Empty Scene...");
        SceneHelper.PendingScene = SceneName;
        if (!noSplash)
        {
            SceneHelper.Instance.loadingBlocker.SetActive(true);
            SceneHelper.SetLoadingSubtext("I am loading the amazing level fuck you");
        }

        // if the bundle isnt loaded yet then like load it :P oh yea and wait for it to load
        if (!Loaded)
            yield return Load();

        // actually fucking load the scene lmao
        yield return SceneManager.LoadSceneAsync("Assets/Maps/RogueMode/Data/EpicLevel.unity");

        // set current scene and last scene once the level is done loading
        if (SceneHelper.CurrentScene != SceneName)
            SceneHelper.LastScene = SceneHelper.CurrentScene;

        SceneHelper.CurrentScene = SceneName;

        // hide the loading blocker and stuff
        logger.LogInfo("Scene loaded!");
        SceneHelper.PendingScene = null;
        SceneHelper.SetLoadingSubtext("");
        SceneHelper.Instance.loadingBlocker.SetActive(false);

        yield return new WaitForSeconds(1f);

        yield return ShaderManager.ApplyShadersAsync(SceneManager.GetActiveScene().GetRootGameObjects());
        yield return ShaderManager.LoadShadersFromDictionaryAsync();

        new GameObject("generator").AddComponent<RoomGenerator>();
        new GameObject("NavMesh").AddComponent<NavMeshSurface>();
    }

    /// <summary> Patches <see cref="SceneHelper.LoadSceneCoroutine(string, bool)"/> to make it use our loader if it's trying to load our scene :3 </summary>
    [HarmonyPrefix] [HarmonyPatch(typeof(SceneHelper), "LoadSceneCoroutine")]
    public static bool RedirectSceneHelperSceneLoader(ref IEnumerator __result, string sceneName, bool noSplash)
    {
        if (sceneName == SceneName)
        {
            __result = LoadLevelAsync(noSplash);
            return false;
        }

        return true;
    }
}
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
    public static void Load()
    {
        // istg why does this crash the game when u dont do this
        Addressables.LoadAssetAsync<GameObject>("FirstRoom").WaitForCompletion();

        BundleLoader.Load();

    }

    static string[] messages = new string[]
    {
        "You should die- You should throw a fucking beach party",
        "Items are items, and weapons are weapons",
        "Banana",
        "I am wrting",
        "Literacy, fuck yeah",
        "FUCK YOU",
        "I am loading level",
        "tip: die",
        "fun fact: die"
    };

    /// <summary> Asynchronously loads the Empty level. </summary>
    public static IEnumerator LoadLevelAsync(bool noSplash)
    {
        // show loading subtext and loading blocker
        logger.LogInfo("Loading Empty Scene...");
        SceneHelper.PendingScene = SceneName;
        if (!noSplash)
        {
            SceneHelper.Instance.loadingBlocker.SetActive(true);

            string randomMessage = messages[Random.Range(0, messages.Length)];
            SceneHelper.SetLoadingSubtext(randomMessage);
        }

        // if the bundle isnt loaded yet then like load it :P oh yea and wait for it to load
        if (!Loaded)
            Load();
        yield return new WaitForSeconds(1f); // idk wait a second ig???
        // actually fucking load the scene lmao
        var op = Addressables.LoadSceneAsync("Assets/Modding/RogueMode/EpicLevel.unity", LoadSceneMode.Single);
        yield return op;

        // set current scene and last scene once the level is done loading
        if (SceneHelper.CurrentScene != SceneName)
            SceneHelper.LastScene = SceneHelper.CurrentScene;

        SceneHelper.CurrentScene = SceneName;

        // hide the loading blocker and stuff
        logger.LogInfo("Scene loaded!");
        SceneHelper.PendingScene = null;
        SceneHelper.SetLoadingSubtext("");
        SceneHelper.Instance.loadingBlocker.SetActive(false);

        //yield return ShaderManager.ApplyShadersAsync(SceneManager.GetActiveScene().GetRootGameObjects());
        //yield return ShaderManager.LoadShadersFromDictionaryAsync();

        //new GameObject("generator").AddComponent<RoomGenerator>();
        new GameObject("NavMesh").AddComponent<NavMeshSurface>();
        Plugin.Instance.StartCoroutine(PlayPixelAnimation());
    }

    static IEnumerator PlayPixelAnimation()
    {
        // Wait until PostProcessV2_Handler is ready
        yield return new WaitUntil(() => PostProcessV2_Handler.Instance != null);

        float target = PostProcessV2_Handler.Instance.downscaleResolution;
        float actualTarget = target;
        // Early out so you at least know why nothing happens
        if (target == 0f)
        {
            target = 720;
        }

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            float pixelizationValue = Mathf.Lerp(1, target, t);
            Shader.SetGlobalFloat("_ResY", pixelizationValue);
            PostProcessV2_Handler.Instance.downscaleResolution = pixelizationValue;
            yield return null;
        }

        Shader.SetGlobalFloat("_ResY", actualTarget);
        PostProcessV2_Handler.Instance.downscaleResolution = actualTarget;

        NewMovement nm = MonoSingleton<NewMovement>.Instance;
        GunControl gc = MonoSingleton<GunControl>.Instance;
        GameStateManager.Instance.PopState("pit-falling");
        if (!nm.activated)
        {
            nm.activated = true;
            nm.cc.activated = true;
            nm.cc.CameraShake(1f);
            nm.cc.enabled = true;
        }

        gc.YesWeapon();
        MonoSingleton<PlayerActivatorRelay>.Instance.ResetIndex();
        MonoSingleton<PlayerActivatorRelay>.Instance.Activate();
        if (nm.levelOver)
        {
            nm.levelOver = false;
            MonoSingleton<StatsManager>.Instance.UnhideShit();
        }
        PlayerActivator.lastActivatedPosition = MonoSingleton<NewMovement>.Instance.transform.position;
        MonoSingleton<FistControl>.Instance.YesFist();
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
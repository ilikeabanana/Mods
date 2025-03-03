using BepInEx;
using HarmonyLib;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[BepInPlugin("doomahreal.ultrakill.ThePStandsForPeppino", "ThePStandsForPeppino", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    public static GameObject PeppinoObject;
    public static AudioClip WinAudioClip;
    public static bool HasTriggered = false;
    public static Harmony harmony;

    private void Awake()
    {
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ThePStandsForPeppino.dogorb.bundle"))
        {
            if (stream != null)
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                PeppinoObject = bundle.LoadAsset<GameObject>("PeppinoObject");
                WinAudioClip = bundle.LoadAsset<AudioClip>("win");
            }
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
        harmony = new Harmony("doomahreal.ultrakill.ThePStandsForPeppino");
        harmony.PatchAll();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Harmony.HasAnyPatches(harmony.Id))
        {
            harmony.PatchAll();
        }
        HasTriggered = false;
    }
}

[HarmonyPatch(typeof(FinalRank))]
[HarmonyPatch("Appear")]
public static class FinalRankPatch
{
    static bool Prefix(FinalRank __instance)
    {
        if (Plugin.HasTriggered)
        {
            Debug.Log("HasTriggered is true, returning false");
            return false;
        }

        Plugin.HasTriggered = true;

        var statsManager = MonoSingleton<StatsManager>.Instance;
        if (__instance.totalRank.text != "<color=#FFFFFF>P</color>" || statsManager.asscon.cheatsEnabled)
        {
            Debug.Log("RankScore is not 12 or cheats enabled, returning true");
            return true;
        }

        Debug.Log("RankScore is 12 and cheats disabled, proceeding with white screen");

        var whiteScreen = new GameObject("WhiteScreen");
        var canvas = whiteScreen.GetComponent<Canvas>() ?? whiteScreen.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var rectTransform = whiteScreen.GetComponent<RectTransform>() ?? whiteScreen.AddComponent<RectTransform>();
        SetStretch(rectTransform);
        canvas.sortingOrder = 99;

        var fadeIn = AddImageFadeIn(whiteScreen.GetComponent<Image>() ?? whiteScreen.AddComponent<Image>(), 1f / 1.145f);

        if (Plugin.WinAudioClip != null)
        {
            Debug.Log("Playing WinAudioClip");
            var audioSource = whiteScreen.GetComponent<AudioSource>() ?? whiteScreen.AddComponent<AudioSource>();
            audioSource.clip = Plugin.WinAudioClip;
            audioSource.playOnAwake = true;
            audioSource.Play();
        }

        fadeIn.onFull.AddListener(() =>
        {
            Debug.Log("FadeIn completed, instantiating Peppino");
            if (Plugin.PeppinoObject == null)
            {
                Debug.Log("PeppinoObject is null, returning");
                return;
            }

            var peppinoInstance = Object.Instantiate(Plugin.PeppinoObject);
            var peppinoCanvas = peppinoInstance.GetComponent<Canvas>() ?? peppinoInstance.AddComponent<Canvas>();
            peppinoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            peppinoCanvas.sortingOrder = 100;

            var peppinoImage = peppinoInstance.GetComponent<Image>() ?? peppinoInstance.AddComponent<Image>();
            SetStretch(peppinoInstance.GetComponent<RectTransform>());

            var animator = peppinoInstance.GetComponent<Animator>();
            __instance.StartCoroutine(DisableAnimatorAfterDelay(animator, __instance));
        });

        return false;
    }

    static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static ImageFadeIn AddImageFadeIn(Image image, float speed)
    {
        var fadeIn = image.gameObject.GetComponent<ImageFadeIn>() ?? image.gameObject.AddComponent<ImageFadeIn>();
        fadeIn.speed = speed;
        fadeIn.maxAlpha = 1f;
        image.color = new Color(1, 1, 1, 0);
        return fadeIn;
    }

    static IEnumerator DisableAnimatorAfterDelay(Animator animator, FinalRank __instance)
    {
        Debug.Log("Disabling animator after delay");
        yield return new WaitForSeconds(10f);
        animator.enabled = false;
        Harmony.UnpatchID(Plugin.harmony.Id);
        __instance.Appear();
    }
}
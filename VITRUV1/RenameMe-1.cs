using BepInEx;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine.Animations;
using UnityEngine.Playables;

[BepInPlugin("doomahreal.ultrakill.VITRUV1", "VITRUV1", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private GameObject modelInstance;
    private Camera renderCamera;
    private RenderTexture renderTexture;
    private Transform modelParent;
    private Animator animator;
    private AssetBundle loadedBundle;

    private readonly string modelName = "veebulshit";
    private readonly string targetImagePath = "Canvas/Main Menu (1)/V1";

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneHelper.CurrentScene != "Main Menu") return;

        GameObject uiImageObject = GameObject.Find(targetImagePath);
        if (uiImageObject == null) return;

        foreach (Transform child in uiImageObject.transform)
        {
            RawImage rawImage = child.GetComponent<RawImage>();
            if (rawImage != null)
            {
                rawImage.texture = null;
                rawImage.color = new Color(1, 1, 1, 0); // Set color to transparent
            }

            Image spriteImage = child.GetComponent<Image>();
            if (spriteImage != null)
            {
                spriteImage.sprite = null;
                spriteImage.color = new Color(1, 1, 1, 0); // Set color to transparent
            }
        }

        SetupRenderTexture(uiImageObject);
        LoadModel(uiImageObject);
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (SceneHelper.CurrentScene == "Main Menu") return;

        if (modelInstance != null) Destroy(modelInstance);
        if (modelParent != null) Destroy(modelParent.gameObject);
        if (renderCamera != null) Destroy(renderCamera.gameObject);
        if (renderTexture != null) renderTexture.Release();

        if (loadedBundle != null)
        {
            loadedBundle.Unload(true);
            loadedBundle = null;
        }
    }

    private void SetupRenderTexture(GameObject uiObject)
    {
        renderTexture = new RenderTexture(1024, 1024, 16);
        renderTexture.Create();

        renderCamera = new GameObject("RenderCamera").AddComponent<Camera>();
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = new Color(0, 0, 0, 0);
        renderCamera.cullingMask = LayerMask.GetMask("Armor");
        renderCamera.transform.position = new Vector3(-0.1f, 2.111f, 4.2928f);
        renderCamera.transform.rotation = Quaternion.Euler(0, 180, 0);
        renderCamera.targetTexture = renderTexture;
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = 2.25f;

        RawImage rawImage = uiObject.GetComponent<RawImage>();
        Image spriteImage = uiObject.GetComponent<Image>();

        if (rawImage == null)
        {
            if (spriteImage != null) DestroyImmediate(spriteImage);
            rawImage = uiObject.AddComponent<RawImage>();
        }

        rawImage.texture = renderTexture;

        RectTransform rectTransform = uiObject.GetComponent<RectTransform>();
        if (rectTransform != null) rectTransform.anchoredPosition -= new Vector2(0, 20);
    }

    private void LoadModel(GameObject uiObject)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(name => name.EndsWith(".bundle"));
        if (resourceName == null) return;

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null) return;

            byte[] bundleData = new byte[stream.Length];
            stream.Read(bundleData, 0, bundleData.Length);
            loadedBundle = AssetBundle.LoadFromMemory(bundleData);
            if (loadedBundle == null) return;

            GameObject modelPrefab = loadedBundle.LoadAsset<GameObject>(modelName);
            if (modelPrefab == null) return;

            modelParent = new GameObject("ModelParent").transform;
            modelInstance = Instantiate(modelPrefab, modelParent);
            modelInstance.layer = LayerMask.NameToLayer("Armor");

            foreach (var child in modelInstance.GetComponentsInChildren<Transform>())
                child.gameObject.layer = LayerMask.NameToLayer("Armor");

            if (modelInstance.transform.childCount > 0)
            {
                animator = modelInstance.transform.GetChild(0).GetComponent<Animator>();
            }

            AnimationClip[] animationClips = loadedBundle.LoadAllAssets<AnimationClip>();
            if (animationClips.Length > 0)
            {
                AnimationClip firstClip = animationClips[0];
                PlayAnimationClip(firstClip);
            }

            LogicButtocksHaHa controller = uiObject.AddComponent<LogicButtocksHaHa>();
            controller.Initialize(modelParent, animator, uiObject.GetComponent<RectTransform>());
        }
    }

    private void PlayAnimationClip(AnimationClip clip)
    {
        if (animator == null || clip == null) return;

        var playableGraph = PlayableGraph.Create();
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "AnimationOutput", animator);
        var clipPlayable = AnimationClipPlayable.Create(playableGraph, clip);
        playableOutput.SetSourcePlayable(clipPlayable);

        playableGraph.Play();
    }
}
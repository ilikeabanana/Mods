using BepInEx;
using BepInEx.Logging;
using GameConsole.pcon;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Specialist_Dance
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger { get; private set; } = null!;
        GameObject Specialist;
        GameObject current = null;
        Dictionary<AnimationClip, AudioClip> clips = new Dictionary<AnimationClip, AudioClip>();
        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            gameObject.hideFlags = HideFlags.DontSaveInEditor;

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

        }

        private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            if (ShaderManager.shaderDictionary.Count == 0)
            {
                StartCoroutine(ShaderManager.LoadShadersAsync());
            }
            else if(Specialist == null)
            {
                var a = Assembly.GetExecutingAssembly();
                AssetBundle bundle = AssetBundle.LoadFromStream(a.GetManifestResourceStream("Specialist_Dance.specialist"));
                Specialist = bundle.LoadAsset<GameObject>("SpecialistDance.prefab");
                foreach (var clip in bundle.LoadAllAssets<AnimationClip>())
                {
                    clips.Add(clip, bundle.LoadAsset<AudioClip>(clip.name + ".wav"));
                }
                StartCoroutine(ShaderManager.ApplyShaderToGameObject(Specialist));
            }

        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.H) && PlayerTracker.Instance.playerType == PlayerType.Platformer) 
            {
                if (current != null) Destroy(current);
                PlatformerMovement.Instance.smr.enabled = false;


                GameObject special = Instantiate(Specialist, PlatformerMovement.Instance.smr.transform.parent);
                special.transform.localPosition = Vector3.zero;
                special.transform.localScale /= 3;

                current = special;

                Animator anim = special.GetComponent<Animator>();

                KeyValuePair<AnimationClip, AudioClip> rng = clips.ElementAt(Random.Range(0, clips.Count));

                anim.runtimeAnimatorController = ClipsOverride(anim, rng.Key);
                if (rng.Value == null)
                {
                    special.GetComponent<AudioSource>().Stop();
                    return;
                }
                special.GetComponent<AudioSource>().clip = rng.Value;
                special.GetComponent<AudioSource>().Play();

            } 
        }


        public RuntimeAnimatorController ClipsOverride(Animator anim, AnimationClip clip)
        {

            AnimatorOverrideController overrider =
                new AnimatorOverrideController(anim.runtimeAnimatorController);

            overrider[anim.runtimeAnimatorController.animationClips[0].name] = clip;

            return overrider;
        }
    }
}

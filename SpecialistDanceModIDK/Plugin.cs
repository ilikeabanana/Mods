using BepInEx;
using BepInEx.Logging;
using GameConsole.pcon;
using HarmonyLib;
using System.Collections;
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
        public static Plugin Instance { get; private set; } = null!;

        GameObject Specialist;
        GameObject current = null;
        Dictionary<AnimationClip, AudioClip> clips = new Dictionary<AnimationClip, AudioClip>();

        // Have we already handed the loaded clips to the EmoteWheel once it exists?
        private bool emotesHandedOff = false;

        Transform parent;

        // Tracks the pending "return to normal" coroutine for the current
        // dance, so a new dance (or a manual cancel) can stop a stale one
        // from firing later and clobbering whatever replaced it.
        private Coroutine danceEndRoutine;

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Instance = this;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            gameObject.hideFlags = HideFlags.DontSaveInEditor;

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

            // Apply the WeaponWheel.Start Harmony patch that spawns EmoteWheel.
            Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            emotesHandedOff = false;
            if (ShaderManager.shaderDictionary.Count == 0)
            {
                StartCoroutine(ShaderManager.LoadShadersAsync());
            }
            else if (Specialist == null)
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
            // Once both the clips are loaded and EmoteWheel has been spawned
            // by the Harmony patch, give it the emote list exactly once.
            if (!emotesHandedOff && clips.Count > 0 && EmoteWheel.Instance != null)
            {
                List<EmoteEntry> entries = clips.Select(kv => new EmoteEntry
                {
                    displayName = kv.Key.name,
                    clip = kv.Key,
                    audio = kv.Value
                }).ToList();

                EmoteWheel.Instance.SetEmotes(entries);
                emotesHandedOff = true;
            }

            // Holding H now opens the wheel instead of instantly picking a
            // random dance; the wheel plays the chosen one on key-up.
            if (Input.GetKeyDown(KeyCode.H)
                && PlayerTracker.Instance.playerType == PlayerType.Platformer
                && EmoteWheel.Instance != null)
            {
                CameraController.Instance.enabled = false;
                EmoteWheel.Instance.Show();
            }
        }

        // Called by EmoteWheel when the player confirms a selection.
        public void PlayDance(AnimationClip clip, AudioClip audio)
        {
            // A previous dance's auto-revert timer is no longer relevant -
            // this new dance is about to become "current".
            CancelPendingReturn();

            if (current == null)
            {
                PlatformerMovement.Instance.smr.transform.parent.localScale = Vector3.zero;
            }
            if (parent == null) parent = PlatformerMovement.Instance.smr.transform.parent.parent;
            GameObject special = Instantiate(Specialist, parent);
            special.transform.localPosition = Vector3.zero;
            //special.transform.localScale /= 2;

            TransformReplicator repl = special.AddComponent<TransformReplicator>();

            repl.Replic = PlatformerMovement.Instance.smr.transform.parent;

            if (current != null) Destroy(current);

            current = special;

            Animator anim = special.GetComponent<Animator>();
            anim.runtimeAnimatorController = ClipsOverride(anim, clip);

            float audioLength = 0f;
            if (audio == null)
            {
                special.GetComponent<AudioSource>().Stop();
            }
            else
            {
                special.GetComponent<AudioSource>().clip = audio;
                special.GetComponent<AudioSource>().Play();
                special.AddComponent<TimePitch>();
                audioLength = audio.length;
            }

            // If the clip isn't set to loop, schedule an automatic return
            // to normal once both the animation and the audio have had
            // time to finish.
            if (!clip.isLooping)
            {
                float duration = Mathf.Max(clip.length, audioLength);
                danceEndRoutine = StartCoroutine(EndDanceAfterDelay(special, duration));
            }
        }

        // Waits for a non-looping dance to finish, then reverts - but only
        // if `danceInstance` is still the active dance (i.e. nothing newer
        // has replaced it in the meantime).
        private IEnumerator EndDanceAfterDelay(GameObject danceInstance, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (current == danceInstance)
            {
                ReturnToNormal();
            }
        }

        // Destroys the dance instance, restores the player model, and
        // hands control back to the camera.
        private void ReturnToNormal()
        {
            if (current != null)
            {
                Destroy(current);
                current = null;
            }

            if (PlatformerMovement.Instance != null && PlatformerMovement.Instance.smr != null)
            {
                PlatformerMovement.Instance.smr.transform.parent.localScale = Vector3.one * 3;
            }

            if (CameraController.Instance != null)
            {
                CameraController.Instance.enabled = true;
            }

            danceEndRoutine = null;
        }

        private void CancelPendingReturn()
        {
            if (danceEndRoutine != null)
            {
                StopCoroutine(danceEndRoutine);
                danceEndRoutine = null;
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
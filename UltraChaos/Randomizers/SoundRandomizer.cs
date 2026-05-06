using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Ultrachaos.Randomizers
{
    internal class SoundRandomizer : Randomizer<AudioClip>
    {
        public static readonly SoundRandomizer Instance = new SoundRandomizer();

        private readonly System.Random _rng = new System.Random();
        protected override int NextIndex(int count) => _rng.Next(count);
        protected override int GetInstanceID(AudioClip item) => item.GetInstanceID();
        protected override RandomConfigValue GetConfigValue() => Plugin.ChangeSounds.Value;

        public override void Initialize()
        {
            Plugin.OnInstantiateMethod.Add((obj) =>
            {
                ProcessInstantiatedObject(obj);
            });
        }

        public static void Init()
        {
            if (Instance.Pool.Count > 0) return;
            Plugin.Instance.StartCoroutine(Instance.InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            AddRangeToPool(Resources.FindObjectsOfTypeAll<AudioClip>());
            Plugin.Logger.LogInfo($"[SoundRandomizer] Resources clips: {Pool.Count}");

            yield return GetAllAddressableClips();

            // Pool is now complete — wipe any mappings made during loading
            // so nothing is stuck to a clip picked from the incomplete pool
            ResetMappings();

            Plugin.Logger.LogInfo($"[SoundRandomizer] Total clips: {Pool.Count}");

            yield return new WaitForSeconds(0.1f);
            RandomizeAllExistingAudioSources();
            yield return new WaitForSeconds(0.1f);
            RandomizeAllExistingAudioSources();
        }
        static List<string> loggedMixerNames = new List<string>();
        private static bool IsMusic(AudioSource source)
        {
            if (source.outputAudioMixerGroup != null)
            {
                if (!loggedMixerNames.Contains(source.outputAudioMixerGroup.audioMixer.name))
                {
                    Plugin.Logger.LogInfo($"{source.outputAudioMixerGroup.audioMixer.name}");
                    loggedMixerNames.Add(source.outputAudioMixerGroup.audioMixer.name);
                }

            }


            return source.outputAudioMixerGroup != null &&
                   source.outputAudioMixerGroup.audioMixer.name == "MusicAudio";
        }

        public void RandomizeAllExistingAudioSources()
        {
            int changed = 0;
            foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (source == null || source.clip == null) continue;
                if (IsMusic(source)) continue;
                source.clip = GetRandom(source.clip);
                changed++;
            }
            Plugin.Logger.LogInfo($"[SoundRandomizer] Randomized {changed} existing AudioSources");
        }

        private IEnumerator GetAllAddressableClips()
        {
            yield return Addressables.InitializeAsync();

            var allLocations = new List<IResourceLocation>();
            foreach (var locator in Addressables.ResourceLocators)
            {
                var keys = locator.Keys.ToList();
                if (keys.Count == 0) continue;

                var locHandle = Addressables.LoadResourceLocationsAsync(
                    keys, Addressables.MergeMode.Union, typeof(AudioClip));
                yield return locHandle;

                if (locHandle.Status == AsyncOperationStatus.Succeeded)
                    allLocations.AddRange(locHandle.Result);

                Addressables.Release(locHandle);
            }

            allLocations = allLocations
                .GroupBy(l => l.InternalId)
                .Select(g => g.First())
                .ToList();

            Plugin.Logger.LogInfo($"[SoundRandomizer] Addressable clip locations: {allLocations.Count}");

            foreach (var location in allLocations)
            {
                AsyncOperationHandle<AudioClip> loadHandle = default;
                try
                {
                    loadHandle = Addressables.LoadAssetAsync<AudioClip>(location);
                }
                catch (System.Exception ex)
                {
                    continue;
                }

                yield return loadHandle;

                if (loadHandle.IsValid())
                {
                    if (loadHandle.Status == AsyncOperationStatus.Succeeded)
                        AddToPool(loadHandle.Result);
                    // else: silently skip — Unity already logged the failure internally
                    //       but we avoid the spam by releasing cleanly
                    Addressables.Release(loadHandle);
                }
            }
        }

        [HarmonyPatch(typeof(AudioSource), nameof(AudioSource.Play), new System.Type[] { })]
        [HarmonyPrefix]
        private static void PlayPrefix(AudioSource __instance)
        {
            if (__instance.clip != null && !IsMusic(__instance))
                __instance.clip = Instance.GetRandom(__instance.clip);
        }

        // Add these missing patches inside SoundRandomizer:

        [HarmonyPatch(typeof(AudioSource), nameof(AudioSource.Play), new[] { typeof(ulong) })]
        [HarmonyPrefix]
        private static void PlayDelayedUlongPrefix(AudioSource __instance)
        {
            if (__instance.clip != null && !IsMusic(__instance))
                __instance.clip = Instance.GetRandom(__instance.clip);
        }

        [HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayDelayed))]
        [HarmonyPrefix]
        private static void PlayDelayedPrefix(AudioSource __instance)
        {
            if (__instance.clip != null && !IsMusic(__instance))
                __instance.clip = Instance.GetRandom(__instance.clip);
        }

        [HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayOneShot), new[] { typeof(AudioClip) })]
        [HarmonyPrefix]
        private static void PlayOneShotPrefix(ref AudioClip clip, AudioSource __instance)
        {
            if (!IsMusic(__instance))
                clip = Instance.GetRandom(clip);
        }

        [HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayOneShot), new[] { typeof(AudioClip), typeof(float) })]
        [HarmonyPrefix]
        private static void PlayOneShotVolumePrefix(ref AudioClip clip, AudioSource __instance)
        {
            if (!IsMusic(__instance))
                clip = Instance.GetRandom(clip);
        }

        [HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayClipAtPoint))]
        [HarmonyPrefix]
        private static void PlayClipAtPointPrefix(ref AudioClip clip, AudioSource __instance)
        {
            if (!IsMusic(__instance))
                clip = Instance.GetRandom(clip);
        }

        [HarmonyPatch(typeof(AudioSource), "set_clip")]
        [HarmonyPrefix]
        private static void SetClipPrefix(AudioSource __instance, ref AudioClip value)
        {
            if (!IsMusic(__instance))
                value = Instance.GetRandom(value);
        }
        [HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayScheduled))]
        [HarmonyPrefix]
        private static void PlayScheduledPrefix(AudioSource __instance)
        {
            if (__instance.clip != null && !IsMusic(__instance))
                __instance.clip = Instance.GetRandom(__instance.clip);
        }
        private static void ProcessInstantiatedObject(GameObject go)
        {
            if (go != null)
            {
                // Search children for any AudioSources on the new object
                var sources = go.GetComponentsInChildren<AudioSource>(true);
                foreach (var source in sources)
                {
                    if (source.clip != null && !IsMusic(source))
                    {
                        source.clip = Instance.GetRandom(source.clip);
                    }
                }
            }
        }
    }
}
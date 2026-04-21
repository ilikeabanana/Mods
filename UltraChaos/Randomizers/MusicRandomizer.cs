using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Ultrachaos.Randomizers
{
    public class MusicRandomizer : Randomizer<AudioClip>
    {
        protected override RandomConfigValue GetConfigValue()
        {
            return Plugin.ChangeMusic.Value;
        }

        protected override int GetInstanceID(AudioClip item)
        {
            return item.GetInstanceID();
        }

        public static MusicRandomizer Instance = new MusicRandomizer();

        public static void Init()
        {

            ReplaceThings();

            Plugin.Instance.StartCoroutine(Instance.ReplaceEveryhtingPlease());
        }

        public static void FillPool()
        {
            if (Instance.Pool.Count <= 0)
                Plugin.Instance.StartCoroutine(Instance.GetAllMusic());

        }
        public IEnumerator ReplaceEveryhtingPlease()
        {
            yield return new WaitForSecondsRealtime(0.1f);
            ReplaceThings();
        }

        public static void ReplaceThings()
        {
            MusicManager man = MusicManager.Instance;
            if (man.bossTheme.clip != null)
                man.bossTheme.clip = Instance.GetRandom(man.bossTheme.clip);
            if (man.cleanTheme.clip != null)
                man.cleanTheme.clip = Instance.GetRandom(man.cleanTheme.clip);
            if (man.battleTheme.clip != null)
                man.battleTheme.clip = Instance.GetRandom(man.battleTheme.clip);

            MusicChanger[] changers = Object.FindObjectsByType<MusicChanger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var change in changers)
            {
                change.battle = Instance.GetRandom(change.battle);
                change.boss = Instance.GetRandom(change.boss);
                change.clean = Instance.GetRandom(change.clean);
            }

            var alreadyReplaced = new HashSet<AudioSource>();

            alreadyReplaced.Add(man.bossTheme);
            alreadyReplaced.Add(man.cleanTheme);
            alreadyReplaced.Add(man.battleTheme);

            foreach (var change in changers)
            {
                var src = change.GetComponent<AudioSource>();
                if (src != null) alreadyReplaced.Add(src);
            }

            AudioSource[] allSources = Object.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var source in allSources)
            {
                if (alreadyReplaced.Contains(source)) continue;   // handled above
                if (source.clip == null) continue;                // nothing to replace
                if (!IsMusic(source)) continue;                   // not a music bus

                source.clip = Instance.GetRandom(source.clip);
            }
        }
        private static bool IsMusic(AudioSource source)
        {

            return source.outputAudioMixerGroup != null &&
                   source.outputAudioMixerGroup.audioMixer.name == "MusicAudio";
        }
        public IEnumerator GetAllMusic()
        {
            // Make sure addressables are initialized
            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            // Collect every resource location that resolves to a SpawnableObject
            var allLocations = new List<IResourceLocation>();

            foreach (var locator in Addressables.ResourceLocators)
            {
                // locator.Keys can contain strings, Guids, etc.
                var keys = locator.Keys.ToList();

                if (keys.Count == 0) continue;

                var locHandle = Addressables.LoadResourceLocationsAsync(
                    keys,
                    Addressables.MergeMode.Union,
                    typeof(SoundtrackSong)
                );
                yield return locHandle;

                if (locHandle.Status == AsyncOperationStatus.Succeeded)
                    allLocations.AddRange(locHandle.Result);

                Addressables.Release(locHandle);
            }

            // Deduplicate (same asset can appear under multiple keys)
            allLocations = allLocations
                .GroupBy(l => l.InternalId)
                .Select(g => g.First())
                .ToList();

            Plugin.Logger.LogInfo($"Found {allLocations.Count} Songs locations");

            foreach (var location in allLocations)
            {
                var loadHandle = Addressables.LoadAssetAsync<SoundtrackSong>(location);
                yield return loadHandle;

                if (loadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    AddRangeToPool(loadHandle.Result.clips);

                }
            }
        }

    }
}
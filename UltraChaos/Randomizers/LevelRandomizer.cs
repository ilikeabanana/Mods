using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Ultrachaos.Randomizers
{
    [HarmonyPatch(typeof(SceneHelper))]
    public class LevelRandomizer : Randomizer<string>
    {
        public static LevelRandomizer Instance = new LevelRandomizer();
        public static void Init()
        {
            Plugin.Instance.StartCoroutine(InitAddressables());
        }
        public static IEnumerator InitAddressables()
        {
            yield return Addressables.InitializeAsync();

            var allLocations = new List<IResourceLocation>();
            foreach (var locator in Addressables.ResourceLocators)
            {
                var keys = locator.Keys.ToList();
                if (keys.Count == 0) continue;

                var locHandle = Addressables.LoadResourceLocationsAsync(
                    keys, Addressables.MergeMode.Union, typeof(SceneInstance));
                yield return locHandle;

                if (locHandle.Status == AsyncOperationStatus.Succeeded)
                    allLocations.AddRange(locHandle.Result);

                Addressables.Release(locHandle);
            }

            // Deduplicate
            allLocations = allLocations
                .GroupBy(l => l.InternalId)
                .Select(g => g.First())
                .ToList();

            Plugin.Logger.LogInfo($"[LevelRandomizer] Found {allLocations.Count} scene locations");

            foreach (var location in allLocations)
            {
                string sceneName = SceneHelper.SanitizeLevelPath(location.InternalId);
                if (sceneName == "Main Menu" || sceneName == "Bootstrap" || sceneName == "Intro") continue;
                Instance.AddToPool(sceneName);
                Plugin.Logger.LogInfo($"[LevelRandomizer] Registered: {sceneName}");
            }
        }

        public static void LoadRandomScene()
        {
            SceneHelper.LoadScene(Instance.Pool[Random.Range(0, Instance.Pool.Count)]);
        }

        [HarmonyPatch(typeof(SceneHelper), nameof(SceneHelper.LoadScene))]
        [HarmonyPrefix]
        public static void RandomizeScene(ref string sceneName)
        {
            if (sceneName == "Main Menu" || sceneName == "Bootstrap" || sceneName == "Intro") return;
            sceneName = Instance.GetRandom(sceneName);
        }

        protected override RandomConfigValue GetConfigValue()
        {
            return Plugin.RandomizeLevels.Value;
        }

        protected override int GetInstanceID(string item)
        {
            return item.GetHashCode();
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class ResourceLoader : MonoBehaviour
{
    private const string ExplosionPath = "Assets/Prefabs/Attacks and Projectiles/Explosions/";
    public static bool IsDone = false;
    private static Dictionary<string, GameObject> loadedExplosions = new Dictionary<string, GameObject>();

    public static IEnumerator GetExplosions(List<string> explosionKeys)
    {
        yield return LoadExplosions(explosionKeys);
    }

    public static IEnumerator GetExplosionPKeys(Action<List<string>> callback)
    {
        yield return GetAllPKeys(keys =>
        {
            var filteredKeys = keys.Where(k => k.StartsWith(ExplosionPath)).ToList();
            callback?.Invoke(filteredKeys);
        });
    }

    public static IEnumerator GetAllPKeys(Action<List<string>> callback)
    {
        AsyncOperationHandle<IResourceLocator> handle = Addressables.InitializeAsync();
        yield return handle;

        List<string> keys = new List<string>();
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            IResourceLocator result = handle.Result;
            foreach (object obj in ((ResourceLocationMap)result).Keys)
            {
                keys.Add(obj as string);
            }
        }
        else
        {
            Debug.LogError("Addressables initialization failed: " + handle.OperationException);
        }

        callback?.Invoke(keys);
    }

    private static IEnumerator LoadExplosions(List<string> explosionKeys)
    {
        AsyncOperationHandle<IResourceLocator> handle = Addressables.InitializeAsync();
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            IResourceLocator result = handle.Result;
            foreach (object obj in ((ResourceLocationMap)result).Keys)
            {
                string key = obj as string;
                if (string.IsNullOrEmpty(key) || !explosionKeys.Contains(key))
                    continue;

                AsyncOperationHandle<IList<IResourceLocation>> locationHandle = Addressables.LoadResourceLocationsAsync(key, typeof(GameObject));
                yield return locationHandle;

                if (locationHandle.Status == AsyncOperationStatus.Succeeded && locationHandle.Result.Count > 0)
                {
                    AsyncOperationHandle<GameObject> assetHandle = Addressables.LoadAssetAsync<GameObject>(key);
                    yield return assetHandle;

                    if (assetHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        GameObject explosion = assetHandle.Result;
                        if (explosion != null)
                        {
                            loadedExplosions[key.Replace(ExplosionPath, "")] = explosion;
                            Debug.Log("Loaded: " + key);
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to load explosion: " + key);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Addressables initialization failed: " + handle.OperationException);
        }

        IsDone = true;
    }

    public static Dictionary<string, GameObject> GetLoadedExplosions()
    {
        return loadedExplosions;
    }
}

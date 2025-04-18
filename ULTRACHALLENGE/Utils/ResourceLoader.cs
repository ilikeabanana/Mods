using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ResourceLoader
{
    private const string ResourcePath = "ULTRACHALLENGE.TXTFiles.Resource_Locations.txt";
    private const string PKeyPattern = @"PKEY:\s+(.*)";

    public static bool isDone = false;

    public static Dictionary<string, GameObject> getExplosions(List<string> explosionKeys)
    {
        return LoadExplosions(explosionKeys);
    }

    public static List<string> GetExplosionPKeys()
    {
        List<string> pKeys = new List<string>();

        string textAsset = LoadEmbeddedResource(ResourcePath);
        if (textAsset == null)
        {
            Debug.LogError("Failed to load embedded resource: " + ResourcePath);
            return pKeys;
        }

        // Read line by line, extracting only PKEY values
        string[] lines = textAsset.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            Match match = Regex.Match(line, PKeyPattern);
            if (match.Success)
            {
                pKeys.Add(match.Groups[1].Value.Trim());
            }
        }
        return pKeys;
    }

    public static List<(string fullPath, string shortPath)> GetAllPKeys()
    {
        List<(string fullPath, string shortPath)> pKeys = new List<(string, string)>();

        string textAsset = LoadEmbeddedResource(ResourcePath);
        if (textAsset == null)
        {
            Debug.LogError("Failed to load embedded resource: " + ResourcePath);
            return pKeys;
        }

        // Read line by line, extracting PKEY values
        string[] lines = textAsset.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            Match match = Regex.Match(line, PKeyPattern);
            if (match.Success)
            {
                string fullPath = match.Groups[1].Value.Trim();
                string shortPath = Path.GetFileNameWithoutExtension(fullPath);
                pKeys.Add((fullPath, shortPath));
            }
        }
        return pKeys;
    }

    private static Dictionary<string, GameObject> LoadExplosions(List<string> explosionKeys)
    {
        Dictionary<string, GameObject> toReturn = new Dictionary<string, GameObject>();
        foreach (string key in explosionKeys)
        {
            Addressables.LoadAssetAsync<GameObject>(key).Completed += handle =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    Debug.Log("Loaded: " + key);
                    toReturn.Add(key.Replace("Assets/Prefabs/Attacks and Projectiles/Explosions/", ""), handle.Result);
                }
                else
                {
                    Debug.LogError("Failed to load: " + key);
                }
            };
        }
        isDone = true;
        return toReturn;
    }

    private static string LoadEmbeddedResource(string resourceName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Debug.LogError("Resource not found: " + resourceName);
                    return null;
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading embedded resource: " + ex.Message);
            return null;
        }
    }
}

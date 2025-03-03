using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public static class AnimationLoader
{
    private const string FolderName = "V1_ANIMS";
    private static string FolderPath => Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), FolderName);
    private static List<AnimationClip> animationClips = new List<AnimationClip>();

    public static void LoadAnimations(Animator animator, AnimationClip fallbackClip = null)
    {
        if (animator == null)
        {
            Debug.LogError("Animator is null.");
            return;
        }

        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
        }

        animationClips.Clear();

        foreach (string file in Directory.GetFiles(FolderPath, "*.json"))
        {
            string jsonData = File.ReadAllText(file);
            AnimationClip clip = JsonToAnimationClip(jsonData);
            if (clip != null)
            {
                animationClips.Add(clip);
            }
        }

        // If no JSON files were found and a fallback clip is provided, use it
        if (animationClips.Count == 0 && fallbackClip != null)
        {
            animationClips.Add(fallbackClip);
        }
    }

    private static AnimationClip JsonToAnimationClip(string jsonData)
    {
        try
        {
            AnimationData data = JsonConvert.DeserializeObject<AnimationData>(jsonData);
            AnimationClip clip = new AnimationClip();

            foreach (var curveData in data.curves)
            {
                AnimationCurve curve = new AnimationCurve();
                foreach (var key in curveData.keys)
                {
                    curve.AddKey(key.time, key.value);
                }
                clip.SetCurve("", typeof(Transform), curveData.property, curve);
            }

            return clip;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse animation JSON: {ex.Message}");
            return null;
        }
    }

    public static List<AnimationClip> GetAnimationClips()
    {
        return animationClips;
    }
}

[Serializable]
public class AnimationData
{
    public List<CurveData> curves;
}

[Serializable]
public class CurveData
{
    public string property;
    public List<KeyframeData> keys;
}

[Serializable]
public class KeyframeData
{
    public float time;
    public float value;
}

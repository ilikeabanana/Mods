using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Replace_Notes
{
    public static class CatAudioReplacer
    {
        private sealed class Mapping
        {
            public AudioClip Replacement;
            public float Pitch = 1f;
            public float Volume = 1f;
        }

        private static readonly Dictionary<string, Mapping> mappings =
            new Dictionary<string, Mapping>(
                StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> seen =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<AudioClip> replacementClips =
            new HashSet<AudioClip>();

        public static bool LogClipNames = true;

        public static void Register(
            string originalName,
            AudioClip replacement,
            float pitch = 1f,
            float volume = 1f)
        {
            if (string.IsNullOrWhiteSpace(originalName))
                throw new ArgumentException("Clip name is required.");

            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            string key = Normalize(originalName);

            // Remove old replacement from the guard set if overwritten.
            if (mappings.TryGetValue(key, out Mapping old))
                replacementClips.Remove(old.Replacement);

            mappings[key] = new Mapping
            {
                Replacement = replacement,
                Pitch = Mathf.Clamp(pitch, 0.1f, 3f),
                Volume = Mathf.Clamp01(volume)
            };

            replacementClips.Add(replacement);

            Plugin.Log?.LogInfo(
                $"Mapped audio '{key}' -> '{replacement.name}'");
        }

        public static void Unregister(string originalName)
        {
            string key = Normalize(originalName);

            if (mappings.TryGetValue(key, out Mapping mapping))
            {
                replacementClips.Remove(mapping.Replacement);
                mappings.Remove(key);
            }
        }

        public static bool IsReplacement(AudioClip clip)
        {
            return clip != null && replacementClips.Contains(clip);
        }

        public static bool TryReplace(
            AudioClip original,
            out AudioClip replacement,
            out float pitch,
            out float volume)
        {
            replacement = original;
            pitch = 1f;
            volume = 1f;

            if (original == null)
                return false;

            // Never remap one of our own replacement clips.
            if (IsReplacement(original))
                return false;

            string key = Normalize(original.name);

            if (LogClipNames && seen.Add(key))
                Plugin.Log?.LogInfo($"Audio clip observed: {key}");

            if (!mappings.TryGetValue(key, out Mapping mapping))
                return false;

            replacement = mapping.Replacement;
            pitch = mapping.Pitch;
            volume = mapping.Volume;

            return replacement != null;
        }

        private static string Normalize(string name)
        {
            return name.Trim();
        }
    }
}

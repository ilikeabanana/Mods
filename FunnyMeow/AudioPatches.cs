
using HarmonyLib;
using Replace_Notes;
using UnityEngine;

namespace Replace_Notes
{
    [HarmonyPatch(
        typeof(AudioSource),
        nameof(AudioSource.PlayOneShot),
        new[] { typeof(AudioClip) })]
    internal static class PlayOneShotSinglePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            AudioSource __instance,
            ref AudioClip clip)
        {
            if (!CatAudioReplacer.TryReplace(
                clip,
                out AudioClip replacement,
                out float pitch,
                out float volume))
                return true;

            AudioPatchState.PlayReplacement(
                __instance, replacement, pitch, volume);

            // Skip the original PlayOneShot call.
            return false;
        }
    }

    [HarmonyPatch(
        typeof(AudioSource),
        nameof(AudioSource.PlayOneShot),
        new[] { typeof(AudioClip), typeof(float) })]
    internal static class PlayOneShotVolumePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            AudioSource __instance,
            ref AudioClip clip,
            ref float volumeScale)
        {
            if (!CatAudioReplacer.TryReplace(
                clip,
                out AudioClip replacement,
                out float pitch,
                out float volume))
                return true;

            AudioPatchState.PlayReplacement(
                __instance,
                replacement,
                pitch,
                volume * volumeScale);

            // Skip the original PlayOneShot call.
            return false;
        }
    }
}

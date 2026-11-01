using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Replace_Notes
{
    internal static class AudioPatchState
    {
        public static void PlayReplacement(
            AudioSource originalSource,
            AudioClip clip,
            float pitch,
            float volume)
        {
            if (originalSource == null || clip == null)
                return;

            GameObject obj = new GameObject("ULTRAMEOW_Replacement");
            obj.transform.SetParent(originalSource.transform, false);

            AudioSource proxy = obj.AddComponent<AudioSource>();

            // Preserve important spatial and routing properties.
            proxy.outputAudioMixerGroup =
                originalSource.outputAudioMixerGroup;

            proxy.spatialBlend = originalSource.spatialBlend;
            proxy.panStereo = originalSource.panStereo;
            proxy.priority = originalSource.priority;
            proxy.dopplerLevel = originalSource.dopplerLevel;
            proxy.spread = originalSource.spread;
            proxy.minDistance = originalSource.minDistance;
            proxy.maxDistance = originalSource.maxDistance;
            proxy.rolloffMode = originalSource.rolloffMode;
            proxy.reverbZoneMix = originalSource.reverbZoneMix;

            proxy.bypassEffects = originalSource.bypassEffects;
            proxy.bypassListenerEffects =
                originalSource.bypassListenerEffects;
            proxy.bypassReverbZones = originalSource.bypassReverbZones;

            proxy.mute = originalSource.mute;
            proxy.loop = false;
            proxy.playOnAwake = false;

            proxy.clip = clip;
            proxy.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            proxy.volume = Mathf.Clamp01(
                originalSource.volume * volume);

            proxy.Play();

            float playbackTime = clip.length / proxy.pitch;
            Object.Destroy(obj, playbackTime + 0.25f);
        }
    }
}

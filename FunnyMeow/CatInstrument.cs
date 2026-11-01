
using System.Collections.Generic;
using UnityEngine;

namespace Replace_Notes
{
    public class CatInstrument : MonoBehaviour
    {
        public AudioClip MeowSample;
        public int RootMidiNote = 60; // C4
        public int VoiceLimit = 32;
        public float MasterVolume = 1f;

        private readonly List<AudioSource> voices =
            new List<AudioSource>();

        private readonly Dictionary<AudioSource, float> noteEnds =
            new Dictionary<AudioSource, float>();

        public void Initialize(AudioClip sample)
        {
            MeowSample = sample;
        }

        public void PlayNote(
            int midiNote,
            float velocity = 1f,
            float duration = 0.25f,
            float pan = 0f)
        {
            if (MeowSample == null)
                return;

            AudioSource voice = GetVoice();

            if (voice == null)
                return;

            // MIDI pitch difference converted to playback rate.
            voice.clip = MeowSample;
            voice.pitch = Mathf.Pow(
                2f, (midiNote - RootMidiNote) / 12f);

            voice.volume = Mathf.Clamp01(
                velocity * MasterVolume);

            voice.panStereo = Mathf.Clamp(pan, -1f, 1f);
            voice.loop = false;
            voice.time = 0f;

            voice.Play();

            // duration controls the note gate, not sample time-stretch.
            noteEnds[voice] = Time.unscaledTime +
                Mathf.Max(0.01f, duration);
        }

        private AudioSource GetVoice()
        {
            // Reuse an idle voice first.
            foreach (AudioSource voice in voices)
            {
                if (!voice.isPlaying)
                    return voice;
            }

            // Create another voice if capacity allows.
            if (voices.Count < Mathf.Max(1, VoiceLimit))
            {
                GameObject obj = new GameObject("CatVoice");
                obj.transform.SetParent(transform, false);

                AudioSource source = obj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;

                voices.Add(source);
                return source;
            }

            // All voices busy: steal the voice ending soonest.
            AudioSource oldest = null;
            float earliest = float.MaxValue;

            foreach (AudioSource voice in voices)
            {
                float end = noteEnds.TryGetValue(
                    voice, out float value)
                    ? value : 0f;

                if (end < earliest)
                {
                    earliest = end;
                    oldest = voice;
                }
            }

            if (oldest != null)
                oldest.Stop();

            return oldest;
        }

        private void Update()
        {
            // Stop sustained samples when their note gate expires.
            foreach (AudioSource voice in voices)
            {
                if (voice.isPlaying &&
                    noteEnds.TryGetValue(voice, out float end) &&
                    Time.unscaledTime >= end)
                {
                    voice.Stop();
                }
            }
        }

        public void StopAllNotes()
        {
            foreach (AudioSource voice in voices)
                voice.Stop();

            noteEnds.Clear();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Replace_Notes
{
    public class CatMusicPlayer : MonoBehaviour
    {
        public CatInstrument Instrument;

        [Serializable]
        public class NoteEvent
        {
            public float StartTime;
            public float Duration = 0.2f;
            public int MidiNote = 60;
            public float Velocity = 1f;
            public float Pan = 0f;
        }

        public NoteEvent[] Notes = new NoteEvent[0];

        private Coroutine playback;
        private bool stopRequested;

        public void Play()
        {
            Stop();

            if (Instrument == null || Notes == null)
                return;

            stopRequested = false;
            playback = StartCoroutine(PlaySequence());
        }

        public void Stop()
        {
            stopRequested = true;

            if (playback != null)
            {
                StopCoroutine(playback);
                playback = null;
            }

            if (Instrument != null)
                Instrument.StopAllNotes();
        }

        private IEnumerator PlaySequence()
        {
            // Sort a copy so the supplied data stays unchanged.
            NoteEvent[] sorted = (NoteEvent[])Notes.Clone();

            Array.Sort(sorted, (a, b) =>
                a.StartTime.CompareTo(b.StartTime));

            float songStart = Time.unscaledTime;
            int index = 0;

            while (index < sorted.Length && !stopRequested)
            {
                float elapsed = Time.unscaledTime - songStart;

                // Trigger every note that is due.
                while (index < sorted.Length &&
                       sorted[index].StartTime <= elapsed)
                {
                    NoteEvent note = sorted[index++];

                    if (note != null)
                    {
                        Instrument.PlayNote(
                            note.MidiNote,
                            note.Velocity,
                            note.Duration,
                            note.Pan);
                    }
                }

                yield return null;
            }

            playback = null;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Specialist_Dance
{
    public class EmoteWheel : MonoBehaviour
    {
        public static EmoteWheel Instance { get; private set; }

        // Same public fields WeaponWheel exposes, copied over by the
        // Harmony patch when this component is created.
        public int segmentCount;
        public GameObject clickSound;
        public GameObject background;

        // KeyCode that closes the wheel and confirms the selection.
        // Change this to whatever you want to actually hold down to open it.
        public KeyCode openKey = KeyCode.H;

        private readonly List<EmoteSegment> segments = new List<EmoteSegment>();
        private List<EmoteEntry> emotes;

        private int selectedSegment = -1;
        private int lastSelectedSegment = -1;
        private Vector2 direction;

        private void Awake()
        {
            Instance = this;

            // WeaponWheel normally does this in its own Start(), but we
            // destroy the cloned WeaponWheel component before its Start()
            // ever runs (Start hasn't fired yet on a same-frame Instantiate
            // + Destroy), so the background art never gets switched on
            // unless we do it ourselves here.
            if (background != null)
            {
                background.SetActive(true);
            }

            Plugin.Logger.LogInfo("EmoteWheel.Awake - background=" + (background != null)
                + " clickSound=" + (clickSound != null)
                + " segmentCount=" + segmentCount);

            foreach (Transform child in transform)
            {
                if (child.gameObject.name.Contains("Segment"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            if (MonoSingleton<InputManager>.Instance == null)
            {
                return;
            }

            Time.timeScale = 0.25f;
            if (MonoSingleton<TimeController>.Instance)
            {
                MonoSingleton<TimeController>.Instance.timeScaleModifier = 0.25f;
            }

            selectedSegment = -1;
            direction = Vector2.zero;

            GameStateManager.Instance.RegisterState(new GameState("emote-wheel", gameObject)
            {
                timerModifier = 4f,
                cameraInputLock = LockMode.Lock
            });
        }

        private void OnDisable()
        {
            if (MonoSingleton<TimeController>.Instance)
            {
                MonoSingleton<TimeController>.Instance.timeScaleModifier = 1f;
                MonoSingleton<TimeController>.Instance.RestoreTime();
                CameraController.Instance.enabled = true;
            }
        }

        private void Update()
        {
            if (MonoSingleton<OptionsManager>.Instance.paused
                || MonoSingleton<NewMovement>.Instance.dead
                || GameStateManager.Instance.PlayerInputLocked)
            {
                gameObject.SetActive(false);
                return;
            }

            // Release the open key to confirm the current selection and close.
            if (Input.GetKeyUp(openKey))
            {

                if (selectedSegment != -1)
                {
                    PlaySelectedEmote();
                }
                gameObject.SetActive(false);
                return;
            }

            if (segments.Count == 0)
            {
                return;
            }

            direction = Vector2.ClampMagnitude(
                direction + MonoSingleton<InputManager>.Instance.InputSource.WheelLook.ReadValue<Vector2>(),
                1f);

            float angle = Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg + 90f, 360f);
            if (Mathf.Approximately(angle, 360f))
            {
                angle = 0f;
            }

            selectedSegment = direction.sqrMagnitude > 0f
                ? (int)(angle / (360f / segmentCount))
                : selectedSegment;

            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].SetActive(i == selectedSegment);
            }

            if (selectedSegment != lastSelectedSegment)
            {
                if (clickSound)
                {
                    Instantiate(clickSound);
                }
                lastSelectedSegment = selectedSegment;
                if (MonoSingleton<RumbleManager>.Instance)
                {
                    MonoSingleton<RumbleManager>.Instance.SetVibration(RumbleProperties.WeaponWheelTick);
                }
            }
        }

        public void Show()
        {
            if (gameObject.activeSelf)
            {
                return;
            }
            lastSelectedSegment = -1;
            gameObject.SetActive(true);
        }

        // Builds/rebuilds the wheel's segments from a list of emotes.
        // Call this once your bundle-loaded clips are ready (mirrors
        // WeaponWheel.SetSegments, minus the weapon-icon specifics).
        public void SetEmotes(List<EmoteEntry> newEmotes)
        {
            emotes = newEmotes;
            segmentCount = emotes.Count;
            lastSelectedSegment = -1;

            foreach (EmoteSegment seg in segments)
            {
                seg.DestroySegment();
            }
            segments.Clear();

            for (int j = 0; j < segmentCount; j++)
            {
                UICircle circle = new GameObject().AddComponent<UICircle>();
                circle.name = "EmoteSegment " + j;
                circle.Arc = 1f / segmentCount - 0.005f;
                circle.ArcRotation = (int)(360f * ((float)j / segmentCount) + 1.8f);
                circle.Fill = false;
                circle.transform.SetParent(transform, false);
                circle.rectTransform.anchorMin = Vector2.zero;
                circle.rectTransform.anchorMax = Vector2.one;
                circle.rectTransform.anchoredPosition = Vector2.zero;
                circle.rectTransform.sizeDelta = Vector2.zero;

                Outline outline = circle.gameObject.AddComponent<Outline>();
                outline.effectDistance = new Vector2(2f, -2f);
                outline.effectColor = Color.white;

                float segAngle = j * 360f / segmentCount;
                float halfArc = circle.Arc * 360f / 2f;
                float midAngle = segAngle + halfArc;
                float rad = midAngle * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(-Mathf.Cos(rad), Mathf.Sin(rad)) * 112f;

                GameObject labelObj = new GameObject("Label " + j);
                labelObj.transform.SetParent(circle.transform, false);
                Text label = labelObj.AddComponent<Text>();
                label.text = emotes[j].displayName;
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.transform.localPosition = pos;
                label.rectTransform.sizeDelta = new Vector2(150f, 40f);

                // Outline component works on any Graphic, Text included -
                // same approach as the segment's own outline above.
                Outline labelOutline = labelObj.AddComponent<Outline>();
                labelOutline.effectDistance = new Vector2(1.5f, -1.5f);
                labelOutline.effectColor = Color.black;

                // If you have icon sprites for each emote, swap the Text
                // above for an Image the same way WeaponWheel does it -
                // just add an Image component here and assign
                // emotes[j].icon to it instead.

                EmoteSegment segment = new EmoteSegment
                {
                    segment = circle,
                    label = label,
                    entry = emotes[j]
                };
                segments.Add(segment);
                segment.SetActive(false);
            }
        }

        private void PlaySelectedEmote()
        {
            if (emotes == null || selectedSegment < 0 || selectedSegment >= emotes.Count)
            {
                return;
            }
            Plugin.Instance.PlayDance(emotes[selectedSegment].clip, emotes[selectedSegment].audio);
        }
    }

    // One entry in the wheel: a dance clip plus its display name/audio/icon.
    public class EmoteEntry
    {
        public string displayName;
        public AnimationClip clip;
        public AudioClip audio;
        public Sprite icon; // optional, unused unless you wire up an Image
    }

    // Analogue of WeaponWheel's private WheelSegment, adapted for emotes.
    public class EmoteSegment 
    {
        public UICircle segment;
        public Text label;
        public EmoteEntry entry;

        public void SetActive(bool active)
        {
            if (segment) segment.gameObject.SetActive(active);
        }

        public void DestroySegment()
        {
            if (segment) Object.Destroy(segment.gameObject);
        }
    }
}
using BepInEx;
using BepInEx.Logging;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using HarmonyLib;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;

namespace KawKaw
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public enum Character
        {
            KawKaw,
            Flowery,
            Queen,
            ShadowMantle,
            Random,
        }

        public static EnumField<Character> character;
        public static IntField amount;

        internal static new ManualLogSource Logger { get; private set; } = null!;

        static List<GameObject> kawkaws = new List<GameObject>();

        public static Texture2D kawKaw;
        public static Texture2D kawKawwhite;

        public const float SpriteScale = 3.75f;

        Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        public static BoolField TrailEnabled;
        public static FloatField TrailLifetime;
        public static FloatField HueShiftSpeed;

        public static BoolField Muted;
        public static FloatField MinPitch;
        public static FloatField MaxPitch;
        public static FloatField Volume;

        public static BoolField PhysicsBounce;
        public static FloatField restitution;
        public static BoolField BounceOnOthers;

        public static BoolField TriggerOnDamage;

        public static BoolField TriggerEverySeconds;
        public static FloatField TriggerInterval;

        public static BoolField TriggerOnKey;
        public static KeyCodeField TriggerKey;

        float triggerTimer;

        private void Awake()
        {
            Harmony.PatchAll();
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            gameObject.hideFlags = HideFlags.DontSaveInEditor;
            //CreateUI();

            SetupConfigs();

            FrameSet baseline = GetOrLoadFrames(Character.KawKaw);
            kawKaw = baseline.idle.Count > 0 ? baseline.idle[0].texture : null;
            kawKawwhite = baseline.idleWhite.Count > 0 ? baseline.idleWhite[0].texture : null;
            config.icon = Sprite.Create(kawKaw, new Rect(0, 0, kawKaw.width, kawKaw.height), new Vector2(0.5f, 0.5f));
        }
        PluginConfigurator config;

        void SetupConfigs()
        {
            config = PluginConfigurator.Create("Bouncing Deltarune", MyPluginInfo.PLUGIN_GUID);

            amount = new IntField(config.rootPanel, "Count", "com.kawkaw.count", 0, false);
            character = new EnumField<Character>(config.rootPanel, "Character", "com.kawkaw.character", Character.KawKaw, true);
            ConfigPanel physicsPanel = new ConfigPanel(config.rootPanel, "Physics", "com.physics.panel");

            PhysicsBounce = new BoolField(physicsPanel, "Physics Bounce", "com.physics.bounce", false, true);
            BounceOnOthers = new BoolField(physicsPanel, "Bounce on Other", "com.others.bounce", true, true);
            restitution = new FloatField(physicsPanel, "Restitution", "com.restitution", 1, true);

            ConfigPanel soundSettings = new ConfigPanel(config.rootPanel, "Sound Settings", "com.sound.settings.panel");

            Muted = new BoolField(soundSettings, "Muted", "com.sound.muted", false);
            MinPitch = new FloatField(soundSettings, "Minimum Pitch", "com.sound.minpitch", 0.9f);
            MaxPitch = new FloatField(soundSettings, "Maximum Pitch", "com.sound.maxpitch", 1.1f);
            Volume = new FloatField(soundSettings, "Volume", "com.sound.volume", 1);


            ConfigPanel trailSettings = new ConfigPanel(config.rootPanel, "Trail Settings", "com.kawkaw.panel.trail");

            TrailEnabled = new BoolField(trailSettings, "Enabled", "com.enabled.trail", true);
            TrailLifetime = new FloatField(trailSettings, "Lifetime", "com.lifetime.trail", 0.5f);
            HueShiftSpeed = new FloatField(trailSettings, "Hue Shift Speed", "com.hueshift.trail", 0.5f);


            ConfigPanel triggerPanel = new ConfigPanel(config.rootPanel, "Triggers", "com.triggers.panel");

            TriggerOnDamage = new BoolField(
                triggerPanel,
                "On Damage",
                "com.trigger.damage",
                true);

            TriggerEverySeconds = new BoolField(
                triggerPanel,
                "Every Few Seconds",
                "com.trigger.timer.enabled",
                false);

            TriggerInterval = new FloatField(
                triggerPanel,
                "Interval",
                "com.trigger.timer.interval",
                2f);

            TriggerOnKey = new BoolField(
                triggerPanel,
                "On Key Press",
                "com.trigger.key.enabled",
                false);

            TriggerKey = new KeyCodeField(
                triggerPanel,
                "Key",
                "com.trigger.key",
                KeyCode.K);

            amount.onValueChange += Amount_onValueChange;
            character.onValueChange += Character_onValueChange;
        }

        private void Amount_onValueChange(IntField.IntValueChangeEvent data)
        {
            if (kawkaws.Count == data.value) return;
            if (kawkaws.Count < data.value)
            {
                int amount = data.value - kawkaws.Count;
                for (int i = 0; i < amount; i++)
                {
                    CreateUI();
                }
            }
            else
            {
                int amount = kawkaws.Count - data.value;
                for (int i = 0; i < amount; i++)
                {
                    GameObject kawkaw = kawkaws[0];
                    kawkaws.Remove(kawkaw);

                    Destroy(kawkaw);
                }
            }
        }

        static GameObject sharedCanvasObj;

        static GameObject GetOrCreateCanvas()
        {
            if (sharedCanvasObj != null) return sharedCanvasObj;

            sharedCanvasObj = new GameObject("KawKawCanvas");
            Canvas canvas = sharedCanvasObj.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = sharedCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            sharedCanvasObj.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(sharedCanvasObj);
            return sharedCanvasObj;
        }

        private void Character_onValueChange(EnumField<Character>.EnumValueChangeEvent data)
        {
            Logger.LogInfo($"Character selection changed to {data.value}.");
            Nyon.RefreshAllFrames();
        }

        Texture2D LoadTextureFromResourceName(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Logger.LogWarning($"[ItemIcon] Resource not found: {resourceName}");
                    return null;
                }

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(buffer);
                texture.filterMode = FilterMode.Point;
                texture.name = resourceName;

                return texture;
            }
        }

        List<Sprite> LoadFrameSet(string folder, bool whiteSuffix, Character ch)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string characterName = ch.ToString();
            string prefix = $"KawKaw.Characters.{characterName}.{folder}.";

            string pattern = whiteSuffix
                ? $@"^{Regex.Escape(prefix)}.+_idle_(\d+)_white\.png$"
                : $@"^{Regex.Escape(prefix)}.+_idle_(\d+)\.png$";

            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            var frames = new SortedDictionary<int, Sprite>();

            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                Match match = regex.Match(resourceName);
                if (!match.Success) continue;

                int index = int.Parse(match.Groups[1].Value);

                Texture2D tex = LoadTextureFromResourceName(resourceName);
                if (tex == null) continue;

                Sprite sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
                sprite.name = resourceName;

                frames[index] = sprite;
            }

            return frames.Values.ToList();
        }

        public struct FrameSet
        {
            public List<Sprite> idle;
            public List<Sprite> idleWhite;
        }

        static Dictionary<Character, FrameSet> frameCache = new Dictionary<Character, FrameSet>();

        public FrameSet GetOrLoadFrames(Character ch)
        {
            if (frameCache.TryGetValue(ch, out FrameSet cached))
                return cached;

            FrameSet set = new FrameSet
            {
                idle = LoadFrameSet("Normal", whiteSuffix: false, ch),
                idleWhite = LoadFrameSet("White", whiteSuffix: true, ch)
            };

            if (set.idle.Count == 0)
                Logger.LogWarning($"No idle frames found for character '{ch}'.");
            if (set.idleWhite.Count == 0)
                Logger.LogWarning($"No white idle frames found for character '{ch}'.");
            if (set.idle.Count > 0 || set.idleWhite.Count > 0)
                Logger.LogInfo($"Loaded {set.idle.Count} idle frames, {set.idleWhite.Count} white idle frames for '{ch}'.");

            frameCache[ch] = set;
            return set;
        }

        public static Vector2 SizeForSprite(Sprite sprite)
        {
            if (sprite == null) return new Vector2(150, 150 * 0.7f);
            return new Vector2(sprite.rect.width, sprite.rect.height) * SpriteScale;
        }

        static Character[] concreteCharacters;
        static Character[] ConcreteCharacters()
        {
            if (concreteCharacters == null)
            {
                concreteCharacters = System.Enum.GetValues(typeof(Character))
                    .Cast<Character>()
                    .Where(c => c != Character.Random)
                    .ToArray();
            }
            return concreteCharacters;
        }

        public static Character RandomConcreteCharacter()
        {
            Character[] options = ConcreteCharacters();
            return options[Random.Range(0, options.Length)];
        }

        public static Character ResolveCharacter()
        {
            return character.value == Character.Random ? RandomConcreteCharacter() : character.value;
        }

        static Dictionary<Character, List<AudioClip>> soundCache = new Dictionary<Character, List<AudioClip>>();

        public static List<AudioClip> GetCachedSounds(Character ch)
        {
            return soundCache.TryGetValue(ch, out List<AudioClip> list) ? list : null;
        }
        static Dictionary<Character, bool> soundLoadInProgress = new Dictionary<Character, bool>();
        static Dictionary<Character, List<System.Action<List<AudioClip>>>> pendingCallbacks = new Dictionary<Character, List<System.Action<List<AudioClip>>>>();
        public static IEnumerator LoadSoundsForCharacter(Character ch, System.Action<List<AudioClip>> onLoaded = null)
        {
            if (soundCache.TryGetValue(ch, out List<AudioClip> cached))
            {
                onLoaded?.Invoke(cached);
                yield break;
            }

            if (soundLoadInProgress.TryGetValue(ch, out bool inProgress) && inProgress)
            {
                if (!pendingCallbacks.ContainsKey(ch)) pendingCallbacks[ch] = new List<System.Action<List<AudioClip>>>();
                if (onLoaded != null) pendingCallbacks[ch].Add(onLoaded);
                yield break;
            }
            soundLoadInProgress[ch] = true;

            var assembly = Assembly.GetExecutingAssembly();

            string prefix = $".Sounds.{ch}.";

            string[] soundResourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.Contains(prefix))
                .ToArray();

            List<AudioClip> loaded = new List<AudioClip>();

            foreach (string resourceName in soundResourceNames)
            {
                AudioType audioType = GetAudioTypeFromName(resourceName);
                if (audioType == AudioType.UNKNOWN)
                {
                    Logger.LogWarning($"Skipping unknown audio type: {resourceName}");
                    continue;
                }

                byte[] buffer;
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Logger.LogWarning($"Resource not found: {resourceName}");
                        continue;
                    }

                    buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                }

                string ext = Path.GetExtension(resourceName);
                string tempPath = Path.Combine(Application.temporaryCachePath, Path.GetRandomFileName() + ext);
                File.WriteAllBytes(tempPath, buffer);

                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, audioType))
                {
                    yield return www.SendWebRequest();

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Logger.LogWarning($"Failed to load {resourceName}: {www.error}");
                    }
                    else
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                        clip.name = Path.GetFileNameWithoutExtension(resourceName);
                        loaded.Add(clip);
                        Logger.LogInfo($"Loaded sound: {clip.name}");
                    }
                }

                try { File.Delete(tempPath); } catch { /* ignore */ }
            }

            if (loaded.Count == 0)
            {
                Logger.LogWarning($"No sounds found for character '{ch}'.");
            }

            soundCache[ch] = loaded;
            soundLoadInProgress[ch] = false;
            onLoaded?.Invoke(loaded);
            if (pendingCallbacks.TryGetValue(ch, out var waiters))
            {
                foreach (var cb in waiters) cb.Invoke(loaded);
                waiters.Clear();
            }

            Logger.LogInfo($"Loaded {loaded.Count} sounds for '{ch}'.");
        }

        static AudioType GetAudioTypeFromName(string resourceName)
        {
            string lower = resourceName.ToLowerInvariant();
            if (lower.EndsWith(".wav")) return AudioType.WAV;
            if (lower.EndsWith(".ogg")) return AudioType.OGGVORBIS;
            if (lower.EndsWith(".mp3")) return AudioType.MPEG;
            return AudioType.UNKNOWN;
        }

        void Update()
        {
            if (TriggerOnKey.value && Input.GetKeyDown(TriggerKey.value))
            {
                amount.value++;
                CreateUI();
            }

            if (TriggerEverySeconds.value)
            {
                triggerTimer += Time.deltaTime;

                if (triggerTimer >= TriggerInterval.value)
                {
                    triggerTimer = 0f;

                    amount.value++;
                    CreateUI();
                }
            }
        }

        public static void CreateUI()
        {
            Character ch = ResolveCharacter();
            FrameSet fs = Instance.GetOrLoadFrames(ch);

            GameObject canvasObj = GetOrCreateCanvas();

            GameObject imageObj = new GameObject("DVD");
            imageObj.SetActive(false);
            imageObj.transform.SetParent(canvasObj.transform, false);

            Image image = imageObj.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;

            if (fs.idle.Count > 0)
            {
                image.sprite = fs.idle[0];
            }
            else if (kawKaw != null)
            {
                image.sprite = Sprite.Create(
                    kawKaw,
                    new Rect(0, 0, kawKaw.width, kawKaw.height),
                    new Vector2(0.5f, 0.5f));
            }
            else
            {
                Logger.LogWarning($"No sprite available for character '{ch}'.");
            }

            RectTransform rect = image.rectTransform;
            rect.sizeDelta = SizeForSprite(image.sprite);
            rect.anchoredPosition = Vector2.zero;

            Nyon nyon = imageObj.AddComponent<Nyon>();
            nyon.Setup(ch, fs.idle, fs.idleWhite);

            imageObj.SetActive(true);

            kawkaws.Add(imageObj);
        }

        static Plugin instance;
        public static Plugin Instance => instance;

        void OnEnable()
        {
            instance = this;
        }

    }
    [HarmonyPatch]
    public class Patches
    {
        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
        [HarmonyPostfix]
        public static void Dmg(int damage)
        {
            if (Plugin.TriggerOnDamage.value && damage > 1)
            {
                Plugin.amount.value++;
                Plugin.CreateUI();
            }
        }
    }

    public class NyonTria : MonoBehaviour
    {
        float t = 1;
        Image img;
        void Awake()
        {
            img = GetComponent<Image>();
        }

        void Update()
        {
            t -= Time.deltaTime / Plugin.TrailLifetime.value;

            if (t <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Color col = img.color;
            col.a = t;
            img.color = col;
        }
    }

    public class Nyon : MonoBehaviour
    {

        public static float gravity = 1500f;

        public Vector2 velocity = new Vector2(400f, 300f);
        public float hueSpeed = 0.5f;
        private RectTransform rect;
        public RectTransform Rect => rect;
        private Canvas canvas;
        private float hue;

        float trailTimer = 0f;
        const float trailInterval = 0.03f;

        AudioSource source;
        Image img;

        public Plugin.Character character;

        List<Sprite> frames;
        List<Sprite> framesWhite;
        List<AudioClip> sounds;
        public float frameRate = 10f;
        int currentFrame = 0;
        float frameTimer = 0f;

        static readonly List<Nyon> activeNyons = new List<Nyon>();

        public void Setup(Plugin.Character ch, List<Sprite> idle, List<Sprite> idleWhite)
        {
            character = ch;
            frames = idle;
            framesWhite = idleWhite;
        }

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            img = GetComponent<Image>();

            if (frames == null)
            {
                character = Plugin.ResolveCharacter();
                Plugin.FrameSet fs = Plugin.Instance.GetOrLoadFrames(character);
                frames = fs.idle;
                framesWhite = fs.idleWhite;
            }

            if (frames != null && frames.Count > 0)
            {
                currentFrame = Random.Range(0, frames.Count);
                img.sprite = frames[currentFrame];
                ApplySizeForCurrentSprite();
            }
            frameTimer = Random.Range(0f, 1f / frameRate);

            hue = Random.value;

            source = gameObject.AddComponent<AudioSource>();

            float speed = Random.Range(300f, 600f);

            velocity = new Vector2(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized * speed;

            activeNyons.Add(this);

            sounds = Plugin.GetCachedSounds(character);
            if (sounds == null)
            {
                StartCoroutine(Plugin.LoadSoundsForCharacter(character, loaded => { sounds = loaded; }));
            }
        }

        void OnDestroy()
        {
            activeNyons.Remove(this);
        }

        void ApplySizeForCurrentSprite()
        {
            if (rect == null || img == null || img.sprite == null) return;
            rect.sizeDelta = Plugin.SizeForSprite(img.sprite);
        }

        public static void RefreshAllFrames()
        {
            foreach (Nyon n in activeNyons)
            {
                if (n == null) continue;

                Plugin.Character ch = Plugin.ResolveCharacter();
                Plugin.FrameSet fs = Plugin.Instance.GetOrLoadFrames(ch);

                n.character = ch;
                n.frames = fs.idle;
                n.framesWhite = fs.idleWhite;

                n.sounds = Plugin.GetCachedSounds(ch);
                if (n.sounds == null)
                {
                    n.StartCoroutine(Plugin.LoadSoundsForCharacter(ch, loaded =>
                    {
                        if (n != null) n.sounds = loaded;
                    }));
                }

                if (n.frames.Count > 0)
                {
                    n.currentFrame %= n.frames.Count;
                    n.img.sprite = n.frames[n.currentFrame];
                    n.ApplySizeForCurrentSprite();
                }
                else
                {
                    n.currentFrame = 0;
                }
            }
        }

        void Bounce()
        {
            if (sounds == null || sounds.Count == 0) return;
            AudioClip randomClip = sounds[Random.Range(0, sounds.Count)];
            source.volume = Plugin.Volume.value;
            source.pitch = Random.Range(Plugin.MinPitch.value, Plugin.MaxPitch.value);
            source.PlayOneShot(randomClip);
        }

        void AdvanceAnimation()
        {
            if (frames == null || frames.Count == 0) return;

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / frameRate;

            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrame = (currentFrame + 1) % frames.Count;
                img.sprite = frames[currentFrame];
            }
        }

        void SpawnKawKawTrail(Vector3 pos)
        {
            if (!Plugin.TrailEnabled.value) return;
            GameObject imageObj = new GameObject("trail");
            imageObj.transform.SetParent(transform.parent, false);

            imageObj.transform.SetSiblingIndex(transform.GetSiblingIndex());

            Image image = imageObj.AddComponent<Image>();
            image.color = Color.HSVToRGB(hue, 1f, 1f);

            Sprite trailSprite = null;
            if (framesWhite != null && framesWhite.Count > 0)
            {
                int idx = Mathf.Min(currentFrame, framesWhite.Count - 1);
                trailSprite = framesWhite[idx];
            }
            else if (Plugin.kawKawwhite != null)
            {
                trailSprite = Sprite.Create(
                    Plugin.kawKawwhite,
                    new Rect(0, 0, Plugin.kawKawwhite.width, Plugin.kawKawwhite.height),
                    new Vector2(0.5f, 0.5f));
            }

            if (trailSprite == null)
            {
                Destroy(imageObj);
                return;
            }

            image.sprite = trailSprite;
            image.raycastTarget = false;

            RectTransform trailRect = image.rectTransform;
            trailRect.sizeDelta = rect.sizeDelta;
            trailRect.anchoredPosition = pos;

            imageObj.AddComponent<NyonTria>();
        }

        void Update()
        {
            hue += Time.deltaTime * Plugin.HueShiftSpeed.value;
            if (hue > 1f) hue -= 1f;

            AdvanceAnimation();

            if (Plugin.PhysicsBounce.value)
            {
                velocity.y -= gravity * Time.deltaTime;
            }

            rect.anchoredPosition += velocity * Time.deltaTime;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
            Vector2 halfSize = rect.sizeDelta * 0.5f;

            Vector2 pos = rect.anchoredPosition;

            float wallBounceFactor = Plugin.PhysicsBounce.value ? -Plugin.restitution.value : -1f;

            if (pos.x + halfSize.x > halfCanvas.x)
            {
                pos.x = halfCanvas.x - halfSize.x;
                velocity.x *= wallBounceFactor;
                Bounce();
            }
            else if (pos.x - halfSize.x < -halfCanvas.x)
            {
                pos.x = -halfCanvas.x + halfSize.x;
                velocity.x *= wallBounceFactor;
                Bounce();
            }

            if (pos.y + halfSize.y > halfCanvas.y)
            {
                pos.y = halfCanvas.y - halfSize.y;
                velocity.y *= wallBounceFactor;
                Bounce();
            }
            else if (pos.y - halfSize.y < -halfCanvas.y)
            {
                pos.y = -halfCanvas.y + halfSize.y;
                velocity.y *= wallBounceFactor;
                Bounce();

                if (Plugin.PhysicsBounce.value && Mathf.Abs(velocity.y) < 40f)
                {
                    velocity.y = 250f;
                }
            }

            rect.anchoredPosition = pos;

            if (Plugin.BounceOnOthers.value)
            {
                HandleNyonCollisions();
            }

            trailTimer += Time.deltaTime;
            if (trailTimer >= trailInterval)
            {
                trailTimer = 0f;
                SpawnKawKawTrail(pos);
            }
        }

        void HandleNyonCollisions()
        {
            for (int i = 0; i < activeNyons.Count; i++)
            {
                Nyon other = activeNyons[i];
                if (other == this || other == null) continue;

                if (other.GetInstanceID() < GetInstanceID()) continue;

                Vector2 delta = rect.anchoredPosition - other.rect.anchoredPosition;
                float minDist = (rect.sizeDelta.x + other.rect.sizeDelta.x) * 0.5f;

                float dist = delta.magnitude;
                if (dist < minDist && dist > 0.0001f)
                {
                    Vector2 normal = delta / dist;

                    float overlap = minDist - dist;
                    rect.anchoredPosition += normal * (overlap * 0.5f);
                    other.rect.anchoredPosition -= normal * (overlap * 0.5f);

                    Vector2 relativeVelocity = velocity - other.velocity;
                    float velAlongNormal = Vector2.Dot(relativeVelocity, normal);

                    if (velAlongNormal < 0f)
                    {
                        Vector2 impulse = normal * velAlongNormal;
                        velocity -= impulse;
                        other.velocity += impulse;

                        Bounce();
                        other.Bounce();
                    }
                }
            }
        }
    }
}
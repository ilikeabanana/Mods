using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Logger = BepInEx.Logging.Logger;

namespace Replace_Notes
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static CatInstrument Instrument { get; private set; }
        public static CatMusicPlayer Music { get; private set; }

        public static Plugin Instance { get; private set; }

        public static ManualLogSource Log;

        private const string PluginName = "Replace_Notes";

        public static readonly List<string> supportedFormats = new List<string>
        {
            ".wav",
            ".mp3",
            ".ogg"
        };
        private Harmony harmony;
        private void Awake()
        {
            Instance = this;
            Log = Logger;

            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll(typeof(Plugin).Assembly);

            Logger.LogInfo("ULTRAMEOW loaded.");

            GameObject host = new GameObject("ULTRAMEOW");
            DontDestroyOnLoad(host);

            Instrument = host.AddComponent<CatInstrument>();
            Music = host.AddComponent<CatMusicPlayer>();

            Music.Instrument = Instrument;

            StartCoroutine(LoadAudio("Meow", "idk", "meow-1.mp3", "default", -1, 0));
        }

        public static IEnumerator LoadAudio(
            string name,
            string enemy,
            string filename,
            string volume,
            int tauntcount,
            int index)
        {
            for (int i = 0; i < index; i++)
            {
                yield return null;
            }

            Instance.Logger.LogInfo($"Starting audio load: {filename}");

            List<AudioClip> clips = new List<AudioClip>();

            string dataPath = Path.Combine(Paths.PluginPath, PluginName);

            UnityWebRequest request = GetAudioFromFile(filename, dataPath);

            if (request == null)
            {
                Instance.Logger.LogError(
                    $"Failed to create audio request for '{filename}'."
                );
                yield break;
            }

            using (request)
            {
                Instance.Logger.LogInfo(
                    $"Sending audio request for '{filename}'."
                );

                yield return request.SendWebRequest();

                Instance.Logger.LogInfo(
                    $"Audio request finished. Result: {request.result}, Error: {request.error}"
                );

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Instance.Logger.LogError(
                        $"Failed to load '{filename}': {request.error}"
                    );
                    yield break;
                }

                AudioClip clip;

                try
                {
                    clip = DownloadHandlerAudioClip.GetContent(request);
                }
                catch (Exception ex)
                {
                    Instance.Logger.LogError(
                        $"Failed to decode audio file '{filename}': {ex}"
                    );
                    yield break;
                }

                if (clip == null)
                {
                    Instance.Logger.LogError(
                        $"Audio file '{filename}' returned a null AudioClip."
                    );
                    yield break;
                }

                clip.name = "PissyEnemies_" + name + "_" +
                    Path.GetFileNameWithoutExtension(filename);

                clips.Add(clip);

                Instance.Logger.LogInfo(
                    $"Audio clip loaded: {clip.name}, length: {clip.length}s"
                );
            }

            if (clips.Count == 0)
            {
                Instance.Logger.LogError(
                    $"No audio clips were loaded for '{filename}'."
                );
                yield break;
            }

            Setup(clips[0]);
        }

        public static UnityWebRequest GetAudioFromFile(
            string filename,
            string dataPath)
        {
            try
            {
                string extension = Path.GetExtension(filename);

                if (!supportedFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    Instance.Logger.LogError(
                        $"Unsupported audio format '{extension}' for '{filename}'."
                    );
                    return null;
                }

                if (!Directory.Exists(dataPath))
                {
                    Instance.Logger.LogError(
                        $"Audio directory does not exist: '{dataPath}'."
                    );
                    return null;
                }

                string[] files = Directory.GetFiles(
                    dataPath,
                    filename,
                    SearchOption.AllDirectories
                );

                if (files.Length == 0)
                {
                    Instance.Logger.LogError(
                        $"Audio file '{filename}' was not found under '{dataPath}'."
                    );
                    return null;
                }

                string filePath = files[0];

                Instance.Logger.LogInfo(
                    $"Found audio file: '{filePath}'."
                );

                string uri = new Uri(filePath).AbsoluteUri;

                return UnityWebRequestMultimedia.GetAudioClip(
                    uri,
                    GetAudioType(extension)
                );
            }
            catch (Exception ex)
            {
                Instance.Logger.LogError(
                    $"Failed to get audio file '{filename}': {ex}"
                );
                return null;
            }
        }

        private static AudioType GetAudioType(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".wav":
                    return AudioType.WAV;

                case ".mp3":
                    return AudioType.MPEG;

                case ".ogg":
                    return AudioType.OGGVORBIS;

                default:
                    return AudioType.UNKNOWN;
            }
        }

        public static void Setup(AudioClip meowSample)
        {
            if (Instance == null)
                return;

            if (Instrument == null)
            {
                Instance.Logger.LogError(
                    "Cannot initialize audio: Instrument is null."
                );
                return;
            }

            if (meowSample == null)
            {
                Instance.Logger.LogError(
                    "Cannot initialize audio: AudioClip is null."
                );
                return;
            }

            Instance.Logger.LogInfo("Loaded");

            Instrument.Initialize(meowSample);
        }
    }
}
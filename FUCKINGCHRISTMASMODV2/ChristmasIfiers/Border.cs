using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FUCKINGCHRISTMASMOD.ChristmasIfiers
{
    public class Border
    {
        static List<Sprite> Borders = new List<Sprite>();

        static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg" };

        public static void Init()
        {
            return; // it is lowkey kinda annoying so im disabling it for now :P
            GetImages();

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private static void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if(Plugin.BorderUI != null)
            {
                Sprite randomSprite = Borders[UnityEngine.Random.Range(0, Borders.Count)];

                GameObject UI = GameObject.Instantiate(Plugin.BorderUI);
                UI.GetComponentInChildren<Image>().sprite = randomSprite;
            }
        }

        public static void GetImages()
        {
            Borders.Clear();

            // Folder the mod's dll lives in
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string imagesFolder = Path.Combine(pluginFolder, "Images");

            // No Images folder yet: create it and move any loose images from the plugin folder into it
            if (!Directory.Exists(imagesFolder))
            {
                Directory.CreateDirectory(imagesFolder);

                foreach (string file in Directory.GetFiles(pluginFolder))
                {
                    if (!IsImage(file)) continue;

                    string target = Path.Combine(imagesFolder, Path.GetFileName(file));
                    if (File.Exists(target))
                    {
                        // Avoid overwriting / crashing on name collisions
                        target = Path.Combine(imagesFolder,
                            Path.GetFileNameWithoutExtension(file) + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + Path.GetExtension(file));
                    }

                    File.Move(file, target);
                }
            }

            // Add every image in the Images folder to the border list
            foreach (string file in Directory.GetFiles(imagesFolder))
            {
                if (!IsImage(file)) continue;

                try
                {
                    byte[] data = File.ReadAllBytes(file);
                    Texture2D texture = new Texture2D(2, 2);
                    if (!texture.LoadImage(data))
                    {
                        UnityEngine.Object.Destroy(texture);
                        continue;
                    }

                    texture.name = Path.GetFileNameWithoutExtension(file);

                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f));
                    sprite.name = texture.name;
                    Borders.Add(sprite);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Failed to load border image '" + file + "': " + e.Message);
                }
            }
        }

        static bool IsImage(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return Array.IndexOf(ImageExtensions, ext) >= 0;
        }
    }
}
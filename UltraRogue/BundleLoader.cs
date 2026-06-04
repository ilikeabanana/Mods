using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace Ultrarogue
{
    /// <summary> Loads the catalog and blah blah :333 </summary>
    public static class BundleLoader
    {
        /// <summary> Directory with all the asset bundles for the catalog to load. </summary>
        public static string EpicScene => Path.Combine(Application.temporaryCachePath, "Ultrarogue");
        static bool alrLoaded = false;
        /// <summary> Gets the embedded resources then moves them to the <see cref="BundleDir"/> , then loads the catalog synchronously. </summary>
        public static void Load()
        {
            if (alrLoaded) return;

            // Try to clean up old directory, but don't crash if we can't
            if (Directory.Exists(EpicScene))
            {
                try
                {
                    Directory.Delete(EpicScene, true);
                }
                catch (IOException ex)
                {
                    // Files may be locked (e.g. from previous run on Wine/CrossOver).
                    // Fall back: check if the catalog already exists and skip extraction.
                    string existingCatalog = Path.Combine(EpicScene, "catalog.json");
                    if (File.Exists(existingCatalog))
                    {
                        Plugin.Logger.LogWarning(
                            $"[BundleLoader] Could not delete temp dir (files locked?), " +
                            $"reusing existing extraction. Error: {ex.Message}");
                        goto SkipExtraction;
                    }

                    // No usable catalog — try deleting file by file
                    TryDeleteContents(EpicScene);
                }
            }

            Directory.CreateDirectory(EpicScene);

            Assembly asm = typeof(Plugin).Assembly;
            foreach (string resourceName in asm.GetManifestResourceNames())
            {
                const string prefix = "Ultrarogue.catalogStuff.";
                string fileName = resourceName.StartsWith(prefix)
                    ? resourceName.Substring(prefix.Length)
                    : resourceName;

                string path = Path.Combine(EpicScene, fileName);
                using Stream resourceStream = asm.GetManifestResourceStream(resourceName);
                using FileStream fileStream = File.Create(path);
                resourceStream.CopyTo(fileStream);
            }

        SkipExtraction:
            Addressables.ResourceManager.InternalIdTransformFunc = (location) =>
            {
                string id = location.InternalId;
                if (id.Contains("Ultrarogue.Bundleloader.EpicScene"))
                    return id.Replace("Ultrarogue.Bundleloader.EpicScene", EpicScene);
                return id;
            };

            Addressables.LoadContentCatalogAsync(
                Path.Combine(EpicScene, "catalog.json"),
                autoReleaseHandle: true
            ).WaitForCompletion();

            alrLoaded = true;
        }

        /// <summary>Deletes files one by one, skipping locked ones.</summary>
        static void TryDeleteContents(string dir)
        {
            foreach (string file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                try { File.Delete(file); }
                catch { /* skip locked files */ }
            }
            // Try removing empty subdirs
            foreach (string sub in Directory.GetDirectories(dir))
            {
                try { Directory.Delete(sub, false); }
                catch { }
            }
        }
    }

}

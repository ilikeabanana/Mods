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

        /// <summary> Gets the embedded resources then moves them to the <see cref="BundleDir"/> , then loads the catalog synchronously. </summary>
        public static void Load()
        {
            if (Directory.Exists(EpicScene))
                Directory.Delete(EpicScene, true);

            Directory.CreateDirectory(EpicScene);

            Addressables.ResourceManager.InternalIdTransformFunc = (location) =>
            {
                string id = location.InternalId;
                if (id.Contains("Ultrarogue.Bundleloader.EpicScene"))
                    return id.Replace("Ultrarogue.Bundleloader.EpicScene", EpicScene);
                return id;
            };

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

            Addressables.LoadContentCatalogAsync(
                Path.Combine(EpicScene, "catalog.json"),
                autoReleaseHandle: true
            ).WaitForCompletion();
        
        }
    }

}

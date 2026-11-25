using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace FUCKINGCHRISTMASMOD.ChristmasIfiers.TextChanger
{
    /// <summary>
    /// Base class for anything that swaps strings using a JSON file.
    /// Derive from it, override <see cref="JsonFileName"/>, and call <see cref="LoadTables"/>.
    ///
    /// The JSON is expected to look like:
    /// { "tableName": { "original": "replacement", ... }, "otherTable": { ... } }
    /// </summary>
    public abstract class BaseStringChanger
    {
        public const string JsonFolderName = "JSONS";

        /// <summary>Folder the mod DLL lives in.</summary>
        protected static readonly string PluginFolder =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>The JSONS folder next to the DLL.</summary>
        protected static string JsonFolder => Path.Combine(PluginFolder, JsonFolderName);

        /// <summary>File name of the JSON this changer uses, e.g. "hud_messages.json".</summary>
        protected abstract string JsonFileName { get; }

        /// <summary>
        /// Makes sure the JSONS folder exists and that this changer's JSON is inside it
        /// (moving it over from the plugin folder if needed). Returns the full path to the JSON.
        /// </summary>
        protected string EnsureJsonInFolder()
        {
            if (!Directory.Exists(JsonFolder))
                Directory.CreateDirectory(JsonFolder);

            string source = Path.Combine(PluginFolder, JsonFileName);
            string destination = Path.Combine(JsonFolder, JsonFileName);

            if (File.Exists(source) && !File.Exists(destination))
            {
                File.Move(source, destination);
            }

            return destination;
        }

        /// <summary>
        /// Loads the JSON from the JSONS folder. Never returns null; on any problem it logs
        /// an error and returns an empty set of tables.
        /// </summary>
        protected Dictionary<string, Dictionary<string, string>> LoadTables()
        {
            var empty = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                string path = EnsureJsonInFolder();

                if (!File.Exists(path))
                {
                    Debug.LogError($"[{GetType().Name}] Could not find '{JsonFileName}' in '{JsonFolder}' or '{PluginFolder}'.");
                    return empty;
                }

                var tables = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path));
                return tables ?? empty;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetType().Name}] Failed to load '{JsonFileName}': {e}");
                return empty;
            }
        }

        /// <summary>Gets one table by name from loaded tables, or an empty one if it's missing.</summary>
        protected static Dictionary<string, string> GetTable(
            Dictionary<string, Dictionary<string, string>> tables, string tableName)
        {
            if (tables != null && tables.TryGetValue(tableName, out var table) && table != null)
                return table;

            Debug.LogWarning($"[BaseStringChanger] Table '{tableName}' was not found in the JSON.");
            return new Dictionary<string, string>();
        }
    }
}

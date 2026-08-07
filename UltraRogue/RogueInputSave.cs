using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ultrarogue
{
    // Saves/loads binding paths for a custom InputAction using PlayerPrefs.
    //
    // NOTE: this game's InputManager.Rebind(...) mutates InputBinding.path
    // directly rather than using the standard overridePath mechanism, so we
    // mirror that here instead of relying on overridePath (which never changes).
    public static class RogueInputSave
    {
        private const string KeyPrefix = "UltraRogue_Binding_";

        // True design-time default paths, captured once before any save is
        // loaded or any rebind happens, since path itself gets overwritten
        // with no separate record of the original value.
        private static readonly Dictionary<string, string> DefaultPaths = new Dictionary<string, string>();

        private static string MakeKey(InputAction action, int index) => KeyPrefix + action.name + "_" + index;

        // Call this once, before LoadBindings, so we know what "default" means later.
        public static void CaptureDefaults(InputAction action, InputControlScheme controlScheme)
        {
            int[] indices = action.GetBindingsWithGroup(controlScheme.bindingGroup);
            foreach (int index in indices)
            {
                string key = MakeKey(action, index);
                if (!DefaultPaths.ContainsKey(key))
                {
                    DefaultPaths[key] = action.bindings[index].path;
                }
            }
        }

        public static string GetDefaultPath(InputAction action, int index)
        {
            string key = MakeKey(action, index);
            return DefaultPaths.TryGetValue(key, out string path) ? path : null;
        }

        public static void SaveBindings(InputAction action, InputControlScheme controlScheme)
        {
            int[] indices = action.GetBindingsWithGroup(controlScheme.bindingGroup);
            foreach (int index in indices)
            {
                string key = MakeKey(action, index);
                string path = action.bindings[index].path;
                PlayerPrefs.SetString(key, path);
            }
            PlayerPrefs.Save();
        }

        public static void LoadBindings(InputAction action, InputControlScheme controlScheme)
        {
            int[] indices = action.GetBindingsWithGroup(controlScheme.bindingGroup);
            foreach (int index in indices)
            {
                string key = MakeKey(action, index);
                if (PlayerPrefs.HasKey(key))
                {
                    string savedPath = PlayerPrefs.GetString(key);
                    action.ChangeBinding(index).WithPath(savedPath);
                }
            }
        }
    }
}
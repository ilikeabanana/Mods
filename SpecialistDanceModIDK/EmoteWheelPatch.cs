using HarmonyLib;
using UnityEngine;

namespace Specialist_Dance
{
    // Fires right after the base game's WeaponWheel.Start() runs.
    // At that point WeaponWheel has already SetActive(false)'d itself and
    // enabled its "background" child, so the hierarchy is in its normal
    // resting state - a safe moment to clone it.
    [HarmonyPatch(typeof(WeaponWheel), "Start")]
    internal static class WeaponWheel_Start_Patch
    {
        private static void Prefix(WeaponWheel __instance) 
        {
            // Only ever want one emote wheel, even if WeaponWheel.Start
            // somehow runs more than once (scene reload, etc).
            if (EmoteWheel.Instance != null)
            {
                return;
            }

            GameObject original = __instance.gameObject;

            // Clone the whole WeaponWheel hierarchy. Unity remaps any
            // serialized references that point *within* this hierarchy
            // (e.g. WeaponWheel.background -> its child GameObject) onto
            // the new clone automatically as part of Instantiate.
            GameObject duplicate = Object.Instantiate(original, original.transform.parent);
            duplicate.name = "EmoteWheel";

            // Read fields off the DUPLICATE's WeaponWheel component, not
            // the original's - the duplicate's copy is the one that got
            // remapped to point at the duplicate's own children.
            WeaponWheel duplicateWheel = duplicate.GetComponent<WeaponWheel>();
            int segmentCount = duplicateWheel.segmentCount;
            GameObject clickSound = duplicateWheel.clickSound;
            GameObject background = duplicateWheel.background;

            // Off with the original script.
            Object.Destroy(duplicateWheel);

            // On with the new one, carrying over the same public config.
            EmoteWheel emoteWheel = duplicate.AddComponent<EmoteWheel>();
            emoteWheel.segmentCount = segmentCount;
            emoteWheel.clickSound = clickSound;
            emoteWheel.background = background;
            duplicate.SetActive(true);
            duplicate.SetActive(false);

            Plugin.Logger.LogInfo("EmoteWheel duplicated from WeaponWheel and installed.");
        }
    }
}

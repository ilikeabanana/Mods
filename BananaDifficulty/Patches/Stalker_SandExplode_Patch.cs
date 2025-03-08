using HarmonyLib;
using ULTRAKILL.Cheats;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(SandificationZone), nameof(SandificationZone.Enter))]
    public class Stalker_SandExplode_Patch
    {
        public static void Postfix(Collider other)
        {
            
            if ((other.gameObject.layer == 10 || other.gameObject.layer == 11) && BananaDifficultyPlugin.CanUseIt(-1))
            {
                EnemyIdentifierIdentifier component = other.gameObject.GetComponent<EnemyIdentifierIdentifier>();
                if (component && component.eid && !component.eid.dead)
                {
                    component.eid.BuffAll();
                }
            }
        }
    }
}
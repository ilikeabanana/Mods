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
    [HarmonyPatch(typeof(Stalker), nameof(Stalker.SandExplode))]
    public class Stalker_SandExplode_Patch
    {
        // Token: 0x060001CF RID: 463 RVA: 0x00018974 File Offset: 0x00016B74
        private static bool Prefix(Stalker __instance, ref int ___difficulty, ref EnemyIdentifier ___eid, int __0, ref bool ___exploding, ref float ___countDownAmount, ref float ___explosionCharge, ref Color ___currentColor, Color[] ___lightColors, AudioSource ___lightAud, AudioClip[] ___lightSounds, ref bool ___blinking, Machine ___mach, ref bool ___exploded)
        {
            if (___difficulty != 5) return true;
            bool flag = true;
            bool flag2 = !(StockMapInfo.Instance != null) || !(StockMapInfo.Instance.levelName == "GOD DAMN THE SUN") || !(__instance.transform.parent != null) || !(__instance.transform.parent.name == "Wave 1") || !(__instance.transform.parent.parent != null) || !__instance.transform.parent.parent.name.StartsWith("5 Stuff");
            if (flag2)
            {
                flag = false;
            }
            GameObject gameObject = Object.Instantiate<GameObject>(__instance.explosion.ToAsset(), __instance.transform.position + Vector3.up * 2.5f, Quaternion.identity);
            bool flag3 = __0 != 1;
            if (flag3)
            {
                gameObject.transform.localScale *= 1.5f;
            }
            bool flag4 = ___eid.stuckMagnets.Count > 0;
            if (flag4)
            {
                float num = 0.75f;
                bool flag5 = ___eid.stuckMagnets.Count > 1;
                if (flag5)
                {
                    num -= 0.125f * (float)(___eid.stuckMagnets.Count - 1);
                }
                gameObject.transform.localScale *= num;
            }
            gameObject.transform.localScale *= 3;
            SandificationZone componentInChildren = gameObject.GetComponentInChildren<SandificationZone>();
            componentInChildren.buffDamage = (componentInChildren.buffHealth = (componentInChildren.buffSpeed = false));
            componentInChildren.healthBuff = ___eid.healthBuffModifier + 1;
            componentInChildren.damageBuff = ___eid.damageBuffModifier + 1;
            componentInChildren.speedBuff = ___eid.speedBuffModifier + 1;
            bool flag6 = (!flag || ___eid.blessed || InvincibleEnemies.Enabled) && __0 != 1;
            bool flag7;
            if (flag6)
            {
                ___exploding = false;
                ___countDownAmount = 0f;
                ___explosionCharge = 0f;
                ___currentColor = ___lightColors[0];
                ___lightAud.clip = ___lightSounds[0];
                ___blinking = false;
                flag7 = false;
            }
            else
            {
                ___exploded = true;
                bool flag8 = !___mach.limp;
                if (flag8)
                {
                    ___mach.GoLimp();
                    ___eid.Death();
                }
                bool flag9 = MonoSingleton<StalkerController>.Instance.targets != null;
                if (flag9)
                {
                }
                bool flag10 = ___eid.drillers.Count != 0;
                if (flag10)
                {
                    for (int i = ___eid.drillers.Count - 1; i >= 0; i--)
                    {
                        Object.Destroy(___eid.drillers[i].gameObject);
                    }
                }
                __instance.gameObject.SetActive(false);
                Object.Destroy(__instance.gameObject);
                flag7 = false;
            }
            return flag7;
        }
    }
}
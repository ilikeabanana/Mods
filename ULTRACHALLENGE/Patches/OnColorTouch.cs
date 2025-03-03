using HarmonyLib;
using System;
using System.Collections.Generic;
using ULTRACHALLENGE.Utils;
using UnityEngine;

namespace ULTRACHALLENGE.Patches
{
    [HarmonyPatch(typeof(NewMovement))]
    internal class OnColorTouch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Start")]
        public static void Prefix(NewMovement __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                switch (item.situation.value)
                {
                    case TypeOfThing.OnTouchColor:
                        __instance.gameObject.AddComponent<DetectColorTouching>().setting = item;
                        break;
                    case TypeOfThing.OnTouchGameObject:
                        __instance.gameObject.AddComponent<DetectObjectTouching>().setting = item;
                        break;
                }
                
            }
            
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(NewMovement.Update))]
        public static void OnDashing(NewMovement __instance)
        {
            if (__instance.inman.InputSource.Dodge.WasPerformedThisFrame)
            {
                if ((__instance.groundProperties && !__instance.groundProperties.canDash) || __instance.modNoDashSlide)
                {
                }
                else if (__instance.boostCharge >= 100f)
                {
                    foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
                    {
                        if (item.situation.value == TypeOfThing.OnDash)
                        {
                            Handlers.HandleThing(item, __instance.transform);
                        }
                    }
                }

            }
            

        }
        [HarmonyPrefix]
        [HarmonyPatch(nameof(NewMovement.Jump))]
        public static void OnJump(NewMovement __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnJump)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(nameof(NewMovement.StartSlide))]
        public static void OnSlide(NewMovement __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnSlide)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(nameof(NewMovement.GetHurt))]
        public static void OnTakeDamage(NewMovement __instance)
        {
            Debug.Log("Im hurting!!!");
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnTakeDamage)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(nameof(NewMovement.GetHealth))]
        public static void Onheal(NewMovement __instance)
        {
            Debug.Log("Im hurting!!!");
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnHeal)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(NewMovement.Respawn))]
        public static void OnRespawn(NewMovement __instance)
        {
            Debug.Log("Im hurting!!!");
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnDeath)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlatformerMovement))]
    public class damagePlatform
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlatformerMovement.GetHit))]
        public static void OnTakeDamage(PlatformerMovement __instance)
        {
            Debug.Log("Im hurting!!!");
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnTakeDamage)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
    }
    [HarmonyPatch(typeof(PlatformerMovement))]
    public class respawPlatform
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlatformerMovement.Respawn))]
        public static void OnTakeDamage(PlatformerMovement __instance)
        {
            Debug.Log("Im hurting!!!");
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnDeath)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
    }
    public class DetectColorTouching : MonoBehaviour
    {
        public Color targetColor = Color.green; // Set the color you want to detect
        public float colorTolerance = 0.5f; // Adjust tolerance for color matching
        public ChallengeSetting setting;
        private float lastTriggerTime = -999f; // Initialize to ensure first trigger works

        void OnCollisionEnter(Collision collision)
        {
            Renderer renderer = collision.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.material.mainTexture != null)
            {
                Color avgColor = GetAverageColor(renderer.material.mainTexture);
                if (IsColorMatch(avgColor * renderer.material.color, setting.color.value, setting.Tolerance.value))
                {
                    Handlers.HandleThing(setting, transform);
                }
            }
        }

        void OnCollisionStay(Collision collision)
        {
            if (!setting.WhileSituation.value) return;
            if (!CanTrigger()) return;

            Renderer renderer = collision.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.material.mainTexture != null)
            {
                Color avgColor = GetAverageColor(renderer.material.mainTexture);
                if (IsColorMatch(avgColor * renderer.material.color, setting.color.value, setting.Tolerance.value))
                {
                    Handlers.HandleThing(setting, transform);
                    UpdateLastTriggerTime();
                }
            }
        }

        private bool CanTrigger()
        {
            return Time.time >= lastTriggerTime + setting.delayField.value;
        }

        private void UpdateLastTriggerTime()
        {
            lastTriggerTime = Time.time;
        }

        Color GetAverageColor(Texture texture)
        {
            // Create a temporary RenderTexture
            RenderTexture renderTex = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, renderTex); // Copy texture to RenderTexture
                                               // Read pixels from RenderTexture
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableTex = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            readableTex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            readableTex.Apply();
            // Restore previous active render texture
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            // Calculate average color
            Color[] pixels = readableTex.GetPixels();
            Color sum = Color.black;
            foreach (Color pixel in pixels)
                sum += pixel;
            return sum / pixels.Length;
        }

        bool IsColorMatch(Color a, Color b, float tolerance)
        {
            return Mathf.Abs(a.r - b.r) < tolerance &&
                   Mathf.Abs(a.g - b.g) < tolerance &&
                   Mathf.Abs(a.b - b.b) < tolerance;
        }
    }
    [HarmonyPatch(typeof(GunControl))]
    internal class OnWeaponSwapTrigger
    {
        [HarmonyPatch(nameof(GunControl.SwitchWeapon))]
        public static void Postfix(GunControl __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnWeaponSwitch)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
    }

    [HarmonyPatch(typeof(EnemyIdentifier))]
    internal class OnEnemyDeath
    {
        [HarmonyPatch(nameof(EnemyIdentifier.Death),
        new[] { typeof(bool) })]
        [HarmonyPrefix]
        public static void DIe(EnemyIdentifier __instance)
        {
            if (__instance.dead) return;
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnEnemyKill)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
    }
    [HarmonyPatch("Shoot")]
    internal class OnShoot
    {
        [HarmonyPatch(typeof(Revolver))]
        [HarmonyPostfix]
        public static void Pew(Revolver __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.Shoot)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
        [HarmonyPatch(typeof(RocketLauncher))]
        [HarmonyPostfix]
        public static void PewRoc(RocketLauncher __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.Shoot)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
        [HarmonyPatch(typeof(Shotgun))]
        [HarmonyPostfix]
        public static void PewSHo(Shotgun __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.Shoot)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
        [HarmonyPatch(typeof(Railcannon))]
        [HarmonyPostfix]
        public static void PewRai(Railcannon __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.Shoot)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
        [HarmonyPatch(typeof(Nailgun))]
        [HarmonyPostfix]
        public static void PewNai(Nailgun __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.Shoot)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
    }
    [HarmonyPatch(typeof(ShotgunHammer), nameof(ShotgunHammer.Impact))]
    internal class OnImpact
    {
        [HarmonyPostfix]
        public static void pow(ShotgunHammer __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.Shoot)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
    }
    [HarmonyPatch(typeof(Punch), nameof(Punch.PunchStart))]
    internal class OnPunch
    {
        [HarmonyPostfix]
        public static void FALCONPUNCH(Punch __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.Punch)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
    }
    [HarmonyPatch(typeof(ChessManager), nameof(ChessManager.UpdateGame))]
    internal class PieceMove
    {
        [HarmonyPostfix]
        public static void MOVE(ChessManager __instance, ChessManager.MoveData move)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnPieceMove)
                {
                    Handlers.HandleThing(item, __instance.allPieces[move.PieceToMove].transform);
                }
            }
        }
    }
    [HarmonyPatch(typeof(ChessPiece), nameof(ChessPiece.Captured))]
    internal class OnPieceCapture
    {
        [HarmonyPostfix]
        public static void CAPTURE(ChessPiece __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnPieceCapture)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Start))]
    internal class OnEnemySpawn
    {
        [HarmonyPostfix]
        public static void SPAWN(EnemyIdentifier __instance)
        {
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnEnemySpawn)
                {
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
    }

    [HarmonyPatch]
    internal class OnParry
    {
        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Parry))]
        [HarmonyPostfix]
        public static void Postfix(NewMovement __instance)
        {
            Debug.Log("I paarried");
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnParry)
                {
                    Debug.Log("HANDLE IT");
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
        [HarmonyPatch(typeof(Punch), nameof(Punch.Parry))]
        [HarmonyPostfix]
        public static void LePunchParry(Punch __instance)
        {
            Debug.Log("I paarried");
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnParry)
                {
                    Debug.Log("HANDLE IT");
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
        [HarmonyPatch(typeof(Punch), nameof(Punch.TryParryProjectile))]
        [HarmonyPostfix]
        public static void ParryPucnhProj(Punch __instance)
        {
            Debug.Log("I paarried");
            foreach (var item in ULTRACHALLENGEPlugin.ChallengeSettings)
            {
                if (item.situation.value == TypeOfThing.OnParry)
                {
                    Debug.Log("HANDLE IT");
                    Handlers.HandleThing(item, __instance.transform);
                }
            }
        }
    }

    public class DetectObjectTouching : MonoBehaviour
    {
        public ChallengeSetting setting;
        private float lastTriggerTime = -999f; // Initialize to ensure first trigger works

        void OnCollisionEnter(Collision collision)
        {
            if (MatchesPattern(collision.gameObject.name.ToLower(), setting.stringParam.value))
            {
                Handlers.HandleThing(setting, transform);
            }
        }

        void OnCollisionStay(Collision collision)
        {
            if (!setting.WhileSituation.value) return;
            if (CanTrigger() && MatchesPattern(collision.gameObject.name.ToLower(), setting.stringParam.value))
            {
                Handlers.HandleThing(setting, transform);
                UpdateLastTriggerTime();
            }
        }

        private bool CanTrigger()
        {
            return Time.time >= lastTriggerTime + setting.delayField.value;
        }

        private void UpdateLastTriggerTime()
        {
            lastTriggerTime = Time.time;
        }

        bool MatchesPattern(string name, string pattern)
        {
            string[] parts = pattern.Split('^');
            if (parts.Length == 0)
                return true;
            // Check start
            if (parts[0].StartsWith("%"))
            {
                if (!name.Contains(parts[0].Trim('%')))
                    return false;
            }
            else if (!name.StartsWith(parts[0]))
            {
                return false;
            }
            // Check middle parts
            for (int i = 1; i < parts.Length - 1; i++)
            {
                if (!name.Contains(parts[i]))
                    return false;
            }
            // Check end
            string lastPart = parts[parts.Length - 1];
            if (lastPart.EndsWith("%"))
            {
                if (!name.Contains(lastPart.Trim('%')))
                    return false;
            }
            else if (!name.EndsWith(lastPart))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GameStateManager))]
    [HarmonyPatch(nameof(GameStateManager.CanSubmitScores), MethodType.Getter)]
    public class dontsubmit
    {
        public static bool Prefix(ref bool __result)
        {
            if (ULTRACHALLENGEPlugin.ChallengeSettings.Count == 0) return true;
            __result = false;
            return false;
        }
    }
}
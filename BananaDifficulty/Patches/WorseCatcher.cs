using HarmonyLib;
using Sandbox;
using System.Collections;
using System.Collections.Generic;
using ULTRAKILL.Portal;
using UnityEngine;

namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(Deathcatcher))]
    public class WorseCatcher
    {
        // ── Per-instance state ──────────────────────────────────────────────
        private static readonly Dictionary<int, float> panicTimers = new Dictionary<int, float>();
        private static readonly Dictionary<int, int> respawnCounts = new Dictionary<int, int>();
        private static readonly Dictionary<int, float> burstWindowStart = new Dictionary<int, float>();
        private static readonly Dictionary<int, int> burstDeathsInWindow = new Dictionary<int, int>();

        // ── Tuning constants ────────────────────────────────────────────────
        private const float SPEED = 20f;
        private const float PANIC_FREQUENCY = 9f;
        private const float PANIC_AMPLITUDE = 0.7f;
        private const float WALL_CHECK_DIST = 2.5f;
        private const float TURN_SPEED = 12f;
        private const float STRAFE_MIN_DIST = 18f;
        private const float BURST_WINDOW = 3f;
        private const int BURST_THRESHOLD = 5;

        private const int MAX_RADIANCE_TIER = 2;
        private const float SPEED_BUFF_PER_WAVE = 0.05f;
        private const float DAMAGE_BUFF_PER_WAVE = 0.005f;
        private const float MAX_BUFF_MODIFIER = 1.23f;


        private static readonly int WallMask = LayerMaskDefaults.Get(LMD.Environment);

        private static T Get<T>(object obj, string fieldName) =>
            Traverse.Create(obj).Field(fieldName).GetValue<T>();

        [HarmonyPatch(nameof(Deathcatcher.Awake))]
        [HarmonyPostfix]
        public static void PortalFix(Deathcatcher __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __instance.gameObject.AddComponent<SimplePortalTraveler>();
            if (SceneHelper.CurrentScene == "Level 8-3")
                __instance.killPuppetsOnDeath = false;
        }

        [HarmonyPatch("TimeUntilRespawn")]
        [HarmonyPostfix]
        public static void FasterRespawn(Deathcatcher __instance, ref float __result)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __result *= 0.5f;
        }

        [HarmonyPatch(nameof(Deathcatcher.EnemyDeath))]
        [HarmonyPostfix]
        public static void TrackBurst(Deathcatcher __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;

            int id = __instance.GetInstanceID();

            // Reset window if it's stale
            if (!burstWindowStart.ContainsKey(id) || Time.time - burstWindowStart[id] > BURST_WINDOW)
            {
                burstWindowStart[id] = Time.time;
                burstDeathsInWindow[id] = 0;
            }

            burstDeathsInWindow[id]++;
            int burstThreshold = BananaDifficultyPlugin.ExtremeMode.Value ? 2 : BURST_THRESHOLD;

            if (burstDeathsInWindow[id] >= burstThreshold)
            {
                // Force countdown only if one isn't already running
                if (__instance.countdownToRespawn <= 0f)
                {
                    float delay = Get<float>(__instance, "respawnDelay");
                    __instance.countdownToRespawn = delay;
                }
                burstDeathsInWindow[id] = 0;
            }
        }

        [HarmonyPatch("RespawnPuppets")]
        [HarmonyPrefix]
        public static bool EscalatingRespawn(Deathcatcher __instance, ref IEnumerator __result)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return true;

            int id = __instance.GetInstanceID();
            if (!respawnCounts.ContainsKey(id)) respawnCounts[id] = 0;
            respawnCounts[id]++;

            __result = EscalatingRespawnCoroutine(__instance, respawnCounts[id]);
            return false; // skip original
        }

        private static IEnumerator EscalatingRespawnCoroutine(Deathcatcher __instance, int wave)
        {
            if (!__instance.active) yield break;

            bool extreme = BananaDifficultyPlugin.ExtremeMode.Value;

            int maxRadiance = extreme ? 4 : MAX_RADIANCE_TIER;
            float speedPerWave = extreme ? 0.2f : SPEED_BUFF_PER_WAVE;
            float damagePerWave = extreme ? 0.15f : DAMAGE_BUFF_PER_WAVE;
            float maxBuff = extreme ? 2f : MAX_BUFF_MODIFIER;

            var enemies = Get<List<CaughtEnemy>>(__instance, "deadCaughtEnemies");
            if (enemies == null || enemies.Count == 0) yield break;

            EnemyIdentifier masterEid = Get<EnemyIdentifier>(__instance, "eid");

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] == null)
                {
                    enemies.RemoveAt(i);
                    continue;
                }

                // Only respawn enemies that don't already have a puppet
                if (enemies[i].puppet != null) continue;

                SavedEnemy saved = enemies[i].savedEnemy;
                SpawnableInstance inst = saved.Spawnable.InstantiateSpawnable(
                    saved, __instance.transform.parent, true);

                inst.transform.position = enemies[i].position;
                inst.transform.rotation = enemies[i].rotation;
                inst.ApplyAlterOptions(new AlterOption[]
                {
                    new AlterOption { targetKey = "puppeted", useBool = true, boolValue = true }
                });

                EnemyIdentifier eid = inst.GetComponentInChildren<EnemyIdentifier>(true);
                if (eid != null)
                {
                    // Escalate radiance tier with each wave, capped at MAX_RADIANCE_TIER
                    int baseTier = masterEid != null ? (int)masterEid.radianceTier : 0;
                    int effectiveWave = extreme ? (wave - 1) : ((wave - 1) / 2);

                    eid.radianceTier = Mathf.Min(baseTier + effectiveWave, maxRadiance);

                    float scaledWave = extreme ? (wave - 1) : Mathf.Sqrt(wave - 1);
                    // Speed buff — scales up per wave
                    eid.speedBuff = true;
                    eid.speedBuffRequests++;
                    eid.speedBuffModifier = Mathf.Min(1f + scaledWave * speedPerWave, maxBuff);

                    // Damage buff — scales up per wave
                    eid.damageBuff = true;
                    eid.damageBuffRequests++;
                    eid.damageBuffModifier = Mathf.Min(1f + scaledWave * damagePerWave, maxBuff);

                    // Propagate health buff from master if present
                    if (masterEid != null && masterEid.healthBuff)
                    {
                        eid.healthBuff = true;
                        eid.healthBuffRequests++;
                        eid.healthBuffModifier = masterEid.healthBuffModifier;
                    }
                }

                enemies[i].UpdatePuppet(inst.gameObject);
                yield return new WaitForSeconds(0.05f); // tighter stagger than vanilla 0.1s
            }
        }


        [HarmonyPatch(nameof(Deathcatcher.Update))]
        [HarmonyPostfix]
        public static void RUNNNN(Deathcatcher __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (__instance.gameObject.name.Contains("DontBeScared")) return;
            if (!__instance.active) return;
            if (NewMovement.Instance == null) return;
            if (SceneHelper.CurrentScene == "Level 8-3") return;
            if (Get<bool>(__instance, "dead")) return;

            int id = __instance.GetInstanceID();
            if (!panicTimers.ContainsKey(id))
                panicTimers[id] = Random.Range(0f, Mathf.PI * 2f);
            panicTimers[id] += Time.deltaTime * PANIC_FREQUENCY;
            float panicPhase = panicTimers[id];

            Transform t = __instance.transform;
            Vector3 pos = t.position;

            // Line-of-sight check — don't move if no clear view
            Vector3 eyePos = pos + Vector3.up * 1.5f;
            Vector3 toPlayerRaw = NewMovement.Instance.transform.position - eyePos;
            if (Physics.Raycast(eyePos, toPlayerRaw.normalized, toPlayerRaw.magnitude, WallMask))
                return;

            Vector3 toPlayer = NewMovement.Instance.transform.position - pos;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            if (dist < 0.01f) return;

            Vector3 toPlayerDir = toPlayer / dist;
            Vector3 perp = new Vector3(-toPlayerDir.z, 0f, toPlayerDir.x);
            float strafe = Mathf.Sin(panicPhase) * PANIC_AMPLITUDE;

            Vector3 primaryDir;
            Vector3 desiredDir;

            if (dist < STRAFE_MIN_DIST)
            {
                // Too close — flee with panic strafe
                primaryDir = -toPlayerDir;
                desiredDir = (primaryDir + perp * strafe).normalized;
            }
            else
            {
                // Far away — approach while strafing to stay in the fight
                primaryDir = toPlayerDir;
                desiredDir = (primaryDir + perp * strafe).normalized;
            }

            Vector3 moveDir = ResolveWallAvoidance(pos, desiredDir, primaryDir);
            float moveDist = SPEED * Time.deltaTime;

            if (Physics.SphereCast(pos, 0.4f, moveDir, out _, moveDist, WallMask))
                return;

            // Apply movement, preserving Y
            Vector3 newPos = pos + moveDir * moveDist;
            t.position = new Vector3(newPos.x, pos.y, newPos.z);

            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                t.rotation = Quaternion.Slerp(
                    t.rotation,
                    Quaternion.Euler(t.eulerAngles.x, targetRot.eulerAngles.y, t.eulerAngles.z),
                    Time.deltaTime * TURN_SPEED);
            }
        }

        private static Vector3 ResolveWallAvoidance(Vector3 pos, Vector3 desiredDir, Vector3 primaryDir)
        {
            if (!Physics.SphereCast(pos, 0.4f, desiredDir, out _, WALL_CHECK_DIST, WallMask))
                return desiredDir;

            int[] angles = { 45, 90, 135 };
            foreach (int angle in angles)
            {
                Vector3 left = Quaternion.Euler(0, -angle, 0) * primaryDir;
                Vector3 right = Quaternion.Euler(0, angle, 0) * primaryDir;

                bool lClear = !Physics.SphereCast(pos, 0.4f, left, out _, WALL_CHECK_DIST, WallMask);
                bool rClear = !Physics.SphereCast(pos, 0.4f, right, out _, WALL_CHECK_DIST, WallMask);

                if (lClear && rClear) return Random.value > 0.5f ? left : right;
                if (lClear) return left;
                if (rClear) return right;
            }

            // Fully cornered — stop rather than clip through walls
            return Vector3.zero;
        }
    }
}
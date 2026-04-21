using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace BananaDifficulty.Patches
{
    [HarmonyPatch(typeof(Power))]
    public class WorsePower
    {
        [HarmonyPatch(nameof(Power.Update))]
        [HarmonyPostfix]
        public static void Postfix(Power __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            //__instance.inAction = true;
            __instance.attackCooldown -= Time.deltaTime;

            if(EnemyCooldowns.Instance.attackingPower != __instance && !__instance.enraged)
            {
                if(Vector3.Distance(__instance.target.position, __instance.transform.position) <= 10f)
                {
                    Vector3 randomTP = UnityEngine.Random.onUnitSphere;
                    randomTP.y = 0;
                    randomTP *= 10;
                    __instance.TeleportTo(__instance.transform.position + randomTP);

                }
            }
        }

        static List<string> powerNames = new List<string>()
        {
            "Iehuiah", "Lehahiah", "Chauakiah", "Manadel", "Aniel", "Haamiah",
            "Rehael", "Ieiazel"
        };
        [HarmonyPatch(nameof(Power.Awake))]
        [HarmonyPostfix]
        public static void Apply_Timer(Power __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            __instance.gameObject.AddComponent<PowerEnrage>();

            if (!__instance.GetComponent<BossHealthBar>())
            {
                __instance.gameObject.AddComponent<BossHealthBar>().bossName = "Power \"" + powerNames[UnityEngine.Random.Range(0, powerNames.Count)] + "\"";
            }
        }
        [HarmonyPatch(nameof(Power.JuggleStart))]
        [HarmonyPrefix]
        public static bool Dont_Juggle(Power __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return true;
            return false;
        }
        [HarmonyPatch(nameof(Power.Flash))]
        [HarmonyPrefix]
        public static bool NoParry(Power __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return true;
            return false;
        }
        [HarmonyPatch(nameof(Power.UpdateSpeed))]
        [HarmonyPostfix]
        public static void Fastah(Power __instance)
        {
            if (!BananaDifficultyPlugin.CanUseIt(__instance.difficulty)) return;
            if (BananaDifficultyPlugin.ExtremeMode.Value)
            {
                __instance.anim.speed *= 2f;
            }
            else
            {
                __instance.anim.speed *= 1.2f;
            }

        }
    }
    public class PowerEnrage : MonoBehaviour
    {
        float timer = 20f;

        const float PROJECTILE_DETECT_RADIUS = 6f;
        const float TELEPORT_COOLDOWN = 2f;

        float teleportCooldownTimer = 0f;

        Power power;

        void Start()
        {
            power = GetComponent<Power>();

            CreateProjectileDetector();
        }

        void Update()
        {
            if (power.eid.dead) return;

            timer -= Time.deltaTime;
            if (!power.enraged && timer <= 0)
            {
                if (power.CanPlaySound(true))
                {
                    power.PlaySound(MonoSingleton<PowerVoiceController>.Instance.Enrage(), false, false, 1f);
                }
                power.EnrageNow();
            }

            if (teleportCooldownTimer > 0f)
                teleportCooldownTimer -= Time.deltaTime;
        }

        void CreateProjectileDetector()
        {
            GameObject detector = new GameObject("ProjectileDetector");
            detector.transform.SetParent(transform);
            detector.transform.localPosition = Vector3.zero;

            detector.layer = 16;

            SphereCollider col = detector.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = PROJECTILE_DETECT_RADIUS;

            Rigidbody rb = detector.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            ProjectileDetector detectorScript = detector.AddComponent<ProjectileDetector>();
            detectorScript.Init(this);
        }

        public void OnProjectileDetected(Projectile p)
        {
            if (teleportCooldownTimer > 0f) return;
            if (p == null || !p.playerBullet) return;

            power.Teleport();
            teleportCooldownTimer = TELEPORT_COOLDOWN;
        }
    }
    public class ProjectileDetector : MonoBehaviour
    {
        PowerEnrage parent;

        public void Init(PowerEnrage p)
        {
            parent = p;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != 14) return;

            Projectile proj = other.GetComponent<Projectile>();
            if (proj == null) return;

            parent.OnProjectileDetected(proj);
        }
    }

    [HarmonyPatch(typeof(PowerVoiceController))]
    public class WorsePower_NoTaunt
    {
        [HarmonyPatch(nameof(PowerVoiceController.Spear))]
        [HarmonyPrefix]
        public static bool NoSpearNoise(AudioClip __result)
        {
            if (!BananaDifficultyPlugin.CanUseIt(-1)) return true;
            __result = null;
            return false;
        }
        [HarmonyPatch(nameof(PowerVoiceController.SpearThrow))]
        [HarmonyPrefix]
        public static bool NoSpearThrowNoise(AudioClip __result)
        {
            if (!BananaDifficultyPlugin.CanUseIt(-1)) return true;
            __result = null;
            return false;
        }
        [HarmonyPatch(nameof(PowerVoiceController.Rapier))]
        [HarmonyPrefix]
        public static bool NoRapierNoise(AudioClip __result)
        {
            if (!BananaDifficultyPlugin.CanUseIt(-1)) return true;
            __result = null;
            return false;
        }
        [HarmonyPatch(nameof(PowerVoiceController.Greatsword))]
        [HarmonyPrefix]
        public static bool NoSwordNoise(AudioClip __result)
        {
            if (!BananaDifficultyPlugin.CanUseIt(-1)) return true;
            __result = null;
            return false;
        }
        [HarmonyPatch(nameof(PowerVoiceController.Glaive))]
        [HarmonyPrefix]
        public static bool NoGlaiveNoise(AudioClip __result)
        {
            if (!BananaDifficultyPlugin.CanUseIt(-1)) return true;
            __result = null;
            return false;
        }
        [HarmonyPatch(nameof(PowerVoiceController.GlaiveThrow))]
        [HarmonyPrefix]
        public static bool NoGlaiveThrowNoise(AudioClip __result)
        {
            if (!BananaDifficultyPlugin.CanUseIt(-1)) return true;
            __result = null;
            return false;
        }
    }
}
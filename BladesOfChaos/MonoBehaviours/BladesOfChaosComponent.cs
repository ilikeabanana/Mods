using HarmonyLib;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
using ULTRAKILL.Cheats;

namespace BladesOfChaos.MonoBehaviours
{
    internal class BladesOfChaosComponent : MonoBehaviour
    {
        [Header("Configuration")]
        public float baseHitColSize = 5f;
        public float chargedHitColSize = 35f;
        public float scalingFactor = 10f; // Constant 'k' in the formula

        [Header("References")]
        public LineRenderer lineL;
        public LineRenderer lineR;
        public Transform bladeR;
        public Transform bladeL;
        public GameObject meteoricSlam;
        public GameObject explosion;
        public Transform positionToSpawnExplosion;

        public static Material chainMat;

        private float hitColSize;
        private InputManager inputManager;
        private GunControl gc;
        private Animator animator;

        public static bool isThrowing;
        public static bool shouldThrow;
        public bool wasThrowing;

        private float nomanCrushCooldown = 30;
        private float meteoricSlamCooldown = 90;
        private float lastNomanCrushTime = -999f;
        private float lastMeteoricSlamTime = -999f;

        public static BladesOfChaosComponent Instance { get; private set; }
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
            inputManager = MonoSingleton<InputManager>.Instance;
            gc = MonoSingleton<GunControl>.Instance;
            chainMat = lineL.material;
            Instance = this;
        }

        private void Update()
        {
            HandleInput();
            UpdateLineRenderers();
        }

        public bool CanUseGrapple()
        {
            return animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "feedbacker_viewmodel_ARM_Idle";
        }

        private void HandleInput()
        {
            if (!gc.activated) return;
            if (NoWeaponCooldown.NoCooldown)
            {
                ResetCooldowns();
            }
            if (inputManager.InputSource.Fire2.IsPressed)
            {
                SetChargeState(true);
            }
            else if (inputManager.InputSource.Fire1.IsPressed)
            {
                SetAttackState(true);
            }
            else
            {
                ResetStates();
            }
            if (!isThrowing && wasThrowing)
            {
                Debug.Log("Stop Grappling");
                animator.SetBool("Grappling", false);
                //CancelInvoke("Pause");
                Debug.Log($"Animator speed before setting: {animator.speed}");
                animator.speed = 1;
                Debug.Log($"Animator speed after setting: {animator.speed}");
                wasThrowing = false;
            }
            if (Input.GetKeyDown(BladesOfChaosPlugin.MeteoricSlam.Value) && Time.time >= lastMeteoricSlamTime + meteoricSlamCooldown)
            {
                if(!NoWeaponCooldown.NoCooldown)
                    lastMeteoricSlamTime = Time.time;
                animator.SetTrigger("MeteoricSlam");
            }
            else if (Input.GetKeyDown(BladesOfChaosPlugin.MeteoricSlam.Value))
            {
                Debug.Log("Meteoric Slam is on cooldown!");
            }

            if (Input.GetKeyDown(BladesOfChaosPlugin.NemeanCrush.Value) && Time.time >= lastNomanCrushTime + nomanCrushCooldown)
            {
                if (!NoWeaponCooldown.NoCooldown)
                    lastNomanCrushTime = Time.time;
                animator.SetTrigger("NomanCrush");
            }
            else if (Input.GetKeyDown(BladesOfChaosPlugin.NemeanCrush.Value))
            {
                Debug.Log("Nemean Crush is on cooldown!");
            }
        }
        private void OnGUI()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.red;

            float meteoricTimeLeft = Mathf.Max(0, (lastMeteoricSlamTime + meteoricSlamCooldown) - Time.time);
            float nomanTimeLeft = Mathf.Max(0, (lastNomanCrushTime + nomanCrushCooldown) - Time.time);

            if (meteoricTimeLeft > 0)
            {
                GUI.Label(new Rect(screenWidth / 2 - 100, screenHeight - 100, 200, 30), $"Meteoric Slam: {meteoricTimeLeft:F1}s", style);
            }

            if (nomanTimeLeft > 0)
            {
                GUI.Label(new Rect(screenWidth / 2 - 100, screenHeight - 70, 200, 30), $"Nemean Crush: {nomanTimeLeft:F1}s", style);
            }
        }
        public void SpawnExplosionInfront()
        {
            float distance = 15;
            // Calculate the position in front of the camera
            Vector3 spawnPosition = MonoSingleton<CameraController>.instance.transform.position
                                    + MonoSingleton<CameraController>.instance.transform.forward * distance;
            // Spawn the explosion at the calculated position
            Instantiate(explosion, spawnPosition, Quaternion.identity);

            Collider[] colliders = Physics.OverlapSphere(spawnPosition, 5);

            if (colliders.Length == 0) return;

            HashSet<EnemyIdentifier> alreadyHitEnemies = new HashSet<EnemyIdentifier>();

            foreach (var collider in colliders)
            {
                GameObject target = collider.gameObject;

                if (target.TryGetComponent(out EnemyIdentifierIdentifier enemyId))
                {
                    HandleEnemyHit(enemyId, target, 4.5f, alreadyHitEnemies);
                }

                if (target.TryGetComponent(out Breakable breakable))
                {
                    breakable.Burn();
                    breakable.Break();
                }

                if (target.TryGetComponent(out Glass glass))
                {
                    if (glass.broken) continue;
                    glass.Shatter();
                }
            }
        }

        public void StartThrow()
        {
            Debug.Log("Grappling");
            animator.SetBool("Grappling", true);
            animator.speed = 1; // Ensure it's set at start
            wasThrowing = true;
        }
        public void Pause()
        {
            Debug.Log("Doing Pause");
            animator.speed = 0;
            shouldThrow = true;
        }

        private void SetChargeState(bool isCharging)
        {
            hitColSize = isCharging ? chargedHitColSize : baseHitColSize;
            animator.SetBool("Mouse2Held", isCharging);
        }

        private void SetAttackState(bool isAttacking)
        {
            hitColSize = baseHitColSize;
            animator.SetBool("Mouse1Held", isAttacking);
        }
        public void slowDown(float amount)
        {
            MonoSingleton<TimeController>.instance.SlowDown(amount);
        }
        public void SpawnExplosionAtATransform()
        {
            Instantiate(explosion, positionToSpawnExplosion.position, Quaternion.identity);
        }

        private void ResetStates()
        {
            hitColSize = baseHitColSize;
            animator.SetBool("Mouse2Held", false);
            animator.SetBool("Mouse1Held", false);
        }

        private void UpdateLineRenderers()
        {
            lineL.SetPosition(0, lineL.transform.position);
            lineL.SetPosition(1, bladeL.position);

            lineR.SetPosition(0, lineR.transform.position);
            lineR.SetPosition(1, bladeR.position);
        }

        private void OnDisable()
        {
            animator.speed = 1;
            bladeL.gameObject.SetActive(true);
        }

        public void CheckForDamage(AttackType attackType)
        {
            float damage = DamageBasedOnType(attackType);
            MonoSingleton<CameraController>.instance.CameraShake(0.1f);
            Collider[] colliders = Physics.OverlapSphere(transform.position, hitColSize);

            if (colliders.Length == 0) return;

            HashSet<EnemyIdentifier> alreadyHitEnemies = new HashSet<EnemyIdentifier>();

            foreach (var collider in colliders)
            {
                GameObject target = collider.gameObject;

                if (target.TryGetComponent(out EnemyIdentifierIdentifier enemyId))
                {
                    HandleEnemyHit(enemyId, target, damage, alreadyHitEnemies);
                }

                if (target.TryGetComponent(out Breakable breakable))
                {
                    breakable.Burn();
                    breakable.Break();
                }

                if (target.TryGetComponent(out Glass glass))
                {
                    if (glass.broken) continue;
                    glass.Shatter();
                }
            }
        }

        private void HandleEnemyHit(EnemyIdentifierIdentifier enemyId, GameObject target, float damage, HashSet<EnemyIdentifier> alreadyHitEnemies)
        {
            EnemyIdentifier eid = enemyId.eid;
            if (alreadyHitEnemies.Contains(eid) || eid.dead) return;

            float distance = Vector3.Distance(transform.position, eid.transform.position);
            float calculatedDamage = (distance > 0) ? scalingFactor / distance : float.MaxValue;

            MonoSingleton<TimeController>.instance.HitStop(0.1f);
            MonoSingleton<StyleHUD>.instance.AddPoints(50, "SLICED", target, eid);
            eid.hitter = "Blade";
            eid.DeliverDamage(target, Vector3.zero, target.transform.position, damage /* * calculatedDamage*/, false);

            if (target.TryGetComponent(out Flammable flammable))
            {
                flammable.Burn(damage / 10f, false);
            }

            alreadyHitEnemies.Add(eid);
        }

        public float DamageBasedOnType(AttackType type)
        {
            switch (type)
            {
                case AttackType.Melee:
                    return 1.5f;
                case AttackType.Ranged:
                    return 0.89f;
                default:
                    return 1;
            }
        }

        public void SpawnSlam()
        {
            if (meteoricSlam != null)
            {
                Instantiate(meteoricSlam, transform.position, transform.rotation);
            }
        }

        public void SetBoolFalse(string boolName)
        {
            animator.SetBool(boolName, false);
        }
        public void ResetCooldowns()
        {
            lastNomanCrushTime = -999f;
            lastMeteoricSlamTime = -999f;
        }
    }
    
    [HarmonyPatch(typeof(HookArm), nameof(HookArm.StopThrow))]
    public class resetOnHookStopThrow
    {
        public static void Postfix(HookArm __instance, float animationTime = 0f, bool sparks = false)
        {
            if(animationTime > 0)
            {
                Object.Destroy(ReplaceHook.blade);
                BladesOfChaosComponent.isThrowing = false;
            }
        }
    }

    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Respawn))]
    public class ResetCooldownsOnRespawn
    {
        public static void Postfix()
        {
            BladesOfChaosComponent.Instance.ResetCooldowns();
        }
    }
    /*
    [HarmonyPatch(typeof(HookArm), nameof(HookArm.CatchOver))]
    public class resetOnHookCatchOver
    {
        public static void Postfix(HookArm __instance)
        {
            if (__instance.state != HookState.Ready || __instance.returning)
            {
                return;
            }
            BladesOfChaosComponent.isThrowing = false;
        }
    }
    [HarmonyPatch(typeof(HookArm), nameof(HookArm.Cancel))]
    public class resetOnHookCancel
    {
        public static void Postfix(HookArm __instance)
        {
            BladesOfChaosComponent.isThrowing = false;
        }
    }*/

    [HarmonyPatch(typeof(HookArm), nameof(HookArm.Update))]
    public class ReplaceHook
    {
        public static GameObject blade;
        public static bool Prefix(HookArm __instance)
        {
            if (BladesOfChaosComponent.Instance == null) return true;
            if (!BladesOfChaosComponent.Instance.gameObject.activeSelf) return true;
            __instance.model.SetActive(false);
            if(BladesOfChaosComponent.chainMat != null)
            {
                if(__instance.lr.material != BladesOfChaosComponent.chainMat)
                {
                    __instance.lr.material = BladesOfChaosComponent.chainMat;
                }
            }
            if (MonoSingleton<InputManager>.Instance.InputSource.Hook.WasPerformedThisFrame && __instance.cooldown <= 0f)
            {
                if (!BladesOfChaosComponent.Instance.CanUseGrapple()) return false;
                BladesOfChaosComponent.isThrowing = true;
                __instance.StartCoroutine(startThrowLate(__instance));
                return false;
            }
            if(blade != null)
            {
                __instance.hook.gameObject.SetActive(false);
                blade.transform.position = __instance.hookPoint;
            }
            /*
            if (MonoSingleton<InputManager>.Instance.InputSource.Hook.WasPerformedThisFrame)
            {
                if (__instance.state == HookState.Pulling)
                {
                    __instance.StopCoroutine(startThrowLate(__instance));
                    BladesOfChaosComponent.isThrowing = false;
                }
            }
            
            if (!MonoSingleton<InputManager>.Instance.InputSource.Hook.IsPressed && (__instance.cooldown <= 0.1f || __instance.caughtObjects.Count > 0))
            {
                __instance.StopCoroutine(startThrowLate(__instance));
                BladesOfChaosComponent.isThrowing = false;
            }*/
            
            if (__instance.state == HookState.Ready && !__instance.returning && BladesOfChaosComponent.isThrowing)
            {
                if(__instance.hookPoint == __instance.hand.position)
                {
                    Object.Destroy(blade);
                    BladesOfChaosComponent.isThrowing = false;
                }
                    
                //Debug.Log("Stop Grappling Hookarm");
            }
            if (__instance.hookPoint == __instance.hand.position)
            {
                Object.Destroy(blade);
                BladesOfChaosComponent.isThrowing = false;
            }
                
            return true;
        }

        public static IEnumerator startThrowLate(HookArm __instance)
        {
            BladesOfChaosComponent.Instance.StartThrow();
            yield return new WaitUntil(() => BladesOfChaosComponent.shouldThrow);
            BladesOfChaosComponent.shouldThrow = false;
            Throw(__instance);
        }

        public static void Throw(HookArm __instance)
        {
            __instance.cooldown = 0.5f;
            //Debug.Log("Start Grappling Hookarm");
            __instance.model.SetActive(true);
            if (!__instance.forcingFistControl)
            {
                if (MonoSingleton<FistControl>.Instance.currentPunch)
                {
                    MonoSingleton<FistControl>.Instance.currentPunch.CancelAttack();
                }
                MonoSingleton<FistControl>.Instance.forceNoHold++;
                __instance.forcingFistControl = true;
                MonoSingleton<FistControl>.Instance.transform.localRotation = Quaternion.identity;
            }
            __instance.lr.enabled = true;
            __instance.hookPoint = __instance.transform.position;
            __instance.previousHookPoint = __instance.hookPoint;
            if (__instance.targeter.CurrentTarget && __instance.targeter.IsAutoAimed)
            {
                __instance.throwDirection = (__instance.targeter.CurrentTarget.bounds.center - __instance.transform.position).normalized;
            }
            else
            {
                __instance.throwDirection = __instance.transform.forward;
            }
            __instance.returning = false;
            if (__instance.caughtObjects.Count > 0)
            {
                foreach (Rigidbody rigidbody in __instance.caughtObjects)
                {
                    if (rigidbody)
                    {
                        rigidbody.velocity = (MonoSingleton<NewMovement>.Instance.transform.position - rigidbody.transform.position).normalized * (100f + __instance.returnDistance / 2f);
                    }
                }
                __instance.caughtObjects.Clear();
            }
            __instance.state = HookState.Throwing;
            __instance.lightTarget = false;
            __instance.throwWarp = 1f;
            __instance.anim.Play("Throw", -1, 0f);
            blade = Object.Instantiate(BladesOfChaosPlugin.bladeModel, __instance.hookPoint, Quaternion.identity);
            __instance.inspectLr.enabled = false;
            __instance.hand.transform.localPosition = new Vector3(0.09f, -0.051f, 0.045f);
            if (MonoSingleton<CameraController>.Instance.defaultFov > 105f)
            {
                __instance.hand.transform.localPosition += new Vector3(0.225f * ((MonoSingleton<CameraController>.Instance.defaultFov - 105f) / 55f), -0.25f * ((MonoSingleton<CameraController>.Instance.defaultFov - 105f) / 55f), 0.05f * ((MonoSingleton<CameraController>.Instance.defaultFov - 105f) / 55f));
            }
            __instance.caughtPoint = Vector3.zero;
            __instance.caughtTransform = null;
            __instance.caughtCollider = null;
            __instance.caughtEid = null;
            Object.Instantiate<GameObject>(__instance.throwSound);
            __instance.aud.clip = __instance.throwLoop;
            __instance.aud.panStereo = 0f;
            __instance.aud.Play();
            __instance.aud.pitch = Random.Range(0.9f, 1.1f);
            __instance.semiBlocked = 0f;
            MonoSingleton<RumbleManager>.Instance.SetVibrationTracked(RumbleProperties.WhiplashThrow, __instance.gameObject);
        }
    }
    [HarmonyPatch(typeof(HookArm), nameof(HookArm.FixedUpdate))]
    public class DontThrowHookFixed
    {
        public static bool Prefix(HookArm __instance)
        {

            if (__instance.state == HookState.Ready && !__instance.returning && BladesOfChaosComponent.isThrowing)
            {
                if (__instance.hookPoint == __instance.hand.position)
                {
                    Object.Destroy(ReplaceHook.blade);
                    BladesOfChaosComponent.isThrowing = false;
                }
                    
                
                //Debug.Log("Stop Grappling Hookarm");
            }
            if (__instance.returning)
            {
                if (Vector3.Distance(__instance.hand.position, __instance.hookPoint) <= 25f)
                {
                    Object.Destroy(ReplaceHook.blade);
                    BladesOfChaosComponent.isThrowing = false;
                }
                    
            }
            //Debug.Log(Vector3.Distance(__instance.hand.position, __instance.hookPoint));
            
            return true;
        }
    }
    public enum AttackType
    {
        Ranged,
        Melee
    }
}

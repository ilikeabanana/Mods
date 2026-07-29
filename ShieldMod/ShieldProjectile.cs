using HarmonyLib;
using Shield_Mod;
using System.Collections.Generic;
using System.Text;
using ULTRAKILL.Cheats;
using UnityEngine;
using System.Linq;

public class ShieldProjectile : MonoBehaviour
{
    float timer = 0.08f;

    List<EnemyType> lightEnemies = new List<EnemyType>();

    float speed = 75f;

    float drillT = 0;

    bool attached = false;

    public int drillHits;
    private int drillHitsLeft;
    private float drillCooldown;
    private bool drilling;
    private bool punched;
    public AudioSource drillSound;
    private AudioSource currentDrillSound;

    AudioSource source;
    private EnemyIdentifier target;
    [HideInInspector]
    public GameObject sourceWeapon;

    // --- Return-to-player behavior ---
    [Header("Return To Player")]
    public int maxBounces = 7;
    public float maxLifetime = 5f;
    public float returnBaseSpeed = 75f;
    public float speedGrowthRate = 1.5f;
    public float catchDistance = 1.25f;

    public ShieldWeapon weapon;

    private int bounceCount = 0;
    private float lifeTimer = 0f;
    private bool returningToPlayer = false;
    private float returnTimer = 0f;
    private Vector3 lastPosition;
    private bool caught = false;

    void Awake()
    {
        lightEnemies.Add(EnemyType.Drone);
        lightEnemies.Add(EnemyType.Filth);
        lightEnemies.Add(EnemyType.Schism);
        lightEnemies.Add(EnemyType.Soldier);
        lightEnemies.Add(EnemyType.Stray);
        lightEnemies.Add(EnemyType.Streetcleaner);

        drillHitsLeft = drillHits;
        lastPosition = transform.position;

        source = GetComponent<AudioSource>();

    }

    void Update()
    {
        if (attached)
        {
            if (drilling)
            {
                transform.Rotate(Vector3.forward, 14400f * Time.deltaTime);
            }
            return;
        }

        if (caught) return;

        if (timer > 0)
            timer -= Time.deltaTime;

        if (!returningToPlayer)
        {
            lifeTimer += Time.deltaTime;

            if (bounceCount >= maxBounces || lifeTimer >= maxLifetime)
            {
                BeginReturnToPlayer();
            }
        }

        lastPosition = transform.position;

        if (returningToPlayer)
        {
            UpdateReturnToPlayer();
        }
        else
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        if (InputManager.Instance.InputSource.Fire2.WasPerformedThisFrame && GunControl.Instance.activated && weapon.gameObject.activeInHierarchy)
        {
            BeginReturnToPlayer();
        }

        CheckCatchAlongPath(lastPosition, transform.position);
    }

    private void BeginReturnToPlayer()
    {
        returningToPlayer = true;
        returnTimer = 0f;
        speed = returnBaseSpeed;
    }

    private void UpdateReturnToPlayer()
    {
        NewMovement player = MonoSingleton<NewMovement>.Instance;
        if (player == null)
            return;

        returnTimer += Time.deltaTime;
        speed = returnBaseSpeed * Mathf.Exp(speedGrowthRate * returnTimer);

        Vector3 toPlayer = player.transform.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance <= catchDistance)
        {
            CatchPlayer();
            return;
        }

        transform.forward = toPlayer / distance;

        float move = speed * Time.deltaTime;

        if (move >= distance)
        {
            transform.position = player.transform.position;
            CatchPlayer();
            return;
        }

        transform.position += transform.forward * move;
    }

    private void CheckCatchAlongPath(Vector3 from, Vector3 to)
    {
        if (caught) return;
        if (!returningToPlayer) return;

        NewMovement player = MonoSingleton<NewMovement>.Instance;
        if (player == null) return;

        Vector3 playerPos = player.transform.position;

        Vector3 segment = to - from;
        float segLenSq = segment.sqrMagnitude;
        float t = segLenSq > 0.0001f ? Mathf.Clamp01(Vector3.Dot(playerPos - from, segment) / segLenSq) : 0f;
        Vector3 closestPoint = from + segment * t;

        if (Vector3.Distance(closestPoint, playerPos) <= catchDistance)
        {
            CatchPlayer();
        }
    }

    private void CatchPlayer()
    {
        if (caught) return;
        caught = true;
        returningToPlayer = false;

        weapon.noShield = false;
        
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (attached && drilling && target)
        {
            if (drillCooldown != 0f)
            {
                drillCooldown = Mathf.MoveTowards(drillCooldown, 0f, Time.deltaTime);
                return;
            }
            drillCooldown = 0.08f;
            target.hitter = "drill";
            target.DeliverDamage(target.gameObject, Vector3.zero, transform.position, 0.0325f, false, 0f, sourceWeapon, false, false);

            if (currentDrillSound)
            {
                currentDrillSound.SetPitch(1.5f - (float)drillHitsLeft / (float)drillHits / 2f);
            }

            drillHitsLeft--;
            if (drillHitsLeft <= 0 && !PauseTimedBombs.Paused)
            {
                Instantiate(AssetsManager.BreakParticleMetalSaw, transform.position, Quaternion.identity);
                Destroy(gameObject);
                return;
            }
        }
        else if (drilling && target == null)
        {
            drilling = false;
        }
    }


    List<EnemyIdentifier> hitEnemies = new List<EnemyIdentifier>();
    void OnTriggerEnter(Collider other)
    {
        
        if (attached) return;

        if (LayerMaskDefaults.IsMatchingLayer(other.gameObject.layer, LMD.Player)) return;

        EnemyIdentifier eid;
        EnemyIdentifierIdentifier eidd;
        if (!other.gameObject.TryGetComponent<EnemyIdentifier>(out eid))
        {
            if (other.gameObject.TryGetComponent<EnemyIdentifierIdentifier>(out eidd))
            {
                eid = eidd.eid;
            }
        }
        if (eid != null)
        {
            if (hitEnemies.Contains(eid)) return;
            hitEnemies.Add(eid);
            eid.hitter = bounceCount == 0 ? "shieldnobounce" : "shieldproj";
            eid.DeliverDamage(other.gameObject, Vector3.zero, other.transform.position, 4, true, sourceWeapon: sourceWeapon);
            if (!lightEnemies.Contains(eid.enemyType))
            {
                if (eid.dead) return;
                attached = true;
                transform.parent = other.transform;
                target = eid;

                drilling = true;
                currentDrillSound = Instantiate<AudioSource>(drillSound, transform.position, transform.rotation);
                currentDrillSound.transform.SetParent(transform, true);
                return;
            }
            else
            {
                eid.DeliverDamage(other.gameObject, Vector3.zero, other.transform.position, 50, true);

                List<EnemyIdentifier> idents = EnemyTracker.Instance.GetCurrentEnemies();

                idents.RemoveAll(x =>
                    x == null ||
                    x.dead ||
                    hitEnemies.Contains(x)
                );

                EnemyIdentifier nearest = null;
                float nearestDistSq = float.MaxValue;

                foreach (EnemyIdentifier enemy in idents)
                {
                    Transform t = enemy.weakPoint != null ? enemy.weakPoint.transform : enemy.transform;

                    float distSq = (t.position - transform.position).sqrMagnitude;
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearest = enemy;
                    }
                }

                if (nearest != null)
                {
                    Transform t = nearest.weakPoint != null
                        ? nearest.weakPoint.transform
                        : nearest.transform;

                    transform.forward = (t.position - transform.position).normalized;
                }
            }
        }
        else if (LayerMaskDefaults.IsMatchingLayer(other.gameObject.layer, LMD.Environment))
        {
            if (timer > 0) return;
            if (returningToPlayer) return;
            source.Play();
            Vector3 point = other.ClosestPoint(transform.position);
            Vector3 normal = (transform.position - point).normalized;
            transform.forward = Vector3.Reflect(transform.forward, normal);
            RicochetAimAssist(gameObject, true);

            bounceCount++;
            if (!returningToPlayer && (bounceCount >= maxBounces || lifeTimer >= maxLifetime))
            {
                BeginReturnToPlayer();
            }
        }

    }
    void OnDestroy()
    {
        CatchPlayer();
    }
    void OnDisable()
    {
        CatchPlayer();
    }

    public void Punched()
    {
        if (!attached) return;

        punched = true;
        drilling = false;

        if (target)
        {
            target.hitter = "drillpunch";
            target.DeliverDamage(target.gameObject, transform.forward * 150f, transform.position, 4f + (float)drillHitsLeft * 0.0325f, true, 0f, null, false, false);
        }

        if (currentDrillSound)
        {
            Destroy(currentDrillSound);
        }

        drillHitsLeft = drillHits;

        attached = false;
        transform.parent = null;
        timer = 0.08f;

        // Punching resets the projectile back into its normal bouncing behavior.
        returningToPlayer = false;
        bounceCount = 0;
        lifeTimer = 0f;
        caught = false;
        speed = 75f;
    }

    private void RicochetAimAssist(GameObject beam, bool aimAtHead = false)
    {
        RaycastHit[] array = Physics.SphereCastAll(beam.transform.position, 5f, beam.transform.forward, float.PositiveInfinity, LayerMaskDefaults.Get(LMD.Enemies));
        if (array == null || array.Length == 0)
        {
            return;
        }
        Vector3 worldPosition = beam.transform.forward * 1000f;
        float num = float.PositiveInfinity;
        GameObject gameObject = null;
        bool flag = false;
        for (int i = 0; i < array.Length; i++)
        {
            Coin coin;
            bool flag2 = MonoSingleton<CoinTracker>.Instance.revolverCoinsList.Count > 0 && array[i].transform.TryGetComponent<Coin>(out coin) && (!coin.shot || coin.shotByEnemy);
            PhysicsCastResult physicsCastResult;
            PortalTraversalV2[] array2;
            Vector3 vector;
            EnemyIdentifierIdentifier enemyIdentifierIdentifier;
            if ((!flag || flag2) && (array[i].distance <= num || (!flag && flag2)) && (array[i].distance >= 0.1f || flag2) && !PortalPhysicsV2.Raycast(beam.transform.position, array[i].point - beam.transform.position, array[i].distance, LayerMaskDefaults.Get(LMD.Environment), out physicsCastResult, out array2, out vector, QueryTriggerInteraction.UseGlobal) && (flag2 || (array[i].transform.TryGetComponent<EnemyIdentifierIdentifier>(out enemyIdentifierIdentifier) && enemyIdentifierIdentifier.eid && !enemyIdentifierIdentifier.eid.dead)))
            {
                if (flag2)
                {
                    flag = true;
                }
                worldPosition = (flag2 ? array[i].transform.position : array[i].point);
                num = array[i].distance;
                gameObject = array[i].transform.gameObject;
            }
        }
        if (gameObject)
        {
            EnemyIdentifierIdentifier enemyIdentifierIdentifier2;
            if (aimAtHead && !flag && gameObject.TryGetComponent<EnemyIdentifierIdentifier>(out enemyIdentifierIdentifier2) && enemyIdentifierIdentifier2.eid && enemyIdentifierIdentifier2.eid.weakPoint && !PortalPhysicsV2.Raycast(beam.transform.position, enemyIdentifierIdentifier2.eid.weakPoint.transform.position - beam.transform.position, Vector3.Distance(enemyIdentifierIdentifier2.eid.weakPoint.transform.position, beam.transform.position), LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.UseGlobal))
            {
                worldPosition = enemyIdentifierIdentifier2.eid.weakPoint.transform.position;
            }
            beam.transform.LookAt(worldPosition);
        }
    }
}

[HarmonyPatch(typeof(Punch), nameof(Punch.PunchSuccess))]
public class PunchPatch
{
    public static void Postfix(Punch __instance, Vector3 point, Transform target)
    {
        __instance.transform.parent.LookAt(point);

        ParryHelper parryHelper;
        if (target.TryGetComponent<ParryHelper>(out parryHelper))
        {
            target = parryHelper.target;
        }

        if (target.gameObject.CompareTag("Enemy") || target.gameObject.CompareTag("Armor") || target.gameObject.CompareTag("Head") || target.gameObject.CompareTag("Body") || target.gameObject.CompareTag("Limb") || target.gameObject.CompareTag("EndLimb"))
        {
            EnemyIdentifier enemyIdentifier = null;
            EnemyIdentifierIdentifier enemyIdentifierIdentifier;
            if (target.TryGetComponent<EnemyIdentifierIdentifier>(out enemyIdentifierIdentifier))
            {
                enemyIdentifier = enemyIdentifierIdentifier.eid;
            }
            if (enemyIdentifier)
            {
                ShieldProjectile shield = enemyIdentifier.GetComponentInChildren<ShieldProjectile>(true);
                if (shield != null)
                {
                    __instance.anim.Play("Hook", 0, 0.065f);
                    MonoSingleton<TimeController>.Instance.ParryFlash();
                    shield.transform.forward = __instance.cc.transform.forward;
                    shield.transform.position = __instance.cc.GetDefaultPos();
                    shield.Punched();
                }
            }
        }
    }
}
using Shield_Mod;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public class ShieldWeapon : MonoBehaviour
{
    public int variation = 2;
    InputManager inman;
    Animator anim;
    AudioSource source;
    GunControl gc;
    bool attackAgain = true;

    GameObject sourceWeapon;

    List<EnemyType> lightEnemies = new List<EnemyType>();


    // Charge things
    bool isCharging;
    float chargeT;
    float chargeForce = 85;

    // Fire cooldowns
    float fire1Cooldown = 0.25f;
    float fire2Cooldown = 0.6f;
    float fire1Timer;
    float fire2Timer;

    public bool noShield = false;

    [Header("Green Variant")]
    public ShieldProjectile ShieldProj;

    [Header("Red Variant")]
    public SpriteRenderer Twirl;
    public AudioSource TwirlSound;

    void Awake()
    {
        sourceWeapon = transform.parent.gameObject;
        AssetsManager.GetAssets();

        inman = InputManager.Instance;
        anim = GetComponent<Animator>();
        gc = GunControl.Instance;
        source = GetComponent<AudioSource>();

        lightEnemies.Add(EnemyType.Drone);
        lightEnemies.Add(EnemyType.Filth);
        lightEnemies.Add(EnemyType.Schism);
        lightEnemies.Add(EnemyType.Soldier);
        lightEnemies.Add(EnemyType.Stray);
        lightEnemies.Add(EnemyType.Streetcleaner);
        // Why Hakita :(
    }
    void Update()
    {
        anim.SetBool("NoShield", noShield);
        if (!gc.activated) return;
        if (noShield) return;

        if (fire1Timer > 0) fire1Timer -= Time.deltaTime;
        if (fire2Timer > 0) fire2Timer -= Time.deltaTime;

        anim.SetBool("Twirling", variation == 2 && inman.InputSource.Fire2.IsPressed);
        HandleInput();
        if (isCharging)
            HandleCharge();
    }
    void OnDisable()
    {
        isCharging = false;
        attackAgain = true;
        chargeT = 0;
        fire1Timer = 0;
        fire2Timer = 0;
    }

    void FixedUpdate()
    {
        if (variation == 2 && inman.InputSource.Fire2.IsPressed)
        {
            MonoSingleton<NewMovement>.Instance.rb.AddForce(
                MonoSingleton<CameraController>.Instance.transform.up * 1200f * twirling * Time.deltaTime,
                ForceMode.Acceleration);
        }
    }

    public void ThrowShield()
    {
        Transform t = CameraController.Instance.transform;
        Vector3 pos = CameraController.Instance.GetDefaultPos();

        ShieldProjectile sh = Instantiate(ShieldProj, pos, Quaternion.identity);
        sh.transform.forward = t.forward;
        sh.weapon = this;
        sh.sourceWeapon = sourceWeapon;
    }
    float twirling = 0;
    void HandleCharge()
    {
        float force = 100;
        attackAgain = true;
        Vector3 pos = CameraController.Instance.GetDefaultPos();
        Vector3 dir = CameraController.Instance.transform.forward;
        Collider[] cols = Physics.OverlapSphere(pos, 2.5f, LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment));

        List<EnemyIdentifier> eidsHit = new List<EnemyIdentifier>();
        foreach (var col in cols)
        {
            EnemyIdentifier eid;
            EnemyIdentifierIdentifier eidd;
            if (!col.gameObject.TryGetComponent<EnemyIdentifier>(out eid))
            {
                if (col.gameObject.TryGetComponent<EnemyIdentifierIdentifier>(out eidd))
                {
                    eid = eidd.eid;
                }
            }

            if (eid != null && !eidsHit.Contains(eid))
            {
                eidsHit.Add(eid);
                eid.hitter = "shieldcharge";
                eid.DeliverDamage(col.gameObject, dir * force, col.transform.position, 7.5f, true, sourceWeapon: sourceWeapon);
                if (eid != null && !lightEnemies.Contains(eid.enemyType))
                {
                    isCharging = false;
                    CameraController.Instance.CameraShake(2);
                    source.Play();
                    NewMovement.Instance.Launch(-dir, force / 2, true);
                }
            }
        }


        if (chargeT > 0) chargeT -= Time.deltaTime;
        else
        {
            if (NewMovement.Instance.gc.onGround) isCharging = false;
        }
    }

    void HandleInput()
    {
        if (inman.InputSource.Fire1.WasPerformedThisFrame && attackAgain && fire1Timer <= 0 && !isCharging && (variation != 2 || !inman.InputSource.Fire2.IsPressed))
        {
            attackAgain = false;
            fire1Timer = fire1Cooldown;
            anim.SetTrigger("Fire1");
        }

        if (variation == 2)
        {
            if (inman.InputSource.Fire2.WasCanceledThisFrame && twirling >= 2.99f)
            {
                GameObject biem = Instantiate(AssetsManager.Lightning, CameraController.Instance.GetDefaultPos(), CameraController.Instance.rotation);
                biem.transform.forward = CameraController.Instance.transform.forward;

                
            }

            if (inman.InputSource.Fire2.IsPressed && attackAgain)
            {
                if (!TwirlSound.isPlaying)
                {
                    anim.SetTrigger("StartTwirl");
                    TwirlSound.Play();
                }
                twirling += Time.deltaTime;
                twirling = Mathf.Clamp(twirling, 0f, 3f);
                anim.SetFloat("TwirlSpeed", twirling);
                Twirl.color = new Color(1, 1, 1, Mathf.Lerp(0, 1, twirling / 3));
                TwirlSound.pitch = twirling;

                if (twirling >= 2.99f && !fullyCharged)
                {
                    fullyCharged = true;

                    GameObject chargedFlash = Instantiate(AssetsManager.flash, CameraController.Instance.GetDefaultPos(), CameraController.Instance.rotation);
                    chargedFlash.transform.forward = CameraController.Instance.transform.forward;
                }
            }
            else
            {
                Twirl.color = new Color(1, 1, 1, 0);
                twirling = 0;
                TwirlSound.Stop();
                fullyCharged = false; // reset so it can trigger again next charge
            }
        }
        else
        {
            if (inman.InputSource.Fire2.WasPerformedThisFrame && attackAgain && fire2Timer <= 0)
            {
                switch (variation)
                {
                    case 0:
                        if (isCharging) return;
                        fire2Timer = fire2Cooldown;
                        Charge();
                        break;
                    case 1:
                        anim.SetTrigger("Fire2Throw");
                        noShield = true;
                        break;
                }
            }
        }

        
    }

    void Charge()
    {
        NewMovement.Instance.Launch(CameraController.Instance.transform.forward, chargeForce, true);
        chargeT = 0.25f; // atleast 0.25 seconds of charging
        isCharging = true;
        source.PlayOneShot(AssetsManager.Woosh);
    }
    bool fullyCharged = false;

    public void DealDamage()
    {
        bool hitSomething = false;
        float force = 50;
        attackAgain = true;
        Vector3 pos = CameraController.Instance.GetDefaultPos();
        Vector3 dir = CameraController.Instance.transform.forward;
        RaycastHit[] cols = Physics.SphereCastAll(pos, 2.5f, dir, 2, LayerMaskDefaults.Get(LMD.Enemies));

        List<EnemyIdentifier> eidsHit = new List<EnemyIdentifier>();
        foreach (var hit in cols)
        {
            hitSomething = true;
            Collider col = hit.collider;
            EnemyIdentifier eid;
            EnemyIdentifierIdentifier eidd;
            if(!col.gameObject.TryGetComponent<EnemyIdentifier>(out eid))
            {
                if (col.gameObject.TryGetComponent<EnemyIdentifierIdentifier>(out eidd))
                {
                    eid = eidd.eid;
                }
            }
            
            if(eid != null && !eidsHit.Contains(eid))
            {
                eidsHit.Add(eid);
                eid.hitter = "shield";
                eid.DeliverDamage(col.gameObject, dir * force, hit.point, 2.5f, true, sourceWeapon: sourceWeapon);

            }
        }

        source.PlayOneShot(hitSomething ? AssetsManager.PunchHeavy : AssetsManager.Woosh);
    }
}

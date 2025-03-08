using BananaDifficulty.Patches;
using UnityEngine;

namespace BananaDifficulty.MonoBehaviours
{
    public class V2Spear : MonoBehaviour
    {
        private void Start()
        {
            BananaDifficultyPlugin.Log.LogInfo("V2Spear Start");
            this.difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty", 0);
            this.lr = base.GetComponentInChildren<LineRenderer>();
            this.rb = base.GetComponent<Rigidbody>();
            this.aud = base.GetComponent<AudioSource>();
            //this.v2 = this.originPoint.parent.parent.parent.parent.parent.GetComponent<V2>();
            base.Invoke("CheckForDistance", 3f / this.speedMultiplier);
            if (this.difficulty == 1)
            {
                this.rb.AddForce(base.transform.forward * 75f * this.speedMultiplier, ForceMode.VelocityChange);
            }
            if (this.difficulty == 2)
            {
                this.rb.AddForce(base.transform.forward * 200f * this.speedMultiplier, ForceMode.VelocityChange);
                return;
            }
            if (this.difficulty >= 3)
            {
                this.rb.AddForce(base.transform.forward * 250f * this.speedMultiplier, ForceMode.VelocityChange);
            }
        }

        // Token: 0x06000E36 RID: 3638 RVA: 0x0007538F File Offset: 0x0007358F
        private void OnDisable()
        {
            if (!this.returning)
            {
                this.Return();
            }
        }

        // Token: 0x06000E37 RID: 3639 RVA: 0x000753A0 File Offset: 0x000735A0
        private void Update()
        {
            if (this.originPoint != null && !this.originPoint.gameObject.activeInHierarchy)
            {
                this.lr.SetPosition(0, this.originPoint.position);
                this.lr.SetPosition(1, this.lr.transform.position);
                if (this.returning)
                {
                    if (!this.originPoint || !this.originPoint.parent || !this.originPoint.parent.gameObject.activeInHierarchy)
                    {
                        Object.Destroy(base.gameObject);
                        return;
                    }
                    base.transform.rotation = Quaternion.LookRotation(base.transform.position - this.originPoint.position, Vector3.up);
                    this.rb.velocity = base.transform.forward * -100f * this.speedMultiplier;
                    if (Vector3.Distance(base.transform.position, this.originPoint.position) < 1f)
                    {
                        if (this.v2 != null)
                        {
                            
                        }
                        Object.Destroy(base.gameObject);
                        return;
                    }
                }
                else if (this.deflected)
                {
                    base.transform.LookAt(this.originPoint.position);
                    this.rb.velocity = base.transform.forward * 100f * this.speedMultiplier;
                    if (Vector3.Distance(base.transform.position, this.originPoint.position) < 1f && this.v2 != null)
                    {
                        //this.mass.SpearReturned();
                        BloodsplatterManager instance = MonoSingleton<BloodsplatterManager>.Instance;
                        EnemyIdentifier component = this.v2.eid;
                        Transform child = this.v2.mac.chest.transform;
                        this.HurtEnemy(child.gameObject, component);
                        for (int i = 0; i < 3; i++)
                        {
                            GameObject gore = instance.GetGore(GoreType.Head, component, false);
                            gore.transform.position = child.position;
                            GoreZone goreZone = GoreZone.ResolveGoreZone(base.transform);
                            if (goreZone)
                            {
                                gore.transform.SetParent(goreZone.goreZone);
                            }
                        }
                        //this.mass.SpearParried();
                        Object.Destroy(base.gameObject);
                        return;
                    }
                }
                else if (this.hitPlayer && !this.returning)
                {
                    if (this.nmov.hp <= 0)
                    {
                        this.Return();
                        Object.Destroy(base.gameObject);
                    }
                    if (this.spearHealth > 0f)
                    {
                        this.spearHealth = Mathf.MoveTowards(this.spearHealth, 0f, Time.deltaTime);
                        return;
                    }
                    if (this.spearHealth <= 0f)
                    {
                        this.Return();
                    }
                }
                return;
            }
            //Object.Destroy(base.gameObject);
        }

        // Token: 0x06000E38 RID: 3640 RVA: 0x00075694 File Offset: 0x00073894
        private void HurtEnemy(GameObject target, EnemyIdentifier eid = null)
        {
            if (eid == null)
            {
                eid = target.GetComponent<EnemyIdentifier>();
                if (!eid)
                {
                    EnemyIdentifierIdentifier component = target.GetComponent<EnemyIdentifierIdentifier>();
                    if (component)
                    {
                        eid = component.eid;
                    }
                }
            }
            if (eid != null && target == null)
            {
                target = eid.gameObject;
            }
            if (!eid)
            {
                return;
            }
            if(eid.enemyType == EnemyType.V2Second)
            {
                return;
            }
            eid.DeliverDamage(target, Vector3.zero, this.originPoint.position, 30f * this.damageMultiplier, false, 0f, null, false, false);
        }

        // Token: 0x06000E39 RID: 3641 RVA: 0x00075720 File Offset: 0x00073920
        private void OnTriggerEnter(Collider other)
        {
            if (this.beenStopped)
            {
                return;
            }
            if (!this.hitPlayer && !this.hittingPlayer && other.gameObject.CompareTag("Player"))
            {
                this.hittingPlayer = true;
                this.beenStopped = true;
                this.rb.isKinematic = true;
                this.rb.useGravity = false;
                this.rb.velocity = Vector3.zero;
                base.transform.position = MonoSingleton<CameraController>.Instance.GetDefaultPos();
                base.Invoke("DelayedPlayerCheck", 0.05f);
                return;
            }
            if (LayerMaskDefaults.IsMatchingLayer(other.gameObject.layer, LMD.Environment))
            {
                this.beenStopped = true;
                this.rb.velocity = Vector3.zero;
                this.rb.useGravity = false;
                base.transform.position += base.transform.forward * 2f;
                base.Invoke("Return", 2f / this.speedMultiplier);
                this.aud.pitch = 1f;
                this.aud.clip = this.stop;
                this.aud.Play();
                return;
            }
            if (this.target != null && this.target.isEnemy && (other.gameObject.CompareTag("Head") || other.gameObject.CompareTag("Body") || other.gameObject.CompareTag("Limb") || other.gameObject.CompareTag("EndLimb")) && !other.gameObject.CompareTag("Armor"))
            {
                EnemyIdentifierIdentifier componentInParent = other.gameObject.GetComponentInParent<EnemyIdentifierIdentifier>();
                EnemyIdentifier enemyIdentifier = null;
                if (componentInParent != null && componentInParent.eid != null)
                {
                    enemyIdentifier = componentInParent.eid;
                }
                if (enemyIdentifier == null || enemyIdentifier != this.target.enemyIdentifier)
                {
                    return;
                }
                if (enemyIdentifier != null)
                {
                    this.HurtEnemy(other.gameObject, enemyIdentifier);
                    this.Return();
                }
            }
        }

        // Token: 0x06000E3A RID: 3642 RVA: 0x00075938 File Offset: 0x00073B38
        private void DelayedPlayerCheck()
        {
            if (this.deflected)
            {
                return;
            }
            this.hittingPlayer = false;
            this.hitPlayer = true;
            this.nmov = MonoSingleton<NewMovement>.Instance;
            this.nmov.GetHurt(Mathf.RoundToInt(25f * this.damageMultiplier), true, 1f, false, false, 0.35f, false);
            this.nmov.slowMode = true;
            base.transform.position = this.nmov.transform.position;
            base.transform.SetParent(this.nmov.transform, true);
            this.rb.velocity = Vector3.zero;
            this.rb.useGravity = false;
            this.rb.isKinematic = true;
            this.beenStopped = true;
            base.GetComponent<CapsuleCollider>().radius *= 0.1f;
            this.aud.pitch = 1f;
            this.aud.clip = this.hit;
            this.aud.Play();
        }

        // Token: 0x06000E3B RID: 3643 RVA: 0x00075A41 File Offset: 0x00073C41
        public void GetHurt(float damage)
        {
            Object.Instantiate<GameObject>(this.breakMetalSmall, base.transform.position, Quaternion.identity);
            this.spearHealth -= ((this.difficulty >= 4) ? (damage / 1.5f) : damage);
        }

        // Token: 0x06000E3C RID: 3644 RVA: 0x00075A7F File Offset: 0x00073C7F
        public void Deflected()
        {
            this.deflected = true;
            this.rb.isKinematic = false;
            base.GetComponent<Collider>().enabled = false;
        }

        // Token: 0x06000E3D RID: 3645 RVA: 0x00075AA0 File Offset: 0x00073CA0
        private void Return()
        {
            if (this.hitPlayer)
            {
                this.nmov.slowMode = false;
                base.transform.SetParent(null, true);
                this.rb.isKinematic = false;
            }
            if (base.gameObject.activeInHierarchy)
            {
                this.aud.clip = this.stop;
                this.aud.pitch = 1f;
                this.aud.Play();
            }
            this.returning = true;
            this.beenStopped = true;
            WorseV2.threwSpear[this.v2] = false;
        }

        // Token: 0x06000E3E RID: 3646 RVA: 0x00075B24 File Offset: 0x00073D24
        private void CheckForDistance()
        {
            if (!this.returning && !this.beenStopped && !this.hitPlayer && !this.deflected)
            {
                this.returning = true;
                this.beenStopped = true;
                base.transform.position = this.originPoint.position;
            }
        }

        // Token: 0x040013F0 RID: 5104
        public EnemyTarget target;

        // Token: 0x040013F1 RID: 5105
        private LineRenderer lr;

        // Token: 0x040013F2 RID: 5106
        private Rigidbody rb;

        // Token: 0x040013F3 RID: 5107
        public bool hittingPlayer;

        // Token: 0x040013F4 RID: 5108
        public bool hitPlayer;

        // Token: 0x040013F5 RID: 5109
        public bool beenStopped;

        // Token: 0x040013F6 RID: 5110
        private bool returning;

        // Token: 0x040013F7 RID: 5111
        private bool deflected;

        // Token: 0x040013F8 RID: 5112
        public Transform originPoint;

        // Token: 0x040013F9 RID: 5113
        private NewMovement nmov;

        // Token: 0x040013FA RID: 5114
        public float spearHealth;

        // Token: 0x040013FB RID: 5115
        private int difficulty;

        // Token: 0x040013FC RID: 5116
        public GameObject breakMetalSmall;

        // Token: 0x040013FD RID: 5117
        private AudioSource aud;

        // Token: 0x040013FE RID: 5118
        public AudioClip hit;

        // Token: 0x040013FF RID: 5119
        public AudioClip stop;

        // Token: 0x04001400 RID: 5120
        public V2 v2;

        // Token: 0x04001401 RID: 5121
        public float speedMultiplier = 1f;

        // Token: 0x04001402 RID: 5122
        public float damageMultiplier = 1f;
    }
}

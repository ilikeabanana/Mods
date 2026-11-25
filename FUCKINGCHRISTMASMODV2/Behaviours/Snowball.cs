using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FUCKINGCHRISTMASMOD.Behaviours
{
    public class Snowball : MonoBehaviour
    {
        float damage = 15f;
        public GameObject snowPartilce;
        bool destroyed = false;
        private void Start()
        {
            this.itid = base.GetComponent<ItemIdentifier>();
            this.rb = base.GetComponent<Rigidbody>();
        }

        // Token: 0x06001DA1 RID: 7585 RVA: 0x000E844F File Offset: 0x000E664F
        private void FixedUpdate()
        {
            if (this.rb)
            {
                this.velocityBeforeCollision = this.rb.velocity;
            }
        }

        public void Kaboom()
        {
            destroyed = true;
            Instantiate(snowPartilce, transform.position, transform.rotation);
            Destroy(gameObject);
        }

        // Token: 0x06001DA2 RID: 7586 RVA: 0x000E8470 File Offset: 0x000E6670
        private void OnCollisionEnter(Collision collision)
        {
            if (destroyed) return;
            if (!this.itid.pickedUp && this.velocityBeforeCollision.magnitude > 15f)
            {
                EnemyIdentifierIdentifier enemyIdentifierIdentifier;
                if ((collision.gameObject.layer == 11 || collision.gameObject.layer == 10) && collision.gameObject.TryGetComponent<EnemyIdentifierIdentifier>(out enemyIdentifierIdentifier))
                {
                    if (enemyIdentifierIdentifier.eid)
                    {
                        enemyIdentifierIdentifier.eid.DeliverDamage(collision.gameObject, Vector3.zero, collision.GetContact(0).point, damage, true, 0f, null, false, false);
                        Kaboom();
                    }
                    this.rb.velocity = Vector3.zero;
                    return;
                }
                Breakable breakable;
                if (collision.gameObject.TryGetComponent<Breakable>(out breakable) && !breakable.specialCaseOnly)
                {
                    breakable.Break();
                    this.rb.velocity = Vector3.zero;
                    return;
                }
                Bleeder bleeder;
                if (collision.gameObject.TryGetComponent<Bleeder>(out bleeder))
                {
                    bleeder.GetHit(base.transform.position, GoreType.Head, false);
                }
            }

            if (!itid.pickedUp && LayerMaskDefaults.IsMatchingLayer(collision.gameObject.layer, LMD.Environment))
            {
                Kaboom();
            }
        }

        // Token: 0x06001DA3 RID: 7587 RVA: 0x000E8570 File Offset: 0x000E6770
        public void HitWith(GameObject target)
        {
            if (destroyed) return;
            EnemyIdentifierIdentifier enemyIdentifierIdentifier;
            if (target.TryGetComponent<EnemyIdentifierIdentifier>(out enemyIdentifierIdentifier))
            {
                enemyIdentifierIdentifier.eid.DeliverDamage(target, Vector3.zero, target.transform.position, damage, true, 0f, null, false, false);
                Kaboom();
            }
        }

        // Token: 0x04002659 RID: 9817
        private ItemIdentifier itid;

        // Token: 0x0400265A RID: 9818
        private Rigidbody rb;

        // Token: 0x0400265B RID: 9819
        private Vector3 velocityBeforeCollision;
    }
}

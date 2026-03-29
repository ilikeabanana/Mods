using System.Collections;
using System.Collections.Generic;
using Ultrarogue;
using UnityEngine;

public class Missle : MonoBehaviour
{
    public EnemyIdentifier enemyThatGotHit;
    private EnemyIdentifier target;

    public float speed = 25f;
    public float upwardForce = 15f;
    public float homingDelay = 0.5f;
    public float turnSpeed = 5f;

    public float damage = 10f;
    public float explosionRadius = 0.1f;

    private bool homingActive = false;
    private Rigidbody rb;

    bool kaboomed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.velocity = Vector3.up * upwardForce;

        target = enemyThatGotHit;

        StartCoroutine(ActivateHoming());
    }

    IEnumerator ActivateHoming()
    {
        yield return new WaitForSeconds(homingDelay);
        homingActive = true;
    }

    void FixedUpdate()
    {
        if (!homingActive)
            return;

        if (target == null || target.dead)
        {
            List<EnemyIdentifier> enemies = EnemyTracker.Instance.GetCurrentEnemies();

            if (enemies.Count > 0)
            {
                target = enemies[Random.Range(0, enemies.Count)];
            }
            else
            {
                return;
            }
        }
        Vector3 point = target.weakPoint == null ? target.transform.position : target.weakPoint.transform.position;
        Vector3 dir = (point - transform.position).normalized;

        Vector3 newVelocity = Vector3.Lerp(rb.velocity, dir * speed, turnSpeed * Time.fixedDeltaTime);
        rb.velocity = newVelocity;

        transform.forward = rb.velocity.normalized;
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.GetComponent<NewMovement>()) return;

        if (!LayerMaskDefaults.IsMatchingLayer(col.gameObject.layer, LMD.EnemiesAndEnvironment)) return;

        if (kaboomed) return;
        kaboomed = true;
        Plugin.Logger.LogInfo($"I hit {col.gameObject.name}");
        EnemyIdentifierIdentifier enemy = col.gameObject.GetComponent<EnemyIdentifierIdentifier>();

        EnemyIdentifier eid = enemy == null ? col.gameObject.GetComponent<EnemyIdentifier>() : enemy.eid;

        if (eid != null)
        {
            eid.hitter = "missle";
            eid.DeliverDamage(
                col.gameObject,
                Vector3.zero,
                enemy.transform.position,
                multiplier: damage,
                false
            );

            
        }
        Explode();
    }

    void Explode()
    {
        GameObject explosion = Object.Instantiate(
            DefaultReferenceManager.Instance.explosion,
            transform.position,
            Quaternion.identity
        );

        foreach (var exp in explosion.GetComponentsInChildren<Explosion>())
        {
            exp.maxSize = explosionRadius;
            exp.canHit = AffectedSubjects.EnemiesOnly;
            exp.damage = Mathf.RoundToInt(damage) * 2;
        }

        Destroy(gameObject);
    }
}

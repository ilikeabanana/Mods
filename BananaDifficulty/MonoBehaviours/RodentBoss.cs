using BananaDifficulty;
using BananaDifficulty.Patches;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RodentBoss : MonoBehaviour
{
    public Transform phase1Escape;
    public Transform phase1ArenaStart;
    public Transform phase2Escape;
    public Transform phase2ArenaStart;

    private CancerousRodent _rodent;
    public bool isTransitioning = false;

    public bool canExecute = true;
    public bool isSlamming = false;

    float attackCooldown = 0f;
    int previousPhase = 1;

    // Per-attack cooldown durations
    private static readonly Dictionary<int, float> AttackCooldownDurations = new Dictionary<int, float>
    {
        { 0,  3.0f },   // dashAttack
        { 1,  5.0f },   // rocketSummon
        { 2,  3.0f },   // spinnyForwardArrow
        { 3,  4.0f },   // lightningBarrage
        { 4,  10.0f },  // mithrixSlam
        { 5,  5.0f },   // shockwaveRing
        { 6,  4.5f },   // homingBarrage
        { 7,  6.0f },   // SpawnBeamCoroutine
        { 8,  7.0f },   // spawnFalls
        { 9,  5.5f },   // thrownSwordBarrage
        { 10, 14.0f },  // blackHoleTrap
        { 11, 6.0f },   // swordRain
        { 12, 9.0f },   // beamSpin x3
        { 13, 7.0f },   // spearPincer
        { 14, 5.0f },   // mirrorReaperSlam
        { 15, 2.5f }
    };

    // Tracks remaining cooldown per attack index
    private Dictionary<int, float> _attackCooldowns = new Dictionary<int, float>();

    public bool shouldBeDoingStuff
    {
        get
        {
            return !isTransitioning && canExecute && !isSlamming;
        }
    }

    float timeBeforePizza = 0;
    void Awake()
    {
        _rodent = GetComponent<CancerousRodent>();

        // Initialize all cooldowns to 0 so every attack is available at start
        foreach (int key in AttackCooldownDurations.Keys)
            _attackCooldowns[key] = 0f;
    }

    public void BossUpdate()
    {
        if (!_rodent.harmless) return;
        if (!shouldBeDoingStuff) return;

        HandleAttacks();
    }

    void HandleAttacks()
    {
        int phase;

        if (_rodent.enemy.health >= 466)
            phase = 1;
        else if (_rodent.enemy.health <= 234)
            phase = 3;
        else
            phase = 2;

        if (previousPhase != phase)
        {
            DoPhaseTransition(phase);
        }

        if (previousPhase != 3 && phase == 3)
        {
            GameObject enrage = Instantiate(DefaultReferenceManager.Instance.enrageEffect, transform);

            _rodent.eid.onDeath.AddListener(() =>
            {
                Destroy(enrage);
            });

            GameObject idol = Instantiate(
                DefaultReferenceManager.Instance.GetEnemyPrefab(EnemyType.Idol),
                transform);

            idol.GetComponent<Idol>().target = _rodent.eid;
            idol.transform.localPosition = Vector3.up * 5;
            idol.name += "DontRadiant";
        }

        previousPhase = phase;

        // Tick down all individual cooldowns
        List<int> keys = new List<int>(_attackCooldowns.Keys);
        foreach (int key in keys)
        {
            if (_attackCooldowns[key] > 0f)
                _attackCooldowns[key] -= Time.deltaTime;
        }

        attackCooldown -= Time.deltaTime;

        if (attackCooldown <= 0)
        {
            ExecuteAttack(phase);
            attackCooldown = Random.Range(0, phase == 3 ? 2f : 4.5f);
        }

        if(timeBeforePizza > 0)
        {
            timeBeforePizza -= Time.deltaTime;
            if(timeBeforePizza < 0)
            {
                StartCoroutine(pizza());
            }
        }
    }

    // Returns true if the given attack index is off cooldown
    bool IsAttackReady(int attack)
    {
        return !_attackCooldowns.ContainsKey(attack) || _attackCooldowns[attack] <= 0f;
    }

    // Puts an attack on its individual cooldown
    void SetAttackCooldown(int attack)
    {
        if (AttackCooldownDurations.TryGetValue(attack, out float duration))
            _attackCooldowns[attack] = duration;
    }

    void ExecuteAttack(int phase)
    {
        int maxAttack = phase == 1 ? 5 : phase == 2 ? 10 : 16;

        // Build a pool of only ready attacks within this phase's range
        List<int> available = new List<int>();
        for (int i = 0; i < maxAttack; i++)
        {
            if (IsAttackReady(i))
                available.Add(i);
        }

        // All attacks on cooldown — do nothing this cycle
        if (available.Count == 0) return;

        int attack = available[Random.Range(0, available.Count)];
        SetAttackCooldown(attack);

        switch (attack)
        {
            case 0: StartCoroutine(dashAttack()); break;
            case 1: StartCoroutine(goMyRats()); break;
            case 2: StartCoroutine(rocketSummon()); break;
            case 3: StartCoroutine(spinnyForwardArrow()); break;
            case 4: StartCoroutine(lightningBarrage()); break;
            case 5: StartCoroutine(mithrixSlam()); break;
            case 6: StartCoroutine(shockwaveRing()); break;
            case 7: StartCoroutine(homingBarrage()); break;
            case 8: StartCoroutine(SpawnBeamCoroutine()); break;
            case 9: StartCoroutine(spawnFalls()); break;
            case 10: StartCoroutine(thrownSwordBarrage()); break;
            case 11: StartCoroutine(blackHoleTrap()); break;
            case 12: StartCoroutine(swordRain()); break;
            case 13:
                for (int i = 0; i < 3; i++)
                    StartCoroutine(beamSpin());
                break;
            case 14: StartCoroutine(spearPincer()); break;
            case 15: StartCoroutine(mirrorReaperSlam()); break;
        }
    }


    public void DoPhaseTransition(int newPhase)
    {
        Transform target = newPhase == 2 ? phase1Escape : phase2Escape;
        Transform targetAr = newPhase == 2 ? phase1ArenaStart : phase2ArenaStart;
        if (target != null)
            StartCoroutine(PhaseTransitionRoutine(target, targetAr));
    }

    public void PlayerReachedArena()
    {
        BananaDifficultyPlugin.Log.LogInfo("Alright goody :D");
        canExecute = true;
    }

    float tiem;

    void Update()
    {
        if (!canExecute)
        {
            tiem += Time.deltaTime;
            if (tiem > 1)
            {
                NewMovement.Instance.GetHurt(5, false);
                tiem = 0;
            }
        }
    }

    private IEnumerator PhaseTransitionRoutine(Transform escapePoint, Transform arenaPoint)
    {
        isTransitioning = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = escapePoint.position;
        float duration = 0.5f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, t / duration);
            yield return null;
        }

        transform.position = endPos;
        escapePoint.parent.gameObject.SetActive(false);
        Object.Instantiate(BananaDifficultyPlugin.rubbleBig, endPos, Quaternion.identity);
        transform.position = arenaPoint.position;

        HudMessageReceiver.Instance.SendHudMessage("<color=orange>[WARNING]</color> Increased radiation detected.");

        isTransitioning = false;
        canExecute = false;
    }

    #region Attacking

    IEnumerator pizza()
    {
        isSlamming = true;

        const int pillarCount = 9;
        const float radius = 10f;
        const float rotSpeed = 25f;
        const float windupDuration = 2f;
        const float strikeRadius = 2.5f;

        for (int rep = 0; rep < 5; rep++)
        {
            float rotationDir = Random.value > 0.5f ? 1f : -1f;

            // Pivot so all pillars rotate as a group
            GameObject pivot = new GameObject("PizzaPivot");
            pivot.transform.position = _rodent.transform.position;

            GameObject[] pillars = new GameObject[pillarCount];

            for (int i = 0; i < pillarCount; i++)
            {
                float angle = i * (360f / pillarCount);
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
                Vector3 pos = GetGroundPosition(_rodent.transform.position + offset);

                pillars[i] = Object.Instantiate(
                    BananaDifficultyPlugin.pizzaAttack,
                    pos,
                    Quaternion.Euler(0, angle, 0),
                    pivot.transform   // child of pivot so it rotates with it
                );
            }

            // Rotate for the windup period
            float t = 0f;
            while (t < windupDuration)
            {
                t += Time.deltaTime;
                pivot.transform.Rotate(0f, rotationDir * rotSpeed * Time.deltaTime, 0f);
                yield return null;
            }

            // Snapshot world-space strike positions before the pivot is destroyed
            Vector3[] strikePositions = new Vector3[pillarCount];
            for (int i = 0; i < pillarCount; i++)
                strikePositions[i] = pillars[i].GetComponentInChildren<Collider>().ClosestPoint(NewMovement.Instance.transform.position);

            Object.Destroy(pivot);

            // Strike — spawn explosion at each pillar's final position
            foreach (Vector3 pos in strikePositions)
            {
                Object.Instantiate(BananaDifficultyPlugin.lightningExplosion, pos, Quaternion.identity);
                if (Vector3.Distance(NewMovement.Instance.transform.position, pos) <= strikeRadius)
                {
                    NewMovement.Instance.GetHurt(50, false);   // 90 HP ≈ 900 % of base 10
                }
            }

            // Brief gap between each of the 5 repetitions
            if (rep < 4)
                yield return new WaitForSeconds(0.75f);
        }

        isSlamming = false;
    }

    IEnumerator goMyRats()
    {
        int amount = Random.Range(0, 15);

        for (int i = 0; i < amount; i++)
        {
            GameObject rat = Object.Instantiate(BananaDifficultyPlugin.ratAttack, transform.position,  Quaternion.identity);
            if (rat.TryGetComponent<Projectile>(out Projectile proj))
            {
                proj.safeEnemyType = _rodent.eid.enemyType;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator mithrixSlam()
    {
        isSlamming = true;
        Vector3 orPos = transform.position;
        Vector3 highPos = orPos + Vector3.up * 1000;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(orPos, highPos, t);
            yield return null;
        }
        transform.position = highPos;

        yield return new WaitForSeconds(3);

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(highPos, orPos, t);
            yield return null;
        }
        transform.position = orPos;

        Object.Instantiate(BananaDifficultyPlugin.shockwave, orPos, Quaternion.identity);
        isSlamming = false;
        timeBeforePizza = 8;
    }

    IEnumerator lightningBarrage()
    {
        int count = Random.Range(3, 6);
        Vector3[] positions = new Vector3[count];
        GameObject[] lightnings = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = _rodent.eid.target.position + new Vector3(
                Random.Range(-12f, 12f), 0,
                Random.Range(-12f, 12f)
            );
            pos = GetGroundPosition(pos);
            positions[i] = pos;

            lightnings[i] = Object.Instantiate(BananaDifficultyPlugin.lightningWindup, pos, Quaternion.identity);

            yield return new WaitForSeconds(0.18f);
        }

        yield return new WaitForSeconds(0.6f);

        for (int i = 0; i < count; i++)
        {
            Object.Instantiate(BananaDifficultyPlugin.lightningExplosion, positions[i], Quaternion.identity);
            Object.Destroy(lightnings[i]);
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator shockwaveRing()
    {
        Object.Instantiate(BananaDifficultyPlugin.bigExplosion, _rodent.transform.position, Quaternion.identity);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Quaternion rot = Quaternion.Euler(0f, angle, 0f);
            GameObject wave = Object.Instantiate(
                BananaDifficultyPlugin.shockwave,
                _rodent.transform.position,
                rot
            );

            foreach (var hz in wave.GetComponentsInChildren<HurtZone>(true))
                hz.ignoredEnemyTypes.Add(_rodent.eid.enemyType);

            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator thrownSwordBarrage()
    {
        int count = Random.Range(5, 9);
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnOffset = Quaternion.Euler(0, i * (360f / count), 0) * Vector3.forward * 8f;
            Vector3 spawnPos = _rodent.transform.position + spawnOffset + Vector3.up * 1.5f;

            Vector3 dir = (_rodent.eid.target.PredictTargetPosition(0.3f) - spawnPos).normalized;

            GameObject sword = Object.Instantiate(
                BananaDifficultyPlugin.thrownSwordH,
                spawnPos,
                Quaternion.LookRotation(dir)
            );

            if (sword.GetComponentInChildren<ThrownSword>())
            {
                ThrownSword tSword = sword.GetComponentInChildren<ThrownSword>();
                tSword.targetPos = _rodent.eid.target.PredictTargetPosition(0.6f);
                tSword.thrownBy = _rodent.eid;
                tSword.difficulty = _rodent.eid.difficulty;
            }

            yield return new WaitForSeconds(0.12f);
        }
    }

    IEnumerator spearPincer()
    {
        Object.Instantiate(BananaDifficultyPlugin.v2FlashUnpariable, _rodent.eid.target.position, Quaternion.identity);
        yield return new WaitForSeconds(0.5f);

        Vector3 center = _rodent.eid.target.position;

        Vector3[] directions = new Vector3[]
        {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right
        };

        foreach (Vector3 dir in directions)
        {
            Vector3 spawnPos = center + dir * 14f + Vector3.up * 1.2f;
            Vector3 aimDir = (center - spawnPos).normalized;

            GameObject spear = Object.Instantiate(
                BananaDifficultyPlugin.gabrielThrownSpear,
                spawnPos,
                Quaternion.LookRotation(aimDir)
            );

            if (spear.TryGetComponent<Projectile>(out Projectile proj))
            {
                proj.target = _rodent.eid.target;
                proj.safeEnemyType = _rodent.eid.enemyType;
                proj.damage *= _rodent.eid.totalDamageModifier;
                proj.speed *= _rodent.eid.totalSpeedModifier;
            }
        }
    }

    IEnumerator mirrorReaperSlam()
    {
        Vector3 targetPos = _rodent.eid.target.position;
        Vector3 dir = (targetPos - _rodent.transform.position).normalized;
        dir.y = 0;

        for (int i = -1; i <= 1; i++)
        {
            Vector3 spreadDir = Quaternion.Euler(0, i * 20f, 0) * dir;
            GameObject wave = Object.Instantiate(
                BananaDifficultyPlugin.mirrorReaperWave,
                _rodent.transform.position + Vector3.up * 0.1f,
                Quaternion.LookRotation(spreadDir)
            );

            foreach (var hz in wave.GetComponentsInChildren<HurtZone>(true))
                hz.ignoredEnemyTypes.Add(_rodent.eid.enemyType);

            if (wave.TryGetComponent<GroundWave>(out GroundWave gw))
            {
                gw.target = _rodent.eid.target;
                gw.eid = _rodent.eid;

                Breakable componentInChildren = gw.GetComponentInChildren<Breakable>();
                if (componentInChildren)
                {
                    componentInChildren.durability = 5;
                }

                gw.transform.SetParent(_rodent.transform.parent ? _rodent.transform.parent : GoreZone.ResolveGoreZone(_rodent.transform).transform);
            }

            wave.AddComponent<MoveForward>();

            yield return null;
        }
    }

    IEnumerator beamSpin()
    {
        yield return new WaitForSeconds(Random.Range(0, 0.75f));
        GameObject beam = Object.Instantiate(
            BananaDifficultyPlugin.mindBeam,
            _rodent.transform.position,
            Quaternion.identity
        );

        beam.transform.parent = _rodent.transform;

        if (beam.TryGetComponent<ContinuousBeam>(out ContinuousBeam abeam))
        {
            abeam.safeEnemyType = _rodent.eid.enemyType;
        }

        float t = 0;

        while (t < 4)
        {
            t += Time.deltaTime;
            beam.transform.Rotate(Vector3.up * 220f * Time.deltaTime);
            yield return null;
        }

        Object.Destroy(beam);
    }

    IEnumerator spinnyForwardArrow()
    {
        GameObject newSpin = new GameObject("SPIUIIINNNNN");
        newSpin.transform.position = _rodent.transform.position;
        for (int i = 0; i < 4; i++)
        {
            GameObject arrow = Object.Instantiate(BananaDifficultyPlugin.forwardArrow, newSpin.transform);
            arrow.transform.Rotate(0, 90 * i, 0);
            arrow.GetComponentInChildren<HurtZone>(true).ignoredEnemyTypes.Add(_rodent.eid.enemyType);
        }

        float t = 0;

        while (t <= 15)
        {
            t += Time.deltaTime;
            newSpin.transform.Rotate(0, 50 * Time.deltaTime, 0);
            yield return null;
        }

        Object.Destroy(newSpin);
    }

    IEnumerator rocketSummon()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 spawn = _rodent.transform.position + new Vector3(
                Random.Range(-6, 6),
                10f,
                Random.Range(-6, 6)
            );

            spawn = GetGroundPosition(spawn) + Vector3.up * 1.5f;

            GameObject rocket = Object.Instantiate(
                BananaDifficultyPlugin.RocketEnemy,
                spawn,
                Quaternion.identity
            );

            if (rocket.TryGetComponent<Grenade>(out Grenade greg))
            {
                greg.enemy = true;
                greg.ignoreEnemyType.Add(_rodent.eid.enemyType);
            }

            rocket.transform.forward = (_rodent.eid.target.PredictTargetPosition(0.5f) - _rodent.transform.position).normalized;

            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator swordRain()
    {
        for (int i = 0; i < 4; i++)
        {
            Vector3 pos = _rodent.eid.target.position +
                          new Vector3(Random.Range(-8, 8), 12, Random.Range(-8, 8));

            GameObject currentSwords = Object.Instantiate(
                BananaDifficultyPlugin.summonedSwords,
                pos,
                Quaternion.identity
            );

            if (currentSwords.TryGetComponent<SummonedSwords>(out SummonedSwords summonedSwords))
            {
                summonedSwords.target = new EnemyTarget(_rodent.transform);
                summonedSwords.speed *= _rodent.eid.totalSpeedModifier;
                summonedSwords.targetEnemy = _rodent.eid.target;
            }
            foreach (Projectile projectile in currentSwords.GetComponentsInChildren<Projectile>())
            {
                projectile.target = _rodent.eid.target;
                projectile.safeEnemyType = _rodent.eid.enemyType;
                if (_rodent.eid.totalDamageModifier != 1f)
                {
                    projectile.damage *= _rodent.eid.totalDamageModifier;
                }
            }

            yield return new WaitForSeconds(0.08f);
        }
    }

    IEnumerator spawnFalls()
    {
        float distance = 100;
        int num;

        int amount = Random.Range(3, 8);

        for (int i = 0; i < amount; i = num + 1)
        {
            Vector3 vector = new Vector3(
                _rodent.transform.position.x + Random.Range(distance, -distance),
                _rodent.transform.position.y,
                _rodent.transform.position.z + Random.Range(distance, -distance)
            );
            if (Vector3.Distance(_rodent.transform.position, vector) > distance)
            {
                vector = _rodent.transform.position + (vector - _rodent.transform.position).normalized * distance;
            }
            vector = GetGroundPosition(vector);

            GameObject warning = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(warning.GetComponent<Collider>());
            warning.GetComponent<Renderer>().material = BananaDifficultyPlugin.fallAttack.GetComponent<Renderer>().material;
            warning.transform.position = vector;

            StartCoroutine(growSize(warning));

            yield return new WaitForSecondsRealtime(0.25f);
            num = i;
        }
    }

    IEnumerator growSize(GameObject increase)
    {
        float t = 0;
        GameObject result = Object.Instantiate(BananaDifficultyPlugin.fallAttack, increase.transform.position + Vector3.up * 500, BananaDifficultyPlugin.fallAttack.transform.rotation);
        while (t <= 1)
        {
            increase.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 10, t);
            result.transform.position = Vector3.Lerp(increase.transform.position + Vector3.up * 500, increase.transform.position, t);
            t += Time.deltaTime / 2;
            yield return null;
        }
        Object.Instantiate(BananaDifficultyPlugin.rubbleBig, increase.transform.position, BananaDifficultyPlugin.rubbleBig.transform.rotation);
        result.AddComponent<ShakyRock>();

        Object.Destroy(increase);
    }

    IEnumerator blackHoleTrap()
    {
        Vector3 pos = GetGroundPosition(_rodent.eid.target.position);
        Object.Instantiate(BananaDifficultyPlugin.blackHoleExplosion, pos, Quaternion.identity);

        yield return new WaitForSeconds(0.4f);

        GameObject black = Object.Instantiate(
            BananaDifficultyPlugin.blackHole,
            pos,
            Quaternion.identity
        );

        if (black.TryGetComponent<BlackHoleProjectile>(out BlackHoleProjectile bl))
        {
            bl.enemy = true;
            bl.safeType = _rodent.eid.enemyType;
            bl.target = _rodent.eid.target;
            bl.speed *= 3;
            bl.Activate();
        }

        Object.Destroy(black, 18f);
    }

    IEnumerator homingBarrage()
    {
        int amount = Random.Range(5, 15);
        for (int i = 0; i < amount; i++)
        {
            Vector3 spawn = _rodent.transform.position + new Vector3(
                Random.Range(-10, 10),
                Random.Range(5, 10),
                Random.Range(-10, 10)
            );

            GameObject proj = Object.Instantiate(
                BananaDifficultyPlugin.projHoming,
                spawn,
                Quaternion.identity
            );

            if (proj.TryGetComponent<Projectile>(out Projectile porj))
            {
                porj.safeEnemyType = _rodent.eid.enemyType;
            }

            yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator SpawnBeamCoroutine()
    {
        float distance = 100;
        int num;
        for (int i = 0; i < 5; i = num + 1)
        {
            Vector3 vector = new Vector3(
                _rodent.transform.position.x + Random.Range(distance, -distance),
                _rodent.transform.position.y,
                _rodent.transform.position.z + Random.Range(distance, -distance)
            );
            if (Vector3.Distance(_rodent.transform.position, vector) > distance)
            {
                vector = _rodent.transform.position + (vector - _rodent.transform.position).normalized * distance;
            }
            vector.y -= 5f;
            GameObject result = Object.Instantiate<GameObject>(
                BananaDifficultyPlugin.upArrow,
                (i == 0) ? new Vector3(_rodent.eid.target.position.x, _rodent.transform.position.y - 5f, _rodent.eid.target.position.z) : vector,
                Quaternion.LookRotation(Vector3.up)
            );

            result.transform.SetParent(_rodent.transform.parent, true);
            result.GetComponentInChildren<HurtZone>(true).ignoredEnemyTypes.Add(_rodent.eid.enemyType);

            yield return new WaitForSecondsRealtime(0.25f);
            num = i;
        }
        yield return null;
        yield break;
    }

    IEnumerator dashAttack()
    {
        Object.Instantiate(BananaDifficultyPlugin.v2FlashUnpariable, _rodent.transform.position, Quaternion.identity);

        Vector3 dir = (_rodent.eid.target.PredictTargetPosition(0.5f) - _rodent.transform.position).normalized;
        dir.y = 0;
        yield return new WaitForSeconds(0.35f);

        float t = 0;

        while (t < 0.5f)
        {
            t += Time.deltaTime;
            _rodent.transform.position += dir * 100 * Time.deltaTime;
            if (Vector3.Distance(_rodent.transform.position, _rodent.eid.target.position) <= 0.5f)
            {
                if (_rodent.eid.target.isPlayer)
                    NewMovement.Instance.GetHurt(15, false);
                if (_rodent.eid.target.isEnemy)
                {
                    _rodent.eid.target.enemyIdentifier.hitter = "rat";
                    _rodent.eid.target.enemyIdentifier.SimpleDamage(15);
                }
            }
            yield return null;
        }

        Object.Instantiate(BananaDifficultyPlugin.bigExplosion, _rodent.transform.position, Quaternion.identity);
    }

    Vector3 GetGroundPosition(Vector3 pos)
    {
        RaycastHit hit;

        if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out hit, 200f))
        {
            return hit.point + Vector3.up * 0.25f;
        }

        return pos;
    }
    #endregion
}

public class PlayerPhaseReacher : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (FindAnyObjectByType<RodentBoss>())
        {
            FindAnyObjectByType<RodentBoss>().PlayerReachedArena();
        }
    }
}
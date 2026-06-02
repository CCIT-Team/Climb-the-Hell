using System;
using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonsterStats
{
    public enum State
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    public State currentState = State.Idle;

    [Header("reward")]
    public int rewardExp;
    public int rewardGold;
    public float rewardflowerleaf;
    public string MonsterName;

    [Header("monster setting")]
    public int AttackSpeed = 1000;
    public float moverange = 10f;

    [Header("target")]
    public Transform player;

    [Header("Surround Slot")]
    public int surroundSlotCount = 16;
    public float surroundRadiusMultiplier = 0.8f;
    public float slotCheckRadius = 0.6f;
    public float slotChangeInterval = 0.35f;
    public float slotSearchNavMeshRange = 2f;

    [Header("Separation")]
    public float monsterCheckRadius = 0.8f;
    public float separationWeight = 0.45f;
    public float attackSeparationWeight = 0.25f;

    [Header("Predictive Pursuit")]
    public float predictionTime = 0.2f;
    public float maxPredictionDistance = 1.5f;

    [Header("AI Rate")]
    public float aiTickRate = 0.15f;
    public float pathUpdateRate = 0.1f;

    [Header("dead")]
    public bool isDead = false;

    public event Action<MonsterAI> OnMonsterDead;

    private NavMeshAgent agent;
    private float attackCooldown;
    private float lastAttackTime;
    private float nextAITick;
    private float nextPathTick;
    private float currentDistance;

    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;

    private float personalAngleOffset;
    private float nextSlotChangeTime;
    private Vector3 currentSlotTarget;

    public MonsterAttackHitbox attackVisual;

    protected override void Awake()
    {
        base.Awake();

        agent = GetComponent<NavMeshAgent>();
        attackVisual = GetComponentInChildren<MonsterAttackHitbox>();

        personalAngleOffset = UnityEngine.Random.Range(0f, 360f);

        ApplyMonsterStatsToAI();
    }

    void Start()
    {
        FindPlayer();

        if (player != null)
        {
            lastPlayerPosition = player.position;
            currentSlotTarget = player.position;
        }
    }

    void Update()
    {
        if (currentState == State.Dead) return;

        if (player == null)
        {
            FindPlayer();

            if (player == null) return;

            lastPlayerPosition = player.position;
        }

        if (agent == null) return;
        if (!agent.isOnNavMesh) return;

        UpdatePlayerVelocity();

        currentDistance = Vector3.Distance(transform.position, player.position);

        if (Time.time >= nextAITick)
        {
            UpdateAI();
            nextAITick = Time.time + aiTickRate;
        }

        if (Time.time >= nextPathTick)
        {
            UpdatePath();
            nextPathTick = Time.time + pathUpdateRate;
        }
    }

    public void ApplyMonsterStatsToAI()
    {
        if (monsterspeed <= 0)
            monsterspeed = 3f;

        if (moverange <= 0)
            moverange = 10f;

        if (monsterrange <= 0)
            monsterrange = 3f;

        if (AttackSpeed <= 0)
            AttackSpeed = 1000;

        attackCooldown = AttackSpeed / 1000f;

        if (agent != null)
        {
            agent.speed = monsterspeed;
            agent.stoppingDistance = 0.05f;
            agent.radius = 0.23f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = UnityEngine.Random.Range(10, 90);
            agent.autoBraking = false;
        }
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");

        if (p != null)
            player = p.transform;
    }

    void UpdatePlayerVelocity()
    {
        if (Time.deltaTime <= 0f) return;

        playerVelocity = (player.position - lastPlayerPosition) / Time.deltaTime;
        playerVelocity.y = 0f;

        lastPlayerPosition = player.position;
    }

    Vector3 GetPredictedPlayerPosition()
    {
        Vector3 predictedOffset = playerVelocity * predictionTime;

        if (predictedOffset.magnitude > maxPredictionDistance)
            predictedOffset = predictedOffset.normalized * maxPredictionDistance;

        return player.position + predictedOffset;
    }

    void UpdateAI()
    {
        switch (currentState)
        {
            case State.Idle:
                if (currentDistance <= moverange)
                    ChangeState(State.Chase);
                break;

            case State.Chase:
                if (currentDistance <= monsterrange)
                    ChangeState(State.Attack);
                else if (currentDistance > moverange)
                    ChangeState(State.Idle);
                break;

            case State.Attack:
                if (currentDistance > monsterrange * 1.3f)
                    ChangeState(State.Chase);
                break;
        }
    }

    void UpdatePath()
    {
        switch (currentState)
        {
            case State.Idle:
                StopAgent();
                break;

            case State.Chase:
                agent.isStopped = false;
                MoveToBestSurroundSlot();
                break;

            case State.Attack:
                agent.isStopped = false;
                LookAtPlayer();

                if (currentDistance > monsterrange * 0.75f)
                    MoveToBestSurroundSlot();
                else
                    KeepPositionButSeparate();

                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    TryAttackPlayer();
                    lastAttackTime = Time.time;
                }
                break;
        }
    }

    void MoveToBestSurroundSlot()
    {
        Vector3 center = GetPredictedPlayerPosition();

        if (Time.time >= nextSlotChangeTime || currentSlotTarget == Vector3.zero)
        {
            currentSlotTarget = GetBestSurroundSlot(center);
            nextSlotChangeTime = Time.time + slotChangeInterval;
        }

        Vector3 separationDir = GetSeparationDirection();
        Vector3 finalTarget = currentSlotTarget + separationDir * separationWeight;

        if (NavMesh.SamplePosition(finalTarget, out NavMeshHit hit, slotSearchNavMeshRange, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(center);
        }
    }

    Vector3 GetBestSurroundSlot(Vector3 center)
    {
        float surroundRadius = Mathf.Max(monsterrange * surroundRadiusMultiplier, 0.8f);

        Vector3 bestPos = center;
        float bestScore = float.MaxValue;

        for (int i = 0; i < surroundSlotCount; i++)
        {
            float angle = personalAngleOffset + (360f / surroundSlotCount) * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            Vector3 rawSlotPos = center + dir * surroundRadius;

            if (!NavMesh.SamplePosition(rawSlotPos, out NavMeshHit hit, slotSearchNavMeshRange, NavMesh.AllAreas))
                continue;

            float score = EvaluateSlotScore(hit.position, center);

            if (score < bestScore)
            {
                bestScore = score;
                bestPos = hit.position;
            }
        }

        return bestPos;
    }

    float EvaluateSlotScore(Vector3 slotPos, Vector3 center)
    {
        Collider[] cols = Physics.OverlapSphere(slotPos, slotCheckRadius);

        int monsterCount = 0;
        float closePenalty = 0f;

        foreach (Collider col in cols)
        {
            if (!col.CompareTag("Monster")) continue;
            if (col.gameObject == gameObject) continue;

            monsterCount++;

            float d = Vector3.Distance(slotPos, col.transform.position);
            closePenalty += 1f / Mathf.Max(d, 0.1f);
        }

        float myDistanceToSlot = Vector3.Distance(transform.position, slotPos);
        float playerDistance = Mathf.Abs(Vector3.Distance(slotPos, center) - monsterrange * surroundRadiusMultiplier);

        return monsterCount * 20f + closePenalty * 3f + myDistanceToSlot * 0.4f + playerDistance;
    }

    void KeepPositionButSeparate()
    {
        Vector3 separationDir = GetSeparationDirection();

        if (separationDir.sqrMagnitude <= 0.01f)
        {
            agent.ResetPath();
            return;
        }

        Vector3 targetPos = transform.position + separationDir.normalized * attackSeparationWeight;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.ResetPath();
        }
    }

    Vector3 GetSeparationDirection()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, monsterCheckRadius);

        Vector3 separationDir = Vector3.zero;

        foreach (Collider col in cols)
        {
            if (col.gameObject == gameObject) continue;
            if (!col.CompareTag("Monster")) continue;

            Vector3 awayDir = transform.position - col.transform.position;
            awayDir.y = 0f;

            float distance = awayDir.magnitude;

            if (distance <= 0.01f) continue;

            float strength = 1f / distance;
            separationDir += awayDir.normalized * strength;
        }

        return separationDir;
    }

    void TryAttackPlayer()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > monsterrange) return;

        Player playerComponent = player.GetComponent<Player>();

        if (playerComponent == null)
            playerComponent = player.GetComponentInParent<Player>();

        if (playerComponent == null) return;

        if (attackVisual != null)
            attackVisual.Attack(playerComponent, monsterattack);
        else
            playerComponent.TakeDamage(monsterattack);
    }

    public override bool TakeDamage(int damage)
    {
        if (isDead) return false;

        bool dead = base.TakeDamage(damage);

        Debug.Log($"{MonsterName} HP: {currentHp}/{monsterhp}");

        if (dead)
        {
            DropReward();
            Die();
        }

        return dead;
    }

    public void DropReward()
    {
        Debug.Log($"[Monster:{MonsterName}] 골드 {rewardGold}개, 꽃잎 {rewardflowerleaf}개 드랍");
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        ChangeState(State.Dead);
        StopAgent();

        Collider col = GetComponent<Collider>();

        if (col != null)
            col.enabled = false;

        OnMonsterDead?.Invoke(this);

        Destroy(gameObject, 0.5f);
    }

    void StopAgent()
    {
        if (agent == null) return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    void ChangeState(State newState)
    {
        if (currentState == newState) return;

        currentState = newState;
    }

    public bool IsAlive()
    {
        return !isDead;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, monsterCheckRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, moverange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, monsterrange);

        if (player != null)
        {
            Gizmos.color = Color.green;

            float radius = Mathf.Max(monsterrange * surroundRadiusMultiplier, 0.8f);

            for (int i = 0; i < surroundSlotCount; i++)
            {
                float angle = (360f / surroundSlotCount) * i;
                float rad = angle * Mathf.Deg2Rad;

                Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
                Vector3 pos = player.position + dir * radius;

                Gizmos.DrawWireSphere(pos, slotCheckRadius);
            }
        }
    }
}
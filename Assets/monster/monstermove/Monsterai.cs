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

    [Header("monster body")]
    public float monsterCheckRadius = 0.8f;
    public float separationWeight = 0.4f;

    [Header("ai rate")]
    public float aiTickRate = 0.2f;
    public float pathUpdateRate = 0.5f;

    [Header("dead")]
    public bool isDead = false;

    public event Action<MonsterAI> OnMonsterDead;

    private NavMeshAgent agent;
    private float attackCooldown;
    private float lastAttackTime;
    private float nextAITick;
    private float nextPathTick;
    private float currentDistance;

    public MonsterAttackHitbox attackVisual;

    protected override void Awake()
    {
        base.Awake();

        agent = GetComponent<NavMeshAgent>();
        attackVisual = GetComponentInChildren<MonsterAttackHitbox>();

        ApplyMonsterStatsToAI();
    }

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (currentState == State.Dead) return;

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        if (agent == null) return;
        if (!agent.isOnNavMesh) return;

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
            agent.stoppingDistance = monsterrange * 0.8f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = UnityEngine.Random.Range(30, 70);
        }
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");

        if (p != null)
            player = p.transform;
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
                if (currentDistance > monsterrange)
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
                agent.SetDestination(GetChaseTarget());
                break;

            case State.Attack:
                StopAgent();
                LookAtPlayer();

                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    TryAttackPlayer();
                    lastAttackTime = Time.time;
                }
                break;
        }
    }

    Vector3 GetChaseTarget()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0;

        if (toPlayer == Vector3.zero)
            return transform.position;

        toPlayer.Normalize();

        Vector3 targetPos = player.position - toPlayer * (monsterrange * 0.6f);
        Vector3 separationDir = GetSeparationDirection();

        if (currentDistance > monsterrange)
            targetPos += separationDir * separationWeight;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;

        return targetPos;
    }

    Vector3 GetSeparationDirection()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, monsterCheckRadius);

        Vector3 separationDir = Vector3.zero;
        int count = 0;

        foreach (Collider col in cols)
        {
            if (col.gameObject == gameObject) continue;

            if (col.CompareTag("Monster"))
            {
                Vector3 awayDir = transform.position - col.transform.position;
                awayDir.y = 0;

                float distance = awayDir.magnitude;

                if (distance > 0)
                {
                    separationDir += awayDir.normalized / distance;
                    count++;
                }
            }
        }

        if (count > 0)
            separationDir /= count;

        return separationDir.normalized;
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
        dir.y = 0;

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
    }
}
using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    public enum State // 상태
    {
        Idle,
        Chase,
        Attack
    }

    public State currentState = State.Idle;

    [Header("attack target")]
    public Transform player;

    [Header("range")]
    public float detectRange = 10f;
    public float attackRange = 2f;

    [Header("attack")]
    public float attackCooldown = 1.5f;
    public float attackOffset = 1f;
    public float attackRadius = 1f;

    [Header("monster body")]
    public float monsterCheckRadius = 0.8f;
    public float surroundDistance = 1.5f;
    public float separationWeight = 0.4f;

    [Header("ai rate")]
    public float aiTickRate = 0.2f;
    public float pathUpdateRate = 0.5f;

    private NavMeshAgent agent;
    private float lastAttackTime;
    private float nextAITick;
    private float nextPathTick;
    private float currentDistance;

    void Start() // 
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.obstacleAvoidanceType =
                ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            agent.avoidancePriority =
                Random.Range(30, 70);

            // 너무 멀리서 멈추지 않게
            agent.stoppingDistance = 0.3f;
        }
    }

    void Update()
    {
        if (player == null) return;
        if (agent == null) return;
        if (!agent.isOnNavMesh) return;

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

    void UpdateAI()
    {
        currentDistance =
            Vector3.Distance(
                transform.position,
                player.position);

        switch (currentState)
        {
            case State.Idle:
                if (currentDistance <= detectRange)
                {
                    ChangeState(State.Chase);
                }
                break;

            case State.Chase:
                if (currentDistance <= attackRange)
                {
                    ChangeState(State.Attack);
                }
                else if (currentDistance > detectRange)
                {
                    ChangeState(State.Idle);
                }
                break;

            case State.Attack:
                if (currentDistance > attackRange)
                {
                    ChangeState(State.Chase);
                }
                break;
        }
    }

    void UpdatePath()
    {
        switch (currentState)
        {
            case State.Idle:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                break;

            case State.Chase:
                agent.isStopped = false;

                Vector3 targetPos =
                    GetChaseTarget();

                agent.SetDestination(targetPos);
                break;

            case State.Attack:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;

                LookAtPlayer();

                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    Attack();
                    lastAttackTime = Time.time;
                }
                break;
        }
    }

    Vector3 GetChaseTarget()
    {
        Vector3 toPlayer =
            player.position - transform.position;

        toPlayer.y = 0;

        if (toPlayer == Vector3.zero)
        {
            return transform.position;
        }

        toPlayer.Normalize();

        // 플레이어 중심보다 살짝 떨어진 곳을 목표로 함
        // attackRange * 0.6f라서 너무 멀리서 멈추지 않음
        Vector3 targetPos =
            player.position -
            toPlayer * (attackRange * 0.6f);

        Vector3 separationDir =
            GetSeparationDirection();

        // 공격 범위 밖에서만 분산 적용
        if (currentDistance > attackRange)
        {
            targetPos +=
                separationDir * separationWeight;
        }

        NavMeshHit hit;

        if (NavMesh.SamplePosition(
            targetPos,
            out hit,
            2f,
            NavMesh.AllAreas))
        {
            return hit.position;
        }

        return targetPos;
    }

    Vector3 GetSeparationDirection()
    {
        Collider[] cols =
            Physics.OverlapSphere(
                transform.position,
                monsterCheckRadius);

        Vector3 separationDir =
            Vector3.zero;

        int count = 0;

        foreach (Collider col in cols)
        {
            if (col.gameObject == gameObject)
                continue;

            if (col.CompareTag("Monster"))
            {
                Vector3 awayDir =
                    transform.position - col.transform.position;

                awayDir.y = 0;

                float distance =
                    awayDir.magnitude;

                if (distance > 0)
                {
                    separationDir +=
                        awayDir.normalized / distance;

                    count++;
                }
            }
        }

        if (count > 0)
        {
            separationDir /= count;
        }

        return separationDir.normalized;
    }

    void Attack()
    {
        Vector3 hitboxPos =
            transform.position +
            transform.forward * attackOffset;

        Collider[] hits =
            Physics.OverlapSphere(
                hitboxPos,
                attackRadius);

        foreach (Collider hit in hits)
        {
            GameObject root =
                hit.transform.root.gameObject;

            if (root.name == "Player")
            {
                Debug.Log("플레이어 공격 성공");

                PlayerHealth hp =
                    root.GetComponent<PlayerHealth>();

                if (hp != null)
                {
                    hp.TakeDamage(10);
                }
            }
        }
    }

    void LookAtPlayer()
    {
        Vector3 dir =
            player.position - transform.position;

        dir.y = 0;

        if (dir != Vector3.zero)
        {
            transform.rotation =
                Quaternion.LookRotation(dir);
        }
    }

    void ChangeState(State newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        Debug.Log("상태 변경 : " + newState);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            monsterCheckRadius);

        Gizmos.color = Color.red;

        Vector3 hitboxPos =
            transform.position +
            transform.forward * attackOffset;

        Gizmos.DrawWireSphere(
            hitboxPos,
            attackRadius);
    }
}
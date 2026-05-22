
using UnityEngine;
using UnityEngine.AI;

public class testmonsterai : MonoBehaviour
{
    public enum State
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

    [Header("monster body")]
    public float monsterCheckRadius = 1.2f;
    public int crowdLimit = 2;
    public float surroundDistance = 1.5f;

    [Header("attack range")]
    public float attackOffset = 1f;
    public float attackRadius = 1f;

    private NavMeshAgent agent;

    private float lastAttackTime;

    private float currentDistance;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.obstacleAvoidanceType =
                ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            agent.avoidancePriority =
                Random.Range(30, 70);

            agent.stoppingDistance = 1f;
        }
    }

    void Update()
    {
        if (player == null) return;
        if (agent == null) return;
        if (!agent.isOnNavMesh) return;

        // AI Tick 제거
        // 매 프레임 실행

        UpdateAI();
        UpdatePath();
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

                break;

            case State.Chase:

                agent.isStopped = false;

                Vector3 targetPos =
                    GetChaseTarget();

                NavMeshPath path =
                    new NavMeshPath();

                bool hasPath =
                    agent.CalculatePath(
                        targetPos,
                        path);

                if (hasPath &&
                    path.status ==
                    NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(targetPos);
                }

                break;

            case State.Attack:

                agent.isStopped = true;

                LookAtPlayer();

                if (Time.time >=
                    lastAttackTime +
                    attackCooldown)
                {
                    Attack();

                    lastAttackTime =
                        Time.time;
                }

                break;
        }
    }

    Vector3 GetChaseTarget()
    {
        int nearbyMonsterCount =
            CountNearbyMonsters();

        // 안 막혔으면 그냥 플레이어 직진 추적
        if (nearbyMonsterCount < crowdLimit)
        {
            return player.position;
        }

        // 플레이어 방향
        Vector3 toPlayer =
            player.position -
            transform.position;

        toPlayer.y = 0;

        if (toPlayer == Vector3.zero)
        {
            return player.position;
        }

        toPlayer.Normalize();

        // 플레이어 방향 기준 옆 방향
        Vector3 sideDir =
            Vector3.Cross(
                Vector3.up,
                toPlayer).normalized;

        // 랜덤 좌우 선택
        float side =
            Random.value < 0.5f ? -1f : 1f;

        // 옆으로 살짝 비켜감
        Vector3 sideTarget =
            player.position +
            sideDir *
            side *
            surroundDistance;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(
            sideTarget,
            out hit,
            2f,
            NavMesh.AllAreas))
        {
            return hit.position;
        }

        return player.position;
    }

    int CountNearbyMonsters()
    {
        Collider[] cols =
            Physics.OverlapSphere(
                transform.position,
                monsterCheckRadius);

        int count = 0;

        foreach (Collider col in cols)
        {
            if (col.gameObject ==
                gameObject)
                continue;

            if (col.CompareTag("Monster"))
            {
                count++;
            }
        }

        return count;
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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position +
            transform.forward * attackOffset,
            attackRadius);
    }

    void LookAtPlayer()
    {
        Vector3 dir =
            player.position -
            transform.position;

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

        currentState =
            newState;

        Debug.Log(
            "상태 변경 : " +
            newState);
    }
}
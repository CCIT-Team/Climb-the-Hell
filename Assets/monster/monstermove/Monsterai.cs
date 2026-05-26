using System;
using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonsterStats // 상속 형태의 구조 MonsterStats는 추상 클래스
{
    public enum State //현재 상태 4개
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    public State currentState = State.Idle;

    [Header("reward")] // 보상들 < 나중에 스탯으로 옮길 예정
    public int rewardExp;
    public int rewardGold;
    public float rewardflowerleaf;
    public string MonsterName;

    [Header("monster setting")] 
    public int AttackSpeed = 1000; // 이동속도
    public float moverange = 10f; // 인식 범위

    [Header("target")]
    public Transform player; // 타켓

    [Header("monster body")]
    public float monsterCheckRadius = 0.8f; // 체크 범위
    public float separationWeight = 0.4f; 

    [Header("ai rate")]
    public float aiTickRate = 0.2f; // ai 틱 주기
    public float pathUpdateRate = 0.5f; // pathupdate 주기

    [Header("dead")]
    public bool isDead = false; // 뒤짐

    public event Action<MonsterAI> OnMonsterDead;

    private NavMeshAgent agent; // 계산을 위한 '현재' 상태 변수
    private float attackCooldown; // 공격 쿨타임
    private float lastAttackTime; // 공격 갱신 주기
    private float nextAITick; // 다음 ai 틱
    private float nextPathTick; // 다음 pathupdate 주기
    private float currentDistance; // 현재 플레이어와 몬스터 거리

    public MonsterAttackHitbox attackVisual;

    protected override void Awake()
    {
        base.Awake(); //부모 MonsterStats의 Awake 실행

        agent = GetComponent<NavMeshAgent>(); // NavMeshAgent 가져옴
        attackVisual = GetComponentInChildren<MonsterAttackHitbox>();// 자식 오브젝트에서 MonsterAttackHitbox를 가져옴

        ApplyMonsterStatsToAI();
    }

    void Start() 
    {
        FindPlayer(); // 플레이어 있는지 검사
    }

    void Update()
    {
        if (currentState == State.Dead) return; 

        if (player == null) // 플레이어 없으면 찾기
        {
            FindPlayer();
            if (player == null) return;
        }

        if (agent == null) return;
        if (!agent.isOnNavMesh) return;

        currentDistance = Vector3.Distance(transform.position, player.position);

        if (Time.time >= nextAITick) //ai tick 시간 계산 (실제 ai 계산)
        {
            UpdateAI();
            nextAITick = Time.time + aiTickRate;
        }

        if (Time.time >= nextPathTick) // ai pathupdate 시간 계산 (상태 판단 주기)
        {
            UpdatePath();
            nextPathTick = Time.time + pathUpdateRate;
        }
    }

    public void ApplyMonsterStatsToAI()
    {
        if (monsterspeed <= 0) // 스탯 체크
            monsterspeed = 3f;

        if (moverange <= 0)
            moverange = 10f;

        if (monsterrange <= 0)
            monsterrange = 3f;

        if (AttackSpeed <= 0)
            AttackSpeed = 1000;

        attackCooldown = AttackSpeed / 1000f; //쿨타임 1000f == 1초임

        if (agent != null)
        {
            agent.speed = monsterspeed; // NavMeshAgent 속도를 몬스터 스탯에 맞춤
            agent.stoppingDistance = monsterrange * 0.8f;// 공격 범위 근처에서 멈추게함
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = UnityEngine.Random.Range(30, 70); //몬스터 끼리 회피 우선순위를 랜덤으로 줌
        }
    }

    void FindPlayer() // 아까 Update에서 한 player찾기 
    {
        GameObject p = GameObject.FindWithTag("Player");

        if (p != null)
            player = p.transform;
    }

    void UpdateAI()
    {
        switch (currentState)
        {
            case State.Idle: //플레이어가 몬스터 인지범위내에 없으면 idle
                if (currentDistance <= moverange)
                    ChangeState(State.Chase);
                break;

            case State.Chase: // 플레이어가 몬스터 인지범위내에 들어오면 chase
                if (currentDistance <= monsterrange)
                    ChangeState(State.Attack);
                else if (currentDistance > moverange)
                    ChangeState(State.Idle);
                break;

            case State.Attack: // 플레이어가 몬스터 공격 범위로 들어오면 attack
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
                StopAgent(); //몬스터가 가만히 멈추는 함수
                break;

            case State.Chase:
                agent.isStopped = false; 
                agent.SetDestination(GetChaseTarget()); // 플레이어 쪽으로 이동
                break;

            case State.Attack:
                StopAgent(); 
                LookAtPlayer(); // 몬스터가 이동 멈춘뒤에 플레이어 바라보기

                if (Time.time >= lastAttackTime + attackCooldown) // 쿨타임 되면 공격함
                {
                    TryAttackPlayer();
                    lastAttackTime = Time.time;
                }
                break;
        }
    }

    Vector3 GetChaseTarget() //플레이어 한테 붙는게 아니라 공격 버리보다 살짝 떨어진 위치를 목표로 잡음
    {
        Vector3 toPlayer = player.position - transform.position; // 플레이어 있는 방향 바라보기
        toPlayer.y = 0;

        if (toPlayer == Vector3.zero)
            return transform.position;

        toPlayer.Normalize();

        Vector3 targetPos = player.position - toPlayer * (monsterrange * 0.6f); // 떨어질 거리 계산
        Vector3 separationDir = GetSeparationDirection();

        if (currentDistance > monsterrange) // 플레이어 근처까지 접근하지만 몬스터끼리 너무 겹치지 않게끔 함
            targetPos += separationDir * separationWeight; 

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;

        return targetPos;
    }

    Vector3 GetSeparationDirection() // 아까 위에 GetChase에서 몬스터 주변 일정 반경 안에 있는 콜라이더를 검사함
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, monsterCheckRadius);

        Vector3 separationDir = Vector3.zero;
        int count = 0;

        foreach (Collider col in cols)
        {
            if (col.gameObject == gameObject) continue;

            if (col.CompareTag("Monster")) // 몬스터 찾기(tag임)
            {
                Vector3 awayDir = transform.position - col.transform.position;
                awayDir.y = 0; // 몬스터에게서 멀어지는 방향을 계산함

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

    void TryAttackPlayer() // 몬스터 > 플레이어 공격 코드
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > monsterrange) return;

        Player playerComponent = player.GetComponent<Player>(); // 플레이어 컴포넌트 찾기

        if (playerComponent == null)
            playerComponent = player.GetComponentInParent<Player>();

        if (playerComponent == null) return;

        if (attackVisual != null)
            attackVisual.Attack(playerComponent, monsterattack); //MonsterAttackHitbox가 있으면 공격 범위 표시 / 판정을 통해 공격
        else
            playerComponent.TakeDamage(monsterattack);
    }

    public override bool TakeDamage(int damage) // 공격 맞는 코드
    {
        if (isDead) return false;

        bool dead = base.TakeDamage(damage); // 부모 클래스(MonsterStats)의 피격 처리를 먼저 진행

        Debug.Log($"{MonsterName} HP: {currentHp}/{monsterhp}");

        if (dead) // 보상(이건 수정 필요할듯)
        {
            DropReward();
            Die();
        }

        return dead;
    }

    public void DropReward() // 보상 로그
    {
        Debug.Log($"[Monster:{MonsterName}] 골드 {rewardGold}개, 꽃잎 {rewardflowerleaf}개 드랍");
    }

    public void Die() // 죽음
    {
        if (isDead) return;

        isDead = true;
        ChangeState(State.Dead); // 죽음 상태로 바꾸기
        StopAgent(); // 이동 멈춤

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false; // 콜라이더 끄기 > 죽은 몬스터와 충돌 하면 안됌

        OnMonsterDead?.Invoke(this); // 몬스터 뒤짐 신호 보냄(스포너 시스템을 위해서), 이거 나중에 통계로 사용도 가능할듯

        Destroy(gameObject, 0.5f); // < 이거 없에기 1순위, 오브젝트 폴링 스포너에 추가해봄
    }

    void StopAgent() // 몬스터 움직임 멈추기
    {
        if (agent == null) return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero; // Vector3값을 0으로 만들기
    }

    void LookAtPlayer() // 플레이어가 있는 방향보기(공격하기 위해서)
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0;

        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    void ChangeState(State newState) // FSM 상태 바꾸기
    {
        if (currentState == newState) return; // 현재 상태랑 새로운 상태를 지속해서 갱신
        currentState = newState;
    }

    public bool IsAlive() // 살아있는지 아닌지 판정
    {
        return !isDead;
    }

    void OnDrawGizmos() // 공격 범위 보여주기
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, monsterCheckRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, moverange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, monsterrange);
    }
}
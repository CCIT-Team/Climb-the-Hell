using System;                 
using UnityEngine;            
using UnityEngine.AI;         // NavMeshAgent, NavMesh.SamplePosition 등 길찾기 기능 사용

public class MonsterAI : MonsterStats
{
    // 몬스터의 상태를 구분하기 위한 enum
    // enum은 정해진 값 중 하나만 가질 수 있는 자료형
    public enum State
    {
        Idle,       // 대기 상태: 플레이어가 멀리 있으면 아무 행동 안 함
        Chase,      // 추적 상태: 플레이어를 따라감
        Attack,     // 공격 상태: 공격 범위 안에서 플레이어를 공격함
        Dead        // 사망 상태: 더 이상 행동하지 않음
    }

    // 현재 몬스터의 상태
    // 처음에는 Idle 상태로 시작
    public State currentState = State.Idle;

    [Header("reward")]
    // 몬스터가 죽었을 때 줄 보상 정보
    public int rewardExp;              // 경험치 보상
    public int rewardGold;             // 골드 보상
    public float rewardflowerleaf;     // 꽃잎 같은 재화 보상
    public string MonsterName;         // 몬스터 이름, 로그 출력 등에 사용

    [Header("monster setting")]
    // 공격 속도
    // 1000이면 1초마다 공격한다는 뜻으로 사용됨
    public int AttackSpeed = 1000;

    // 몬스터가 플레이어를 인식할 수 있는 거리
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

    // 다음 AI 상태 판단 시간
    private float nextAITick;

    // 다음 경로 갱신 시간
    private float nextPathTick;

    // 현재 몬스터와 플레이어 사이의 거리
    private float currentDistance;

    // 이전 프레임의 플레이어 위치
    private Vector3 lastPlayerPosition;

    // 플레이어의 이동 속도 벡터 < 이동 예측
    private Vector3 playerVelocity;

    // 몬스터마다 서로 다른 각도 오프셋을 주기 위한 값
    // 모든 몬스터가 같은 슬롯을 고르지 않게 만드는 역할
    private float personalAngleOffset;

    // 다음에 슬롯을 다시 고를 수 있는 시간
    private float nextSlotChangeTime;

    // 현재 선택된 플레이어 주변 목표 슬롯 위치
    private Vector3 currentSlotTarget;

    // 공격 범위를 시각화하거나 히트박스로 공격 처리하는 컴포넌트
    public MonsterAttackHitbox attackVisual;

    // Awake는 오브젝트가 생성될 때 Start보다 먼저 호출됨
    protected override void Awake()
    {
        // 부모 클래스인 MonsterStats의 Awake 실행
        // MonsterStats에서 체력 초기화 같은 작업을 할 가능성이 있음
        base.Awake();

        // 현재 오브젝트에 붙어 있는 NavMeshAgent 컴포넌트를 가져옴
        agent = GetComponent<NavMeshAgent>();

        // 자식 오브젝트 중 MonsterAttackHitbox 컴포넌트를 가져옴
        // 공격 범위 오브젝트가 자식으로 붙어 있을 때 사용 가능
        attackVisual = GetComponentInChildren<MonsterAttackHitbox>();

        // 0도부터 360도 사이의 랜덤 각도를 부여
        // 몬스터마다 주변 슬롯 탐색 시작 각도가 달라져서 몰림을 줄임
        personalAngleOffset = UnityEngine.Random.Range(0f, 360f);

        // MonsterStats의 능력치를 AI와 NavMeshAgent에 적용
        ApplyMonsterStatsToAI();
    }

    // Start는 Awake 이후, 첫 Update 전에 한 번 호출됨
    void Start()
    {
        // Player 태그를 가진 오브젝트를 찾아 player 변수에 저장
        FindPlayer();

        // 플레이어를 찾았다면
        if (player != null)
        {
            // 현재 플레이어 위치를 이전 위치로 저장
            // 이후 UpdatePlayerVelocity에서 속도 계산에 사용됨
            lastPlayerPosition = player.position;

            // 처음 슬롯 목표 위치를 플레이어 위치로 설정
            currentSlotTarget = player.position;
        }
    }

    void Update()
    {
        // 죽은 상태라면 AI 로직을 더 이상 실행하지 않음
        if (currentState == State.Dead) return;

        // player가 비어 있으면 다시 찾음
        if (player == null)
        {
            FindPlayer();

            
            if (player == null) return;

            // 새로 찾은 플레이어의 현재 위치를 저장
            lastPlayerPosition = player.position;
        }

        // NavMeshAgent가 없으면 이동 처리 불가
        if (agent == null) return;

        // 몬스터가 NavMesh 위에 없으면 SetDestination 등을 사용할 수 없으므로 종료
        if (!agent.isOnNavMesh) return;

        // 플레이어의 현재 이동 속도를 계산
        UpdatePlayerVelocity();

        // 몬스터와 플레이어 사이의 현재 거리 계산
        currentDistance = Vector3.Distance(transform.position, player.position);

        // 일정 시간마다 상태 판단 실행
        if (Time.time >= nextAITick)
        {
            UpdateAI();
            nextAITick = Time.time + aiTickRate;
        }

        // 일정 시간마다 이동 목적지 갱신 실행
        if (Time.time >= nextPathTick)
        {
            UpdatePath();
            nextPathTick = Time.time + pathUpdateRate;
        }
    }

    // MonsterStats에 있는 능력치 값을 AI 이동, 공격 관련 값에 적용하는 함수
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

        // 밀리초 단위 AttackSpeed를 초 단위 쿨타임으로 변환
        attackCooldown = AttackSpeed / 1000f;

        // NavMeshAgent가 정상적으로 있다면 이동 관련 설정 적용
        if (agent != null)
        {
            // 몬스터 이동속도를 NavMeshAgent 속도에 적용
            agent.speed = monsterspeed;

            // 목표 지점 근처에서 멈추는 거리
            // 거의 딱 붙게 하려고 0.05로 작게 설정
            agent.stoppingDistance = 0.05f;

            // NavMeshAgent의 반지름
            // 작게 설정하면 몬스터가 좁은 곳을 더 잘 지나가지만 겹칠 가능성도 생김
            agent.radius = 0.23f;

            // 장애물 회피 품질을 높게 설정
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            // 회피 우선순위를 랜덤으로 줌
            // 여러 몬스터가 있을 때 모두 같은 우선순위면 서로 막히는 상황이 생길 수 있음
            // 숫자가 낮을수록 보통 더 높은 우선순위로 취급됨
            agent.avoidancePriority = UnityEngine.Random.Range(10, 90);

            // 자동 감속 끄기
            // 목표점 근처에서 너무 빨리 속도를 줄이지 않게 함
            agent.autoBraking = false;
        }
    }

    // Player 태그를 가진 오브젝트를 찾아 player 변수에 저장하는 함수
    void FindPlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");

        if (p != null)
            player = p.transform;
    }

    // 플레이어의 이동 속도를 계산하는 함수
    void UpdatePlayerVelocity()
    {
        // Time.deltaTime이 0이면 나누기 오류가 날 수 있으므로 방지
        if (Time.deltaTime <= 0f) return;

        // 속도 = 위치 변화량 / 시간
        // 현재 위치에서 이전 위치를 뺀 뒤 deltaTime으로 나눔
        playerVelocity = (player.position - lastPlayerPosition) / Time.deltaTime;

        // y축 이동은 무시
        // 3D 게임에서 바닥 기준 추적만 하려는 의도
        playerVelocity.y = 0f;

        // 다음 프레임 계산을 위해 현재 위치를 저장
        lastPlayerPosition = player.position;
    }

    // 플레이어가 잠시 뒤에 있을 위치를 예측하는 함수
    Vector3 GetPredictedPlayerPosition()
    {
        // 플레이어 속도에 예측 시간을 곱해서 예상 이동량 계산
        Vector3 predictedOffset = playerVelocity * predictionTime;

        // 예상 이동량이 너무 크면 최대 예측 거리로 제한
        // 플레이어가 순간이동하거나 프레임 튐이 생겼을 때 몬스터가 엉뚱한 곳으로 가지 않게 함
        if (predictedOffset.magnitude > maxPredictionDistance)
            predictedOffset = predictedOffset.normalized * maxPredictionDistance;

        // 현재 플레이어 위치 + 예상 이동량 = 예측 위치
        return player.position + predictedOffset;
    }

    // 몬스터 상태를 판단하고 바꾸는 함수
    void UpdateAI()
    {
        switch (currentState)
        {
            case State.Idle:
                // 대기 상태에서 플레이어가 인식 범위 안에 들어오면 추적 상태로 전환
                if (currentDistance <= moverange)
                    ChangeState(State.Chase);
                break;

            case State.Chase:
                // 추적 중 플레이어가 공격 범위 안에 들어오면 공격 상태로 전환
                if (currentDistance <= monsterrange)
                    ChangeState(State.Attack);

                // 플레이어가 인식 범위 밖으로 나가면 다시 대기 상태로 전환
                else if (currentDistance > moverange)
                    ChangeState(State.Idle);
                break;

            case State.Attack:
                // 공격 상태에서 플레이어가 너무 멀어지면 다시 추적 상태로 전환
                // 1.3을 곱해서 약간의 여유 범위를 둠
                if (currentDistance > monsterrange * 1.3f)
                    ChangeState(State.Chase);
                break;
        }
    }

    // 현재 상태에 따라 실제 행동을 실행하는 함수
    void UpdatePath()
    {
        switch (currentState)
        {
            case State.Idle:
                // 대기 상태에서는 이동을 멈춤
                StopAgent();
                break;

            case State.Chase:
                // 추적 상태에서는 NavMeshAgent 이동을 활성화
                agent.isStopped = false;

                // 플레이어 주변의 가장 좋은 슬롯으로 이동
                MoveToBestSurroundSlot();
                break;

            case State.Attack:
                // 공격 상태에서도 위치 보정이 필요하므로 이동은 켜둠
                agent.isStopped = false;

                // 플레이어 방향을 바라보게 함
                LookAtPlayer();

                // 공격 범위 안이지만 너무 멀면 다시 좋은 슬롯으로 접근
                if (currentDistance > monsterrange * 0.75f)
                    MoveToBestSurroundSlot();
                else
                    // 충분히 가까우면 자리를 크게 옮기지 않고 몬스터끼리만 살짝 분리
                    KeepPositionButSeparate();

                // 공격 쿨타임이 지났다면 공격 시도
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    TryAttackPlayer();
                    lastAttackTime = Time.time;
                }
                break;
        }
    }

    // 플레이어 주변 슬롯 중 좋은 위치를 골라 이동하는 함수
    void MoveToBestSurroundSlot()
    {
        // 플레이어 현재 위치가 아니라 예측 위치를 중심으로 삼음
        Vector3 center = GetPredictedPlayerPosition();

        // 슬롯 변경 시간이 되었거나, 아직 목표 슬롯이 없는 경우 새 슬롯 선택
        if (Time.time >= nextSlotChangeTime || currentSlotTarget == Vector3.zero)
        {
            currentSlotTarget = GetBestSurroundSlot(center);
            nextSlotChangeTime = Time.time + slotChangeInterval;
        }

        // 주변 몬스터와 겹치지 않도록 밀어내는 방향 계산
        Vector3 separationDir = GetSeparationDirection();

        // 선택된 슬롯 위치에 분리 방향을 조금 더해서 최종 목표 위치 계산
        Vector3 finalTarget = currentSlotTarget + separationDir * separationWeight;

        // 최종 목표 위치가 NavMesh 위에 있는지 검사
        if (NavMesh.SamplePosition(finalTarget, out NavMeshHit hit, slotSearchNavMeshRange, NavMesh.AllAreas))
        {
            // NavMesh 위의 유효한 위치로 이동
            agent.SetDestination(hit.position);
        }
        else
        {
            // 슬롯 위치가 부적절하면 예측된 플레이어 위치 쪽으로 이동
            agent.SetDestination(center);
        }
    }

    // 플레이어 주변 여러 슬롯 중 가장 좋은 슬롯을 찾는 함수
    Vector3 GetBestSurroundSlot(Vector3 center)
    {
        // 슬롯 반지름 계산
        // 공격 범위 기준으로 잡되, 너무 작아지지 않게 최소 0.8 보장
        float surroundRadius = Mathf.Max(monsterrange * surroundRadiusMultiplier, 0.8f);

        // 기본값은 중심 위치
        Vector3 bestPos = center;

        // 점수가 낮을수록 좋은 슬롯
        // 처음에는 매우 큰 값으로 설정
        float bestScore = float.MaxValue;

        // 슬롯 개수만큼 반복
        for (int i = 0; i < surroundSlotCount; i++)
        {
            // 현재 슬롯의 각도 계산
            // personalAngleOffset 때문에 몬스터마다 시작 각도가 달라짐
            float angle = personalAngleOffset + (360f / surroundSlotCount) * i;

            // Mathf.Sin, Cos는 라디안 단위를 사용하므로 도를 라디안으로 변환
            float rad = angle * Mathf.Deg2Rad;

            // 각도에 따른 방향 벡터 생성
            // x축은 Cos, z축은 Sin 사용
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

            // 중심 위치에서 해당 방향으로 surroundRadius만큼 떨어진 슬롯 위치
            Vector3 rawSlotPos = center + dir * surroundRadius;

            // 슬롯 위치가 NavMesh 근처에 없다면 이 슬롯은 사용하지 않음
            if (!NavMesh.SamplePosition(rawSlotPos, out NavMeshHit hit, slotSearchNavMeshRange, NavMesh.AllAreas))
                continue;

            // 해당 슬롯의 점수 계산
            float score = EvaluateSlotScore(hit.position, center);

            // 지금까지 찾은 슬롯보다 점수가 낮으면 좋은 슬롯으로 갱신
            if (score < bestScore)
            {
                bestScore = score;
                bestPos = hit.position;
            }
        }

        // 최종적으로 가장 점수가 낮은 슬롯 위치 반환
        return bestPos;
    }

    // 슬롯이 얼마나 좋은지 점수로 평가하는 함수, 점수가 낮을수록 좋은 슬롯
    float EvaluateSlotScore(Vector3 slotPos, Vector3 center)
    {
        // 슬롯 위치 주변의 Collider들을 검사
        Collider[] cols = Physics.OverlapSphere(slotPos, slotCheckRadius);

        // 해당 슬롯 주변에 있는 몬스터 수
        int monsterCount = 0;

        // 가까운 몬스터가 있을수록 추가되는 벌점
        float closePenalty = 0f;

        foreach (Collider col in cols)
        {
            // Monster 태그가 아니면 무시
            if (!col.CompareTag("Monster")) continue;

            // 자기 자신은 검사 대상에서 제외
            if (col.gameObject == gameObject) continue;

            // 다른 몬스터가 있으므로 개수 증가
            monsterCount++;

            // 슬롯과 해당 몬스터 사이 거리 계산
            float d = Vector3.Distance(slotPos, col.transform.position);

            // 가까울수록 1 / 거리 값이 커짐
            // 즉, 가까운 몬스터가 있을수록 점수 벌점이 커짐
            closePenalty += 1f / Mathf.Max(d, 0.1f);
        }

        // 현재 몬스터 위치에서 슬롯까지의 거리
        // 너무 멀리 있는 슬롯을 고르지 않게 하기 위한 값
        float myDistanceToSlot = Vector3.Distance(transform.position, slotPos);

        // 슬롯이 원하는 반지름에서 얼마나 벗어났는지 계산
        // 플레이어 주변 원형 배치를 유지하기 위한 값
        float playerDistance = Mathf.Abs(Vector3.Distance(slotPos, center) - monsterrange * surroundRadiusMultiplier);

        // 최종 점수
        // 몬스터가 많은 슬롯은 큰 벌점
        // 가까운 몬스터가 있는 슬롯도 벌점
        // 너무 먼 슬롯도 약간 벌점
        // 원하는 원형 거리에서 벗어난 슬롯도 벌점
        return monsterCount * 20f + closePenalty * 3f + myDistanceToSlot * 0.4f + playerDistance;
    }

    // 공격 상태에서 위치는 크게 바꾸지 않고 몬스터끼리 겹침만 줄이는 함수
    void KeepPositionButSeparate()
    {
        // 주변 몬스터와 떨어지는 방향 계산
        Vector3 separationDir = GetSeparationDirection();

        // 분리 방향이 거의 없으면 이동 경로를 초기화하고 멈춤
        if (separationDir.sqrMagnitude <= 0.01f)
        {
            agent.ResetPath();
            return;
        }

        // 현재 위치에서 분리 방향으로 아주 조금 이동할 목표점 계산
        Vector3 targetPos = transform.position + separationDir.normalized * attackSeparationWeight;

        // 목표점이 NavMesh 위에 있는지 확인
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            // 유효한 위치라면 그쪽으로 이동
            agent.SetDestination(hit.position);
        }
        else
        {
            // 유효하지 않으면 경로 초기화
            agent.ResetPath();
        }
    }

    // 주변 몬스터들과 겹치지 않도록 멀어지는 방향을 계산하는 함수
    Vector3 GetSeparationDirection()
    {
        // 현재 몬스터 주변 monsterCheckRadius 안의 Collider들을 찾음
        Collider[] cols = Physics.OverlapSphere(transform.position, monsterCheckRadius);

        // 최종 분리 방향
        Vector3 separationDir = Vector3.zero;

        foreach (Collider col in cols)
        {
            // 자기 자신은 제외
            if (col.gameObject == gameObject) continue;

            // Monster 태그가 아니면 제외
            if (!col.CompareTag("Monster")) continue;

            // 다른 몬스터로부터 멀어지는 방향
            Vector3 awayDir = transform.position - col.transform.position;
            awayDir.y = 0f;

            // 거리 계산
            float distance = awayDir.magnitude;

            // 거리가 너무 작으면 방향 계산이 불안정하므로 무시
            if (distance <= 0.01f) continue;

            // 가까울수록 더 강하게 밀어냄
            float strength = 1f / distance;

            // 정규화된 방향에 힘을 곱해서 누적
            separationDir += awayDir.normalized * strength;
        }

        // 주변 몬스터들로부터 멀어지는 방향의 합 반환
        return separationDir;
    }

    // 플레이어를 공격하는 함수
    void TryAttackPlayer()
    {
        // 플레이어가 없으면 공격 불가
        if (player == null) return;

        // 현재 플레이어와의 거리 계산
        float distance = Vector3.Distance(transform.position, player.position);

        // 공격 범위 밖이면 공격하지 않음
        if (distance > monsterrange) return;

        // 플레이어 오브젝트에서 Player 컴포넌트를 찾음
        Player playerComponent = player.GetComponent<Player>();

        // 직접 붙어 있지 않으면 부모 오브젝트에서 Player 컴포넌트 찾음
        if (playerComponent == null)
            playerComponent = player.GetComponentInParent<Player>();

        // 그래도 없으면 공격 처리 불가
        if (playerComponent == null) return;

        // 공격 히트박스 컴포넌트가 있으면 히트박스를 통해 공격 처리
        if (attackVisual != null)
            attackVisual.Attack(playerComponent, monsterattack);
        else
            // 히트박스가 없으면 바로 데미지 적용
            playerComponent.TakeDamage(monsterattack);
    }

    // 데미지를 받는 함수
    // MonsterStats의 TakeDamage를 오버라이드해서 죽음 처리와 보상 처리를 추가함
    public override bool TakeDamage(int damage)
    {
        // 이미 죽은 몬스터는 데미지를 받지 않음
        if (isDead) return false;

        // 부모 클래스의 데미지 처리 실행
        // dead가 true면 체력이 0 이하가 되었다는 뜻
        bool dead = base.TakeDamage(damage);

        // 현재 체력 로그 출력
        Debug.Log($"{MonsterName} HP: {currentHp}/{monsterhp}");

        // 죽었다면 보상 드랍 후 사망 처리
        if (dead)
        {
            DropReward();
            Die();
        }

        // 죽었는지 여부 반환
        return dead;
    }

    // 보상 드랍 처리 함수
    public void DropReward()
    {
        // 현재는 실제 보상 지급이 아니라 로그만 출력하는 상태
        Debug.Log($"[Monster:{MonsterName}] 골드 {rewardGold}개, 꽃잎 {rewardflowerleaf}개 드랍");
    }

    // 몬스터 사망 처리 함수
    public void Die()
    {
        // 이미 죽었다면 중복 실행 방지
        if (isDead) return;

        // 죽음 상태로 설정
        isDead = true;

        // 상태를 Dead로 변경
        ChangeState(State.Dead);

        // 이동 정지
        StopAgent();

        // 몬스터의 Collider를 꺼서 더 이상 충돌하지 않게 함
        Collider col = GetComponent<Collider>();

        if (col != null)
            col.enabled = false;

        // 사망 이벤트 호출
        // ?.는 구독자가 있을 때만 Invoke 하라는 뜻
        OnMonsterDead?.Invoke(this);

        // 0.5초 뒤 오브젝트 제거
        Destroy(gameObject, 0.5f);
    }

    // NavMeshAgent 이동을 멈추는 함수
    void StopAgent()
    {
        // agent가 없으면 종료
        if (agent == null) return;

        // 이동 정지
        agent.isStopped = true;

        // 현재 속도도 0으로 만들어 밀려가는 느낌 방지
        agent.velocity = Vector3.zero;
    }

    // 몬스터가 플레이어 방향을 바라보게 하는 함수
    void LookAtPlayer()
    {
        // 플레이어가 없으면 종료
        if (player == null) return;

        // 몬스터에서 플레이어로 향하는 방향 계산
        Vector3 dir = player.position - transform.position;

        // y축은 무시해서 위아래로 기울어지지 않게 함
        dir.y = 0f;

        // 방향이 0이 아닐 때만 회전
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    // 상태를 바꾸는 함수
    void ChangeState(State newState)
    {
        // 이미 같은 상태면 아무것도 하지 않음
        if (currentState == newState) return;

        // 현재 상태를 새 상태로 변경
        currentState = newState;
    }

    // 몬스터가 살아 있는지 확인하는 함수
    public bool IsAlive()
    {
        // isDead가 false면 살아 있음
        return !isDead;
    }

    // Scene 뷰에서 디버그용 기즈모를 그리는 함수
    // 실제 게임 화면에는 보이지 않고 개발 중 확인용으로 사용
    void OnDrawGizmos()
    {
        // 노란색: 몬스터 간 분리 감지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, monsterCheckRadius);

        // 파란색: 플레이어 인식 범위
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, moverange);

        // 빨간색: 공격 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, monsterrange);

        // 플레이어가 있을 때만 주변 슬롯 표시
        if (player != null)
        {
            // 초록색: 플레이어 주변 슬롯 검사 범위
            Gizmos.color = Color.green;

            // 슬롯 반지름 계산
            float radius = Mathf.Max(monsterrange * surroundRadiusMultiplier, 0.8f);

            // 슬롯 개수만큼 원형으로 표시
            for (int i = 0; i < surroundSlotCount; i++)
            {
                // 슬롯 각도 계산
                float angle = (360f / surroundSlotCount) * i;

                // 도를 라디안으로 변환
                float rad = angle * Mathf.Deg2Rad;

                // 각도 기반 방향 벡터
                Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

                // 플레이어 위치 기준 슬롯 위치 계산
                Vector3 pos = player.position + dir * radius;

                // 슬롯 위치에 검사 반지름 표시
                Gizmos.DrawWireSphere(pos, slotCheckRadius);
            }
        }
    }
}
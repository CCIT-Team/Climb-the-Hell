using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 3종 공통 AI 최적화 버전.
///
/// 최적화 내용:
/// 1. 일반 AI 판단은 InvokeRepeating으로 일정 간격 실행
/// 2. 몬스터마다 첫 실행 시간을 랜덤하게 분산
/// 3. 주변 몬스터 OverlapSphere 검사 제거
/// 4. NavMeshAgent 자체 회피 기능 사용
/// 5. 목표 지점이 충분히 바뀌었을 때만 SetDestination 호출
/// 6. 공격 중에만 코루틴이 매 프레임 실행
///
/// 공격 범위:
/// 옥졸 = okjolAttackRadius
/// 야차 = dashDistance
/// 나찰 = nachalAttackRange
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : MonsterStats
{
    public enum MonsterType
    {
        Okjol,
        Yacha,
        Nachal
    }

    public enum State
    {
        Idle,
        Chase,
        MaintainDistance,
        Telegraph,
        Attacking,
        Recovery,
        Return,
        Dead
    }

    [Header("몬스터 종류")]
    [SerializeField]
    private MonsterType monsterType = MonsterType.Okjol;

    [Header("현재 상태")]
    public State currentState = State.Idle;

    [Header("보상")]
    public int rewardExp;
    public int rewardGold;
    public float rewardflowerleaf;
    public string MonsterName;

    [Header("플레이어 인식")]
    [Tooltip("플레이어를 발견하는 거리")]
    [Min(0.1f)]
    public float moverange = 10f;

    [Header("타겟")]
    [SerializeField]
    private Transform player;

    [Header("AI 갱신")]
    [Tooltip("AI 판단 간격. 일반적으로 0.15~0.3 권장")]
    [Min(0.05f)]
    [SerializeField]
    private float aiUpdateInterval = 0.2f;

    [Tooltip("목표가 이 거리 이상 바뀌었을 때만 경로를 다시 계산")]
    [Min(0.05f)]
    [SerializeField]
    private float repathDistance = 0.6f;

    [Tooltip("생성된 회피 위치를 NavMesh에서 찾는 반경")]
    [Min(0.1f)]
    [SerializeField]
    private float navMeshSampleRadius = 1.5f;

    [Header("NavMesh 복구")]
    [Tooltip("몬스터가 NavMesh 영역을 벗어났을 때, 복귀할 가장 가까운 지점을 찾는 검색 반경")]
    [Min(0.5f)]
    [SerializeField]
    private float navMeshRecoverySampleRadius = 5f;

    [Header("NavMesh 회피")]
    [Tooltip("몬스터가 많으면 Low 또는 Med 권장")]
    [SerializeField]
    private ObstacleAvoidanceType avoidanceQuality =
        ObstacleAvoidanceType.LowQualityObstacleAvoidance;

    [Min(0.05f)]
    [SerializeField]
    private float agentRadius = 0.3f;

    [Header("귀환")]
    [Min(1f)]
    [SerializeField]
    private float disengageRangeMultiplier = 1.35f;

    [Min(1f)]
    [SerializeField]
    private float leashDistance = 18f;

    [Min(0.1f)]
    [SerializeField]
    private float returnCompleteDistance = 0.4f;

    [Header("공격 시간")]
    [Tooltip("체크하면 옥졸, 야차, 나찰의 기본 공격 시간을 사용")]
    [SerializeField]
    private bool useTypeTimingPreset = true;

    [Tooltip("프리셋을 끈 경우 사용하는 공격 예고 시간")]
    [Min(0f)]
    [SerializeField]
    private float chargeTime = 1f;

    [Tooltip("프리셋을 끈 경우 사용하는 공격 후딜")]
    [Min(0f)]
    [SerializeField]
    private float recoveryTime = 1f;

    [Min(0f)]
    [SerializeField]
    private float attackRearmDelay = 0.1f;

    [Tooltip("공격 순간 진한 빨간 범위를 유지하는 시간")]
    [Min(0f)]
    [SerializeField]
    private float attackResultDisplayTime = 0.2f;

    [Header("공격 위치 예측")]
    [Range(0f, 0.5f)]
    [SerializeField]
    private float attackPredictionTime = 0.12f;

    [Min(0f)]
    [SerializeField]
    private float maxPredictionDistance = 1f;

    [Header("공격 범위 표시")]
    [SerializeField]
    private MonsterAttackTelegraph attackTelegraph;

    [Header("옥졸 공격")]
    [Tooltip("옥졸 원형 공격 반경")]
    [Min(0.1f)]
    [SerializeField]
    private float okjolAttackRadius = 2f;

    [Tooltip("공격 원 중심을 몬스터 앞쪽으로 옮기는 거리")]
    [Min(0f)]
    [SerializeField]
    private float meleeForwardOffset = 0.65f;

    [Header("야차 돌진")]
    [Tooltip("야차 공격 사거리이자 돌진 거리")]
    [Min(0.1f)]
    [SerializeField]
    private float dashDistance = 4f;

    [Min(0.1f)]
    [SerializeField]
    private float dashSpeed = 12f;

    [Tooltip("빨간 돌진 범위의 너비")]
    [Min(0.1f)]
    [SerializeField]
    private float dashWidth = 1.2f;

    [Tooltip("돌진 중 플레이어 타격 반경")]
    [Min(0.1f)]
    [SerializeField]
    private float dashHitRadius = 0.75f;

    [Min(0f)]
    [SerializeField]
    private float wallCrashStun = 0.8f;

    [Tooltip("돌진을 막는 벽 레이어만 선택")]
    [SerializeField]
    private LayerMask wallLayer;

    [Header("나찰 공격")]
    [Tooltip("나찰이 공격을 시작할 수 있는 최대 사거리")]
    [Min(0.1f)]
    [SerializeField]
    private float nachalAttackRange = 6f;

    [Tooltip("플레이어와 유지하려는 거리")]
    [Min(0.1f)]
    [SerializeField]
    private float preferredRange = 5f;

    [Tooltip("이 거리 안에서는 공격보다 회피를 우선")]
    [Min(0.1f)]
    [SerializeField]
    private float evadeRange = 2.2f;

    [Min(0f)]
    [SerializeField]
    private float rangeTolerance = 0.75f;

    [Min(0.1f)]
    [SerializeField]
    private float strafeDistance = 2.5f;

    [Min(0.1f)]
    [SerializeField]
    private float strafeChangeInterval = 1.4f;

    [Header("나찰 투사체")]
    [SerializeField]
    private MonsterProjectile projectilePrefab;

    [SerializeField]
    private Transform projectileSpawnPoint;

    [SerializeField]
    private float projectileTargetHeight = 0.8f;

    [Tooltip("빨간 발사 경로의 너비")]
    [Min(0.05f)]
    [SerializeField]
    private float projectileTelegraphWidth = 0.65f;

    [Header("시야 검사")]
    [Tooltip("시야를 막는 벽 레이어만 선택. 비우면 검사하지 않음")]
    [SerializeField]
    private LayerMask sightObstacleLayer;

    [SerializeField]
    private float eyeHeight = 1f;

    [SerializeField]
    private float playerTargetHeight = 0.8f;

    [Header("애니메이터")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string moveBoolName = "IsMoving";

    [SerializeField]
    private string windupTriggerName = "Windup";

    [SerializeField]
    private string attackTriggerName = "Attack";

    [SerializeField]
    private string recoveryTriggerName = "Recover";

    [Header("사망")]
    public bool isDead;

    public event Action<MonsterAI> OnMonsterDead;

    private NavMeshAgent agent;
    private Player playerComponent;

    private Vector3 homePosition;

    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;
    private float lastPlayerSampleTime;

    private Vector3 lockedTargetPosition;
    private Vector3 lockedAttackDirection;

    private Vector3 lastRequestedDestination;
    private bool hasRequestedDestination;

    private float currentDistance;
    private float nextAttackTime;
    private float nextStrafeChangeTime;

    private int strafeDirection = 1;

    private Coroutine attackRoutine;
    private bool hasStarted;

    private readonly HashSet<int> animatorParameterHashes =
        new HashSet<int>();

    protected override void Awake()
    {
        base.Awake();

        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if (attackTelegraph == null)
        {
            attackTelegraph =
                GetComponent<MonsterAttackTelegraph>();
        }

        if (attackTelegraph == null)
        {
            attackTelegraph =
                gameObject.AddComponent<MonsterAttackTelegraph>();
        }

        CacheAnimatorParameters();
        ApplyMonsterStatsToAI();
    }

    private void Start()
    {
        homePosition = transform.position;

        FindPlayer();

        if (player != null)
        {
            lastPlayerPosition = player.position;
            lastPlayerSampleTime = Time.time;
        }

        ChangeState(State.Idle);

        hasStarted = true;

        StartAITick();
    }

    private void OnEnable()
    {
        if (hasStarted)
        {
            StartAITick();
        }
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(AITick));

        StopAllCoroutines();
        attackRoutine = null;

        if (attackTelegraph != null)
        {
            attackTelegraph.Hide();
        }
    }

    /// <summary>
    /// 모든 몬스터의 AI 계산이 같은 프레임에 몰리지 않도록
    /// 첫 실행 시간을 랜덤하게 분산한다.
    /// </summary>
    private void StartAITick()
    {
        CancelInvoke(nameof(AITick));

        float interval =
            Mathf.Max(0.05f, aiUpdateInterval);

        float randomDelay =
            UnityEngine.Random.Range(0f, interval);

        InvokeRepeating(
            nameof(AITick),
            randomDelay,
            interval
        );
    }

    /// <summary>
    /// 일반 상태 판단과 경로 갱신은 이 메서드에서만 실행한다.
    /// 공격 중의 방향 회전과 돌진은 공격 코루틴이 처리한다.
    /// </summary>
    private void AITick()
    {
        if (isDead ||
            currentState == State.Dead)
        {
            return;
        }

        if (player == null)
        {
            FindPlayer();

            if (player == null)
            {
                StopAgent();
                return;
            }

            lastPlayerPosition = player.position;
            lastPlayerSampleTime = Time.time;
        }

        if (agent == null)
        {
            return;
        }

        // NavMesh 영역을 벗어난 상태라면 즉시 가장 가까운 지점으로 복귀시킨다.
        // 복귀 전까지는 상태 판단(Chase/Attack 등)을 진행하지 않는다.
        if (!agent.isOnNavMesh)
        {
            TryRecoverToNavMesh();
            return;
        }

        UpdatePlayerVelocity();

        currentDistance =
            GetPlanarDistance(
                transform.position,
                player.position
            );

        UpdateAnimator();

        if (attackRoutine != null)
        {
            return;
        }

        switch (currentState)
        {
            case State.Idle:
                UpdateIdle();
                break;

            case State.Chase:
                UpdateChase();
                break;

            case State.MaintainDistance:
                UpdateMaintainDistance();
                break;

            case State.Return:
                UpdateReturn();
                break;
        }
    }

    /// <summary>
    /// 에이전트가 NavMesh 영역 밖에 있을 때 호출된다.
    /// 현재 위치를 기준으로 NavMesh.SamplePosition을 통해
    /// 가장 가까운 유효 지점을 찾고, agent.Warp으로 그 지점에 즉시 복귀시킨다.
    /// Warp은 isOnNavMesh 여부와 무관하게 호출 가능하며,
    /// 호출 시 내부 경로/속도를 리셋하므로 SetDestinationIfChanged와의
    /// 목표 캐시(lastRequestedDestination)도 함께 초기화한다.
    /// </summary>
    private void TryRecoverToNavMesh()
    {
        if (agent == null)
        {
            return;
        }

        // 1단계: 현재(이탈한) 위치 근처에서 가장 가까운 NavMesh 지점을 찾는다.
        // 넉백/충돌 등으로 아주 멀리 튕겨나간 경우, 좁은 반경 안에는
        // NavMesh가 아예 없을 수 있으므로 이 단계는 실패할 수 있다.
        if (NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                navMeshRecoverySampleRadius,
                NavMesh.AllAreas
            ))
        {
            agent.Warp(hit.position);

            hasRequestedDestination = false;

            return;
        }

        // 2단계: 현재 위치 근처에서 찾지 못했다면, 스폰 시점 위치(homePosition)는
        // 항상 NavMesh 위였다고 신뢰할 수 있으므로 그 근처에서 재시도한다.
        // 아무리 멀리 날아갔더라도 이 단계에서는 사실상 항상 성공한다.
        if (NavMesh.SamplePosition(
                homePosition,
                out NavMeshHit homeHit,
                navMeshRecoverySampleRadius,
                NavMesh.AllAreas
            ))
        {
            agent.Warp(homeHit.position);

            hasRequestedDestination = false;

            Debug.LogWarning(
                $"[{MonsterName}] " +
                "현재 위치 근처에서 NavMesh를 찾지 못해 " +
                "스폰 지점(homePosition) 근처로 강제 복귀했습니다.",
                gameObject
            );

            return;
        }

        // 스폰 지점 근처에서도 실패하는 경우는 사실상 발생하면 안 되는
        // 예외 상황이다(NavMesh 자체가 손상되었거나 재빌드된 경우 등).
        Debug.LogWarning(
            $"[{MonsterName}] " +
            "NavMesh 복귀 지점을 찾지 못했습니다(스폰 지점 포함). " +
            "navMeshRecoverySampleRadius를 늘리거나 NavMesh 베이크 상태를 확인해야 합니다.",
            gameObject
        );
    }

    public void ApplyMonsterStatsToAI()
    {
        if (monsterspeed <= 0f)
        {
            monsterspeed = 3f;
        }

        if (moverange <= 0f)
        {
            moverange = 10f;
        }

        if (agent == null)
        {
            return;
        }

        agent.speed = monsterspeed;

        agent.acceleration =
            Mathf.Max(
                monsterspeed * 4f,
                8f
            );

        agent.angularSpeed = 720f;
        agent.stoppingDistance = 0.1f;
        agent.radius = agentRadius;

        // HighQuality는 몬스터가 많을 때 비용이 크다.
        agent.obstacleAvoidanceType =
            avoidanceQuality;

        agent.avoidancePriority =
            UnityEngine.Random.Range(10, 90);

        agent.autoBraking = true;
    }

    private void UpdateIdle()
    {
        StopAgent();

        float playerHomeDistance =
            GetPlanarDistance(
                homePosition,
                player.position
            );

        if (currentDistance <= moverange &&
            playerHomeDistance <= leashDistance)
        {
            BeginPursuit();
        }
    }

    private void UpdateChase()
    {
        if (ShouldReturnToHome())
        {
            ChangeState(State.Return);
            return;
        }

        if (CanStartAttack())
        {
            BeginAttack();
            return;
        }

        // 플레이어는 NavMesh 위에 있다고 가정하므로
        // 매번 SamplePosition을 호출하지 않는다.
        SetDestinationIfChanged(
            player.position,
            false
        );
    }

    private void UpdateMaintainDistance()
    {
        if (ShouldReturnToHome())
        {
            ChangeState(State.Return);
            return;
        }

        LookAtPlayer();

        if (currentDistance >= evadeRange &&
            CanStartAttack())
        {
            BeginAttack();
            return;
        }

        if (Time.time >= nextStrafeChangeTime)
        {
            strafeDirection =
                UnityEngine.Random.value < 0.5f
                    ? -1
                    : 1;

            nextStrafeChangeTime =
                Time.time +
                strafeChangeInterval;
        }

        Vector3 toPlayer =
            player.position -
            transform.position;

        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 targetPosition;

        if (currentDistance < evadeRange)
        {
            Vector3 awayDirection =
                -toPlayer.normalized;

            Vector3 sideDirection =
                Vector3.Cross(
                    Vector3.up,
                    awayDirection
                ) * strafeDirection;

            Vector3 evadeDirection =
                (
                    awayDirection +
                    sideDirection * 0.65f
                ).normalized;

            targetPosition =
                transform.position +
                evadeDirection *
                strafeDistance;
        }
        else if (currentDistance >
                 preferredRange +
                 rangeTolerance)
        {
            targetPosition =
                player.position;
        }
        else
        {
            Vector3 sideDirection =
                Vector3.Cross(
                    Vector3.up,
                    toPlayer.normalized
                ) * strafeDirection;

            targetPosition =
                transform.position +
                sideDirection *
                strafeDistance;
        }

        // 회피 위치는 임의 계산 위치이므로
        // 이 경우에만 SamplePosition을 사용한다.
        SetDestinationIfChanged(
            targetPosition,
            true
        );
    }

    private void UpdateReturn()
    {
        float distanceToHome =
            GetPlanarDistance(
                transform.position,
                homePosition
            );

        if (distanceToHome <=
            returnCompleteDistance)
        {
            StopAgent();
            ChangeState(State.Idle);
            return;
        }

        SetDestinationIfChanged(
            homePosition,
            true
        );
    }

    private void BeginPursuit()
    {
        if (monsterType ==
            MonsterType.Nachal)
        {
            ChangeState(
                State.MaintainDistance
            );
        }
        else
        {
            ChangeState(State.Chase);
        }
    }

    private bool CanStartAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return false;
        }

        if (currentDistance >
            GetAttackStartRange())
        {
            return false;
        }

        return HasLineOfSight();
    }

    private void BeginAttack()
    {
        if (attackRoutine != null)
        {
            return;
        }

        attackRoutine =
            StartCoroutine(
                AttackSequence()
            );
    }

    private IEnumerator AttackSequence()
    {
        StopAgent();
        CaptureAttackTarget();

        ChangeState(State.Telegraph);

        SetAnimatorTrigger(
            windupTriggerName
        );

        ShowAttackTelegraph();

        float elapsed = 0f;
        float currentChargeTime =
            GetChargeTime();

        while (elapsed < currentChargeTime)
        {
            if (isDead)
            {
                yield break;
            }

            LookAtDirection(
                lockedAttackDirection
            );

            elapsed += Time.deltaTime;

            yield return null;
        }

        if (attackTelegraph != null)
        {
            attackTelegraph
                .ShowAttackResult();
        }

        ChangeState(State.Attacking);

        SetAnimatorTrigger(
            attackTriggerName
        );

        yield return ExecuteAttack();

        if (isDead)
        {
            yield break;
        }

        if (attackResultDisplayTime > 0f)
        {
            yield return
                new WaitForSeconds(
                    attackResultDisplayTime
                );
        }

        if (attackTelegraph != null)
        {
            attackTelegraph.Hide();
        }

        ChangeState(State.Recovery);

        StopAgent();

        SetAnimatorTrigger(
            recoveryTriggerName
        );

        float currentRecoveryTime =
            GetRecoveryTime();

        if (currentRecoveryTime > 0f)
        {
            yield return
                new WaitForSeconds(
                    currentRecoveryTime
                );
        }

        nextAttackTime =
            Time.time +
            attackRearmDelay;

        attackRoutine = null;

        if (ShouldReturnToHome())
        {
            ChangeState(State.Return);
        }
        else
        {
            BeginPursuit();
        }
    }

    private IEnumerator ExecuteAttack()
    {
        switch (monsterType)
        {
            case MonsterType.Okjol:
                ExecuteOkjolAttack();
                break;

            case MonsterType.Yacha:
                yield return
                    ExecuteYachaDash();
                break;

            case MonsterType.Nachal:
                ExecuteNachalProjectile();
                break;
        }
    }

    private void ExecuteOkjolAttack()
    {
        Vector3 attackCenter =
            transform.position +
            lockedAttackDirection *
            meleeForwardOffset;

        if (IsPlayerInsideRadius(
                attackCenter,
                okjolAttackRadius
            ))
        {
            DamagePlayer();
        }
    }

    private IEnumerator ExecuteYachaDash()
    {
        bool hasDamagedPlayer = false;
        bool collidedWithWall = false;

        float travelledDistance = 0f;

        LookAtDirection(
            lockedAttackDirection
        );

        agent.isStopped = false;

        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        hasRequestedDestination = false;

        while (travelledDistance <
               dashDistance)
        {
            if (isDead ||
                !agent.isOnNavMesh)
            {
                yield break;
            }

            float moveDistance =
                dashSpeed *
                Time.deltaTime;

            float remainingDistance =
                dashDistance -
                travelledDistance;

            moveDistance =
                Mathf.Min(
                    moveDistance,
                    remainingDistance
                );

            Vector3 castOrigin =
                transform.position +
                Vector3.up * 0.5f;

            // 돌진 중에만 짧은 SphereCast를 실행한다.
            if (wallLayer.value != 0 &&
                Physics.SphereCast(
                    castOrigin,
                    Mathf.Max(
                        agent.radius * 0.8f,
                        0.15f
                    ),
                    lockedAttackDirection,
                    out _,
                    moveDistance + 0.08f,
                    wallLayer,
                    QueryTriggerInteraction.Ignore
                ))
            {
                collidedWithWall = true;
                break;
            }

            agent.Move(
                lockedAttackDirection *
                moveDistance
            );

            travelledDistance +=
                moveDistance;

            if (!hasDamagedPlayer &&
                IsPlayerInsideRadius(
                    transform.position,
                    dashHitRadius
                ))
            {
                DamagePlayer();
                hasDamagedPlayer = true;
            }

            yield return null;
        }

        StopAgent();

        if (collidedWithWall &&
            wallCrashStun > 0f)
        {
            yield return
                new WaitForSeconds(
                    wallCrashStun
                );
        }
    }

    private void ExecuteNachalProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning(
                $"[{MonsterName}] " +
                "Projectile Prefab이 없습니다.",
                gameObject
            );

            return;
        }

        Vector3 spawnPosition =
            projectileSpawnPoint != null
                ? projectileSpawnPoint.position
                : transform.position +
                  Vector3.up;

        Vector3 targetPosition =
            lockedTargetPosition +
            Vector3.up *
            projectileTargetHeight;

        Vector3 direction =
            targetPosition -
            spawnPosition;

        if (direction.sqrMagnitude <=
            0.001f)
        {
            direction =
                transform.forward;
        }

        direction.Normalize();

        MonsterProjectile projectile =
            Instantiate(
                projectilePrefab,
                spawnPosition,
                Quaternion.LookRotation(
                    direction
                )
            );

        projectile.Initialize(
            direction,
            monsterattack,
            transform
        );
    }

    private void CaptureAttackTarget()
    {
        Vector3 predictedOffset =
            playerVelocity *
            attackPredictionTime;

        predictedOffset.y = 0f;

        if (predictedOffset.magnitude >
            maxPredictionDistance)
        {
            predictedOffset =
                predictedOffset.normalized *
                maxPredictionDistance;
        }

        lockedTargetPosition =
            player.position +
            predictedOffset;

        Vector3 direction =
            lockedTargetPosition -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <=
            0.001f)
        {
            direction =
                transform.forward;
        }

        lockedAttackDirection =
            direction.normalized;
    }

    private void ShowAttackTelegraph()
    {
        if (attackTelegraph == null)
        {
            return;
        }

        switch (monsterType)
        {
            case MonsterType.Okjol:
            {
                Vector3 center =
                    transform.position +
                    lockedAttackDirection *
                    meleeForwardOffset;

                attackTelegraph.ShowCircle(
                    center,
                    okjolAttackRadius
                );

                break;
            }

            case MonsterType.Yacha:
            {
                attackTelegraph.ShowBox(
                    transform.position,
                    lockedAttackDirection,
                    dashWidth,
                    dashDistance
                );

                break;
            }

            case MonsterType.Nachal:
            {
                Vector3 startPosition =
                    projectileSpawnPoint != null
                        ? projectileSpawnPoint
                            .position
                        : transform.position;

                Vector3 flatTarget =
                    lockedTargetPosition;

                flatTarget.y =
                    startPosition.y;

                float length =
                    Vector3.Distance(
                        startPosition,
                        flatTarget
                    );

                length =
                    Mathf.Clamp(
                        length,
                        0.5f,
                        nachalAttackRange
                    );

                attackTelegraph.ShowBox(
                    startPosition,
                    lockedAttackDirection,
                    projectileTelegraphWidth,
                    length
                );

                break;
            }
        }
    }

    private float GetAttackStartRange()
    {
        switch (monsterType)
        {
            case MonsterType.Okjol:
                return okjolAttackRadius;

            case MonsterType.Yacha:
                return dashDistance;

            case MonsterType.Nachal:
                return nachalAttackRange;

            default:
                return 2f;
        }
    }

    private float GetChargeTime()
    {
        if (!useTypeTimingPreset)
        {
            return chargeTime;
        }

        switch (monsterType)
        {
            case MonsterType.Okjol:
                return 1.2f;

            case MonsterType.Yacha:
                return 0.5f;

            case MonsterType.Nachal:
                return 0.8f;

            default:
                return 1f;
        }
    }

    private float GetRecoveryTime()
    {
        if (!useTypeTimingPreset)
        {
            return recoveryTime;
        }

        switch (monsterType)
        {
            case MonsterType.Okjol:
                return 1f;

            case MonsterType.Yacha:
                return 0.9f;

            case MonsterType.Nachal:
                return 1.3f;

            default:
                return 1f;
        }
    }

    private bool ShouldReturnToHome()
    {
        if (player == null)
        {
            return true;
        }

        float selfHomeDistance =
            GetPlanarDistance(
                transform.position,
                homePosition
            );

        float playerHomeDistance =
            GetPlanarDistance(
                player.position,
                homePosition
            );

        float livePlayerDistance =
            GetPlanarDistance(
                transform.position,
                player.position
            );

        return
            selfHomeDistance >
                leashDistance ||
            playerHomeDistance >
                leashDistance ||
            livePlayerDistance >
                moverange *
                disengageRangeMultiplier;
    }

    private bool HasLineOfSight()
    {
        if (sightObstacleLayer.value == 0)
        {
            return true;
        }

        Vector3 origin =
            transform.position +
            Vector3.up *
            eyeHeight;

        Vector3 target =
            player.position +
            Vector3.up *
            playerTargetHeight;

        return !Physics.Linecast(
            origin,
            target,
            sightObstacleLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    /// <summary>
    /// 목표가 충분히 바뀌었을 때만 SetDestination을 호출한다.
    /// 잦은 NavMesh 경로 재계산을 줄이는 핵심 부분이다.
    /// </summary>
    private void SetDestinationIfChanged(
        Vector3 targetPosition,
        bool sampleNavMesh
    )
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            return;
        }

        Vector3 finalTarget =
            targetPosition;

        if (sampleNavMesh)
        {
            if (!NavMesh.SamplePosition(
                    targetPosition,
                    out NavMeshHit hit,
                    navMeshSampleRadius,
                    NavMesh.AllAreas
                ))
            {
                return;
            }

            finalTarget = hit.position;
        }

        float threshold =
            Mathf.Max(
                0.05f,
                repathDistance
            );

        if (hasRequestedDestination &&
            agent.hasPath &&
            (
                finalTarget -
                lastRequestedDestination
            ).sqrMagnitude <
            threshold * threshold)
        {
            return;
        }

        agent.isStopped = false;

        if (agent.SetDestination(
                finalTarget
            ))
        {
            lastRequestedDestination =
                finalTarget;

            hasRequestedDestination =
                true;
        }
    }

    private void UpdatePlayerVelocity()
    {
        if (player == null)
        {
            return;
        }

        float now = Time.time;

        float deltaTime =
            now -
            lastPlayerSampleTime;

        if (deltaTime <= 0.0001f)
        {
            return;
        }

        playerVelocity =
            (
                player.position -
                lastPlayerPosition
            ) / deltaTime;

        playerVelocity.y = 0f;

        lastPlayerPosition =
            player.position;

        lastPlayerSampleTime = now;
    }

    private bool IsPlayerInsideRadius(
        Vector3 center,
        float radius
    )
    {
        if (player == null)
        {
            return false;
        }

        const float playerBodyRadius =
            0.4f;

        float distance =
            GetPlanarDistance(
                center,
                player.position
            );

        return distance <=
               radius +
               playerBodyRadius;
    }

    private void DamagePlayer()
    {
        if (playerComponent == null)
        {
            FindPlayer();
        }

        if (playerComponent == null)
        {
            return;
        }

        playerComponent.TakeDamage(
            monsterattack
        );
    }

    private void FindPlayer()
    {
        GameObject target =
            GameObject.FindWithTag("Player");

        if (target == null)
        {
            player = null;
            playerComponent = null;
            return;
        }

        player = target.transform;

        playerComponent =
            target.GetComponent<Player>();

        if (playerComponent == null)
        {
            playerComponent =
                target.GetComponentInParent<Player>();
        }
    }

    private void StopAgent()
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            return;
        }

        agent.isStopped = true;

        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        agent.velocity =
            Vector3.zero;

        hasRequestedDestination =
            false;
    }

    private void LookAtPlayer()
    {
        if (player == null)
        {
            return;
        }

        LookAtDirection(
            player.position -
            transform.position
        );
    }

    private void LookAtDirection(
        Vector3 direction
    )
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                720f * Time.deltaTime
            );
    }

    private static float GetPlanarDistance(
        Vector3 first,
        Vector3 second
    )
    {
        first.y = 0f;
        second.y = 0f;

        return Vector3.Distance(
            first,
            second
        );
    }

    private void ChangeState(
        State newState
    )
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;
    }

    public override bool TakeDamage(
        int damage
    )
    {
        if (isDead)
        {
            return false;
        }

        bool dead =
            base.TakeDamage(damage);

        Debug.Log(
            $"{MonsterName} HP: " +
            $"{currentHp}/{monsterhp}"
        );

        if (dead)
        {
            DropReward();
            Die();
        }

        return dead;
    }

    public void DropReward()
    {
        GrantGoldToPlayer(rewardGold);
        GrantFlowerLeafToPlayer(rewardflowerleaf);

        ShowGoldNumber(
            rewardGold,
            transform.position
        );

        ShowFlowerNumber(
            rewardflowerleaf,
            transform.position
        );

        Debug.Log(
            $"[Monster:{MonsterName}] " +
            $"경험치 {rewardExp}, " +
            $"골드 {rewardGold}, " +
            $"꽃잎 {rewardflowerleaf} 드랍"
        );
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentState = State.Dead;

        CancelInvoke(nameof(AITick));

        StopAllCoroutines();
        attackRoutine = null;

        if (attackTelegraph != null)
        {
            attackTelegraph.Hide();
        }

        StopAgent();

        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider targetCollider
                 in colliders)
        {
            targetCollider.enabled = false;
        }

        OnMonsterDead?.Invoke(this);

        Destroy(gameObject, 0.5f);
    }

    public bool IsAlive()
    {
        return !isDead;
    }

    private void CacheAnimatorParameters()
    {
        animatorParameterHashes.Clear();

        if (animator == null ||
            animator.runtimeAnimatorController ==
            null)
        {
            return;
        }

        foreach (
            AnimatorControllerParameter parameter
            in animator.parameters
        )
        {
            animatorParameterHashes.Add(
                parameter.nameHash
            );
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null ||
            string.IsNullOrEmpty(
                moveBoolName
            ))
        {
            return;
        }

        int moveHash =
            Animator.StringToHash(
                moveBoolName
            );

        if (!animatorParameterHashes
                .Contains(moveHash))
        {
            return;
        }

        bool isMoving =
            agent != null &&
            agent.velocity.sqrMagnitude >
                0.05f &&
            currentState !=
                State.Telegraph &&
            currentState !=
                State.Recovery;

        animator.SetBool(
            moveHash,
            isMoving
        );
    }

    private void SetAnimatorTrigger(
        string parameterName
    )
    {
        if (animator == null ||
            string.IsNullOrEmpty(
                parameterName
            ))
        {
            return;
        }

        int parameterHash =
            Animator.StringToHash(
                parameterName
            );

        if (!animatorParameterHashes
                .Contains(parameterHash))
        {
            return;
        }

        animator.SetTrigger(
            parameterHash
        );
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 drawHomePosition =
            Application.isPlaying
                ? homePosition
                : transform.position;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(
            transform.position,
            moverange
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            GetAttackStartRange()
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            drawHomePosition,
            leashDistance
        );
    }
}
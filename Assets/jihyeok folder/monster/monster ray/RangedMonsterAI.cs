using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RangedMonsterAI : MonsterStats
{
    public enum State
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    [Header("State")]
    public State currentState = State.Idle;

    [Header("Reward")]
    public int rewardExp;
    public int rewardGold;
    public float rewardflowerleaf;
    public string MonsterName;

    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float detectRange = 12f;
    public float attackRange = 8f;
    public float stoppingDistance = 5f;

    [Header("Attack")]
    [Tooltip("1000이면 1초마다 공격")]
    public int attackSpeed = 1500;

    [Tooltip("공격 전 조준 시간")]
    public float aimTime = 0.6f;

    [Tooltip("몬스터에서 플레이어까지 시각 오브젝트 이동 시간")]
    public float forwardVisualTime = 0.25f;

    [Tooltip("플레이어에서 몬스터까지 반사 오브젝트 이동 시간")]
    public float reflectedVisualTime = 0.25f;

    [Tooltip("반사 피해 배율")]
    public float reflectedDamageMultiplier = 1f;

    [Header("Attack Position")]
    [Tooltip("몬스터 손이나 무기 끝")]
    public Transform firePoint;

    [Header("Visual Projectile")]
    [Tooltip("판정 없는 시각용 투사체")]
    public GameObject projectileVisualPrefab;

    [Header("Line Renderer")]
    public LineRenderer aimLine;
    public LineRenderer shotLine;
    public float shotLineTime = 0.08f;

    [Header("Raycast Mask")]
    [Tooltip("Player, Obstacle 포함")]
    public LayerMask attackHitMask;

    [Tooltip("Monster, Obstacle 포함")]
    public LayerMask reflectedHitMask;

    [Header("Reflection")]
    [Tooltip("반사 Ray 최대 거리")]
    public float reflectedRayDistance = 30f;

    [Tooltip("반사 Ray 시작 위치 보정")]
    public float reflectedOriginOffset = 0.05f;

    [Header("Debug")]
    public bool showDebugLog = true;
    public bool drawDebugRay = true;

    [Header("Dead")]
    public bool isDead;

    public event Action<RangedMonsterAI>
        OnMonsterDead;

    private NavMeshAgent agent;

    private float attackCooldown;
    private float lastAttackTime;
    private float distanceToPlayer;

    private bool isAttacking;

    protected override void Awake()
    {
        base.Awake();

        agent = GetComponent<NavMeshAgent>();

        if (monsterspeed <= 0f)
        {
            monsterspeed = 3f;
        }

        if (detectRange <= 0f)
        {
            detectRange = 12f;
        }

        if (attackRange <= 0f)
        {
            attackRange = 8f;
        }

        if (stoppingDistance <= 0f)
        {
            stoppingDistance = 5f;
        }

        if (attackSpeed <= 0)
        {
            attackSpeed = 1500;
        }

        attackCooldown =
            attackSpeed / 1000f;

        if (agent != null)
        {
            agent.speed = monsterspeed;

            agent.stoppingDistance =
                stoppingDistance;

            agent.autoBraking = true;
        }

        SetupLineRenderer(aimLine);
        SetupLineRenderer(shotLine);
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (currentState == State.Dead)
        {
            return;
        }

        if (player == null)
        {
            FindPlayer();

            if (player == null)
            {
                return;
            }
        }

        if (
            agent == null ||
            !agent.isOnNavMesh
        )
        {
            return;
        }

        distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );

        UpdateState();
        UpdateAction();
    }

    private void SetupLineRenderer(
        LineRenderer line
    )
    {
        if (line == null)
        {
            return;
        }

        line.positionCount = 2;
        line.useWorldSpace = true;
        line.enabled = false;
    }

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindWithTag("Player");

        if (playerObject != null)
        {
            player =
                playerObject.transform;
        }
    }

    private void UpdateState()
    {
        switch (currentState)
        {
            case State.Idle:

                if (
                    distanceToPlayer <=
                    detectRange
                )
                {
                    ChangeState(
                        State.Chase
                    );
                }

                break;

            case State.Chase:

                if (
                    distanceToPlayer <=
                    attackRange
                )
                {
                    ChangeState(
                        State.Attack
                    );
                }
                else if (
                    distanceToPlayer >
                    detectRange
                )
                {
                    ChangeState(
                        State.Idle
                    );
                }

                break;

            case State.Attack:

                if (
                    distanceToPlayer >
                    attackRange * 1.2f
                )
                {
                    ChangeState(
                        State.Chase
                    );
                }

                break;
        }
    }

    private void UpdateAction()
    {
        switch (currentState)
        {
            case State.Idle:

                StopAgent();
                break;

            case State.Chase:

                ChasePlayer();
                break;

            case State.Attack:

                KeepAttackDistance();
                LookAtPlayer();

                if (
                    Time.time >=
                    lastAttackTime +
                    attackCooldown &&
                    !isAttacking
                )
                {
                    StartCoroutine(
                        AttackRoutine()
                    );

                    lastAttackTime =
                        Time.time;
                }

                break;
        }
    }

    private void ChasePlayer()
    {
        agent.isStopped = false;

        agent.SetDestination(
            player.position
        );
    }

    private void KeepAttackDistance()
    {
        if (
            distanceToPlayer >
            stoppingDistance
        )
        {
            agent.isStopped = false;

            agent.SetDestination(
                player.position
            );
        }
        else
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    private IEnumerator AttackRoutine()
    {
        if (
            player == null ||
            firePoint == null
        )
        {
            yield break;
        }

        isAttacking = true;

        float elapsed = 0f;

        Vector3 targetPosition =
            GetPlayerAimPoint();

        if (aimLine != null)
        {
            aimLine.enabled = true;
        }

        while (elapsed < aimTime)
        {
            if (player == null)
            {
                StopAttackVisual();

                isAttacking = false;

                yield break;
            }

            targetPosition =
                GetPlayerAimPoint();

            if (aimLine != null)
            {
                aimLine.SetPosition(
                    0,
                    firePoint.position
                );

                aimLine.SetPosition(
                    1,
                    targetPosition
                );
            }

            elapsed += Time.deltaTime;

            yield return null;
        }

        if (aimLine != null)
        {
            aimLine.enabled = false;
        }

        Vector3 rayStart =
            firePoint.position;

        Vector3 incomingDirection =
            targetPosition -
            rayStart;

        if (
            incomingDirection.sqrMagnitude <=
            0.001f
        )
        {
            isAttacking = false;

            yield break;
        }

        incomingDirection.Normalize();

        Vector3 visualEndPosition =
            rayStart +
            incomingDirection *
            attackRange;

        /*
         * 일반 Collider 미리보기 판정
         */
        bool previewHasHit =
            Physics.Raycast(
                rayStart,
                incomingDirection,
                out RaycastHit previewHit,
                attackRange,
                attackHitMask,
                QueryTriggerInteraction.Ignore
            );

        float previewNormalDistance =
            previewHasHit
                ? previewHit.distance
                : float.MaxValue;

        /*
         * 뮐러–트럼보어 알고리즘으로
         * 반사 면 미리보기 판정
         */
        bool previewReflectHit =
            TryGetReflectHit(
                rayStart,
                incomingDirection,
                attackRange,
                out Vector3 previewReflectPoint,
                out float previewReflectDistance
            );

        if (
            previewReflectHit &&
            previewReflectDistance <=
            previewNormalDistance
        )
        {
            visualEndPosition =
                previewReflectPoint;
        }
        else if (
            previewHasHit &&
            previewHit.collider != null
        )
        {
            visualEndPosition =
                previewHit.point;
        }

        if (shotLine != null)
        {
            StartCoroutine(
                ShowShotLine(
                    rayStart,
                    visualEndPosition
                )
            );
        }

        GameObject visualProjectile =
            CreateVisualProjectile(
                rayStart,
                incomingDirection
            );

        yield return StartCoroutine(
            MoveVisualProjectile(
                visualProjectile,
                rayStart,
                visualEndPosition,
                forwardVisualTime
            )
        );

        /*
         * 실제 일반 Collider 공격 판정
         */
        bool actualHasHit =
            Physics.Raycast(
                rayStart,
                incomingDirection,
                out RaycastHit actualHit,
                attackRange,
                attackHitMask,
                QueryTriggerInteraction.Ignore
            );

        float actualNormalDistance =
            actualHasHit
                ? actualHit.distance
                : float.MaxValue;

        /*
         * 실제 뮐러–트럼보어 반사 판정
         */
        bool actualReflectHit =
            TryGetReflectHit(
                rayStart,
                incomingDirection,
                attackRange,
                out Vector3 reflectHitPoint,
                out float reflectHitDistance
            );

        if (drawDebugRay)
        {
            Debug.DrawRay(
                rayStart,
                incomingDirection *
                attackRange,
                actualReflectHit
                    ? Color.cyan
                    : actualHasHit
                        ? Color.red
                        : Color.yellow,
                1f
            );
        }

        /*
         * 반사 면이 장애물이나 플레이어보다
         * 먼저 교차했을 때만 반사 성공
         */
        if (
            actualReflectHit &&
            reflectHitDistance <=
            actualNormalDistance
        )
        {
            if (showDebugLog)
            {
                Debug.Log(
                    $"{name}: 뮐러–트럼보어 " +
                    "교차 판정으로 반사 성공"
                );
            }

            yield return StartCoroutine(
                ReflectAttack(
                    reflectHitPoint,
                    incomingDirection,
                    visualProjectile
                )
            );

            isAttacking = false;

            yield break;
        }

        if (
            !actualHasHit ||
            actualHit.collider == null
        )
        {
            if (showDebugLog)
            {
                Debug.Log(
                    $"{name}: 공격이 아무것도 " +
                    "맞히지 못함"
                );
            }

            DestroyVisualProjectile(
                visualProjectile
            );

            isAttacking = false;

            yield break;
        }

        yield return StartCoroutine(
            ProcessAttackHit(
                actualHit,
                visualProjectile
            )
        );

        isAttacking = false;
    }

    private IEnumerator ProcessAttackHit(
        RaycastHit attackHit,
        GameObject visualProjectile
    )
    {
        if (attackHit.collider == null)
        {
            DestroyVisualProjectile(
                visualProjectile
            );

            yield break;
        }

        Player playerComponent =
            attackHit.collider
                .GetComponentInParent<Player>();

        if (playerComponent != null)
        {
            playerComponent.TakeDamage(
                monsterattack
            );

            if (showDebugLog)
            {
                Debug.Log(
                    $"{name}: 플레이어 피격, " +
                    $"{monsterattack} 데미지"
                );
            }
        }
        else if (showDebugLog)
        {
            Debug.Log(
                $"{name}: 공격이 " +
                $"{attackHit.collider.name}에 막힘"
            );
        }

        DestroyVisualProjectile(
            visualProjectile
        );

        yield return null;
    }

    /*
     * 플레이어 반사 사각형을
     * 삼각형 두 개로 나눈 후
     * 뮐러–트럼보어 알고리즘 실행
     */
    private bool TryGetReflectHit(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        float maxDistance,
        out Vector3 hitPoint,
        out float hitDistance
    )
    {
        hitPoint = Vector3.zero;
        hitDistance = 0f;

        if (player == null)
        {
            return false;
        }

        PlayerAttackReflect reflect =
            player.GetComponent<
                PlayerAttackReflect
            >();

        if (reflect == null)
        {
            reflect =
                player.GetComponentInChildren<
                    PlayerAttackReflect
                >();
        }

        if (
            reflect == null ||
            !reflect.IsReflecting
        )
        {
            return false;
        }

        reflect.GetReflectVertices(
            out Vector3 v0,
            out Vector3 v1,
            out Vector3 v2,
            out Vector3 v3
        );

        bool hitTriangle1 =
            MollerTrumboreIntersect(
                rayOrigin,
                rayDirection,
                v0,
                v1,
                v2,
                out float distance1
            );

        bool hitTriangle2 =
            MollerTrumboreIntersect(
                rayOrigin,
                rayDirection,
                v0,
                v2,
                v3,
                out float distance2
            );

        if (
            !hitTriangle1 &&
            !hitTriangle2
        )
        {
            return false;
        }

        if (
            hitTriangle1 &&
            hitTriangle2
        )
        {
            hitDistance =
                Mathf.Min(
                    distance1,
                    distance2
                );
        }
        else if (hitTriangle1)
        {
            hitDistance = distance1;
        }
        else
        {
            hitDistance = distance2;
        }

        if (
            hitDistance < 0f ||
            hitDistance > maxDistance
        )
        {
            return false;
        }

        hitPoint =
            rayOrigin +
            rayDirection.normalized *
            hitDistance;

        return true;
    }

    /*
     * 뮐러–트럼보어 광선-삼각형
     * 교차 알고리즘 직접 구현
     */
    private bool MollerTrumboreIntersect(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        Vector3 vertex0,
        Vector3 vertex1,
        Vector3 vertex2,
        out float distance
    )
    {
        distance = 0f;

        const float epsilon =
            0.000001f;

        if (
            rayDirection.sqrMagnitude <=
            epsilon
        )
        {
            return false;
        }

        rayDirection.Normalize();

        // 삼각형의 두 변
        Vector3 edge1 =
            vertex1 - vertex0;

        Vector3 edge2 =
            vertex2 - vertex0;

        // 광선 방향과 두 번째 변의 외적
        Vector3 pVector =
            Vector3.Cross(
                rayDirection,
                edge2
            );

        // 행렬식 계산
        float determinant =
            Vector3.Dot(
                edge1,
                pVector
            );

        /*
         * 행렬식이 0에 가까우면
         * 광선과 삼각형 평면이 평행
         */
        if (
            determinant > -epsilon &&
            determinant < epsilon
        )
        {
            return false;
        }

        float inverseDeterminant =
            1f / determinant;

        Vector3 tVector =
            rayOrigin - vertex0;

        // 무게중심 좌표 u 계산
        float u =
            Vector3.Dot(
                tVector,
                pVector
            ) *
            inverseDeterminant;

        if (
            u < 0f ||
            u > 1f
        )
        {
            return false;
        }

        Vector3 qVector =
            Vector3.Cross(
                tVector,
                edge1
            );

        // 무게중심 좌표 v 계산
        float v =
            Vector3.Dot(
                rayDirection,
                qVector
            ) *
            inverseDeterminant;

        if (
            v < 0f ||
            u + v > 1f
        )
        {
            return false;
        }

        // 광선 시작점부터 교차점까지 거리
        float t =
            Vector3.Dot(
                edge2,
                qVector
            ) *
            inverseDeterminant;

        if (t <= epsilon)
        {
            return false;
        }

        distance = t;

        return true;
    }

    private IEnumerator ReflectAttack(
        Vector3 reflectHitPoint,
        Vector3 incomingDirection,
        GameObject visualProjectile
    )
    {
        Vector3 directionToMonster =
            GetMonsterAimPoint() -
            reflectHitPoint;

        if (
            directionToMonster.sqrMagnitude <=
            0.001f
        )
        {
            DestroyVisualProjectile(
                visualProjectile
            );

            yield break;
        }

        /*
         * 공격자를 향하도록 가상 법선 계산
         */
        Vector3 reflectNormal =
            (
                directionToMonster.normalized -
                incomingDirection.normalized
            ).normalized;

        Vector3 reflectedDirection =
            Vector3.Reflect(
                incomingDirection.normalized,
                reflectNormal
            ).normalized;

        /*
         * 반사 방향이 공격자 반대쪽이면
         * 공격자 방향으로 보정
         */
        if (
            Vector3.Dot(
                reflectedDirection,
                directionToMonster.normalized
            ) < 0.5f
        )
        {
            reflectedDirection =
                directionToMonster.normalized;
        }

        Vector3 reflectedOrigin =
            reflectHitPoint +
            reflectedDirection *
            reflectedOriginOffset;

        bool hasReflectedHit =
            Physics.Raycast(
                reflectedOrigin,
                reflectedDirection,
                out RaycastHit reflectedHit,
                reflectedRayDistance,
                reflectedHitMask,
                QueryTriggerInteraction.Ignore
            );

        Vector3 reflectedEndPosition =
            hasReflectedHit &&
            reflectedHit.collider != null
                ? reflectedHit.point
                : GetMonsterAimPoint();

        if (drawDebugRay)
        {
            Debug.DrawRay(
                reflectedOrigin,
                reflectedDirection *
                reflectedRayDistance,
                Color.cyan,
                2f
            );
        }

        if (visualProjectile != null)
        {
            visualProjectile.transform.position =
                reflectHitPoint;

            if (
                reflectedDirection.sqrMagnitude >
                0.001f
            )
            {
                visualProjectile.transform.rotation =
                    Quaternion.LookRotation(
                        reflectedDirection
                    );
            }
        }

        yield return StartCoroutine(
            MoveVisualProjectile(
                visualProjectile,
                reflectHitPoint,
                reflectedEndPosition,
                reflectedVisualTime
            )
        );

        if (
            hasReflectedHit &&
            reflectedHit.collider != null
        )
        {
            ApplyReflectedDamage(
                reflectedHit
            );
        }
        else
        {
            int reflectedDamage =
                CalculateReflectedDamage();

            TakeDamage(
                reflectedDamage
            );

            if (showDebugLog)
            {
                Debug.LogWarning(
                    $"{name}: 반사 Ray가 몬스터를 " +
                    $"찾지 못해 공격자 자신에게 " +
                    $"{reflectedDamage} 데미지"
                );
            }
        }

        DestroyVisualProjectile(
            visualProjectile
        );
    }

    private void ApplyReflectedDamage(
        RaycastHit reflectedHit
    )
    {
        if (reflectedHit.collider == null)
        {
            return;
        }

        int reflectedDamage =
            CalculateReflectedDamage();

        RangedMonsterAI rangedMonster =
            reflectedHit.collider
                .GetComponentInParent<
                    RangedMonsterAI
                >();

        if (rangedMonster != null)
        {
            rangedMonster.TakeDamage(
                reflectedDamage
            );

            if (showDebugLog)
            {
                Debug.Log(
                    $"반사 공격 적중: " +
                    $"{rangedMonster.name}, " +
                    $"{reflectedDamage} 데미지"
                );
            }

            return;
        }

        MonsterAI meleeMonster =
            reflectedHit.collider
                .GetComponentInParent<
                    MonsterAI
                >();

        if (meleeMonster != null)
        {
            meleeMonster.TakeDamage(
                reflectedDamage
            );

            if (showDebugLog)
            {
                Debug.Log(
                    $"반사 공격 적중: " +
                    $"{meleeMonster.name}, " +
                    $"{reflectedDamage} 데미지"
                );
            }

            return;
        }

        MonsterStats monsterStats =
            reflectedHit.collider
                .GetComponentInParent<
                    MonsterStats
                >();

        if (monsterStats != null)
        {
            monsterStats.TakeDamage(
                reflectedDamage
            );
        }
    }

    private int CalculateReflectedDamage()
    {
        return Mathf.Max(
            1,
            Mathf.RoundToInt(
                monsterattack *
                reflectedDamageMultiplier
            )
        );
    }

    private GameObject CreateVisualProjectile(
        Vector3 position,
        Vector3 direction
    )
    {
        if (projectileVisualPrefab == null)
        {
            return null;
        }

        Quaternion rotation =
            direction.sqrMagnitude >
            0.001f
                ? Quaternion.LookRotation(
                    direction
                )
                : Quaternion.identity;

        return Instantiate(
            projectileVisualPrefab,
            position,
            rotation
        );
    }

    private IEnumerator MoveVisualProjectile(
        GameObject projectile,
        Vector3 startPosition,
        Vector3 endPosition,
        float travelTime
    )
    {
        float duration =
            Mathf.Max(
                0.01f,
                travelTime
            );

        if (projectile == null)
        {
            yield return new WaitForSeconds(
                duration
            );

            yield break;
        }

        Vector3 direction =
            endPosition -
            startPosition;

        if (
            direction.sqrMagnitude >
            0.001f
        )
        {
            projectile.transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized
                );
        }

        projectile.transform.position =
            startPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (projectile == null)
            {
                yield break;
            }

            float t =
                elapsed / duration;

            projectile.transform.position =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    t
                );

            elapsed += Time.deltaTime;

            yield return null;
        }

        if (projectile != null)
        {
            projectile.transform.position =
                endPosition;
        }
    }

    private void DestroyVisualProjectile(
        GameObject projectile
    )
    {
        if (projectile != null)
        {
            Destroy(projectile);
        }
    }

    private Vector3 GetPlayerAimPoint()
    {
        if (player == null)
        {
            return transform.position;
        }

        Collider playerCollider =
            player.GetComponentInChildren<
                Collider
            >();

        if (playerCollider != null)
        {
            return playerCollider
                .bounds.center;
        }

        return player.position +
            Vector3.up;
    }

    private Vector3 GetMonsterAimPoint()
    {
        Collider monsterCollider =
            GetComponentInChildren<
                Collider
            >();

        if (monsterCollider != null)
        {
            return monsterCollider
                .bounds.center;
        }

        return transform.position +
            Vector3.up;
    }

    private IEnumerator ShowShotLine(
        Vector3 startPosition,
        Vector3 endPosition
    )
    {
        if (shotLine == null)
        {
            yield break;
        }

        shotLine.enabled = true;

        shotLine.SetPosition(
            0,
            startPosition
        );

        shotLine.SetPosition(
            1,
            endPosition
        );

        yield return new WaitForSeconds(
            shotLineTime
        );

        shotLine.enabled = false;
    }

    private void LookAtPlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;

        if (
            direction.sqrMagnitude >
            0.001f
        )
        {
            transform.rotation =
                Quaternion.LookRotation(
                    direction
                );
        }
    }

    private void StopAgent()
    {
        if (agent == null)
        {
            return;
        }

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    private void StopAttackVisual()
    {
        if (aimLine != null)
        {
            aimLine.enabled = false;
        }

        if (shotLine != null)
        {
            shotLine.enabled = false;
        }
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

        int safeDamage =
            Mathf.Max(
                0,
                damage
            );

        bool dead =
            base.TakeDamage(
                safeDamage
            );

        Debug.Log(
            $"{MonsterName} 피격: " +
            $"-{safeDamage}, " +
            $"HP {currentHp}/{monsterhp}"
        );

        if (dead)
        {
            DropReward();
            Die();
        }

        return dead;
    }

    private void DropReward()
    {
        Debug.Log(
            $"[Monster:{MonsterName}] " +
            $"골드 {rewardGold}개, " +
            $"꽃잎 {rewardflowerleaf}개 드랍"
        );
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        ChangeState(
            State.Dead
        );

        StopAgent();
        StopAttackVisual();

        Collider[] colliders =
            GetComponentsInChildren<
                Collider
            >();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        OnMonsterDead?.Invoke(
            this
        );

        Destroy(
            gameObject,
            0.5f
        );
    }

    private void OnDisable()
    {
        StopAttackVisual();
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;

        Gizmos.DrawWireSphere(
            transform.position,
            detectRange
        );

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            stoppingDistance
        );
    }
}
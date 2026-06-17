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
    [Tooltip("Player, Reflect, Obstacle 포함")]
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
            player = playerObject.transform;
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
            targetPosition - rayStart;

        if (
            incomingDirection.sqrMagnitude <=
            0.001f
        )
        {
            isAttacking = false;
            yield break;
        }

        incomingDirection.Normalize();

        /*
         * 이 Raycast는 시각 오브젝트의
         * 이동 종료 위치만 계산합니다.
         */
        Vector3 visualEndPosition =
            rayStart +
            incomingDirection *
            attackRange;

        bool previewHasHit =
            Physics.Raycast(
                rayStart,
                incomingDirection,
                out RaycastHit previewHit,
                attackRange,
                attackHitMask,
                QueryTriggerInteraction.Collide
            );

        if (
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

        /*
         * 판정 없는 시각 오브젝트를
         * 플레이어 쪽으로 이동합니다.
         */
        yield return StartCoroutine(
            MoveVisualProjectile(
                visualProjectile,
                rayStart,
                visualEndPosition,
                forwardVisualTime
            )
        );

        /*
         * 시각 오브젝트가 도착한 순간
         * 실제 공격 Raycast를 실행합니다.
         */
        bool actualHasHit =
            Physics.Raycast(
                rayStart,
                incomingDirection,
                out RaycastHit actualHit,
                attackRange,
                attackHitMask,
                QueryTriggerInteraction.Collide
            );

        if (drawDebugRay)
        {
            Debug.DrawRay(
                rayStart,
                incomingDirection *
                attackRange,
                actualHasHit
                    ? Color.red
                    : Color.yellow,
                1f
            );
        }

        if (
            !actualHasHit ||
            actualHit.collider == null
        )
        {
            if (showDebugLog)
            {
                Debug.Log(
                    $"{name}: 실제 공격 Ray가 " +
                    "아무것도 맞히지 못함"
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
                incomingDirection,
                visualProjectile
            )
        );

        isAttacking = false;
    }

    private IEnumerator ProcessAttackHit(
        RaycastHit attackHit,
        Vector3 incomingDirection,
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

        PlayerAttackReflect reflect =
            attackHit.collider
                .GetComponentInParent<
                    PlayerAttackReflect
                >();

        bool reflected = false;

        if (reflect != null)
        {
            reflected =
                reflect.IsReflectCollider(
                    attackHit.collider
                );
        }

        if (reflected)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    $"{name}: 플레이어 반사 성공"
                );
            }

            /*
             * 플레이어 피해는 주지 않고
             * 같은 시각 오브젝트를 몬스터 쪽으로 보냅니다.
             */
            yield return StartCoroutine(
                ReflectAttack(
                    attackHit,
                    incomingDirection,
                    visualProjectile
                )
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
    }

    private IEnumerator ReflectAttack(
        RaycastHit attackHit,
        Vector3 incomingDirection,
        GameObject visualProjectile
    )
    {
        /*
         * Ray 반사 공식:
         *
         * R = I - 2(N · I)N
         *
         * Vector3.Reflect가 위 공식을 계산합니다.
         */

        Vector3 directionToMonster =
            GetMonsterAimPoint() -
            attackHit.point;

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
         * 공격한 몬스터 방향을 기준으로
         * 가상의 반사 표면 법선을 계산합니다.
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
         * 계산 결과가 몬스터 반대쪽이면
         * 안전하게 몬스터 방향을 사용합니다.
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
            attackHit.point +
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

        /*
         * 같은 시각 오브젝트를
         * 플레이어 위치에서 몬스터 방향으로 이동합니다.
         */
        if (visualProjectile != null)
        {
            visualProjectile.transform.position =
                attackHit.point;

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
                attackHit.point,
                reflectedEndPosition,
                reflectedVisualTime
            )
        );

        /*
         * 반사 시각 오브젝트가 도착한 뒤
         * 실제 몬스터 피해를 처리합니다.
         */
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
            /*
             * LayerMask나 Collider 문제로
             * 공격자 자신을 찾지 못하면
             * 공격자에게 직접 피해를 줍니다.
             */
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
            direction.sqrMagnitude > 0.001f
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
            endPosition - startPosition;

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
            return playerCollider.bounds.center;
        }

        return player.position + Vector3.up;
    }

    private Vector3 GetMonsterAimPoint()
    {
        Collider monsterCollider =
            GetComponentInChildren<
                Collider
            >();

        if (monsterCollider != null)
        {
            return monsterCollider.bounds.center;
        }

        return transform.position + Vector3.up;
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
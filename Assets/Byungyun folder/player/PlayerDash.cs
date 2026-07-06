using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Player))]
public class PlayerDash : MonoBehaviour
{
    [Header("대시 입력")]
    [SerializeField]
    private KeyCode dashKey = KeyCode.Space;

    [Header("입력 버퍼링")]
    [Tooltip("대시 종료 직전 입력해도 예약되는 허용 시간")]
    [Min(0f)]
    [SerializeField]
    private float dashBufferWindow = 0.15f;

    [Header("테스트용 연속 대시 설정")]
    [Tooltip("체크 시 PlayerStats.MaxDashCount 대신 아래 testMaxDashCount 값을 사용합니다. 득도/특성 확정 전 밸런스 테스트용.")]
    [SerializeField]
    private bool overrideDashCountForTesting;

    [Tooltip("연속으로 사용 가능한 대시 횟수(테스트용). overrideDashCountForTesting이 켜져 있을 때만 적용됩니다.")]
    [Min(1)]
    [SerializeField]
    private int testMaxDashCount = 1;

    [Header("낮은 장애물 올라가기")]
    [Tooltip("이 높이 이하의 장애물은 대시로 올라갑니다.")]
    [Min(0f)]
    [SerializeField]
    private float maxStepHeight = 0.6f;

    [Tooltip("장애물 윗면을 찾기 위한 앞쪽 검사 거리")]
    [Min(0f)]
    [SerializeField]
    private float stepForwardProbe = 0.15f;

    [Tooltip("장애물 윗면 위에 살짝 띄워 배치하는 값")]
    [Min(0f)]
    [SerializeField]
    private float stepUpPadding = 0.03f;

    [Header("충돌 검사")]
    [Tooltip("높은 벽 앞에서 남겨둘 간격")]
    [Min(0f)]
    [SerializeField]
    private float collisionOffset = 0.05f;

    [Tooltip("대시가 막히는 레이어")]
    [SerializeField]
    private LayerMask obstacleLayers = ~0;

    [Header("애니메이션")]
    [SerializeField]
    private Animator animator;

    private Rigidbody rb;
    private Collider playerCollider;

    private PlayerController playerController;
    private Player player;
    private PlayerStats stats;

    private bool isDashing;

    private float dashTimer;
    private float cooldownTimer;

    private int currentDashCount;
    private int cachedMaxDashCount;

    private Vector3 dashDirection;

    private bool hasBufferedDash;
    private float dashBufferTimer;

    /*
     * 낮은 장애물 위에 올라간 위치에
     * 플레이어가 들어갈 공간이 있는지 검사할 때 사용.
     */
    private readonly Collider[] clearanceResults =
        new Collider[16];

    private static readonly int IsDashingHash =
        Animator.StringToHash("isDashing");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        playerCollider =
            GetComponent<Collider>();

        playerController =
            GetComponent<PlayerController>();

        player =
            GetComponent<Player>();

        if (player != null)
        {
            stats = player.stats;
        }

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (stats == null)
        {
            Debug.LogError(
                "[PlayerDash] PlayerStats를 찾지 못했습니다.",
                this
            );

            return;
        }

        RefreshDashCount(true);
    }

    private void Update()
    {
        if (stats == null)
        {
            return;
        }

        /*
         * 득도로 추가 대시 횟수가 바뀌었을 수 있으므로
         * 현재 최대 대시 횟수를 갱신.
         */
        RefreshDashCount(false);

        HandleDashInput();
        UpdateDashTimer();
        UpdateDashBuffer();
        UpdateCooldown();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (!isDashing)
        {
            return;
        }

        DashMove();
    }

    private Vector3 ComputeDashDirection()
    {
        Vector3 moveDirection =
            playerController.GetMoveDirection();

        /*
         * 이동 중이면 이동 방향으로 대시.
         * 정지 중이면 현재 바라보는 방향으로 대시.
         */
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            return moveDirection.normalized;
        }

        Vector3 facingDirection =
            playerController.GetFacingDirection();

        facingDirection.y = 0f;

        if (facingDirection.sqrMagnitude < 0.01f)
        {
            facingDirection =
                transform.forward;
        }

        facingDirection.Normalize();

        return facingDirection;
    }

    private void HandleDashInput()
    {
        if (!Input.GetKeyDown(dashKey))
        {
            return;
        }

        /*
         * 대시 중에 들어온 입력은 버리지 않고
         * 대시 종료 시점에 즉시 이어지도록 예약.
         */
        if (isDashing)
        {
            if (currentDashCount > 0)
            {
                hasBufferedDash = true;
                dashBufferTimer = dashBufferWindow;
            }

            return;
        }

        if (currentDashCount <= 0)
        {
            return;
        }

        dashDirection =
            ComputeDashDirection();

        StartDash();
    }

    private void UpdateDashBuffer()
    {
        if (!hasBufferedDash)
        {
            return;
        }

        dashBufferTimer -=
            Time.deltaTime;

        if (dashBufferTimer <= 0f)
        {
            hasBufferedDash = false;
        }
    }

    private void TryConsumeBufferedDash()
    {
        if (!hasBufferedDash)
        {
            return;
        }

        hasBufferedDash = false;

        if (currentDashCount <= 0)
        {
            return;
        }

        dashDirection =
            ComputeDashDirection();

        StartDash();
    }

    private void StartDash()
    {
        if (stats == null)
        {
            return;
        }

        isDashing = true;

        /*
         * 최신 PlayerStats의 최종 대시 지속시간.
         *
         * 기본 스탯
         * + 특성 추가 스탯
         * + 득도 추가 스탯
         * + 임시 효과
         */
        dashTimer =
            Mathf.Max(
                0.01f,
                stats.DashDuration
            );

        currentDashCount--;

        /*
         * 재충전 타이머는 여기서 시작하지 않음.
         * 연속 대시 체인이 완전히 끊기는 시점(EndDash)에만
         * 시작되도록 StartRechargeIfNeeded로 책임을 이전.
         */

        playerController.FaceDirection(
            dashDirection
        );

        /*
         * 이전 낙하·상승 속도를 제거해
         * 장애물과 충돌할 때 위로 튀는 현상 방지.
         */
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (stats.DashInvincible &&
            player != null)
        {
            player.SetInvincible(true);
        }
    }

    private void DashMove()
    {
        if (stats == null)
        {
            EndDash();
            return;
        }

        /*
         * 대시 속도
         * = 대시 거리 ÷ 대시 지속시간
         */
        float dashSpeed =
            stats.DashDistance /
            Mathf.Max(
                0.01f,
                stats.DashDuration
            );

        float moveDistance =
            dashSpeed *
            Time.fixedDeltaTime;

        /*
         * 대시 중에는 기존 Rigidbody 속도를 사용하지 않고
         * MovePosition으로 위치를 직접 계산.
         */
        rb.velocity = Vector3.zero;

        bool hitSomething =
            rb.SweepTest(
                dashDirection,
                out RaycastHit hit,
                moveDistance +
                collisionOffset,
                QueryTriggerInteraction.Ignore
            );

        if (hitSomething)
        {
            /*
             * 낮은 장애물이라면 위에 올라가면서
             * 대시를 계속 진행.
             */
            if (TryStepOntoObstacle(
                hit,
                moveDistance
            ))
            {
                return;
            }

            /*
             * 높은 벽이라면 충돌 직전까지 이동한 뒤
             * 대시 종료.
             */
            float allowedDistance =
                Mathf.Max(
                    0f,
                    hit.distance -
                    collisionOffset
                );

            if (allowedDistance > 0f)
            {
                Vector3 stopPosition =
                    rb.position +
                    dashDirection *
                    allowedDistance;

                rb.MovePosition(
                    stopPosition
                );
            }

            EndDash();
            return;
        }

        Vector3 nextPosition =
            rb.position +
            dashDirection *
            moveDistance;

        rb.MovePosition(
            nextPosition
        );
    }

    private bool TryStepOntoObstacle(
        RaycastHit forwardHit,
        float moveDistance
    )
    {
        if (maxStepHeight <= 0f)
        {
            return false;
        }

        if (forwardHit.collider == null)
        {
            return false;
        }

        /*
         * 플레이어 Collider의 가장 아래쪽 Y값.
         */
        float playerBottomY =
            playerCollider.bounds.min.y;

        /*
         * 충돌 지점보다 조금 앞쪽에서
         * 위에서 아래로 Raycast를 발사해
         * 장애물 윗면을 찾는다.
         */
        Vector3 probeOrigin =
            forwardHit.point +
            dashDirection *
            stepForwardProbe;

        probeOrigin.y =
            playerBottomY +
            maxStepHeight +
            0.5f;

        float probeDistance =
            maxStepHeight +
            1f;

        bool foundTop =
            Physics.Raycast(
                probeOrigin,
                Vector3.down,
                out RaycastHit topHit,
                probeDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore
            );

        if (!foundTop)
        {
            return false;
        }

        /*
         * 전방에서 부딪힌 장애물과
         * 위에서 찾은 장애물이 같은 오브젝트인지 확인.
         */
        if (!IsSameObstacle(
            forwardHit.collider,
            topHit.collider
        ))
        {
            return false;
        }

        float stepHeight =
            topHit.point.y -
            playerBottomY;

        /*
         * 장애물이 플레이어 발보다 아래에 있거나
         * 허용 높이보다 높으면 올라가지 않음.
         */
        if (stepHeight < -0.01f ||
            stepHeight > maxStepHeight)
        {
            return false;
        }

        float centerToBottom =
            rb.position.y -
            playerBottomY;

        Vector3 steppedPosition =
            rb.position +
            dashDirection *
            moveDistance;

        steppedPosition.y =
            topHit.point.y +
            centerToBottom +
            stepUpPadding;

        /*
         * 장애물 위 위치에 다른 벽이나 천장이 있으면
         * 플레이어를 올리지 않음.
         */
        if (!HasClearanceAt(
            steppedPosition,
            topHit.collider
        ))
        {
            return false;
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.MovePosition(
            steppedPosition
        );

        return true;
    }

    private bool HasClearanceAt(
        Vector3 targetPosition,
        Collider steppedCollider
    )
    {
        Bounds bounds =
            playerCollider.bounds;

        /*
         * CapsuleCollider와 비슷한 형태로
         * 목표 위치에 플레이어 공간이 있는지 검사.
         */
        float radius =
            Mathf.Min(
                bounds.extents.x,
                bounds.extents.z
            );

        radius =
            Mathf.Max(
                0.05f,
                radius - 0.03f
            );

        float halfHeight =
            Mathf.Max(
                radius,
                bounds.extents.y
            );

        Vector3 bottom =
            targetPosition +
            Vector3.up *
            (radius - halfHeight);

        Vector3 top =
            targetPosition +
            Vector3.up *
            (halfHeight - radius);

        int hitCount =
            Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                radius,
                clearanceResults,
                obstacleLayers,
                QueryTriggerInteraction.Ignore
            );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider =
                clearanceResults[i];

            if (hitCollider == null)
            {
                continue;
            }

            /*
             * 플레이어 자신의 Collider 무시.
             */
            if (hitCollider == playerCollider ||
                hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            /*
             * 현재 올라가려는 장애물 자체는 허용.
             */
            if (IsSameObstacle(
                steppedCollider,
                hitCollider
            ))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private bool IsSameObstacle(
        Collider first,
        Collider second
    )
    {
        if (first == null ||
            second == null)
        {
            return false;
        }

        if (first == second)
        {
            return true;
        }

        Transform firstRoot =
            first.attachedRigidbody != null
                ? first.attachedRigidbody.transform
                : first.transform.root;

        Transform secondRoot =
            second.attachedRigidbody != null
                ? second.attachedRigidbody.transform
                : second.transform.root;

        return firstRoot ==
               secondRoot;
    }

    private void RefreshDashCount(
        bool fillDash
    )
    {
        if (stats == null)
        {
            cachedMaxDashCount =
                overrideDashCountForTesting
                    ? Mathf.Max(1, testMaxDashCount)
                    : 1;

            if (fillDash)
            {
                currentDashCount = cachedMaxDashCount;
            }

            return;
        }

        /*
         * 최신 PlayerStats에서는
         * 기본 1회 + 추가 대시 횟수를 계산한 결과를
         * MaxDashCount 속성으로 반환.
         */
        int newMaxDashCount =
            overrideDashCountForTesting
                ? Mathf.Max(1, testMaxDashCount)
                : stats.MaxDashCount;

        if (fillDash)
        {
            cachedMaxDashCount =
                newMaxDashCount;

            currentDashCount =
                cachedMaxDashCount;

            return;
        }

        /*
         * 득도로 최대 대시 횟수가 증가하면
         * 증가한 만큼 현재 대시도 추가.
         */
        if (newMaxDashCount >
            cachedMaxDashCount)
        {
            int difference =
                newMaxDashCount -
                cachedMaxDashCount;

            currentDashCount +=
                difference;
        }

        cachedMaxDashCount =
            newMaxDashCount;

        currentDashCount =
            Mathf.Clamp(
                currentDashCount,
                0,
                cachedMaxDashCount
            );
    }

    private void UpdateDashTimer()
    {
        if (!isDashing)
        {
            return;
        }

        dashTimer -=
            Time.deltaTime;

        if (dashTimer <= 0f)
        {
            EndDash();
        }
    }

    private void UpdateCooldown()
    {
        if (stats == null)
        {
            return;
        }

        if (currentDashCount >=
            cachedMaxDashCount)
        {
            cooldownTimer = 0f;
            return;
        }

        /*
         * 연속 대시 체인이 진행 중인 동안에는
         * 재충전이 진행되지 않도록 일시 정지.
         * 체인이 완전히 끊기는 순간(EndDash)에
         * StartRechargeIfNeeded가 타이머를 새로 시작함.
         */
        if (isDashing || hasBufferedDash)
        {
            return;
        }

        cooldownTimer -=
            Time.deltaTime;

        if (cooldownTimer > 0f)
        {
            return;
        }

        /*
         * 쿨타임이 끝날 때마다
         * 대시 한 칸 충전.
         */
        currentDashCount++;

        currentDashCount =
            Mathf.Min(
                currentDashCount,
                cachedMaxDashCount
            );

        /*
         * 아직 덜 충전된 대시가 있다면
         * 다음 대시 충전 타이머 시작.
         */
        if (currentDashCount <
            cachedMaxDashCount)
        {
            cooldownTimer =
                stats.GetEffectiveDashCooldown();
        }
        else
        {
            cooldownTimer = 0f;
        }
    }

    private void EndDash()
    {
        if (!isDashing)
        {
            return;
        }

        isDashing = false;
        dashTimer = 0f;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        /*
         * 대시 시작 시 실제로 무적이 아니었더라도
         * false로 해제하는 것은 문제없음.
         */
        if (player != null)
        {
            player.SetInvincible(false);
        }

        TryConsumeBufferedDash();

        /*
         * TryConsumeBufferedDash가 다음 대시로 체인을
         * 이어가지 못한 경우에만 진짜로 체인이 끊긴 것.
         * 이 시점에만 재충전 타이머를 시작.
         */
        if (!isDashing)
        {
            StartRechargeIfNeeded();
        }
    }

    private void StartRechargeIfNeeded()
    {
        if (stats == null)
        {
            return;
        }

        if (currentDashCount < cachedMaxDashCount &&
            cooldownTimer <= 0f)
        {
            cooldownTimer =
                stats.GetEffectiveDashCooldown();
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(
            IsDashingHash,
            isDashing
        );
    }

    private void OnDisable()
    {
        if (isDashing)
        {
            EndDash();
        }

        hasBufferedDash = false;

        if (animator != null)
        {
            animator.SetBool(
                IsDashingHash,
                false
            );
        }
    }

    public bool IsDashing()
    {
        return isDashing;
    }

    public bool CanDash()
    {
        return !isDashing &&
               currentDashCount > 0;
    }

    public int GetCurrentDashCount()
    {
        return currentDashCount;
    }

    public int GetMaxDashCount()
    {
        return cachedMaxDashCount;
    }

    public float GetCooldownRatio()
    {
        if (stats == null)
        {
            return 0f;
        }

        float cooldown =
            stats.GetEffectiveDashCooldown();

        if (cooldown <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(
            cooldownTimer /
            cooldown
        );
    }

    public float GetRemainingCooldown()
    {
        return Mathf.Max(
            0f,
            cooldownTimer
        );
    }
}
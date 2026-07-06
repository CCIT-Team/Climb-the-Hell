using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 5f;

    [Header("카메라")]
    [SerializeField] private Transform cameraTransform;

    [Header("모델 회전 보정")]
    [Tooltip("모델이 반대로 보이면 180, 옆으로 보이면 90 또는 -90")]
    [SerializeField] private float modelRotationOffset = 0f;

    private Rigidbody rb;
    private Animator animator;
    private PlayerDash playerDash;

    private Vector3 moveDirection;
    private Vector3 facingDirection = Vector3.forward;

    private float horizontalInput;
    private float verticalInput;

    private bool isAttacking;

    private static readonly int IsMovingHash =
        Animator.StringToHash("isMoving");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        playerDash = GetComponent<PlayerDash>();

        SetupRigidbody();
        FindCamera();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    private void Update()
    {
        GatherInput();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        // 대시 중에는 PlayerDash가 이동과 회전을 담당
        if (playerDash != null &&
            playerDash.IsDashing())
        {
            return;
        }

        Move();
        RotatePlayerByMovement();
    }

    private void SetupRigidbody()
    {
        /*
         * 이 캐릭터의 회전(X, Y, Z 전부)은 물리 솔버의 토크가 아니라
         * PlayerController 스크립트(MoveRotation/FaceDirection)가
         * 전적으로 결정한다. 따라서 세 축을 모두 잠가
         * 충돌 임펄스가 각속도로 흡수되는 경로 자체를 차단한다.
         */
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |   // ← 추가
            RigidbodyConstraints.FreezeRotationZ;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;
    }

    private void FindCamera()
    {
        if (cameraTransform != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }
        else
        {
            Debug.LogError(
                "[PlayerController] MainCamera를 찾지 못했습니다.",
                this
            );
        }
    }

    private void GatherInput()
    {
        horizontalInput =
            Input.GetAxisRaw("Horizontal");

        verticalInput =
            Input.GetAxisRaw("Vertical");

        if (cameraTransform == null)
        {
            moveDirection = Vector3.zero;
            return;
        }

        Vector3 cameraForward =
            cameraTransform.forward;

        Vector3 cameraRight =
            cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        if (cameraForward.sqrMagnitude > 0.001f)
        {
            cameraForward.Normalize();
        }

        if (cameraRight.sqrMagnitude > 0.001f)
        {
            cameraRight.Normalize();
        }

        /*
         * 카메라 기준 이동 방향 계산.
         *
         * W: 카메라 앞쪽
         * S: 카메라 뒤쪽
         * A: 카메라 왼쪽
         * D: 카메라 오른쪽
         */
        moveDirection =
            cameraForward * verticalInput +
            cameraRight * horizontalInput;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }
    }

    private void Move()
    {
        Vector3 targetVelocity =
            moveDirection * moveSpeed;

        rb.velocity =
            new Vector3(
                targetVelocity.x,
                rb.velocity.y,
                targetVelocity.z
            );
    }

    private void RotatePlayerByMovement()
    {
        // 공격 중에는 마우스로 지정한 공격 방향을 유지한다.
        if (isAttacking)
        {
            return;
        }

        if (moveDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        facingDirection =
            moveDirection.normalized;

        Quaternion lookRotation =
            Quaternion.LookRotation(
                facingDirection,
                Vector3.up
            );

        Quaternion offsetRotation =
            Quaternion.Euler(
                0f,
                modelRotationOffset,
                0f
            );

        rb.MoveRotation(
            lookRotation * offsetRotation
        );
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        bool isMoving =
            moveDirection.sqrMagnitude > 0.01f;

        if (playerDash != null &&
            playerDash.IsDashing())
        {
            isMoving = false;
        }

        animator.SetBool(
            IsMovingHash,
            isMoving
        );
    }

    /// <summary>
    /// 공격 시 플레이어를 지정된 방향으로 회전시킨다.
    /// </summary>
    public void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        facingDirection =
            direction.normalized;

        Quaternion lookRotation =
            Quaternion.LookRotation(
                facingDirection,
                Vector3.up
            );

        Quaternion offsetRotation =
            Quaternion.Euler(
                0f,
                modelRotationOffset,
                0f
            );

        /*
         * 공격 입력은 Update에서 들어오기 때문에
         * 즉시 회전시켜 공격 방향과 판정 방향을 맞춘다.
         */
        rb.rotation =
            lookRotation * offsetRotation;
    }

    /// <summary>
    /// 공격 중 이동 방향으로 회전하는 것을 막는다.
    /// </summary>
    public void SetAttacking(bool attacking)
    {
        isAttacking = attacking;

        /*
         * 공격 종료 시 이동 중이면
         * 다시 이동 방향을 바라보게 한다.
         */
        if (!isAttacking &&
            moveDirection.sqrMagnitude > 0.001f)
        {
            facingDirection =
                moveDirection.normalized;
        }
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    public Vector3 GetMoveDirection()
    {
        return moveDirection;
    }

    public Vector3 GetFacingDirection()
    {
        if (facingDirection.sqrMagnitude < 0.001f)
        {
            Vector3 currentForward =
                transform.forward;

            currentForward.y = 0f;

            if (currentForward.sqrMagnitude < 0.001f)
            {
                return Vector3.forward;
            }

            return currentForward.normalized;
        }

        return facingDirection.normalized;
    }

    /*
     * 기존 PlayerDash와의 호환을 위해 유지한다.
     */
    public Transform GetVisualRoot()
    {
        return transform;
    }

    public float GetMoveSpeed()
    {
        return moveSpeed;
    }

    public void SetMoveSpeed(float value)
    {
        moveSpeed =
            Mathf.Max(0f, value);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 direction =
            Application.isPlaying
                ? GetFacingDirection()
                : transform.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        Gizmos.DrawRay(
            transform.position + Vector3.up,
            direction.normalized * 2f
        );
    }
}
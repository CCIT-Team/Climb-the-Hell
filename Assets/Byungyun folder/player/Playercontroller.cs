using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    [Min(0f)]
    [SerializeField]
    private float moveSpeed = 5f;

    [Header("카메라")]
    [SerializeField]
    private Transform cameraTransform;

    [SerializeField]
    private bool autoFindCamera = true;

    [Min(0.05f)]
    [SerializeField]
    private float cameraFindInterval = 0.25f;

    [Header("모델 회전 보정")]
    [Tooltip("모델이 반대로 보이면 180, 옆으로 보이면 90 또는 -90")]
    [SerializeField]
    private float modelRotationOffset = 0f;

    [Header("디버그")]
    [SerializeField]
    private bool showCameraLog = true;

    private Rigidbody rb;
    private Animator animator;
    private PlayerDash playerDash;

    private Vector3 moveDirection;
    private Vector3 facingDirection = Vector3.forward;

    private float horizontalInput;
    private float verticalInput;
    private float cameraFindTimer;

    private bool isAttacking;

    private static readonly int IsMovingHash =
        Animator.StringToHash("isMoving");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        playerDash = GetComponent<PlayerDash>();

        SetupRigidbody();
        TryFindCamera(true);

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    private void OnEnable()
    {
        /*
         * 씬 이동 후 PlayerController가 다시 켜질 때
         * 기존 카메라 참조가 사라졌을 수 있으므로 다시 찾는다.
         */
        TryFindCamera(false);
    }

    private void Update()
    {
        if (autoFindCamera &&
            cameraTransform == null)
        {
            cameraFindTimer -= Time.deltaTime;

            if (cameraFindTimer <= 0f)
            {
                cameraFindTimer = cameraFindInterval;
                TryFindCamera(false);
            }
        }

        GatherInput();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        // 정지 상태에서 팽이처럼 회전하는 현상 방지:
        // 충돌 등으로 발생한 각속도가 Angular Drag(기본 0.05)로 서서히 감쇠되는 동안
        // 육안으로 "빙글빙글 도는" 것처럼 보이므로, 회전을 스크립트가 100% 소유하도록
        // 매 프레임 물리 엔진의 각속도를 0으로 강제 초기화한다.
        rb.angularVelocity = Vector3.zero;

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
         * X/Z 회전은 충돌 때문에 넘어지는 걸 막는다.
         * Y 회전은 스크립트에서 직접 돌려야 하므로 잠그지 않는다.
         */
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;
    }

    private void TryFindCamera(bool logIfFailed)
    {
        if (cameraTransform != null)
        {
            return;
        }

        Camera mainCamera =
            Camera.main;

        if (mainCamera != null)
        {
            cameraTransform =
                mainCamera.transform;

            if (showCameraLog)
            {
                Debug.Log(
                    "[PlayerController] Camera.main을 이동 기준 카메라로 설정했습니다.",
                    this
                );
            }

            return;
        }

        QuarterViewCamera quarterViewCamera =
            FindObjectOfType<QuarterViewCamera>(true);

        if (quarterViewCamera != null)
        {
            Camera camera =
                quarterViewCamera.GetComponent<Camera>();

            if (camera != null)
            {
                cameraTransform =
                    camera.transform;

                if (showCameraLog)
                {
                    Debug.Log(
                        "[PlayerController] QuarterViewCamera를 이동 기준 카메라로 설정했습니다.",
                        this
                    );
                }

                return;
            }
        }

        Camera[] cameras =
            FindObjectsOfType<Camera>(true);

        if (cameras != null &&
            cameras.Length > 0 &&
            cameras[0] != null)
        {
            cameraTransform =
                cameras[0].transform;

            if (showCameraLog)
            {
                Debug.Log(
                    $"[PlayerController] 씬의 첫 번째 Camera를 이동 기준으로 설정했습니다. / Camera={cameras[0].name}",
                    this
                );
            }

            return;
        }

        if (logIfFailed && showCameraLog)
        {
            Debug.LogWarning(
                "[PlayerController] 카메라를 찾지 못했습니다. 카메라를 찾을 때까지 월드 기준으로 이동합니다.",
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

        Vector3 forward;
        Vector3 right;

        if (cameraTransform != null)
        {
            forward =
                cameraTransform.forward;

            right =
                cameraTransform.right;
        }
        else
        {
            /*
             * 카메라를 못 찾아도 이동 자체는 막지 않는다.
             * 이 fallback이 없으면 moveDirection이 zero가 되어
             * 플레이어가 아예 안 움직인다.
             */
            forward = Vector3.forward;
            right = Vector3.right;
        }

        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude > 0.001f)
        {
            forward.Normalize();
        }
        else
        {
            forward = Vector3.forward;
        }

        if (right.sqrMagnitude > 0.001f)
        {
            right.Normalize();
        }
        else
        {
            right = Vector3.right;
        }

        moveDirection =
            forward * verticalInput +
            right * horizontalInput;

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

        rb.rotation =
            lookRotation * offsetRotation;
    }

    /// <summary>
    /// 공격 중 이동 방향으로 회전하는 것을 막는다.
    /// </summary>
    public void SetAttacking(bool attacking)
    {
        isAttacking = attacking;

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

    public void SetCameraTransform(Transform newCameraTransform)
    {
        cameraTransform =
            newCameraTransform;
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
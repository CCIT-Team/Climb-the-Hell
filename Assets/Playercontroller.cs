using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 15f;

    [Header("대시 설정")]
    public KeyCode dashKey = KeyCode.Space;
    public float dashSpeed = 20f;       // 대시 순간 속도
    public float dashDuration = 0.15f;  // 대시 지속 시간 (초)
    public float dashCooldown = 1f;     // 쿨타임 (초)
    public bool dashInvincible = true;  // 무적 여부

    [Header("바닥 감지")]
    public float groundCheckDistance = 0.6f;
    public LayerMask groundLayer;

    [Header("카메라 참조")]
    public Transform cameraTransform;

    private Rigidbody _rb;
    private Vector3 _moveDir;
    private bool _isGrounded;

    private Animator _animator;

    // 대시 상태
    private bool _isDashing = false;
    private float _dashTimer = 0f;
    private float _cooldownTimer = 0f;
    private Vector3 _dashDir;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;

        _animator = GetComponentInChildren<Animator>();


        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
        
        if (_animator == null){
            Debug.LogError("Animator를 찾을 수 없어요! Body에 Animator 컴포넌트가 있는지 확인해줘요.");
        }
    }

    void Update()
    {
        GatherInput();
        CheckGround();
        HandleDashInput();
        UpdateTimers();
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (_isDashing)
            DashMove();
        else
            Move();

        Rotate();
    }

    void GatherInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight   = cameraTransform.right;
        camForward.y = 0f;
        camRight.y   = 0f;
        camForward.Normalize();
        camRight.Normalize();

        _moveDir = (camForward * v + camRight * h).normalized;
    }

    void HandleDashInput()
    {
        // 쿨타임 중이거나 이미 대시 중이면 무시
        if (_isDashing || _cooldownTimer > 0f) return;

        if (Input.GetKeyDown(dashKey))
        {
            // 이동 방향이 없으면 바라보는 방향으로 대시
            _dashDir = _moveDir.sqrMagnitude > 0.01f
                ? _moveDir
                : transform.forward;

            _isDashing = true;
            _dashTimer = dashDuration;
            _cooldownTimer = dashCooldown;

            // 무적 처리 (태그나 레이어로 관리 가능)
            if (dashInvincible)
                StartInvincible();
        }
    }

    void UpdateTimers()
    {
        if (_dashTimer > 0f)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f)
            {
                _isDashing = false;
                StopInvincible();
            }
        }

        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;
    }

    void DashMove()
    {
        _rb.velocity = new Vector3(
            _dashDir.x * dashSpeed,
            _rb.velocity.y,
            _dashDir.z * dashSpeed
        );
    }

    void Move()
    {
        if (_moveDir.sqrMagnitude < 0.01f)
        {
            _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
            return;
        }

        Vector3 targetVelocity = _moveDir * moveSpeed;
        _rb.velocity = new Vector3(targetVelocity.x, _rb.velocity.y, targetVelocity.z);
    }

    void Rotate()
    {
        if (_moveDir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(_moveDir, Vector3.up);
        Quaternion yOnlyRot  = Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f);
        transform.rotation   = Quaternion.Slerp(transform.rotation, yOnlyRot, Time.fixedDeltaTime * rotateSpeed);
    }

    void CheckGround()
    {
        _isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );
    }

    // 무적 처리 (필요에 따라 확장)
    void StartInvincible()
    {
        // 예: gameObject.layer = LayerMask.NameToLayer("Invincible");
        // 애니메이션, 이펙트 등 여기서 추가
        Debug.Log("대시 무적 시작");
    }

    void StopInvincible()
    {
        Debug.Log("대시 무적 종료");
    }

    // 쿨타임 UI 등에서 참조용
    public float GetCooldownRatio() => Mathf.Clamp01(_cooldownTimer / dashCooldown);
    public bool IsDashing() => _isDashing;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }

    void UpdateAnimation()
    {
        if (_animator == null) return;
        _animator.SetBool("isMoving", _moveDir.sqrMagnitude > 0.01f);
        _animator.SetBool("isDashing", _isDashing);
    }
}
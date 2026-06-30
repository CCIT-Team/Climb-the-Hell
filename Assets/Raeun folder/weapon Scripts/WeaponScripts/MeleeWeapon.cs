using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Weapon
{
    [Header("플레이어 연결")]
    [SerializeField] private Player player;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private PlayerController playerController;

    [Header("기본 공격")]
    [SerializeField] private Transform attackPoint;

    [Min(0f)]
    [SerializeField] private float attackRange = 2f;

    [Range(0f, 360f)]
    [SerializeField] private float attackAngle = 90f;

    [SerializeField] private LayerMask monsterLayer;

    [Header("공격 판정 버퍼")]
    [Tooltip("한 번의 공격으로 검사할 수 있는 최대 Collider 수")]
    [Min(8)]
    [SerializeField] private int initialHitBufferSize = 32;

    [Tooltip("판정 버퍼가 자동 확장될 수 있는 최대 크기")]
    [Min(32)]
    [SerializeField] private int maximumHitBufferSize = 256;

    [Header("기본 공격 시간")]
    [Min(0f)]
    [SerializeField] private float attackDelay = 0.1f;

    [Min(0f)]
    [SerializeField] private float attackRecovery = 0.3f;

    [Header("부채꼴 표시")]
    [Tooltip("공격 중이 아닐 때도 기본 공격 범위를 표시할지 여부")]
    [SerializeField] private bool showFanWhenIdle = true;

    [Min(3)]
    [SerializeField] private int fanSegments = 40;

    [SerializeField]
    private Color normalColor =
        new Color(1f, 1f, 1f, 0.3f);

    [SerializeField]
    private Color attackColor =
        new Color(1f, 0.2f, 0.2f, 0.7f);

    [Header("LineRenderer 재질")]
    [Tooltip("가능하면 공용 Material을 연결하세요.")]
    [SerializeField] private Material lineMaterial;

    [Header("특수 공격")]
    [Min(0f)]
    [SerializeField] private float specialRange = 3.5f;

    [Min(0f)]
    [SerializeField] private float specialDelay = 0.2f;

    [Min(0f)]
    [SerializeField] private float specialRecovery = 0.4f;

    [SerializeField]
    private Color specialColor = Color.yellow;

    [Header("특수 공격 피해 배율")]
    [Min(1)]
    [SerializeField] private int specialDamageMultiplier = 2;

    private LineRenderer fanRenderer;
    private LineRenderer specialRenderer;

    private Vector3[] fanPositions;
    private Collider[] hitBuffer;

    private readonly HashSet<MonsterAI> hitMonsters =
        new HashSet<MonsterAI>();

    private static readonly int IsAttackingHash =
        Animator.StringToHash("IsAttacking");

    private static Material sharedFallbackMaterial;

    private Coroutine attackCoroutine;

    private WaitForSeconds attackDelayWait;
    private WaitForSeconds attackRecoveryWait;
    private WaitForSeconds specialDelayWait;
    private WaitForSeconds specialRecoveryWait;

    private Vector3 previousFanOrigin;
    private Vector3 previousFanForward;

    private float previousAttackRange = -1f;
    private float previousAttackAngle = -1f;
    private int previousFanSegments = -1;

    private bool fanInitialized;

    private void Awake()
    {
        ValidateValues();
        FindPlayerComponents();
        SetupHitBuffer();
        SetupWaitInstructions();
        SetupRenderers();
    }

    private void Start()
    {
        if (trailEffect != null)
        {
            trailEffect.enabled = false;
        }

        if (attackPoint == null)
        {
            Debug.LogError(
                "[MeleeWeapon] AttackPoint가 연결되지 않았습니다.",
                this
            );
        }

        UpdateFanIfNeeded(true);
    }

    private void LateUpdate()
    {
        if (fanRenderer == null)
        {
            return;
        }

        bool shouldShowFan =
            showFanWhenIdle || isAttacking;

        fanRenderer.enabled =
            shouldShowFan;

        if (!shouldShowFan)
        {
            return;
        }

        UpdateFanIfNeeded(false);
    }

    private void ValidateValues()
    {
        attackRange =
            Mathf.Max(0f, attackRange);

        specialRange =
            Mathf.Max(0f, specialRange);

        attackDelay =
            Mathf.Max(0f, attackDelay);

        attackRecovery =
            Mathf.Max(0f, attackRecovery);

        specialDelay =
            Mathf.Max(0f, specialDelay);

        specialRecovery =
            Mathf.Max(0f, specialRecovery);

        fanSegments =
            Mathf.Max(3, fanSegments);

        initialHitBufferSize =
            Mathf.Max(8, initialHitBufferSize);

        maximumHitBufferSize =
            Mathf.Max(
                initialHitBufferSize,
                maximumHitBufferSize
            );

        specialDamageMultiplier =
            Mathf.Max(
                1,
                specialDamageMultiplier
            );
    }

    private void FindPlayerComponents()
    {
        if (player == null)
        {
            player =
                GetComponentInParent<Player>();
        }

        if (playerAnimator == null)
        {
            playerAnimator =
                GetComponentInParent<Animator>();
        }

        if (playerController == null)
        {
            playerController =
                GetComponentInParent<PlayerController>();
        }

        if (playerAnimator == null &&
            playerController != null)
        {
            playerAnimator =
                playerController
                    .GetComponentInChildren<Animator>();
        }

        if (player == null)
        {
            Debug.LogError(
                "[MeleeWeapon] 부모에서 Player 스크립트를 찾지 못했습니다.",
                this
            );
        }

        if (playerAnimator == null)
        {
            Debug.LogWarning(
                "[MeleeWeapon] Animator를 찾지 못했습니다.",
                this
            );
        }

        if (playerController == null)
        {
            Debug.LogError(
                "[MeleeWeapon] PlayerController를 찾지 못했습니다.",
                this
            );
        }
    }

    private int GetPlayerDamage()
    {
        if (player == null)
        {
            Debug.LogError(
                "[MeleeWeapon] Player가 없어 공격력을 가져올 수 없습니다.",
                this
            );

            return 0;
        }

        if (player.stats == null)
        {
            Debug.LogError(
                "[MeleeWeapon] PlayerStats가 없습니다.",
                this
            );

            return 0;
        }

        return player.stats.CalculateDamage();
    }

    private void SetupHitBuffer()
    {
        hitBuffer =
            new Collider[initialHitBufferSize];
    }

    private void SetupWaitInstructions()
    {
        attackDelayWait =
            attackDelay > 0f
                ? new WaitForSeconds(attackDelay)
                : null;

        attackRecoveryWait =
            attackRecovery > 0f
                ? new WaitForSeconds(attackRecovery)
                : null;

        specialDelayWait =
            specialDelay > 0f
                ? new WaitForSeconds(specialDelay)
                : null;

        specialRecoveryWait =
            specialRecovery > 0f
                ? new WaitForSeconds(specialRecovery)
                : null;
    }

    private void SetupRenderers()
    {
        SetupFanRenderer();
        SetupSpecialRenderer();
        ResizeFanPositionArray();
    }

    private void SetupFanRenderer()
    {
        fanRenderer =
            GetComponent<LineRenderer>();

        if (fanRenderer == null)
        {
            fanRenderer =
                gameObject.AddComponent<LineRenderer>();
        }

        fanRenderer.useWorldSpace = true;
        fanRenderer.loop = false;
        fanRenderer.widthMultiplier = 0.05f;

        ApplyLineMaterial(fanRenderer);

        SetLineColor(
            fanRenderer,
            normalColor
        );
    }

    private void SetupSpecialRenderer()
    {
        Transform existingRenderer =
            transform.Find("SpecialRenderer");

        GameObject rendererObject;

        if (existingRenderer != null)
        {
            rendererObject =
                existingRenderer.gameObject;
        }
        else
        {
            rendererObject =
                new GameObject("SpecialRenderer");

            rendererObject.transform.SetParent(
                transform,
                false
            );
        }

        specialRenderer =
            rendererObject.GetComponent<LineRenderer>();

        if (specialRenderer == null)
        {
            specialRenderer =
                rendererObject.AddComponent<LineRenderer>();
        }

        specialRenderer.useWorldSpace = true;
        specialRenderer.loop = true;
        specialRenderer.widthMultiplier = 0.05f;
        specialRenderer.positionCount = 0;
        specialRenderer.enabled = false;

        ApplyLineMaterial(specialRenderer);

        SetLineColor(
            specialRenderer,
            specialColor
        );
    }

    private void ApplyLineMaterial(
        LineRenderer targetRenderer
    )
    {
        if (targetRenderer == null)
        {
            return;
        }

        if (lineMaterial != null)
        {
            targetRenderer.sharedMaterial =
                lineMaterial;

            return;
        }

        if (sharedFallbackMaterial == null)
        {
            Shader shader =
                Shader.Find("Sprites/Default");

            if (shader != null)
            {
                sharedFallbackMaterial =
                    new Material(shader)
                    {
                        name =
                            "Shared Melee Line Material"
                    };
            }
        }

        if (sharedFallbackMaterial != null)
        {
            targetRenderer.sharedMaterial =
                sharedFallbackMaterial;
        }
    }

    private void ResizeFanPositionArray()
    {
        int requiredLength =
            fanSegments + 3;

        if (fanPositions == null ||
            fanPositions.Length != requiredLength)
        {
            fanPositions =
                new Vector3[requiredLength];
        }

        if (fanRenderer != null)
        {
            fanRenderer.positionCount =
                requiredLength;
        }

        fanInitialized = false;
    }

    private void UpdateFanIfNeeded(bool force)
    {
        if (attackPoint == null ||
            fanRenderer == null)
        {
            return;
        }

        int requiredLength =
            fanSegments + 3;

        if (fanPositions == null ||
            fanPositions.Length != requiredLength)
        {
            ResizeFanPositionArray();
            force = true;
        }

        Vector3 origin =
            attackPoint.position;

        Vector3 forward =
            GetAttackForward();

        bool positionChanged =
            (origin - previousFanOrigin)
            .sqrMagnitude > 0.000001f;

        bool directionChanged =
            Vector3.Dot(
                forward,
                previousFanForward
            ) < 0.99999f;

        bool settingsChanged =
            !Mathf.Approximately(
                previousAttackRange,
                attackRange
            ) ||
            !Mathf.Approximately(
                previousAttackAngle,
                attackAngle
            ) ||
            previousFanSegments != fanSegments;

        if (!force &&
            fanInitialized &&
            !positionChanged &&
            !directionChanged &&
            !settingsChanged)
        {
            return;
        }

        DrawFan(
            origin,
            forward
        );

        previousFanOrigin = origin;
        previousFanForward = forward;
        previousAttackRange = attackRange;
        previousAttackAngle = attackAngle;
        previousFanSegments = fanSegments;

        fanInitialized = true;
    }

    private void DrawFan(
        Vector3 origin,
        Vector3 forward
    )
    {
        float halfAngle =
            attackAngle * 0.5f;

        fanPositions[0] = origin;

        for (int i = 0; i <= fanSegments; i++)
        {
            float ratio =
                (float)i / fanSegments;

            float angle =
                Mathf.Lerp(
                    -halfAngle,
                    halfAngle,
                    ratio
                );

            Vector3 direction =
                Quaternion.AngleAxis(
                    angle,
                    Vector3.up
                ) * forward;

            fanPositions[i + 1] =
                origin +
                direction * attackRange;
        }

        fanPositions[fanPositions.Length - 1] =
            origin;

        fanRenderer.SetPositions(
            fanPositions
        );
    }

    private Vector3 GetAttackForward()
    {
        if (playerController != null)
        {
            Vector3 direction =
                playerController.GetFacingDirection();

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                return direction.normalized;
            }
        }

        Vector3 forward =
            transform.root.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            return Vector3.forward;
        }

        return forward.normalized;
    }

    public override void Use()
    {
        if (isAttacking)
        {
            return;
        }

        if (attackPoint == null)
        {
            Debug.LogError(
                "[MeleeWeapon] AttackPoint가 없습니다.",
                this
            );

            return;
        }

        if (player == null ||
            player.stats == null)
        {
            Debug.LogError(
                "[MeleeWeapon] PlayerStats가 없어 공격할 수 없습니다.",
                this
            );

            return;
        }

        attackCoroutine =
            StartCoroutine(Swing());
    }

    private IEnumerator Swing()
    {
        SetAttackState(true);

        if (fanRenderer != null)
        {
            fanRenderer.enabled = true;

            SetLineColor(
                fanRenderer,
                attackColor
            );
        }

        UpdateFanIfNeeded(true);

        if (attackDelayWait != null)
        {
            yield return attackDelayWait;
        }

        if (trailEffect != null)
        {
            trailEffect.enabled = true;
        }

        PerformAttack();

        if (attackRecoveryWait != null)
        {
            yield return attackRecoveryWait;
        }

        if (trailEffect != null)
        {
            trailEffect.enabled = false;
        }

        if (fanRenderer != null)
        {
            SetLineColor(
                fanRenderer,
                normalColor
            );

            fanRenderer.enabled =
                showFanWhenIdle;
        }

        SetAttackState(false);
        attackCoroutine = null;
    }

    private void PerformAttack()
    {
        int colliderCount =
            GetOverlapCount(attackRange);

        hitMonsters.Clear();

        Vector3 forward =
            GetAttackForward();

        int hitCount = 0;

        for (int i = 0; i < colliderCount; i++)
        {
            Collider hit =
                hitBuffer[i];

            if (hit == null)
            {
                continue;
            }

            MonsterAI monster =
                hit.GetComponentInParent<MonsterAI>();

            if (monster == null ||
                !monster.IsAlive() ||
                !hitMonsters.Add(monster))
            {
                continue;
            }

            Vector3 direction =
                monster.transform.position -
                attackPoint.position;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                continue;
            }

            direction.Normalize();

            float angle =
                Vector3.Angle(
                    forward,
                    direction
                );

            if (angle >
                attackAngle * 0.5f)
            {
                continue;
            }

            int damage =
                GetPlayerDamage();

            if (damage <= 0)
            {
                continue;
            }

            monster.TakeDamage(
                damage
            );

            hitCount++;

            Debug.Log(
                $"[MeleeWeapon] 기본 공격: " +
                $"{monster.name}에게 {damage} 피해",
                monster
            );
        }

        if (hitCount == 0)
        {
            Debug.Log(
                "[MeleeWeapon] 기본 공격 빗나감",
                this
            );
        }

        ClearUsedBuffer(colliderCount);
    }

    public void SpecialUse()
    {
        if (isAttacking)
        {
            return;
        }

        if (attackPoint == null)
        {
            Debug.LogError(
                "[MeleeWeapon] AttackPoint가 없습니다.",
                this
            );

            return;
        }

        if (player == null ||
            player.stats == null)
        {
            Debug.LogError(
                "[MeleeWeapon] PlayerStats가 없어 특수 공격할 수 없습니다.",
                this
            );

            return;
        }

        attackCoroutine =
            StartCoroutine(SpecialSwing());
    }

    private IEnumerator SpecialSwing()
    {
        SetAttackState(true);

        DrawSpecialCircle();

        if (specialDelayWait != null)
        {
            yield return specialDelayWait;
        }

        if (trailEffect != null)
        {
            trailEffect.enabled = true;
        }

        PerformSpecialAttack();

        if (specialRecoveryWait != null)
        {
            yield return specialRecoveryWait;
        }

        if (trailEffect != null)
        {
            trailEffect.enabled = false;
        }

        if (specialRenderer != null)
        {
            specialRenderer.enabled = false;
        }

        SetAttackState(false);
        attackCoroutine = null;
    }

    private void PerformSpecialAttack()
    {
        int colliderCount =
            GetOverlapCount(specialRange);

        hitMonsters.Clear();

        /*
         * 특수 공격 1회당 치명타를 한 번만 계산.
         * 범위 안의 모든 몬스터가 같은 피해를 받음.
         */
        int baseDamage =
            GetPlayerDamage();

        int damage =
            baseDamage *
            specialDamageMultiplier;

        int hitCount = 0;

        for (int i = 0; i < colliderCount; i++)
        {
            Collider hit =
                hitBuffer[i];

            if (hit == null)
            {
                continue;
            }

            MonsterAI monster =
                hit.GetComponentInParent<MonsterAI>();

            if (monster == null ||
                !monster.IsAlive() ||
                !hitMonsters.Add(monster))
            {
                continue;
            }

            if (damage <= 0)
            {
                continue;
            }

            monster.TakeDamage(
                damage
            );

            hitCount++;

            Debug.Log(
                $"[MeleeWeapon] 특수 공격: " +
                $"{monster.name}에게 {damage} 피해",
                monster
            );
        }

        if (hitCount == 0)
        {
            Debug.Log(
                "[MeleeWeapon] 특수 공격 빗나감",
                this
            );
        }

        ClearUsedBuffer(colliderCount);
    }

    private int GetOverlapCount(float range)
    {
        if (hitBuffer == null ||
            hitBuffer.Length == 0)
        {
            SetupHitBuffer();
        }

        int colliderCount =
            Physics.OverlapSphereNonAlloc(
                attackPoint.position,
                range,
                hitBuffer,
                monsterLayer,
                QueryTriggerInteraction.Collide
            );

        while (colliderCount >= hitBuffer.Length &&
               hitBuffer.Length < maximumHitBufferSize)
        {
            int newSize =
                Mathf.Min(
                    hitBuffer.Length * 2,
                    maximumHitBufferSize
                );

            hitBuffer =
                new Collider[newSize];

            colliderCount =
                Physics.OverlapSphereNonAlloc(
                    attackPoint.position,
                    range,
                    hitBuffer,
                    monsterLayer,
                    QueryTriggerInteraction.Collide
                );
        }

        if (colliderCount >= hitBuffer.Length &&
            hitBuffer.Length >= maximumHitBufferSize)
        {
            Debug.LogWarning(
                $"[MeleeWeapon] 공격 범위 내 Collider가 " +
                $"버퍼 최대 크기 {maximumHitBufferSize}개를 초과했습니다.",
                this
            );
        }

        return colliderCount;
    }

    private void ClearUsedBuffer(int usedCount)
    {
        int clearCount =
            Mathf.Min(
                usedCount,
                hitBuffer.Length
            );

        for (int i = 0; i < clearCount; i++)
        {
            hitBuffer[i] = null;
        }
    }

    private void DrawSpecialCircle()
    {
        if (specialRenderer == null ||
            attackPoint == null)
        {
            return;
        }

        const int segments = 50;

        specialRenderer.enabled = true;
        specialRenderer.positionCount =
            segments;

        Vector3 center =
            attackPoint.position;

        for (int i = 0; i < segments; i++)
        {
            float angle =
                i * Mathf.PI * 2f /
                segments;

            Vector3 offset =
                new Vector3(
                    Mathf.Sin(angle) *
                    specialRange,
                    0f,
                    Mathf.Cos(angle) *
                    specialRange
                );

            specialRenderer.SetPosition(
                i,
                center + offset
            );
        }
    }

    private void SetAttackState(bool attacking)
    {
        isAttacking =
            attacking;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool(
                IsAttackingHash,
                attacking
            );
        }

        if (playerController != null)
        {
            playerController.SetAttacking(
                attacking
            );
        }
    }

    private static void SetLineColor(
        LineRenderer targetRenderer,
        Color color
    )
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.startColor = color;
        targetRenderer.endColor = color;
    }

    private void OnDisable()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        SetAttackState(false);

        if (trailEffect != null)
        {
            trailEffect.enabled = false;
        }

        if (fanRenderer != null)
        {
            SetLineColor(
                fanRenderer,
                normalColor
            );

            fanRenderer.enabled =
                showFanWhenIdle;
        }

        if (specialRenderer != null)
        {
            specialRenderer.enabled = false;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ValidateValues();

        if (!Application.isPlaying)
        {
            return;
        }

        SetupWaitInstructions();
        ResizeFanPositionArray();
        UpdateFanIfNeeded(true);
    }
#endif

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            return;
        }

        Gizmos.color = Color.blue;

        Gizmos.DrawWireSphere(
            attackPoint.position,
            attackRange
        );

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            attackPoint.position,
            specialRange
        );

        Vector3 direction =
            Application.isPlaying
                ? GetAttackForward()
                : transform.root.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Gizmos.color = Color.red;

        Gizmos.DrawRay(
            attackPoint.position,
            direction.normalized *
            attackRange
        );
    }
}
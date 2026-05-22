using System.Collections;
using UnityEngine;

/// <summary>
/// 임시 전투 입력 — 무기 시스템 완성 전까지 사용
/// Player.cs의 Attack()을 마우스 클릭으로 호출 + 전방 박스 공격 범위 시각화
/// </summary>
[RequireComponent(typeof(Player))]
public class TempCombatInput : MonoBehaviour
{
    [Header("박스 공격 범위")]
    [Tooltip("박스 가로 (좌우)")]
    public float boxWidth  = 1.5f;
    [Tooltip("박스 세로 (높이)")]
    public float boxHeight = 1.0f;
    [Tooltip("박스 깊이 (전방 길이)")]
    public float boxDepth  = 1.5f;
    [Tooltip("플레이어 중심에서 전방으로 얼마나 띄울지")]
    public float boxOffset = 1.0f;

    [Tooltip("공격 범위 표시 지속 시간 (초)")]
    public float attackVisualDuration = 0.15f;

    [Header("공격 범위 시각화 색상")]
    public Color idleColor   = new Color(1f, 1f, 0f, 0.25f);  // 대기: 노랑 반투명
    public Color attackColor = new Color(1f, 0f, 0f, 0.5f);   // 공격: 빨강 반투명

    // ── 내부 참조 ──────────────────────────────────
    private Player       player;
    private GameObject   rangeVisual;
    private MeshRenderer rangeRenderer;
    private bool         isOnCooldown = false;

    // ── 쿨다운 ─────────────────────────────────────
    [Header("임시 공격 쿨다운 (초)")]
    public float attackCooldown = 0.5f;

    // ────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        player = GetComponent<Player>();
        CreateRangeVisual();
    }

    private void Update()
    {
        // 마우스 방향으로 박스 회전
        RotateBoxToMouse();

        // Inspector에서 박스 크기/위치 조절 시 실시간 반영
        if (rangeVisual != null)
            UpdateVisualTransform();

        // 마우스 좌클릭 → 공격
        if (Input.GetMouseButtonDown(0) && !isOnCooldown)
        {
            StartCoroutine(DoAttack());
        }
    }

    /// <summary>
    /// 쿼터뷰 기준 — 마우스 커서의 월드 좌표를 구해
    /// 플레이어 → 마우스 방향으로 transform을 회전
    /// (Y축 회전만 적용, 이동 로직과 충돌 주의)
    /// </summary>
    private void RotateBoxToMouse()
    {
        // 쿼터뷰는 카메라가 비스듬히 내려다보므로
        // 마우스 레이를 Y=playerPosition.y 수평면에 교차시켜 월드 좌표를 구함
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(distance);
            Vector3 dir = mouseWorldPos - transform.position;
            dir.y = 0f; // 수평 방향만 사용

            if (dir.sqrMagnitude > 0.01f) // 너무 가까우면 무시 (떨림 방지)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized);
            }
        }
    }

    #endregion

    // ────────────────────────────────────────────────
    #region 공격 실행

    private IEnumerator DoAttack()
    {
        isOnCooldown = true;

        // 1) 공격 범위 빨갛게 표시
        SetVisualColor(attackColor);

        // 2) 전방 박스 범위로 몬스터 탐지 후 데미지 적용
        AttackBoxArea();

        // 3) 짧게 유지 후 원래 색으로
        yield return new WaitForSeconds(attackVisualDuration);
        SetVisualColor(idleColor);

        // 4) 쿨다운 대기
        yield return new WaitForSeconds(attackCooldown - attackVisualDuration);
        isOnCooldown = false;
    }

    /// <summary>
    /// 플레이어 전방 박스 범위 안의 몬스터에게 데미지
    /// </summary>
    private void AttackBoxArea()
    {
        Vector3 boxCenter = GetBoxCenter();
        Vector3 halfExtents = new Vector3(boxWidth / 2f, boxHeight / 2f, boxDepth / 2f);

        Collider[] hits = Physics.OverlapBox(
            boxCenter,
            halfExtents,
            transform.rotation,
            LayerMask.GetMask("Monster")
        );

        foreach (var hit in hits)
        {
            Monster monster = hit.GetComponent<Monster>();
            if (monster != null && monster.IsAlive())
            {
                // Player.cs의 내부 DealDamage 대신 임시로 OnHit 직접 호출
                int damage = player.stats.attack;
                monster.OnHit(damage);
                Debug.Log($"[TempCombat] {monster.MonsterName} 공격 — {damage} 데미지");
            }
        }
    }

    /// <summary>
    /// 박스 중심 위치 = 플레이어 위치 + 전방으로 offset + 박스 깊이의 절반
    /// </summary>
    private Vector3 GetBoxCenter()
    {
        return transform.position + transform.forward * (boxOffset + boxDepth / 2f);
    }

    #endregion

    // ────────────────────────────────────────────────
    #region 범위 시각화 생성

    /// <summary>
    /// 플레이어 전방에 반투명 박스를 생성해 공격 범위를 표시
    /// </summary>
    private void CreateRangeVisual()
    {
        rangeVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rangeVisual.name = "[TempAttackRange]";

        // 충돌 판정 제거 (시각용이므로)
        Destroy(rangeVisual.GetComponent<Collider>());

        // 플레이어 자식으로 붙여서 같이 이동/회전
        rangeVisual.transform.SetParent(transform);
        UpdateVisualTransform();

        // 반투명 머티리얼 설정
        rangeRenderer = rangeVisual.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Standard"));

        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        rangeRenderer.material = mat;
        SetVisualColor(idleColor);
    }

    /// <summary>
    /// 박스 위치/크기를 Inspector 값과 동기화 (Runtime 조절 반영)
    /// </summary>
    private void UpdateVisualTransform()
    {
        // localPosition: 전방으로 offset + 박스 절반 깊이만큼 이동
        rangeVisual.transform.localPosition = new Vector3(0f, 0f, boxOffset + boxDepth / 2f);
        rangeVisual.transform.localScale    = new Vector3(boxWidth, boxHeight, boxDepth);
        rangeVisual.transform.localRotation = Quaternion.identity;
    }

    private void SetVisualColor(Color color)
    {
        if (rangeRenderer != null)
            rangeRenderer.material.color = color;
    }

    #endregion

    // ────────────────────────────────────────────────
    #region 기즈모 (Scene 뷰에서도 확인 가능)

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(
            transform.position + transform.forward * (boxOffset + boxDepth / 2f),
            transform.rotation,
            Vector3.one
        );
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxWidth, boxHeight, boxDepth));
        Gizmos.matrix = Matrix4x4.identity;
    }

    #endregion
}

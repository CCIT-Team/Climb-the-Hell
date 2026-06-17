using System.Collections;
using UnityEngine;

// Player 컴포넌트가 반드시 있어야 동작
[RequireComponent(typeof(Player))]
public class TempCombatInput : MonoBehaviour
{
    [Header("박스 공격 범위")]
    // 공격 판정 박스 크기
    public float boxWidth = 1.5f;
    public float boxHeight = 1.0f;
    public float boxDepth = 1.5f;

    // 플레이어 앞쪽으로 얼마나 떨어져 생성될지
    public float boxOffset = 1.0f;

    // 공격 범위가 빨갛게 보이는 시간
    public float attackVisualDuration = 0.15f;

    [Header("공격 범위 시각화 색상")]
    // 평소 색상
    public Color idleColor = new Color(1f, 1f, 0f, 0.25f);

    // 공격 시 색상
    public Color attackColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("임시 공격 쿨다운")]
    // 공격 간격
    public float attackCooldown = 0.5f;

    private Player player;

    // 공격 범위를 보여주는 큐브
    private GameObject rangeVisual;

    // 큐브 색상 변경용
    private MeshRenderer rangeRenderer;

    // 현재 공격 쿨타임 여부
    private bool isOnCooldown = false;

    private void Awake()
    {
        // Player 컴포넌트 가져오기
        player = GetComponent<Player>();

        // 공격 범위 시각화 생성
        CreateRangeVisual();
    }

    private void Update()
    {
        // 마우스 방향으로 플레이어 회전
        RotateBoxToMouse();

        // 공격 범위 위치 갱신
        if (rangeVisual != null)
            UpdateVisualTransform();

        // 좌클릭 시 공격
        if (Input.GetMouseButtonDown(0) && !isOnCooldown)
        {
            StartCoroutine(DoAttack());
        }
    }

    // 플레이어를 마우스 방향으로 회전
    private void RotateBoxToMouse()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Plane groundPlane = new Plane(
            Vector3.up,
            new Vector3(0f, transform.position.y, 0f)
        );

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(distance);

            Vector3 dir = mouseWorldPos - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation =
                    Quaternion.LookRotation(dir.normalized);
            }
        }
    }

    // 공격 처리 코루틴
    private IEnumerator DoAttack()
    {
        isOnCooldown = true;

        // 공격 시작 시 빨간색
        SetVisualColor(attackColor);

        // 실제 공격 판정
        AttackBoxArea();

        yield return new WaitForSeconds(attackVisualDuration);

        // 다시 노란색으로 변경
        SetVisualColor(idleColor);

        float remainCooldown =
            attackCooldown - attackVisualDuration;

        if (remainCooldown > 0f)
            yield return new WaitForSeconds(remainCooldown);

        isOnCooldown = false;
    }

    // 박스 범위 공격
    private void AttackBoxArea()
    {
        // 공격 박스 중심 계산
        Vector3 boxCenter = GetBoxCenter();

        // OverlapBox용 반 크기
        Vector3 halfExtents = new Vector3(
            boxWidth / 2f,
            boxHeight / 2f,
            boxDepth / 2f
        );

        // 박스 범위 안의 몬스터 탐색
        Collider[] hits = Physics.OverlapBox(
            boxCenter,
            halfExtents,
            transform.rotation,
            LayerMask.GetMask("Monster")
        );

        foreach (Collider hit in hits)
        {
            MonsterAI monster =
                hit.GetComponentInParent<MonsterAI>();

            if (monster != null && monster.IsAlive())
            {
                // 플레이어 공격력 계산
                int damage =
                    player.stats.CalculateDamage();

                // 몬스터에게 데미지
                monster.TakeDamage(damage);

                Debug.Log(
                    $"[TempCombat] {monster.MonsterName} 공격 — {damage} 데미지"
                );
            }
        }
    }

    // 공격 박스 중심 위치 계산
    private Vector3 GetBoxCenter()
    {
        return transform.position +
               transform.forward *
               (boxOffset + boxDepth / 2f);
    }

    // 공격 범위 표시용 큐브 생성
    private void CreateRangeVisual()
    {
        rangeVisual =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        rangeVisual.name = "[TempAttackRange]";

        // 충돌 제거
        Destroy(rangeVisual.GetComponent<Collider>());

        // 플레이어 자식으로 설정
        rangeVisual.transform.SetParent(transform);

        UpdateVisualTransform();

        rangeRenderer =
            rangeVisual.GetComponent<MeshRenderer>();

        // 반투명 머티리얼 생성
        Material mat =
            new Material(Shader.Find("Standard"));

        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend",
            (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend",
            (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        mat.renderQueue = 3000;

        rangeRenderer.material = mat;

        // 기본 색상 적용
        SetVisualColor(idleColor);
    }

    // 공격 범위 큐브 위치/크기 갱신
    private void UpdateVisualTransform()
    {
        rangeVisual.transform.localPosition =
            new Vector3(
                0f,
                0f,
                boxOffset + boxDepth / 2f
            );

        rangeVisual.transform.localScale =
            new Vector3(
                boxWidth,
                boxHeight,
                boxDepth
            );

        rangeVisual.transform.localRotation =
            Quaternion.identity;
    }

    // 공격 범위 색상 변경
    private void SetVisualColor(Color color)
    {
        if (rangeRenderer != null)
            rangeRenderer.material.color = color;
    }

    // Scene 뷰에서 공격 범위 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.matrix = Matrix4x4.TRS(
            transform.position +
            transform.forward *
            (boxOffset + boxDepth / 2f),

            transform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                boxWidth,
                boxHeight,
                boxDepth
            )
        );

        Gizmos.matrix = Matrix4x4.identity;
    }
}
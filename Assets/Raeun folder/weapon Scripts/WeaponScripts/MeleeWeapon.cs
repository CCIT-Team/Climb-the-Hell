using System.Collections;
using UnityEngine;

public class MeleeWeapon : Weapon
{
    [Header("Base Attack")]
    [SerializeField] private Transform attackPoint;     //무기 위치
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackAngle = 90f;   //90도 부채꼴 범위
    [SerializeField] private LayerMask monsterLayer;    //몬스터 레이어 감지

    //기본 공격(부채꼴) 시각 효과
    [Header("Fan Visualization")]
    [SerializeField] private int fanSegments = 40;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color attackColor = new Color(1f, 0.2f, 0.2f, 0.7f);

    //특수 공격(원형) 시각 효과
    [Header("Special Attack")]
    [SerializeField] private float specialRange = 3.5f;
    [SerializeField] private Color specialColor = Color.yellow;

    //부채꼴 그리는 렌더러와 원형 그리는 렌더러
    private LineRenderer fanRenderer;
    private LineRenderer specialRenderer;

    //private bool isAttacking = false;

    void Awake()
    {
        SetupRenderers();   //라인 렌더러 생성
    }

    void Start()
    {
        trailEffect.enabled = false;
    }

    void Update()
    {
        UpdateFan(); //매 프레임 부채꼴 위치 업데이트

        if (Input.GetMouseButtonDown(0)) Use();         //좌클 일반 공격
        if (Input.GetMouseButtonDown(1)) SpecialUse();  //우클 특수 공격
    }

    private void SetupRenderers()
    {
        //일반 공격(부채꼴)
        fanRenderer = gameObject.AddComponent<LineRenderer>();
        fanRenderer.useWorldSpace = true;
        fanRenderer.material = new Material(Shader.Find("Sprites/Default"));
        fanRenderer.widthMultiplier = 0.05f;
        
        //중심점 + 부채꼴 점들
        fanRenderer.positionCount = fanSegments + 2;
        SetLineColor(fanRenderer, normalColor);

        //특수 공격(원형)
        //별도 객체 생성
        specialRenderer = new GameObject("SpecialRenderer").AddComponent<LineRenderer>();
        specialRenderer.transform.SetParent(transform);
        specialRenderer.useWorldSpace = true;
        specialRenderer.material = new Material(Shader.Find("Sprites/Default"));
        specialRenderer.widthMultiplier = 0.05f;
        specialRenderer.positionCount = 0;
        SetLineColor(specialRenderer, specialColor);
        //처음엔 비활성화
        specialRenderer.enabled = false;
    }

    //부채꼴 시각 효과 업데이트
    private void UpdateFan()
    {
        if (attackPoint == null) return;

        float halfAngle = attackAngle * 0.5f;
        //중심점
        Vector3 origin = attackPoint.position;

        //플레이어 정면 방향 계산
        Vector3 forward = transform.parent != null
            ? transform.parent.forward
            : transform.forward;

        //수평 방향으로만 계산
        forward.y = 0;
        forward.Normalize();

        //부채꼴 점 저장
        Vector3[] positions = new Vector3[fanSegments + 2];
        positions[0] = origin;

        for (int i = 0; i <= fanSegments; i++)
        {
            float t = (float)i / fanSegments;
            //각도 계산
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);

            //Y축 회전으로 방향 계산
            Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;
            //부채꼴 끝점 계산
            positions[i + 1] = origin + dir * attackRange;
        }

        fanRenderer.positionCount = positions.Length;
        fanRenderer.SetPositions(positions);
    }

    //일반 공격 실행
    public override void Use()
    {
        if (isAttacking) return;
        StartCoroutine(Swing());
    }
    //일반 공격 코루틴
    IEnumerator Swing()
    {
        isAttacking = true;

        SetLineColor(fanRenderer, attackColor);

        yield return new WaitForSeconds(0.1f);

        trailEffect.enabled = true;
        PerformAttack();    //공격 판정

        yield return new WaitForSeconds(0.3f);

        trailEffect.enabled = false;
        SetLineColor(fanRenderer, normalColor);

        isAttacking = false;
    }

    //공격 판정: 부채꼴 범위 내 몬스터 감지
    private void PerformAttack()
    {
        //공격 범위 내 모든 콜라이더 감지
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, monsterLayer);

        //방향 계산: 플레이어 정면 방향
        Vector3 forward = transform.parent != null
            ? transform.parent.forward
            : transform.forward;

        forward.y = 0;
        forward.Normalize();

        int hitCount = 0;

        foreach (Collider hit in hits)
        {
            Vector3 dir = hit.transform.position - attackPoint.position;
            dir.y = 0;
            dir.Normalize();

            //각도 계산: 공격 방향과 몬스터 방향이 부채꼴 범위 내인지
            if (Vector3.Angle(forward, dir) <= attackAngle * 0.5f)
            {
                MonsterAI monster = hit.GetComponentInParent<MonsterAI>();

                if (monster != null && monster.IsAlive())
                {
                    monster.TakeDamage(data.playerattack);
                    hitCount++;
                }
            }
        }

        Debug.Log(hitCount == 0 ? "[Melee] Miss" : $"[Melee] Hit: {hitCount}");
    }

    //특수 공격 실행
    public void SpecialUse()
    {
        if (isAttacking) return;
        StartCoroutine(SpecialSwing());
    }

    IEnumerator SpecialSwing()
    {
        isAttacking = true;

        DrawSpecialCircle();

        yield return new WaitForSeconds(0.2f);

        PerformSpecialAttack();

        yield return new WaitForSeconds(0.4f);

        specialRenderer.enabled = false;

        isAttacking = false;
    }

    //특수 공격 판정: 원형 범위 내 몬스터 감지
    private void PerformSpecialAttack()
    {
        Collider[] hits = Physics.OverlapSphere(
            attackPoint.position,
            specialRange,
            monsterLayer
        );

        foreach (Collider hit in hits)
        {
            MonsterAI monster = hit.GetComponentInParent<MonsterAI>();

            if (monster != null && monster.IsAlive())
            {
                monster.TakeDamage(data.playerattack * 2);  //데미지 2배
            }
        }

        Debug.Log("[Special Attack] Hit!");
    }

    //원형 시각 효과
    private void DrawSpecialCircle()
    {
        specialRenderer.enabled = true;

        //원형을 부드럽게 그리기 위한 세그먼트 수
        int segments = 50;
        specialRenderer.positionCount = segments + 1;

        Vector3 center = attackPoint.position;

        for (int i = 0; i <= segments; i++)
        {
            //0~360도 각도 계산
            float angle = i * Mathf.PI * 2f / segments;

            //원 좌표 계산
            Vector3 pos = new Vector3(
                Mathf.Sin(angle) * specialRange,
                0,
                Mathf.Cos(angle) * specialRange
            );

            specialRenderer.SetPosition(i, center + pos);
        }
    }

    //라인 렌더러 색상 설정
    private void SetLineColor(LineRenderer lr, Color color)
    {
        lr.startColor = color;
        lr.endColor = color;
    }

    //씬에서 공격 범위 시각화
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        //일반 공격 범위(부채꼴) 시각화
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        //특수 공격 범위(원형) 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, specialRange);
    }
}
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class TempCombatInput : MonoBehaviour
{
    [Header("박스 공격 범위")]
    public float boxWidth = 1.5f;
    public float boxHeight = 1.0f;
    public float boxDepth = 1.5f;
    public float boxOffset = 1.0f;

    public float attackVisualDuration = 0.15f;

    [Header("공격 범위 시각화 색상")]
    public Color idleColor = new Color(1f, 1f, 0f, 0.25f);
    public Color attackColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("임시 공격 쿨다운")]
    public float attackCooldown = 0.5f;

    private Player player;
    private GameObject rangeVisual;
    private MeshRenderer rangeRenderer;
    private bool isOnCooldown = false;

    private void Awake()
    {
        player = GetComponent<Player>();
        CreateRangeVisual();
    }

    private void Update()
    {
        RotateBoxToMouse();

        if (rangeVisual != null)
            UpdateVisualTransform();

        if (Input.GetMouseButtonDown(0) && !isOnCooldown)
        {
            StartCoroutine(DoAttack());
        }
    }

    private void RotateBoxToMouse()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(distance);
            Vector3 dir = mouseWorldPos - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized);
            }
        }
    }

    private IEnumerator DoAttack()
    {
        isOnCooldown = true;

        SetVisualColor(attackColor);

        AttackBoxArea();

        yield return new WaitForSeconds(attackVisualDuration);

        SetVisualColor(idleColor);

        float remainCooldown = attackCooldown - attackVisualDuration;

        if (remainCooldown > 0f)
            yield return new WaitForSeconds(remainCooldown);

        isOnCooldown = false;
    }

    private void AttackBoxArea()
    {
        Vector3 boxCenter = GetBoxCenter();

        Vector3 halfExtents = new Vector3(
            boxWidth / 2f,
            boxHeight / 2f,
            boxDepth / 2f
        );

        Collider[] hits = Physics.OverlapBox(
            boxCenter,
            halfExtents,
            transform.rotation,
            LayerMask.GetMask("Monster")
        );

        foreach (Collider hit in hits)
        {
            MonsterAI monster = hit.GetComponentInParent<MonsterAI>();

            if (monster != null && monster.IsAlive())
            {
                int damage = player.stats.CalculateDamage();

                monster.TakeDamage(damage);

                Debug.Log($"[TempCombat] {monster.MonsterName} 공격 — {damage} 데미지");
            }
        }
    }

    private Vector3 GetBoxCenter()
    {
        return transform.position + transform.forward * (boxOffset + boxDepth / 2f);
    }

    private void CreateRangeVisual()
    {
        rangeVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rangeVisual.name = "[TempAttackRange]";

        Destroy(rangeVisual.GetComponent<Collider>());

        rangeVisual.transform.SetParent(transform);
        UpdateVisualTransform();

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

    private void UpdateVisualTransform()
    {
        rangeVisual.transform.localPosition = new Vector3(0f, 0f, boxOffset + boxDepth / 2f);
        rangeVisual.transform.localScale = new Vector3(boxWidth, boxHeight, boxDepth);
        rangeVisual.transform.localRotation = Quaternion.identity;
    }

    private void SetVisualColor(Color color)
    {
        if (rangeRenderer != null)
            rangeRenderer.material.color = color;
    }

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
}
using System.Collections;
using UnityEngine;

public class MonsterAttackHitbox : MonoBehaviour
{
    [Header("박스 공격 범위")]
    public float boxWidth = 1.5f;
    public float boxHeight = 1.2f;
    public float boxDepth = 1.5f;
    public float boxOffset = 0.8f;

    [Header("표시 시간")]
    public float visualDuration = 0.15f;

    [Header("색상")]
    public Color idleColor = new Color(1f, 1f, 0f, 0.2f);
    public Color attackColor = new Color(1f, 0f, 0f, 0.5f);

    private GameObject rangeVisual;
    private MeshRenderer rangeRenderer;
    private bool isAttacking = false;

    void Awake()
    {
        CreateRangeVisual();
    }

    void Update()
    {
        if (rangeVisual != null)
            UpdateVisualTransform();
    }

    public void Attack(Player target, int damage)
    {
        if (isAttacking) return;
        StartCoroutine(AttackRoutine(target, damage));
    }

    private IEnumerator AttackRoutine(Player target, int damage)
    {
        isAttacking = true;

        SetVisualColor(attackColor);

        if (IsPlayerInHitbox(target))
        {
            target.TakeDamage(damage);
        }

        yield return new WaitForSeconds(visualDuration);

        SetVisualColor(idleColor);

        isAttacking = false;
    }

    private bool IsPlayerInHitbox(Player target)
    {
        if (target == null) return false;

        Vector3 boxCenter = GetBoxCenter();
        Vector3 halfExtents = new Vector3(boxWidth / 2f, boxHeight / 2f, boxDepth / 2f);

        Collider[] hits = Physics.OverlapBox(
            boxCenter,
            halfExtents,
            transform.rotation
        );

        foreach (Collider hit in hits)
        {
            Player player = hit.GetComponentInParent<Player>();

            if (player == target)
                return true;
        }

        return false;
    }

    private Vector3 GetBoxCenter()
    {
        return transform.position + transform.forward * (boxOffset + boxDepth / 2f);
    }

    private void CreateRangeVisual()
    {
        rangeVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rangeVisual.name = "[MonsterAttackRange]";

        Destroy(rangeVisual.GetComponent<Collider>());

        rangeVisual.transform.SetParent(transform);
        UpdateVisualTransform();

        rangeRenderer = rangeVisual.GetComponent<MeshRenderer>();

        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;

        rangeRenderer.material = mat;
        SetVisualColor(idleColor);
    }

    private void UpdateVisualTransform()
    {
        rangeVisual.transform.localPosition =
            new Vector3(0f, 0f, boxOffset + boxDepth / 2f);

        rangeVisual.transform.localScale =
            new Vector3(boxWidth, boxHeight, boxDepth);

        rangeVisual.transform.localRotation =
            Quaternion.identity;
    }

    private void SetVisualColor(Color color)
    {
        if (rangeRenderer != null)
            rangeRenderer.material.color = color;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.matrix = Matrix4x4.TRS(
            transform.position + transform.forward * (boxOffset + boxDepth / 2f),
            transform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(boxWidth, boxHeight, boxDepth)
        );

        Gizmos.matrix = Matrix4x4.identity;
    }
}
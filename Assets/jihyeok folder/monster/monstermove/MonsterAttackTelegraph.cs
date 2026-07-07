using UnityEngine;

/// <summary>
/// MonsterAI와 공용 AttackTelegraphPool을 연결한다.
///
/// 공격 예고:
/// 반투명 빨간색.
///
/// 실제 공격:
/// 진한 빨간색.
/// </summary>
public class MonsterAttackTelegraph : MonoBehaviour
{
    [Header("바닥 표시 높이")]
    [Tooltip("바닥과 겹치는 현상을 막기 위해 살짝 띄운다.")]
    [Min(0f)]
    [SerializeField]
    private float groundHeightOffset = 0.05f;

    private AttackTelegraphPool.Handle currentHandle;

    public void ShowCircle(
        Vector3 center,
        float radius
    )
    {
        Hide();

        center.y =
            transform.position.y +
            groundHeightOffset;

        currentHandle =
            AttackTelegraphPool.Instance
                .RentCircle(
                    center,
                    radius
                );
    }

    public void ShowBox(
        Vector3 startPosition,
        Vector3 direction,
        float width,
        float length
    )
    {
        Hide();

        startPosition.y =
            transform.position.y +
            groundHeightOffset;

        currentHandle =
            AttackTelegraphPool.Instance
                .RentBox(
                    startPosition,
                    direction,
                    width,
                    length
                );
    }

    /// <summary>
    /// 현재 반투명 공격 예고를
    /// 진한 실제 공격 범위로 변경한다.
    /// </summary>
    public void ShowAttackResult()
    {
        if (currentHandle == null ||
            !currentHandle.IsValid)
        {
            return;
        }

        AttackTelegraphPool.Instance
            .ShowImpact(currentHandle);
    }

    public void Hide()
    {
        if (currentHandle == null)
        {
            return;
        }

        if (currentHandle.IsValid)
        {
            AttackTelegraphPool.Instance
                .Release(currentHandle);
        }

        currentHandle = null;
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        Hide();
    }
}

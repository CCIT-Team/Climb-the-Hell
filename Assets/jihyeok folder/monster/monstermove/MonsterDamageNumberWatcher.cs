using UnityEngine;

/// <summary>
/// MonsterStats의 OnDamaged/OnHealed 이벤트를 구독해
/// 데미지/회복 숫자를 요청한다.
/// </summary>
public class MonsterDamageNumberWatcher : MonoBehaviour
{
    [Header("몬스터 스탯")]
    [SerializeField]
    private MonsterStats monsterStats;

    [Header("숫자 생성 위치")]
    [SerializeField]
    private Transform damageNumberPoint;

    [SerializeField]
    private Vector3 fallbackOffset =
        new Vector3(0f, 2f, 0f);

    private int nextSlotIndex;

    private void Awake()
    {
        if (monsterStats == null)
        {
            monsterStats =
                GetComponent<MonsterStats>();
        }

        if (monsterStats == null)
        {
            monsterStats =
                GetComponentInParent<MonsterStats>();
        }

        if (monsterStats == null)
        {
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (monsterStats == null)
        {
            return;
        }

        monsterStats.OnDamaged += HandleDamaged;
        monsterStats.OnHealed += HandleHealed;
    }

    private void OnDisable()
    {
        if (monsterStats == null)
        {
            return;
        }

        monsterStats.OnDamaged -= HandleDamaged;
        monsterStats.OnHealed -= HandleHealed;
    }

    private void HandleDamaged(int damage, bool isCritical)
    {
        DamageNumberManager manager =
            DamageNumberManager.Instance;

        if (manager == null)
        {
            return;
        }

        manager.ShowDamage(
            damage,
            GetBasePosition(),
            NextSlot(manager),
            isCritical
        );
    }

    private void HandleHealed(int amount)
    {
        DamageNumberManager manager =
            DamageNumberManager.Instance;

        if (manager == null)
        {
            return;
        }

        manager.ShowHeal(
            amount,
            GetBasePosition(),
            NextSlot(manager)
        );
    }

    private Vector3 GetBasePosition()
    {
        return damageNumberPoint != null
            ? damageNumberPoint.position
            : monsterStats.transform.position +
              fallbackOffset;
    }

    private int NextSlot(DamageNumberManager manager)
    {
        int slot = nextSlotIndex;

        nextSlotIndex++;

        if (nextSlotIndex >=
            manager.GetSlotCount())
        {
            nextSlotIndex = 0;
        }

        return slot;
    }

    /// <summary>
    /// 오브젝트 풀에서 재사용할 때 호출한다.
    /// </summary>
    public void ResetWatcher()
    {
        nextSlotIndex = 0;
    }
}
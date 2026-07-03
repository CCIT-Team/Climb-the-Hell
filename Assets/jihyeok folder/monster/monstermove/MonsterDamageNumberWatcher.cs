using UnityEngine;

/// <summary>
/// 기존 몬스터 코드를 수정하지 않고
/// 체력 감소량을 확인해 데미지 숫자를 요청한다.
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

    private int previousHp;
    private int nextSlotIndex;
    private bool initialized;

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

    private void Start()
    {
        if (monsterStats == null)
        {
            return;
        }

        previousHp =
            monsterStats.currentHp;

        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            return;
        }

        int currentHp =
            monsterStats.currentHp;

        if (currentHp < previousHp)
        {
            ShowDamage(
                previousHp - currentHp
            );
        }

        previousHp =
            currentHp;
    }

    private void ShowDamage(
        int damage
    )
    {
        DamageNumberManager manager =
            DamageNumberManager.Instance;

        if (manager == null ||
            damage <= 0)
        {
            return;
        }

        Vector3 basePosition =
            damageNumberPoint != null
                ? damageNumberPoint.position
                : monsterStats.transform.position +
                  fallbackOffset;

        manager.ShowDamage(
            damage,
            basePosition,
            nextSlotIndex,
            false
        );

        nextSlotIndex++;

        if (nextSlotIndex >=
            manager.GetSlotCount())
        {
            nextSlotIndex = 0;
        }
    }

    /// <summary>
    /// 오브젝트 풀에서 체력을 초기화한 뒤 호출한다.
    /// </summary>
    public void ResetWatcher()
    {
        previousHp =
            monsterStats.currentHp;

        nextSlotIndex = 0;
        initialized = true;
    }
}
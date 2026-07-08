using UnityEngine;
using System;

/// <summary>
/// 몬스터의 체력과 기본 능력치를 관리한다.
/// 피격 순간에만 HP바를 갱신한다.
/// </summary>
public class MonsterStats : MonoBehaviour
{
    [Header("몬스터 체력")]
    [Min(1)]
    public int monsterhp = 100;

    [HideInInspector]
    public int currentHp;

    [Header("몬스터 공격력")]
    [Min(0)]
    public int monsterattack = 10;

    [Header("몬스터 이동속도")]
    [Min(0f)]
    public float monsterspeed = 3f;

    [Header("몬스터 공격 범위")]
    [Min(0f)]
    public float monsterrange = 2f;

    [Header("몬스터 HP바")]
    [Tooltip("비워두면 자기 자식에서 자동으로 찾는다.")]
    [SerializeField]
    private MonsterHealthBar monsterHealthBar;

    protected virtual void Awake()
    {
        EnsurePlayerPassThrough();

        currentHp = monsterhp;

        FindHealthBar();

        if (monsterHealthBar != null)
        {
            monsterHealthBar.ResetHealthBar(
                currentHp,
                monsterhp
            );
        }
    }

    private void EnsurePlayerPassThrough()
    {
        if (GetComponent<MonsterPlayerCollisionPassThrough>() != null)
        {
            return;
        }

        gameObject.AddComponent<MonsterPlayerCollisionPassThrough>();
    }

    public event Action<int, bool> OnDamaged;
    public event Action<int> OnHealed;

    /// <summary>
    /// 몬스터에게 데미지를 적용한다.
    /// </summary>
    public virtual bool TakeDamage(int damage)
    {
        return TakeDamage(damage, false);
    }

    /// <summary>
    /// 몬스터에게 데미지를 적용한다. 치명타 여부를 함께 전달한다.
    /// </summary>
    public virtual bool TakeDamage(int damage, bool isCritical)
    {
        if (damage <= 0)
        {
            return false;
        }

        if (currentHp <= 0)
        {
            return true;
        }

        int previousHp = currentHp;

        currentHp =
            Mathf.Max(
                0,
                currentHp - damage
            );

        RefreshHealthBar();

        int actualDamage =
            previousHp - currentHp;

        if (actualDamage > 0)
        {
            OnDamaged?.Invoke(actualDamage, isCritical);
        }

        return currentHp <= 0;
    }

    /// <summary>
    /// 몬스터 체력을 회복한다.
    /// </summary>
    public virtual void Heal(int amount)
    {
        if (amount <= 0 ||
            currentHp <= 0)
        {
            return;
        }

        int previousHp = currentHp;

        currentHp =
            Mathf.Min(
                monsterhp,
                currentHp + amount
            );

        RefreshHealthBar();

        int actualHeal =
            currentHp - previousHp;

        if (actualHeal > 0)
        {
            OnHealed?.Invoke(actualHeal);
        }
    }

    protected void GrantGoldToPlayer(
        int rewardGold,
        int defaultGold = 3
    )
    {
        int gold =
            rewardGold > 0
                ? rewardGold
                : defaultGold;

        if (gold <= 0)
        {
            return;
        }

        Player player =
            PlayerSceneMover.Instance != null
                ? PlayerSceneMover.Instance.CurrentPlayer
                : null;

        if (player == null)
        {
            player =
                FindFirstObjectByType<Player>(
                    FindObjectsInactive.Include
                );
        }

        if (player == null ||
            player.money == null)
        {
            return;
        }

        player.money.AddMoney(gold);
    }

    protected void ShowGoldNumber(
        int rewardGold,
        Vector3 position
    )
    {
        DamageNumberManager manager =
            DamageNumberManager.Instance;

        if (manager == null)
        {
            return;
        }

        int gold =
            rewardGold > 0
                ? rewardGold
                : 3;

        manager.ShowHeal(
            gold,
            position + Vector3.up * 1.6f,
            UnityEngine.Random.Range(
                0,
                manager.GetSlotCount()
            )
        );
    }

    /// <summary>
    /// 오브젝트 풀에서 몬스터를 다시 사용할 때 호출한다.
    /// </summary>
    public virtual void ResetStats()
    {
        currentHp = monsterhp;

        FindHealthBar();

        if (monsterHealthBar != null)
        {
            monsterHealthBar.ResetHealthBar(
                currentHp,
                monsterhp
            );
        }
    }

    /// <summary>
    /// 자기 몬스터 프리팹 안의 HP바만 찾는다.
    /// </summary>
    private void FindHealthBar()
    {
        if (monsterHealthBar != null)
        {
            return;
        }

        monsterHealthBar =
            GetComponentInChildren<MonsterHealthBar>(
                true
            );
    }

    /// <summary>
    /// 현재 체력을 HP바에 전달한다.
    /// </summary>
    private void RefreshHealthBar()
    {
        if (monsterHealthBar == null)
        {
            FindHealthBar();
        }

        if (monsterHealthBar == null)
        {
            Debug.LogWarning(
                $"[{name}] MonsterHealthBar를 찾지 못했습니다.",
                gameObject
            );

            return;
        }

        monsterHealthBar.SetHealth(
            currentHp,
            monsterhp
        );
    }
}

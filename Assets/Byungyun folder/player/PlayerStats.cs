using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 최종 스탯과 현재 체력을 관리한다.
///
/// 최종 스탯 =
/// 기본 스탯
/// + 특성 보너스
/// + 득도 보너스
/// + 임시 보정
///
/// 마나 스탯은 사용하지 않는다.
/// </summary>
[Serializable]
public class PlayerStats
{
    [Header("기본 스탯")]

    [SerializeField]
    private PlayerStatValues baseStats =
        PlayerStatValues.CreateDefaultBaseStats();

    [Header("특성 추가 스탯")]

    [SerializeField]
    private PlayerStatValues traitBonusStats =
        new PlayerStatValues();

    [Header("득도 추가 스탯")]

    [SerializeField]
    private PlayerStatValues boonBonusStats =
        new PlayerStatValues();

    [Header("레벨")]

    [Min(1)]
    public int playerLevel = 1;

    [Header("현재 체력")]

    [SerializeField]
    private int currentHp;

    /*
     * 실제 사용으로 소비된 사망 저항과 재선택 횟수.
     * 프로퍼티에 -- 연산을 사용해도 동작하도록 분리한다.
     */
    [SerializeField]
    private int consumedDeathResist;

    [SerializeField]
    private int consumedRerollCount;

    [NonSerialized]
    private readonly List<PlayerStatValues>
        runtimeModifiers =
            new List<PlayerStatValues>();

    /// <summary>
    /// 스탯 또는 현재 체력이 변경될 때 호출된다.
    /// PlayerUI에서 구독하는 기존 이벤트와 호환된다.
    /// </summary>
    public event Action OnStatsChanged;

    public PlayerStatValues BaseStats =>
        baseStats;

    public PlayerStatValues TraitBonusStats =>
        traitBonusStats;

    public PlayerStatValues BoonBonusStats =>
        boonBonusStats;

    public int CurrentHp =>
        currentHp;

    public int MaxHp =>
        Mathf.Max(
            1,
            Mathf.RoundToInt(
                GetTotal(value => value.maxHp)
            )
        );

    public int Attack =>
        Mathf.Max(
            0,
            Mathf.RoundToInt(
                GetTotal(value => value.attack)
            )
        );

    public float MoveSpeed =>
        Mathf.Max(
            0f,
            GetTotal(value => value.moveSpeed)
        );

    public float CriticalChance =>
        Mathf.Clamp01(
            GetTotal(
                value => value.criticalChance
            )
        );

    public float CriticalMultiplier =>
        Mathf.Max(
            1f,
            GetTotal(
                value => value.criticalMultiplier
            )
        );

    public float DashDistance =>
        Mathf.Max(
            0.1f,
            GetTotal(
                value => value.dashDistance
            )
        );

    public float DashDuration =>
        Mathf.Max(
            0.01f,
            GetTotal(
                value => value.dashDuration
            )
        );

    public float DashCooldown =>
        Mathf.Max(
            0f,
            GetTotal(
                value => value.dashCooldown
            )
        );

    public float DashCooldownRecoveryMultiplier =>
        Mathf.Max(
            0.01f,
            GetTotal(
                value =>
                    value.dashCooldownRecoveryMultiplier
            )
        );

    public int MaxDashCount =>
        Mathf.Max(
            1,
            1 +
            Mathf.RoundToInt(
                GetTotal(
                    value => value.extraDashCount
                )
            )
        );

    /// <summary>
    /// 사망 저항의 현재 사용 가능 횟수.
    /// Player.cs에서 DeathResist-- 형태로 사용해도 동작한다.
    /// </summary>
    public int DeathResist
    {
        get
        {
            return Mathf.Max(
                0,
                GetRawDeathResist() -
                consumedDeathResist
            );
        }

        set
        {
            int rawValue =
                GetRawDeathResist();

            int targetValue =
                Mathf.Clamp(
                    value,
                    0,
                    rawValue
                );

            consumedDeathResist =
                rawValue -
                targetValue;

            NotifyChange();
        }
    }

    /// <summary>
    /// 최종 골드 획득 배율.
    /// 기본값은 1이다.
    /// </summary>
    public float GoldMultiplier =>
        Mathf.Max(
            0f,
            GetTotal(
                value => value.goldMultiplier
            )
        );

    /// <summary>
    /// 현재 사용 가능한 재선택 횟수.
    /// RerollCount-- 형태로 사용해도 동작한다.
    /// </summary>
    public int RerollCount
    {
        get
        {
            return Mathf.Max(
                0,
                GetRawRerollCount() -
                consumedRerollCount
            );
        }

        set
        {
            int rawValue =
                GetRawRerollCount();

            int targetValue =
                Mathf.Clamp(
                    value,
                    0,
                    rawValue
                );

            consumedRerollCount =
                rawValue -
                targetValue;

            NotifyChange();
        }
    }

    public bool DashInvincible
    {
        get
        {
            EnsureStatContainers();

            if (baseStats.dashInvincible ||
                traitBonusStats.dashInvincible ||
                boonBonusStats.dashInvincible)
            {
                return true;
            }

            for (int i = 0;
                 i < runtimeModifiers.Count;
                 i++)
            {
                PlayerStatValues modifier =
                    runtimeModifiers[i];

                if (modifier != null &&
                    modifier.dashInvincible)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 체력과 소비형 스탯을 초기화한다.
    /// </summary>
    public void Init(bool fullHeal = true)
    {
        EnsureStatContainers();

        consumedDeathResist = 0;
        consumedRerollCount = 0;

        if (fullHeal ||
            currentHp <= 0)
        {
            currentHp = MaxHp;
        }
        else
        {
            ClampCurrentHp();
        }

        NotifyChange();
    }

    /// <summary>
    /// 피해를 적용하고 사망 여부를 반환한다.
    /// </summary>
    public bool TakeDamage(int damage)
    {
        damage =
            Mathf.Max(
                0,
                damage
            );

        currentHp =
            Mathf.Max(
                0,
                currentHp - damage
            );

        NotifyChange();

        return currentHp <= 0;
    }

    /// <summary>
    /// 체력을 회복한다.
    /// </summary>
    public void Heal(int amount)
    {
        AddCurrentHp(
            Mathf.Max(
                0,
                amount
            )
        );
    }

    /// <summary>
    /// 현재 체력에 값을 더한다.
    /// 음수를 전달하면 체력이 감소한다.
    /// TraitManager의 기존 호출과 호환된다.
    /// </summary>
    public void AddCurrentHp(int amount)
    {
        currentHp =
            Mathf.Clamp(
                currentHp + amount,
                0,
                MaxHp
            );

        NotifyChange();
    }

    public bool IsDead()
    {
        return currentHp <= 0;
    }

    /// <summary>
    /// 사망 저항을 1회 소비한다.
    /// </summary>
    public bool TryConsumeDeathResist()
    {
        if (DeathResist <= 0)
        {
            return false;
        }

        consumedDeathResist++;

        NotifyChange();

        return true;
    }

    /// <summary>
    /// 재선택 횟수를 1회 소비한다.
    /// </summary>
    public bool TryConsumeReroll()
    {
        if (RerollCount <= 0)
        {
            return false;
        }

        consumedRerollCount++;

        NotifyChange();

        return true;
    }

    /// <summary>
    /// 최종 공격력과 치명타 스탯으로 피해를 계산한다.
    /// </summary>
    public int CalculateDamage()
    {
        bool isCritical =
            UnityEngine.Random.value <
            CriticalChance;

        if (!isCritical)
        {
            return Attack;
        }

        return Mathf.Max(
            0,
            Mathf.RoundToInt(
                Attack *
                CriticalMultiplier
            )
        );
    }

    public float GetEffectiveDashCooldown()
    {
        return
            DashCooldown /
            DashCooldownRecoveryMultiplier;
    }

    public void AddTraitBonus(
        PlayerStatValues bonus)
    {
        if (bonus == null)
        {
            return;
        }

        EnsureStatContainers();

        int previousMaxHp =
            MaxHp;

        traitBonusStats.Add(bonus);

        ApplyMaxHpIncrease(
            previousMaxHp
        );

        ClampConsumedValues();
        NotifyChange();
    }

    public void RemoveTraitBonus(
        PlayerStatValues bonus)
    {
        if (bonus == null)
        {
            return;
        }

        EnsureStatContainers();

        traitBonusStats.Subtract(bonus);

        ClampCurrentHp();
        ClampConsumedValues();
        NotifyChange();
    }

    /// <summary>
    /// 득도에서 받은 스탯을 플레이어에게 누적한다.
    /// </summary>
    public void AddBoonBonus(
        PlayerStatValues bonus)
    {
        if (bonus == null)
        {
            return;
        }

        EnsureStatContainers();

        int previousMaxHp =
            MaxHp;

        boonBonusStats.Add(bonus);

        ApplyMaxHpIncrease(
            previousMaxHp
        );

        ClampConsumedValues();
        NotifyChange();
    }

    public void RemoveBoonBonus(
        PlayerStatValues bonus)
    {
        if (bonus == null)
        {
            return;
        }

        EnsureStatContainers();

        boonBonusStats.Subtract(bonus);

        ClampCurrentHp();
        ClampConsumedValues();
        NotifyChange();
    }

    public void ClearBoonBonuses()
    {
        EnsureStatContainers();

        boonBonusStats.Clear();

        ClampCurrentHp();
        ClampConsumedValues();
        NotifyChange();
    }

    public void AddRuntimeModifier(
        PlayerStatValues modifier)
    {
        if (modifier == null ||
            runtimeModifiers.Contains(
                modifier
            ))
        {
            return;
        }

        int previousMaxHp =
            MaxHp;

        runtimeModifiers.Add(
            modifier
        );

        ApplyMaxHpIncrease(
            previousMaxHp
        );

        ClampConsumedValues();
        NotifyChange();
    }

    public void RemoveRuntimeModifier(
        PlayerStatValues modifier)
    {
        if (modifier == null)
        {
            return;
        }

        runtimeModifiers.Remove(
            modifier
        );

        ClampCurrentHp();
        ClampConsumedValues();
        NotifyChange();
    }

    public void ClearRuntimeModifiers()
    {
        runtimeModifiers.Clear();

        ClampCurrentHp();
        ClampConsumedValues();
        NotifyChange();
    }

    /// <summary>
    /// 외부에서 스탯 값을 직접 수정한 뒤
    /// UI 등에 변경 사실을 알릴 때 호출한다.
    /// </summary>
    public void NotifyChange()
    {
        EnsureStatContainers();

        ClampCurrentHp();
        ClampConsumedValues();

        OnStatsChanged?.Invoke();
    }

    private int GetRawDeathResist()
    {
        return Mathf.Max(
            0,
            Mathf.RoundToInt(
                GetTotal(
                    value => value.deathResist
                )
            )
        );
    }

    private int GetRawRerollCount()
    {
        return Mathf.Max(
            0,
            Mathf.RoundToInt(
                GetTotal(
                    value => value.rerollCount
                )
            )
        );
    }

    private float GetTotal(
        Func<PlayerStatValues, float> selector)
    {
        EnsureStatContainers();

        float total =
            selector(baseStats) +
            selector(traitBonusStats) +
            selector(boonBonusStats);

        for (int i = 0;
             i < runtimeModifiers.Count;
             i++)
        {
            PlayerStatValues modifier =
                runtimeModifiers[i];

            if (modifier != null)
            {
                total +=
                    selector(modifier);
            }
        }

        return total;
    }

    private void ApplyMaxHpIncrease(
        int previousMaxHp)
    {
        int increasedAmount =
            MaxHp -
            previousMaxHp;

        if (increasedAmount > 0)
        {
            currentHp +=
                increasedAmount;
        }

        ClampCurrentHp();
    }

    private void EnsureStatContainers()
    {
        if (baseStats == null)
        {
            baseStats =
                PlayerStatValues
                    .CreateDefaultBaseStats();
        }

        if (traitBonusStats == null)
        {
            traitBonusStats =
                new PlayerStatValues();
        }

        if (boonBonusStats == null)
        {
            boonBonusStats =
                new PlayerStatValues();
        }
    }

    private void ClampCurrentHp()
    {
        currentHp =
            Mathf.Clamp(
                currentHp,
                0,
                MaxHp
            );
    }

    private void ClampConsumedValues()
    {
        consumedDeathResist =
            Mathf.Clamp(
                consumedDeathResist,
                0,
                GetRawDeathResist()
            );

        consumedRerollCount =
            Mathf.Clamp(
                consumedRerollCount,
                0,
                GetRawRerollCount()
            );
    }
}

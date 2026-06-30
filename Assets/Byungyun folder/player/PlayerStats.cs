using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerStats
{
    [Header("기본 스탯")]
    [SerializeField]
    private PlayerStatValues baseStats =
        PlayerStatValues.CreateDefaultBaseStats();

    [Header("특성 추가 스탯")]
    [Tooltip("특성 시스템은 나중에 이 데이터에 값을 넣으면 됩니다.")]
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
     * 작두 타기처럼 일정 시간만 적용되는 효과.
     * PlayerStats 자체는 MonoBehaviour가 아니므로
     * 런타임 리스트로만 보관한다.
     */
    [NonSerialized]
    private readonly List<PlayerStatValues>
        runtimeModifiers = new List<PlayerStatValues>();

    public PlayerStatValues BaseStats => baseStats;
    public PlayerStatValues TraitBonusStats => traitBonusStats;
    public PlayerStatValues BoonBonusStats => boonBonusStats;

    public int CurrentHp => currentHp;

    public int MaxHp =>
        Mathf.Max(
            1,
            Mathf.RoundToInt(GetTotal(
                value => value.maxHp
            ))
        );

    public int Attack =>
        Mathf.Max(
            0,
            Mathf.RoundToInt(GetTotal(
                value => value.attack
            ))
        );

    public float MoveSpeed =>
        Mathf.Max(
            0f,
            GetTotal(value => value.moveSpeed)
        );

    public int Mana =>
        Mathf.Max(
            0,
            Mathf.RoundToInt(GetTotal(
                value => value.mana
            ))
        );

    public float CriticalChance =>
        Mathf.Clamp01(
            GetTotal(value => value.criticalChance)
        );

    public float CriticalMultiplier =>
        Mathf.Max(
            1f,
            GetTotal(value => value.criticalMultiplier)
        );

    public float DashDistance =>
        Mathf.Max(
            0.1f,
            GetTotal(value => value.dashDistance)
        );

    public float DashDuration =>
        Mathf.Max(
            0.01f,
            GetTotal(value => value.dashDuration)
        );

    public float DashCooldown =>
        Mathf.Max(
            0f,
            GetTotal(value => value.dashCooldown)
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
            1 + Mathf.RoundToInt(
                GetTotal(
                    value => value.extraDashCount
                )
            )
        );

    public bool DashInvincible
    {
        get
        {
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
                if (runtimeModifiers[i] != null &&
                    runtimeModifiers[i].dashInvincible)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public void Init(bool fullHeal = true)
    {
        if (fullHeal || currentHp <= 0)
        {
            currentHp = MaxHp;
        }
        else
        {
            currentHp =
                Mathf.Clamp(
                    currentHp,
                    0,
                    MaxHp
                );
        }
    }

    public bool TakeDamage(int damage)
    {
        damage = Mathf.Max(0, damage);

        currentHp =
            Mathf.Max(
                0,
                currentHp - damage
            );

        return currentHp <= 0;
    }

    public void Heal(int amount)
    {
        amount = Mathf.Max(0, amount);

        currentHp =
            Mathf.Min(
                MaxHp,
                currentHp + amount
            );
    }

    public bool IsDead()
    {
        return currentHp <= 0;
    }

    public int CalculateDamage()
    {
        bool critical =
            UnityEngine.Random.value <
            CriticalChance;

        if (!critical)
        {
            return Attack;
        }

        return Mathf.RoundToInt(
            Attack *
            CriticalMultiplier
        );
    }

    public float GetEffectiveDashCooldown()
    {
        return DashCooldown /
               DashCooldownRecoveryMultiplier;
    }

    public void AddTraitBonus(
        PlayerStatValues bonus
    )
    {
        traitBonusStats.Add(bonus);
        ClampCurrentHp();
    }

    public void RemoveTraitBonus(
        PlayerStatValues bonus
    )
    {
        traitBonusStats.Subtract(bonus);
        ClampCurrentHp();
    }

    public void AddBoonBonus(
        PlayerStatValues bonus
    )
    {
        boonBonusStats.Add(bonus);
        ClampCurrentHp();
    }

    public void RemoveBoonBonus(
        PlayerStatValues bonus
    )
    {
        boonBonusStats.Subtract(bonus);
        ClampCurrentHp();
    }

    public void AddRuntimeModifier(
        PlayerStatValues modifier
    )
    {
        if (modifier == null ||
            runtimeModifiers.Contains(modifier))
        {
            return;
        }

        runtimeModifiers.Add(modifier);
        ClampCurrentHp();
    }

    public void RemoveRuntimeModifier(
        PlayerStatValues modifier
    )
    {
        if (modifier == null)
        {
            return;
        }

        runtimeModifiers.Remove(modifier);
        ClampCurrentHp();
    }

    public void ClearRuntimeModifiers()
    {
        runtimeModifiers.Clear();
        ClampCurrentHp();
    }

    private float GetTotal(
        Func<PlayerStatValues, float> selector
    )
    {
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
                total += selector(modifier);
            }
        }

        return total;
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
}

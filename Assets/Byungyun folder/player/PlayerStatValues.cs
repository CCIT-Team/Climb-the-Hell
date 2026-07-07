using System;
using UnityEngine;

/// <summary>
/// 플레이어의 기본 스탯, 특성 보너스, 득도 보너스에서
/// 공통으로 사용하는 스탯 값 묶음.
///
/// 마나 스탯은 사용하지 않는다.
/// </summary>
[Serializable]
public class PlayerStatValues
{
    [Header("전투")]

    [Tooltip("최대 체력에 더해지는 값")]
    public int maxHp;

    [Tooltip("공격력에 더해지는 값")]
    public int attack;

    [Tooltip("이동속도에 더해지는 값")]
    public float moveSpeed;

    [Header("크리티컬")]

    [Tooltip("0.1 = 치명타 확률 10%")]
    public float criticalChance;

    [Tooltip("1.5 = 치명타 피해 150%")]
    public float criticalMultiplier;

    [Header("대시")]

    [Tooltip("대시 거리에 더해지는 값")]
    public float dashDistance;

    [Tooltip("대시 지속시간에 더해지는 값")]
    public float dashDuration;

    [Tooltip(
        "대시 쿨타임에 더해지는 값입니다.\n" +
        "쿨타임 감소 효과는 음수 값을 사용하세요."
    )]
    public float dashCooldown;

    [Tooltip(
        "대시 쿨타임 회복 배율에 더해지는 값입니다.\n" +
        "0.2를 추가하면 기본 1에서 최종 1.2가 됩니다."
    )]
    public float dashCooldownRecoveryMultiplier;

    [Tooltip("기본 대시 횟수에 추가되는 횟수")]
    public int extraDashCount;

    [Tooltip("하나라도 활성화되면 대시 중 무적")]
    public bool dashInvincible;

    [Header("성장 및 편의")]

    [Tooltip("사망을 버틸 수 있는 추가 횟수")]
    public int deathResist;

    [Tooltip(
        "획득 골드 배율에 더해지는 값입니다.\n" +
        "기본 스탯은 1, 보너스는 0.1처럼 입력합니다."
    )]
    public float goldMultiplier;

    [Tooltip("보상 재선택 가능 횟수")]
    public int rerollCount;

    /// <summary>
    /// 현재 스탯 값의 복사본을 만든다.
    /// </summary>
    public PlayerStatValues Clone()
    {
        return (PlayerStatValues)MemberwiseClone();
    }

    /// <summary>
    /// 전달받은 값을 현재 값에 누적한다.
    /// </summary>
    public void Add(PlayerStatValues other)
    {
        if (other == null)
        {
            return;
        }

        maxHp += other.maxHp;
        attack += other.attack;
        moveSpeed += other.moveSpeed;

        criticalChance += other.criticalChance;
        criticalMultiplier += other.criticalMultiplier;

        dashDistance += other.dashDistance;
        dashDuration += other.dashDuration;
        dashCooldown += other.dashCooldown;

        dashCooldownRecoveryMultiplier +=
            other.dashCooldownRecoveryMultiplier;

        extraDashCount += other.extraDashCount;
        dashInvincible |= other.dashInvincible;

        deathResist += other.deathResist;
        goldMultiplier += other.goldMultiplier;
        rerollCount += other.rerollCount;
    }

    /// <summary>
    /// 전달받은 값을 현재 값에서 제거한다.
    /// </summary>
    public void Subtract(PlayerStatValues other)
    {
        if (other == null)
        {
            return;
        }

        maxHp -= other.maxHp;
        attack -= other.attack;
        moveSpeed -= other.moveSpeed;

        criticalChance -= other.criticalChance;
        criticalMultiplier -= other.criticalMultiplier;

        dashDistance -= other.dashDistance;
        dashDuration -= other.dashDuration;
        dashCooldown -= other.dashCooldown;

        dashCooldownRecoveryMultiplier -=
            other.dashCooldownRecoveryMultiplier;

        extraDashCount -= other.extraDashCount;

        deathResist -= other.deathResist;
        goldMultiplier -= other.goldMultiplier;
        rerollCount -= other.rerollCount;

        /*
         * dashInvincible은 여러 효과가 동시에 true를 제공할 수 있으므로
         * 단순 제거만으로 false 처리하지 않는다.
         * 전체 초기화 시 Clear()를 사용한다.
         */
    }

    /// <summary>
    /// 모든 보너스 값을 초기화한다.
    /// </summary>
    public void Clear()
    {
        maxHp = 0;
        attack = 0;
        moveSpeed = 0f;

        criticalChance = 0f;
        criticalMultiplier = 0f;

        dashDistance = 0f;
        dashDuration = 0f;
        dashCooldown = 0f;
        dashCooldownRecoveryMultiplier = 0f;

        extraDashCount = 0;
        dashInvincible = false;

        deathResist = 0;
        goldMultiplier = 0f;
        rerollCount = 0;
    }

    /// <summary>
    /// 플레이어 기본 스탯 초기값을 생성한다.
    /// </summary>
    public static PlayerStatValues CreateDefaultBaseStats()
    {
        return new PlayerStatValues
        {
            maxHp = 100,
            attack = 10,
            moveSpeed = 5f,

            criticalChance = 0.1f,
            criticalMultiplier = 1.5f,

            dashDistance = 3f,
            dashDuration = 0.15f,
            dashCooldown = 1f,
            dashCooldownRecoveryMultiplier = 1f,

            extraDashCount = 0,
            dashInvincible = true,

            deathResist = 0,
            goldMultiplier = 1f,
            rerollCount = 0
        };
    }
}

using System;
using UnityEngine;

[Serializable]
public class PlayerStatValues
{
    [Header("전투")]
    public int maxHp;
    public int attack;
    public float moveSpeed;
    public int mana;

    [Header("크리티컬")]
    public float criticalChance;
    public float criticalMultiplier;

    [Header("대시")]
    public float dashDistance;
    public float dashDuration;
    public float dashCooldown;
    public float dashCooldownRecoveryMultiplier;
    public int extraDashCount;
    public bool dashInvincible;

    public PlayerStatValues Clone()
    {
        return (PlayerStatValues)MemberwiseClone();
    }

    public void Add(PlayerStatValues other)
    {
        if (other == null)
        {
            return;
        }

        maxHp += other.maxHp;
        attack += other.attack;
        moveSpeed += other.moveSpeed;
        mana += other.mana;

        criticalChance += other.criticalChance;
        criticalMultiplier += other.criticalMultiplier;

        dashDistance += other.dashDistance;
        dashDuration += other.dashDuration;
        dashCooldown += other.dashCooldown;
        dashCooldownRecoveryMultiplier +=
            other.dashCooldownRecoveryMultiplier;
        extraDashCount += other.extraDashCount;

        dashInvincible |= other.dashInvincible;
    }

    public void Subtract(PlayerStatValues other)
    {
        if (other == null)
        {
            return;
        }

        maxHp -= other.maxHp;
        attack -= other.attack;
        moveSpeed -= other.moveSpeed;
        mana -= other.mana;

        criticalChance -= other.criticalChance;
        criticalMultiplier -= other.criticalMultiplier;

        dashDistance -= other.dashDistance;
        dashDuration -= other.dashDuration;
        dashCooldown -= other.dashCooldown;
        dashCooldownRecoveryMultiplier -=
            other.dashCooldownRecoveryMultiplier;
        extraDashCount -= other.extraDashCount;

        /*
         * bool은 단순 뺄셈이 불가능하다.
         * 최종 dashInvincible은 PlayerStats에서
         * 모든 출처를 OR 연산해서 계산한다.
         */
    }

    public static PlayerStatValues CreateDefaultBaseStats()
    {
        return new PlayerStatValues
        {
            maxHp = 100,
            attack = 10,
            moveSpeed = 5f,
            mana = 50,
            criticalChance = 0.1f,
            criticalMultiplier = 1.5f,

            dashDistance = 3f,
            dashDuration = 0.15f,
            dashCooldown = 1f,
            dashCooldownRecoveryMultiplier = 1f,
            extraDashCount = 0,
            dashInvincible = true
        };
    }
}

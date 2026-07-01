using System;
using UnityEngine;

/// <summary>
/// 대시 관련 추가 효과.
/// PlayerStats에 더할 대시 스탯도 함께 보관한다.
/// </summary>
[Serializable]
public class DashBoonEffectData
{
    [Header("사용 여부")]
    public bool enabled;

    [Header("대시 스탯 추가")]
    [Tooltip("대시 거리, 쿨타임, 횟수 등에 더할 값")]
    public PlayerStatValues statBonus =
        new PlayerStatValues();
}

/// <summary>
/// 공격 계열 득도의 특수 효과.
/// </summary>
[Serializable]
public class AttackBoonEffectData
{
    [Header("대시 피해")]

    [Tooltip("대시 중 적에게 피해를 주는지 여부")]
    public bool enableDashDamage;

    [Min(0f)]
    [Tooltip("대시 충돌 시 입힐 기본 피해")]
    public float dashDamage;

    [Header("공격속도")]

    [Min(0f)]
    [Tooltip("0.2면 공격속도 20% 증가")]
    public float attackSpeedIncreaseRate;

    [Header("경직")]

    [Min(0f)]
    [Tooltip("공격 적중 시 추가되는 경직 시간")]
    public float additionalStaggerDuration;
}

/// <summary>
/// 수비 계열 효과.
/// </summary>
[Serializable]
public class DefenseBoonEffectData
{
    [Header("받는 피해 감소")]

    [Range(0f, 0.95f)]
    [Tooltip("0.2면 받는 피해 20% 감소")]
    public float damageReductionRate;
}

/// <summary>
/// 적 전체 능력치를 감소시키는 효과.
/// 플레이어가 득도를 보유한 동안 적용하는 용도.
/// </summary>
[Serializable]
public class EnemyStatReductionEffectData
{
    [Header("사용 여부")]
    public bool enabled;

    [Range(0f, 0.95f)]
    [Tooltip("0.2면 적 이동속도 20% 감소")]
    public float moveSpeedReductionRate;

    [Range(0f, 0.95f)]
    [Tooltip("0.2면 적 공격력 20% 감소")]
    public float attackReductionRate;

    [Range(0f, 0.95f)]
    [Tooltip("0.2면 적 최대 체력 20% 감소")]
    public float maxHpReductionRate;
}

/// <summary>
/// 공격 적중 시 적에게 적용되는 디버프.
/// </summary>
[Serializable]
public class DebuffEffectData
{
    [Header("사용 여부")]
    public bool enabled;

    [Header("발동 행동")]
    [Tooltip("일반 공격, 대시, 특수 공격 중 어떤 행동으로 부여되는지 설정")]
    public BoonTriggerType triggerType =
        BoonTriggerType.NormalAttack;

    [Header("디버프 종류")]
    public DebuffType type =
        DebuffType.None;

    [Header("공통 설정")]

    [Min(0f)]
    [Tooltip("디버프 지속시간")]
    public float duration = 3f;

    [Range(0f, 1f)]
    [Tooltip("디버프 발동 확률. 1이면 100%")]
    public float applyChance = 1f;

    [Min(1)]
    [Tooltip("최대 중첩 수")]
    public int maxStack = 1;

    [Header("도트딜 설정")]

    [Min(0f)]
    [Tooltip("틱 한 번당 피해")]
    public float damagePerTick;

    [Min(0.01f)]
    [Tooltip("도트 피해가 발생하는 간격")]
    public float tickInterval = 1f;

    [Header("감소 효과")]

    [Range(0f, 0.95f)]
    [Tooltip("이동속도 감소율")]
    public float moveSpeedReductionRate;

    [Range(0f, 0.95f)]
    [Tooltip("공격력 감소율")]
    public float attackReductionRate;

    [Range(0f, 0.95f)]
    [Tooltip("최대 체력 감소율")]
    public float maxHpReductionRate;

    [Header("기절")]

    [Min(0f)]
    [Tooltip("기절 지속시간")]
    public float stunDuration;
}

/// <summary>
/// 공격 반사 효과.
/// </summary>
[Serializable]
public class ReflectEffectData
{
    [Header("사용 여부")]
    public bool enabled;

    [Header("발동 조건")]
    [Tooltip("일반 공격, 대시, 특수 공격 중 반사 판정이 열리는 행동")]
    public BoonTriggerType triggerType =
        BoonTriggerType.None;

    [Range(0f, 1f)]
    [Tooltip("반사 성공 확률. 1이면 100%")]
    public float reflectChance = 1f;

    [Min(0f)]
    [Tooltip("원래 공격력에 곱할 반사 피해 배율")]
    public float reflectedDamageMultiplier = 1f;

    [Min(0f)]
    [Tooltip("행동 후 반사 판정이 유지되는 시간")]
    public float activeDuration = 0.25f;
}

/// <summary>
/// 작두 타기 효과.
/// 좋은 효과와 나쁜 효과를 동시에 보유한다.
/// </summary>
[Serializable]
public class JakduRideEffectData
{
    [Header("사용 여부")]
    public bool enabled;

    [Header("버프")]
    public PlayerStatValues buffStats =
        new PlayerStatValues();

    [Header("디버프")]
    public PlayerStatValues debuffStats =
        new PlayerStatValues();

    [Min(0f)]
    [Tooltip("효과 지속시간")]
    public float duration = 5f;

    [Tooltip("체크하면 시간 제한 없이 유지")]
    public bool permanent;
}
using System;
using UnityEngine;

/// <summary>
/// 득도 등급.
/// </summary>
public enum BoonGrade
{
    Normal,
    Legendary,
    Duo
}

/// <summary>
/// 득도 계열.
/// </summary>
public enum BoonCategory
{
    None,
    Attack,
    Defense,
    Mobility,
    Debuff,
    Jakdu
}

/// <summary>
/// 디버프 종류.
/// </summary>
public enum DebuffType
{
    None,
    DamageOverTime,
    MoveSpeedDown,
    AttackDown,
    MaxHpDown,
    Stun
}

/// <summary>
/// 효과가 발동되는 행동.
/// </summary>
public enum BoonTriggerType
{
    None,

    // 일반 공격
    NormalAttack,

    // 대시
    Dash,

    // 특수 공격
    SpecialAttack
}

/// <summary>
/// 전설·듀오 득도의 분류 조건.
/// 예: 공격 득도 3개 이상 보유.
/// </summary>
[Serializable]
public class BoonRequirement
{
    [Tooltip("필요한 득도 분류")]
    public BoonCategory category =
        BoonCategory.None;

    [Min(1)]
    [Tooltip("해당 분류의 최소 보유 개수")]
    public int requiredCount = 1;
}

/// <summary>
/// 특정 득도를 반드시 보유해야 하는 조건.
/// </summary>
[Serializable]
public class RequiredBoonId
{
    [Tooltip("필수 보유 득도의 고유 ID")]
    public string boonId;
}
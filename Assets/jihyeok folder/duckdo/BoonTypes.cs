using System;
using UnityEngine;

public enum BoonGrade
{
    Normal,
    Legendary,
    Duo
}

public enum BoonCategory
{
    None,
    Attack,
    Defense,
    Mobility,
    Debuff
}

public enum DebuffType
{
    None,
    DamageOverTime,
    MoveSpeedDown,
    AttackDown,
    MaxHpDown,
    Stun
}

[Serializable]
public class BoonRequirement
{
    [Tooltip("필요한 득도 분류")]
    public BoonCategory category;

    [Min(1)]
    public int requiredCount = 1;
}

[Serializable]
public class RequiredBoonId
{
    [Tooltip("반드시 보유해야 하는 득도 ID")]
    public string boonId;
}
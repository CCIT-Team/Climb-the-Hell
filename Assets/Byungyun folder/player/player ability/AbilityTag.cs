public enum AbilityCategory
{
    Attack,     // 공격
    Defense,    // 수비
    Mobility,   // 기동
    Debuff      // 디버프
}

public enum AbilityTag
{
    // 공격
    Critical,
    DoubleAttack,
    DashAttack,

    // 수비
    AttackReflect,
    DashReflect,
    DamageReduction,
    Invincible,

    // 기동
    AttackSpeed,
    MoveSpeed,
    DashDistance,
    Dodge,

    // 디버프
    MoveSpeedDown,
    DamageOverTime,
    AttackDown,
    HealthDown,
    Stun
}
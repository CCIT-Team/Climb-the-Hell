using System;
using UnityEngine;

[Serializable]
public class DashBoonEffectData
{
    public bool enabled;

    [Tooltip("PlayerStats의 득도 추가 스탯에 더할 값")]
    public PlayerStatValues statBonus =
        new PlayerStatValues();
}

[Serializable]
public class EnemyStatReductionEffectData
{
    public bool enabled;

    [Range(0f, 1f)]
    public float moveSpeedReductionRate;

    [Range(0f, 1f)]
    public float attackReductionRate;

    [Range(0f, 1f)]
    public float maxHpReductionRate;
}

[Serializable]
public class DebuffEffectData
{
    public bool enabled;

    public DebuffType debuffType;

    [Min(0f)]
    public float value;

    [Min(0f)]
    public float duration;

    [Min(0.01f)]
    public float tickInterval = 1f;

    [Min(1)]
    public int maxStack = 1;
}

[Serializable]
public class ReflectEffectData
{
    public bool enabled;

    [Range(0f, 1f)]
    public float reflectChance = 1f;

    [Min(0f)]
    public float reflectedDamageMultiplier = 1f;

    [Min(0f)]
    public float activeDuration = 0.25f;
}

[Serializable]
public class JakduRideEffectData
{
    public bool enabled;

    [Header("버프")]
    public PlayerStatValues buffStats =
        new PlayerStatValues();

    [Header("디버프")]
    [Tooltip("감소시킬 값은 음수로 입력합니다.")]
    public PlayerStatValues debuffStats =
        new PlayerStatValues();

    [Header("지속시간")]
    [Min(0f)]
    public float duration = 10f;

    [Tooltip("켜면 지속시간 없이 런이 끝날 때까지 유지")]
    public bool permanent;
}

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewBoon",
    menuName = "Game/Boon/Boon Data"
)]
public class BoonData : ScriptableObject
{
    [Header("기본 정보")]
    public string boonId;
    public string displayName;

    [TextArea]
    public string description;

    public Sprite icon;

    public BoonGrade grade =
        BoonGrade.Normal;

    public BoonCategory category =
        BoonCategory.None;

    [Header("중첩")]
    public bool stackable = true;

    [Min(1)]
    public int maxStack = 1;

    [Header("전설/듀오 등장 조건")]
    [Tooltip("분류별 최소 보유 개수")]
    public List<BoonRequirement>
        categoryRequirements =
            new List<BoonRequirement>();

    [Tooltip("필수 보유 득도 ID")]
    public List<RequiredBoonId>
        requiredBoonIds =
            new List<RequiredBoonId>();

    [Header("즉시 스탯")]
    public PlayerStatValues instantStatBonus =
        new PlayerStatValues();

    [Header("대시 추가")]
    public DashBoonEffectData dashEffect =
        new DashBoonEffectData();

    [Header("적 이동/공격/체력 감소")]
    public EnemyStatReductionEffectData
        enemyReductionEffect =
            new EnemyStatReductionEffectData();

    [Header("디버프")]
    public DebuffEffectData debuffEffect =
        new DebuffEffectData();

    [Header("반사")]
    public ReflectEffectData reflectEffect =
        new ReflectEffectData();

    [Header("작두 타기")]
    public JakduRideEffectData jakduRideEffect =
        new JakduRideEffectData();
}

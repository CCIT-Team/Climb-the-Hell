using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewBoon",
    menuName = "Game/Boon/Boon Data"
)]
public class BoonData : ScriptableObject
{
    [Header("기본 정보")]

    [Tooltip("다른 득도와 겹치지 않는 고유 ID")]
    public string boonId;

    public string displayName;

    [TextArea(2, 5)]
    public string description;

    [Tooltip("보유 목록과 보상 선택 UI에서 사용하는 이미지")]
    public Sprite icon;

    [Header("보상 선택 UI")]

    [Tooltip("비워두면 Display Name 사용")]
    public string rewardTitle;

    [TextArea(2, 5)]
    [Tooltip("비워두면 Description 사용")]
    public string rewardDescription;

    [Header("등급 및 분류")]

    public BoonGrade grade =
        BoonGrade.Normal;

    public BoonCategory category =
        BoonCategory.None;

    [Header("중첩")]

    public bool stackable = true;

    [Min(1)]
    public int maxStack = 1;

    [Header("전설/듀오 등장 조건")]

    public List<BoonRequirement>
        categoryRequirements =
            new List<BoonRequirement>();

    public List<RequiredBoonId>
        requiredBoonIds =
            new List<RequiredBoonId>();

    [Header("즉시 플레이어 스탯")]

    public PlayerStatValues instantStatBonus =
        new PlayerStatValues();

    [Header("공격 특수 효과")]

    public AttackBoonEffectData attackEffect =
        new AttackBoonEffectData();

    [Header("수비 효과")]

    public DefenseBoonEffectData defenseEffect =
        new DefenseBoonEffectData();

    [Header("대시 효과")]

    public DashBoonEffectData dashEffect =
        new DashBoonEffectData();

    [Header("적 전체 능력치 감소")]

    public EnemyStatReductionEffectData
        enemyReductionEffect =
            new EnemyStatReductionEffectData();

    [Header("적중 디버프")]

    public DebuffEffectData debuffEffect =
        new DebuffEffectData();

    [Header("반사")]

    public ReflectEffectData reflectEffect =
        new ReflectEffectData();

    [Header("기존 작두 타기 효과")]

    public JakduRideEffectData jakduRideEffect =
        new JakduRideEffectData();


    public string GetRewardTitle()
    {
        if (!string.IsNullOrWhiteSpace(
                rewardTitle
            ))
        {
            return rewardTitle;
        }

        return displayName;
    }

    public string GetRewardDescription()
    {
        if (!string.IsNullOrWhiteSpace(
                rewardDescription
            ))
        {
            return rewardDescription;
        }

        return description;
    }

    /// <summary>
    /// 이전 보상 슬롯 코드 호환용.
    /// 현재는 일반 보상 설명만 반환한다.
    /// </summary>
    public string GetContextRewardDescription()
    {
        return GetRewardDescription();
    }

    /// <summary>
    /// 작두 보상 여부는 별도 타입 없이 카테고리로 판단한다.
    /// </summary>
    public bool IsJakduPoint =>
        category ==
        BoonCategory.Jakdu;

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxStack =
            Mathf.Max(
                1,
                maxStack
            );

        if (!stackable)
        {
            maxStack = 1;
        }

    }
#endif
}

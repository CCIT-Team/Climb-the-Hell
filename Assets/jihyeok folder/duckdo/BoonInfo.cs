using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class BoonInfo : MonoBehaviour
{
    [Header("플레이어")]

    [SerializeField]
    private Player player;

    [Header("현재 획득한 득도")]

    [SerializeField]
    private List<BoonData> ownedBoons =
        new List<BoonData>();

    /*
     * 득도 ID별 보유 개수.
     * 예:
     * attack_power_01 → 2개
     */
    private readonly Dictionary<string, int>
        boonStackCounts =
            new Dictionary<string, int>();

    /*
     * 계열별 보유 개수.
     * 전설·듀오 득도의 등장 조건 검사에 사용한다.
     */
    private readonly Dictionary<BoonCategory, int>
        categoryCounts =
            new Dictionary<BoonCategory, int>();

    private JakduRide jakduRide;

    public event Action<BoonData> OnBoonAdded;

    private void Awake()
    {
        InitializeReferences();

        RebuildCounts();
    }

    /// <summary>
    /// 필요한 컴포넌트 참조를 가져온다.
    /// </summary>
    private void InitializeReferences()
    {
        if (player == null)
        {
            player =
                GetComponent<Player>();
        }

        if (player == null)
        {
            Debug.LogError(
                "[BoonInfo] Player 컴포넌트를 찾을 수 없습니다.",
                this
            );

            return;
        }

        jakduRide =
            GetComponent<JakduRide>();
    }

    /// <summary>
    /// 해당 득도가 현재 보상으로 등장할 수 있는지 검사한다.
    /// </summary>
    public bool CanOffer(
        BoonData boon
    )
    {
        if (boon == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                boon.boonId
            ))
        {
            Debug.LogWarning(
                $"[BoonInfo] {boon.name}의 Boon ID가 비어 있습니다.",
                boon
            );

            return false;
        }

        int currentStack =
            GetBoonStack(
                boon.boonId
            );

        /*
         * 중첩 불가능한 득도를 이미 가지고 있으면
         * 다시 등장하지 않는다.
         */
        if (!boon.stackable &&
            currentStack > 0)
        {
            return false;
        }

        int maximumStack =
            Mathf.Max(
                1,
                boon.maxStack
            );

        if (currentStack >=
            maximumStack)
        {
            return false;
        }

        /*
         * 일반 등급은 중첩 조건만 통과하면 등장한다.
         */
        if (boon.grade ==
            BoonGrade.Normal)
        {
            return true;
        }

        /*
         * 전설과 듀오 등급은
         * 계열 개수와 필수 득도 조건도 검사한다.
         */
        return MeetsCategoryRequirements(
                   boon.categoryRequirements
               ) &&
               MeetsRequiredBoonIds(
                   boon.requiredBoonIds
               );
    }

    /// <summary>
    /// 선택한 득도를 실제 보유 목록에 추가하고 효과를 적용한다.
    /// </summary>
    public bool TryAddBoon(
        BoonData boon
    )
    {
        if (boon == null)
        {
            Debug.LogError(
                "[BoonInfo] 선택한 득도가 null입니다.",
                this
            );

            return false;
        }

        if (!CanOffer(boon))
        {
            Debug.LogWarning(
                $"[BoonInfo] 득도 획득 실패\n" +
                $"이름: {boon.displayName}\n" +
                $"현재 중첩: {GetBoonStack(boon.boonId)}\n" +
                $"최대 중첩: {boon.maxStack}",
                boon
            );

            return false;
        }

        if (!ValidatePlayer())
        {
            return false;
        }

        ownedBoons.Add(boon);

        IncrementBoonStack(
            boon.boonId
        );

        IncrementCategory(
            boon.category
        );

        /*
         * 득도에 설정된 효과를 실제 플레이어에 전달한다.
         */
        ApplyBoonEffects(boon);

        OnBoonAdded?.Invoke(boon);

        Debug.Log(
            $"[BoonInfo] 득도 획득 완료\n" +
            $"이름: {boon.displayName}\n" +
            $"ID: {boon.boonId}\n" +
            $"현재 중첩: {GetBoonStack(boon.boonId)}\n" +
            $"계열: {boon.category}\n" +
            $"계열 보유 개수: {GetCategoryCount(boon.category)}",
            this
        );

        return true;
    }

    /// <summary>
    /// 득도의 각 효과 데이터를 적용한다.
    /// </summary>
    private void ApplyBoonEffects(
        BoonData boon
    )
    {
        if (boon == null ||
            !ValidatePlayer())
        {
            return;
        }

        ApplyImmediateStats(boon);
        ApplyDashStats(boon);
        ApplyJakduRide(boon);

        /*
         * attackEffect, defenseEffect, debuffEffect,
         * reflectEffect, enemyReductionEffect는
         * 각 전투 스크립트에서 읽어 사용해야 한다.
         *
         * BoonInfo는 해당 BoonData를 ownedBoons에 보관한다.
         */
    }

    /// <summary>
    /// 공격력, 최대 체력, 이동속도 등
    /// 기본 추가 스탯을 PlayerStats에 전달한다.
    /// </summary>
    private void ApplyImmediateStats(
        BoonData boon
    )
    {
        if (boon.instantStatBonus == null)
        {
            Debug.LogWarning(
                $"[BoonInfo] {boon.displayName}의 " +
                "Instant Stat Bonus가 null입니다.",
                boon
            );

            return;
        }

        PlayerStatValues bonus =
            boon.instantStatBonus;

        Debug.Log(
            $"[BoonInfo] 즉시 스탯 전달\n" +
            $"득도: {boon.displayName}\n" +
            $"최대 체력: {bonus.maxHp}\n" +
            $"공격력: {bonus.attack}\n" +
            $"이동속도: {bonus.moveSpeed}\n" +
            $"크리티컬 확률: {bonus.criticalChance}\n" +
            $"크리티컬 배율: {bonus.criticalMultiplier}",
            boon
        );

        player.stats.AddBoonBonus(
            bonus
        );
    }

    /// <summary>
    /// 대시 효과 안에 설정한 추가 스탯을 전달한다.
    /// </summary>
    private void ApplyDashStats(
        BoonData boon
    )
    {
        if (boon.dashEffect == null)
        {
            return;
        }

        if (!boon.dashEffect.enabled)
        {
            return;
        }

        if (boon.dashEffect.statBonus == null)
        {
            Debug.LogWarning(
                $"[BoonInfo] {boon.displayName}의 " +
                "Dash Stat Bonus가 null입니다.",
                boon
            );

            return;
        }

        PlayerStatValues dashBonus =
            boon.dashEffect.statBonus;

        Debug.Log(
            $"[BoonInfo] 대시 스탯 전달\n" +
            $"득도: {boon.displayName}\n" +
            $"대시 거리: {dashBonus.dashDistance}\n" +
            $"대시 지속시간: {dashBonus.dashDuration}\n" +
            $"대시 쿨타임: {dashBonus.dashCooldown}\n" +
            $"쿨타임 회복 배율: " +
            $"{dashBonus.dashCooldownRecoveryMultiplier}\n" +
            $"추가 대시 횟수: {dashBonus.extraDashCount}\n" +
            $"대시 무적: {dashBonus.dashInvincible}",
            boon
        );

        player.stats.AddBoonBonus(
            dashBonus
        );
    }

    /// <summary>
    /// 작두 타기 효과를 JakduRide에 전달한다.
    /// </summary>
    private void ApplyJakduRide(
        BoonData boon
    )
    {
        if (boon.jakduRideEffect == null)
        {
            return;
        }

        if (!boon.jakduRideEffect.enabled)
        {
            return;
        }

        if (jakduRide == null)
        {
            Debug.LogError(
                $"[BoonInfo] {boon.displayName}에 " +
                "작두 타기 효과가 설정되어 있지만 " +
                "Player에 JakduRide 컴포넌트가 없습니다.",
                player
            );

            return;
        }

        jakduRide.Apply(
            boon.jakduRideEffect
        );
    }

    /// <summary>
    /// 전설 득도가 현재 등장 가능한지 검사한다.
    /// </summary>
    public bool CanOfferLegendary(
        BoonData boon
    )
    {
        return boon != null &&
               boon.grade ==
                   BoonGrade.Legendary &&
               CanOffer(boon);
    }

    /// <summary>
    /// 듀오 득도가 현재 등장 가능한지 검사한다.
    /// </summary>
    public bool CanOfferDuo(
        BoonData boon
    )
    {
        return boon != null &&
               boon.grade ==
                   BoonGrade.Duo &&
               CanOffer(boon);
    }

    /// <summary>
    /// 특정 계열의 현재 보유 개수를 반환한다.
    /// </summary>
    public int GetCategoryCount(
        BoonCategory category
    )
    {
        return categoryCounts.TryGetValue(
            category,
            out int count
        )
            ? count
            : 0;
    }

    /// <summary>
    /// 특정 ID 득도의 현재 중첩 수를 반환한다.
    /// </summary>
    public int GetBoonStack(
        string boonId
    )
    {
        if (string.IsNullOrWhiteSpace(
                boonId
            ))
        {
            return 0;
        }

        return boonStackCounts.TryGetValue(
            boonId,
            out int count
        )
            ? count
            : 0;
    }

    /// <summary>
    /// 특정 ID의 득도를 가지고 있는지 확인한다.
    /// </summary>
    public bool HasBoon(
        string boonId
    )
    {
        return GetBoonStack(
                   boonId
               ) > 0;
    }

    /// <summary>
    /// 현재 보유한 득도 목록을 읽기 전용으로 반환한다.
    /// </summary>
    public IReadOnlyList<BoonData>
        GetOwnedBoons()
    {
        return ownedBoons;
    }

    /// <summary>
    /// 특정 트리거를 사용하는 디버프 득도를 반환한다.
    /// 공격 및 대시 코드에서 사용할 수 있다.
    /// </summary>
    public List<BoonData> GetDebuffBoons(
        BoonTriggerType triggerType
    )
    {
        List<BoonData> results =
            new List<BoonData>();

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            if (boon == null ||
                boon.debuffEffect == null ||
                !boon.debuffEffect.enabled)
            {
                continue;
            }

            if (boon.debuffEffect.triggerType ==
                triggerType)
            {
                results.Add(boon);
            }
        }

        return results;
    }

    /// <summary>
    /// 특정 트리거를 사용하는 반사 득도를 반환한다.
    /// </summary>
    public List<BoonData> GetReflectBoons(
        BoonTriggerType triggerType
    )
    {
        List<BoonData> results =
            new List<BoonData>();

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            if (boon == null ||
                boon.reflectEffect == null ||
                !boon.reflectEffect.enabled)
            {
                continue;
            }

            if (boon.reflectEffect.triggerType ==
                triggerType)
            {
                results.Add(boon);
            }
        }

        return results;
    }

    /// <summary>
    /// 현재 보유한 모든 피해 감소율을 합산한다.
    /// 최댓값은 95%로 제한한다.
    /// </summary>
    public float GetTotalDamageReductionRate()
    {
        float totalRate = 0f;

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            if (boon == null ||
                boon.defenseEffect == null)
            {
                continue;
            }

            totalRate +=
                boon.defenseEffect
                    .damageReductionRate;
        }

        return Mathf.Clamp(
            totalRate,
            0f,
            0.95f
        );
    }

    /// <summary>
    /// 현재 보유한 공격속도 증가율을 합산한다.
    /// </summary>
    public float GetTotalAttackSpeedIncreaseRate()
    {
        float totalRate = 0f;

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            if (boon == null ||
                boon.attackEffect == null)
            {
                continue;
            }

            totalRate +=
                boon.attackEffect
                    .attackSpeedIncreaseRate;
        }

        return Mathf.Max(
            0f,
            totalRate
        );
    }

    /// <summary>
    /// 현재 보유한 추가 경직 시간을 합산한다.
    /// </summary>
    public float GetTotalAdditionalStaggerDuration()
    {
        float totalDuration = 0f;

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            if (boon == null ||
                boon.attackEffect == null)
            {
                continue;
            }

            totalDuration +=
                boon.attackEffect
                    .additionalStaggerDuration;
        }

        return Mathf.Max(
            0f,
            totalDuration
        );
    }

    /// <summary>
    /// 대시 피해가 활성화된 모든 득도의 피해를 합산한다.
    /// </summary>
    public float GetTotalDashDamage()
    {
        float totalDamage = 0f;

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            if (boon == null ||
                boon.attackEffect == null ||
                !boon.attackEffect
                    .enableDashDamage)
            {
                continue;
            }

            totalDamage +=
                boon.attackEffect
                    .dashDamage;
        }

        return Mathf.Max(
            0f,
            totalDamage
        );
    }

    /// <summary>
    /// 전설·듀오의 계열 조건을 검사한다.
    /// </summary>
    private bool MeetsCategoryRequirements(
        List<BoonRequirement> requirements
    )
    {
        if (requirements == null ||
            requirements.Count == 0)
        {
            return true;
        }

        for (int i = 0;
             i < requirements.Count;
             i++)
        {
            BoonRequirement requirement =
                requirements[i];

            if (requirement == null)
            {
                continue;
            }

            int requiredCount =
                Mathf.Max(
                    1,
                    requirement.requiredCount
                );

            int ownedCount =
                GetCategoryCount(
                    requirement.category
                );

            if (ownedCount <
                requiredCount)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 전설·듀오의 필수 득도 ID 조건을 검사한다.
    /// </summary>
    private bool MeetsRequiredBoonIds(
        List<RequiredBoonId> requirements
    )
    {
        if (requirements == null ||
            requirements.Count == 0)
        {
            return true;
        }

        for (int i = 0;
             i < requirements.Count;
             i++)
        {
            RequiredBoonId requirement =
                requirements[i];

            if (requirement == null ||
                string.IsNullOrWhiteSpace(
                    requirement.boonId
                ))
            {
                continue;
            }

            if (!HasBoon(
                    requirement.boonId
                ))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 인스펙터의 보유 목록을 기반으로
    /// 중첩 수와 계열 개수를 다시 만든다.
    /// </summary>
    private void RebuildCounts()
    {
        boonStackCounts.Clear();
        categoryCounts.Clear();

        if (ownedBoons == null)
        {
            ownedBoons =
                new List<BoonData>();

            return;
        }

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            if (boon == null)
            {
                continue;
            }

            IncrementBoonStack(
                boon.boonId
            );

            IncrementCategory(
                boon.category
            );
        }
    }

    private void IncrementBoonStack(
        string boonId
    )
    {
        if (string.IsNullOrWhiteSpace(
                boonId
            ))
        {
            return;
        }

        boonStackCounts[boonId] =
            GetBoonStack(
                boonId
            ) + 1;
    }

    private void IncrementCategory(
        BoonCategory category
    )
    {
        categoryCounts[category] =
            GetCategoryCount(
                category
            ) + 1;
    }

    /// <summary>
    /// Player와 PlayerStats 참조가 존재하는지 확인한다.
    /// </summary>
    private bool ValidatePlayer()
    {
        if (player == null)
        {
            player =
                GetComponent<Player>();
        }

        if (player == null)
        {
            Debug.LogError(
                "[BoonInfo] Player가 없습니다.",
                this
            );

            return false;
        }

        if (player.stats == null)
        {
            Debug.LogError(
                "[BoonInfo] Player.stats가 null입니다.",
                player
            );

            return false;
        }

        return true;
    }
}
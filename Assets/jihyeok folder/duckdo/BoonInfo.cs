using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class BoonInfo : MonoBehaviour
{
    [Header("현재 보유 득도")]
    [SerializeField]
    private List<BoonData> ownedBoons =
        new List<BoonData>();

    private Player player;
    private PlayerStats stats;
    private JakduRide jakduRide;

    /*
     * Boon ID별 중첩 횟수.
     * 매번 리스트 전체를 검색하지 않도록 Dictionary로 관리한다.
     */
    private readonly Dictionary<string, int>
        boonStackCounts =
            new Dictionary<string, int>();

    /*
     * 분류별 보유 개수.
     * 전설·듀오 조건 검사에 사용한다.
     */
    private readonly Dictionary<BoonCategory, int>
        categoryCounts =
            new Dictionary<BoonCategory, int>();

    public event Action<BoonData>
        OnBoonObtained;

    public IReadOnlyList<BoonData>
        OwnedBoons =>
            ownedBoons;

    private void Awake()
    {
        player =
            GetComponent<Player>();

        if (player != null)
        {
            stats = player.stats;
        }

        jakduRide =
            GetComponent<JakduRide>();

        RebuildCaches();
    }

    /// <summary>
    /// Inspector에 미리 들어 있던 득도를 기준으로
    /// 중첩 수와 분류 개수를 다시 계산한다.
    /// </summary>
    private void RebuildCaches()
    {
        boonStackCounts.Clear();
        categoryCounts.Clear();

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

            AddToCaches(boon);
        }
    }

    /// <summary>
    /// 현재 조건에서 보상 후보로 등장할 수 있는지 검사한다.
    /// </summary>
    public bool CanOffer(
        BoonData boon
    )
    {
        if (boon == null)
        {
            return false;
        }

        // 중첩 제한 검사
        int currentStack =
            GetBoonStackCount(
                boon.boonId
            );

        if (!boon.stackable &&
            currentStack > 0)
        {
            return false;
        }

        if (currentStack >=
            Mathf.Max(1, boon.maxStack))
        {
            return false;
        }

        // 분류별 요구 개수 검사
        if (!MeetsCategoryRequirements(
                boon
            ))
        {
            return false;
        }

        // 필수 선행 득도 검사
        if (!MeetsRequiredBoonIds(
                boon
            ))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 득도를 실제로 획득하고 효과를 적용한다.
    /// </summary>
    public bool TryAddBoon(
        BoonData boon
    )
    {
        if (!CanOffer(boon))
        {
            return false;
        }

        ownedBoons.Add(boon);
        AddToCaches(boon);

        ApplyImmediateEffects(boon);

        OnBoonObtained?.Invoke(boon);

        Debug.Log(
            $"[BoonInfo] {boon.displayName} 획득 " +
            $"({GetBoonStackCount(boon.boonId)}중첩)",
            this
        );

        return true;
    }

    /// <summary>
    /// 획득 즉시 적용할 플레이어 효과를 처리한다.
    /// </summary>
    private void ApplyImmediateEffects(
        BoonData boon
    )
    {
        if (stats != null)
        {
            // 공격력, 이동속도, 치명타 등의 즉시 스탯
            if (boon.instantStatBonus != null)
            {
                stats.AddBoonBonus(
                    boon.instantStatBonus
                );
            }

            // 대시 거리, 횟수, 쿨타임 등의 스탯
            if (boon.dashEffect != null &&
                boon.dashEffect.enabled &&
                boon.dashEffect.statBonus != null)
            {
                stats.AddBoonBonus(
                    boon.dashEffect.statBonus
                );
            }
        }

        // 작두 타기 효과는 별도 실행 클래스에 전달
        if (boon.jakduRideEffect != null &&
            boon.jakduRideEffect.enabled)
        {
            if (jakduRide == null)
            {
                jakduRide =
                    GetComponent<JakduRide>();
            }

            if (jakduRide != null)
            {
                jakduRide.Apply(
                    boon.jakduRideEffect
                );
            }
        }
    }

    private void AddToCaches(
        BoonData boon
    )
    {
        if (!string.IsNullOrWhiteSpace(
                boon.boonId
            ))
        {
            boonStackCounts.TryGetValue(
                boon.boonId,
                out int currentStack
            );

            boonStackCounts[boon.boonId] =
                currentStack + 1;
        }

        categoryCounts.TryGetValue(
            boon.category,
            out int categoryCount
        );

        categoryCounts[boon.category] =
            categoryCount + 1;
    }

    private bool MeetsCategoryRequirements(
        BoonData boon
    )
    {
        if (boon.categoryRequirements ==
            null)
        {
            return true;
        }

        for (int i = 0;
             i < boon.categoryRequirements.Count;
             i++)
        {
            BoonRequirement requirement =
                boon.categoryRequirements[i];

            if (requirement == null)
            {
                continue;
            }

            int ownedCount =
                GetCategoryCount(
                    requirement.category
                );

            if (ownedCount <
                requirement.requiredCount)
            {
                return false;
            }
        }

        return true;
    }

    private bool MeetsRequiredBoonIds(
        BoonData boon
    )
    {
        if (boon.requiredBoonIds ==
            null)
        {
            return true;
        }

        for (int i = 0;
             i < boon.requiredBoonIds.Count;
             i++)
        {
            RequiredBoonId requirement =
                boon.requiredBoonIds[i];

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

    public bool HasBoon(
        string boonId
    )
    {
        return GetBoonStackCount(
            boonId
        ) > 0;
    }

    public int GetBoonStackCount(
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

    // =========================================================
    // 공격 계열 조회
    // =========================================================

    /// <summary>
    /// 모든 득도의 공격속도 증가율을 합산한다.
    /// </summary>
    public float GetAttackSpeedIncreaseRate()
    {
        float total = 0f;

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            if (boon?.attackEffect == null)
            {
                continue;
            }

            total +=
                boon.attackEffect
                    .attackSpeedIncreaseRate;
        }

        return Mathf.Max(0f, total);
    }

    /// <summary>
    /// 공격속도 계산에 사용할 최종 배율.
    /// 20% 증가라면 1.2를 반환한다.
    /// </summary>
    public float GetAttackSpeedMultiplier()
    {
        return 1f +
               GetAttackSpeedIncreaseRate();
    }

    /// <summary>
    /// 공격 적중 시 추가 경직 시간을 합산한다.
    /// </summary>
    public float GetAdditionalStaggerDuration()
    {
        float total = 0f;

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            if (boon?.attackEffect == null)
            {
                continue;
            }

            total +=
                boon.attackEffect
                    .additionalStaggerDuration;
        }

        return Mathf.Max(0f, total);
    }

    /// <summary>
    /// 대시 충돌 시 입힐 총 피해를 반환한다.
    /// </summary>
    public float GetDashDamage()
    {
        float total = 0f;

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            if (boon?.attackEffect == null ||
                !boon.attackEffect
                    .enableDashDamage)
            {
                continue;
            }

            total +=
                boon.attackEffect
                    .dashDamage;
        }

        return Mathf.Max(0f, total);
    }

    // =========================================================
    // 수비 계열 조회
    // =========================================================

    /// <summary>
    /// 받는 피해 감소율을 모두 합산한다.
    /// 최대 95%까지만 허용한다.
    /// </summary>
    public float GetDamageReductionRate()
    {
        float total = 0f;

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            if (boon?.defenseEffect ==
                null)
            {
                continue;
            }

            total +=
                boon.defenseEffect
                    .damageReductionRate;
        }

        return Mathf.Clamp(
            total,
            0f,
            0.95f
        );
    }

    /// <summary>
    /// 피해 감소를 반영한 최종 피해량을 반환한다.
    /// </summary>
    public float CalculateReceivedDamage(
        float originalDamage
    )
    {
        float reductionRate =
            GetDamageReductionRate();

        return Mathf.Max(
            0f,
            originalDamage *
            (1f - reductionRate)
        );
    }

    // =========================================================
    // 반사 조회
    // =========================================================

    /// <summary>
    /// 특정 행동에서 발동하는 반사 득도들을 반환한다.
    /// 새 리스트 생성을 피하기 위해 결과 리스트를 외부에서 전달받는다.
    /// </summary>
    public void GetReflectEffects(
        BoonTriggerType triggerType,
        List<ReflectEffectData> results
    )
    {
        if (results == null)
        {
            return;
        }

        results.Clear();

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            ReflectEffectData effect =
                boon?.reflectEffect;

            if (effect == null ||
                !effect.enabled)
            {
                continue;
            }

            if (effect.triggerType !=
                triggerType)
            {
                continue;
            }

            results.Add(effect);
        }
    }

    /// <summary>
    /// 특정 행동에 반사 효과가 하나라도 있는지 검사한다.
    /// </summary>
    public bool HasReflectEffect(
        BoonTriggerType triggerType
    )
    {
        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            ReflectEffectData effect =
                ownedBoons[i]
                    ?.reflectEffect;

            if (effect != null &&
                effect.enabled &&
                effect.triggerType ==
                triggerType)
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // 디버프 조회
    // =========================================================

    /// <summary>
    /// 특정 행동으로 부여되는 디버프들을 가져온다.
    /// </summary>
    public void GetDebuffEffects(
        BoonTriggerType triggerType,
        List<DebuffEffectData> results
    )
    {
        if (results == null)
        {
            return;
        }

        results.Clear();

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            DebuffEffectData effect =
                ownedBoons[i]
                    ?.debuffEffect;

            if (effect == null ||
                !effect.enabled)
            {
                continue;
            }

            if (effect.triggerType !=
                triggerType)
            {
                continue;
            }

            results.Add(effect);
        }
    }

    // =========================================================
    // 적 전체 능력치 감소 조회
    // =========================================================

    public float GetEnemyMoveSpeedReductionRate()
    {
        float total = 0f;

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            EnemyStatReductionEffectData effect =
                ownedBoons[i]
                    ?.enemyReductionEffect;

            if (effect == null ||
                !effect.enabled)
            {
                continue;
            }

            total +=
                effect.moveSpeedReductionRate;
        }

        return Mathf.Clamp(
            total,
            0f,
            0.95f
        );
    }

    public float GetEnemyAttackReductionRate()
    {
        float total = 0f;

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            EnemyStatReductionEffectData effect =
                ownedBoons[i]
                    ?.enemyReductionEffect;

            if (effect == null ||
                !effect.enabled)
            {
                continue;
            }

            total +=
                effect.attackReductionRate;
        }

        return Mathf.Clamp(
            total,
            0f,
            0.95f
        );
    }

    public float GetEnemyMaxHpReductionRate()
    {
        float total = 0f;

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            EnemyStatReductionEffectData effect =
                ownedBoons[i]
                    ?.enemyReductionEffect;

            if (effect == null ||
                !effect.enabled)
            {
                continue;
            }

            total +=
                effect.maxHpReductionRate;
        }

        return Mathf.Clamp(
            total,
            0f,
            0.95f
        );
    }

    /// <summary>
    /// 새 런 시작 시 득도 보유 정보를 초기화한다.
    /// PlayerStats의 득도 스탯 초기화도 별도로 연결해야 한다.
    /// </summary>
    public void ClearAllBoons()
    {
        ownedBoons.Clear();
        boonStackCounts.Clear();
        categoryCounts.Clear();

        Debug.Log(
            "[BoonInfo] 모든 득도 정보 초기화",
            this
        );
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 획득한 득도와 중첩 수를 관리한다.
///
/// 득도 획득 흐름:
/// BoonRewardInteractable
/// -> TryAddBoon
/// -> ApplyImmediateEffects
/// -> PlayerStats.AddBoonBonus
/// </summary>
[RequireComponent(typeof(Player))]
public class BoonInfo : MonoBehaviour
{
    [Header("현재 보유 득도")]
    [SerializeField]
    private List<BoonData> ownedBoons =
        new List<BoonData>();

    [Header("디버그")]
    [SerializeField]
    private bool showLogs = true;

    private Player player;
    private PlayerStats stats;
    private JakduRide jakduRide;

    private readonly Dictionary<string, int>
        boonStackCounts =
            new Dictionary<string, int>();

    private readonly Dictionary<BoonCategory, int>
        categoryCounts =
            new Dictionary<BoonCategory, int>();

    public event Action<BoonData>
        OnBoonObtained;

    // 이전 코드 호환용 이벤트
    public event Action<BoonData>
        OnBoonAdded;

    public IReadOnlyList<BoonData>
        OwnedBoons =>
            ownedBoons;

    private void Awake()
    {
        ResolveReferences();
        RebuildCaches();
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            player =
                GetComponent<Player>();
        }

        if (player != null)
        {
            stats =
                player.stats;
        }

        if (jakduRide == null)
        {
            jakduRide =
                GetComponent<JakduRide>();
        }
    }

    /// <summary>
    /// 현재 조건에서 보상 후보로 등장 가능한지 검사한다.
    /// </summary>
    public bool CanOffer(
        BoonData boon)
    {
        if (boon == null ||
            string.IsNullOrWhiteSpace(
                boon.boonId
            ))
        {
            return false;
        }

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
            Mathf.Max(
                1,
                boon.maxStack
            ))
        {
            return false;
        }

        if (!MeetsCategoryRequirements(
                boon
            ))
        {
            return false;
        }

        if (!MeetsRequiredBoonIds(
                boon
            ))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 득도를 실제로 획득하고 플레이어에게 효과를 적용한다.
    /// </summary>
    public bool TryAddBoon(
        BoonData boon)
    {
        if (!CanOffer(boon))
        {
            if (showLogs && boon != null)
            {
                Debug.LogWarning(
                    $"[BoonInfo] 득도 획득 실패 / " +
                    $"이름={boon.displayName}, " +
                    $"현재 중첩={GetBoonStackCount(boon.boonId)}, " +
                    $"최대 중첩={boon.maxStack}",
                    boon
                );
            }

            return false;
        }

        ResolveReferences();

        if (player == null ||
            stats == null)
        {
            Debug.LogError(
                "[BoonInfo] Player 또는 PlayerStats를 찾지 못했습니다.",
                this
            );

            return false;
        }

        int beforeMaxHp =
            stats.MaxHp;

        int beforeAttack =
            stats.Attack;

        float beforeMoveSpeed =
            stats.MoveSpeed;

        float beforeCriticalChance =
            stats.CriticalChance;

        ownedBoons.Add(boon);
        AddToCaches(boon);

        /*
         * 이 호출에서 BoonData의 instantStatBonus와
         * dashEffect.statBonus가 PlayerStats에 들어간다.
         */
        ApplyImmediateEffects(boon);

        OnBoonObtained?.Invoke(boon);
        OnBoonAdded?.Invoke(boon);

        if (showLogs)
        {
            Debug.Log(
                $"[BoonInfo] 득도 획득 및 스탯 적용 완료\n" +
                $"득도={boon.displayName}\n" +
                $"중첩={GetBoonStackCount(boon.boonId)}\n" +
                $"최대 체력={beforeMaxHp} -> {stats.MaxHp}\n" +
                $"공격력={beforeAttack} -> {stats.Attack}\n" +
                $"이동속도={beforeMoveSpeed:0.##} -> {stats.MoveSpeed:0.##}\n" +
                $"치명타 확률={beforeCriticalChance:0.###} -> " +
                $"{stats.CriticalChance:0.###}",
                this
            );
        }

        return true;
    }

    /// <summary>
    /// 획득 즉시 적용되는 플레이어 효과를 처리한다.
    /// </summary>
    private void ApplyImmediateEffects(
        BoonData boon)
    {
        if (boon == null)
        {
            return;
        }

        ResolveReferences();

        if (stats == null)
        {
            return;
        }

        // 공격력, 최대 체력, 이동속도, 마나, 치명타
        if (boon.instantStatBonus != null)
        {
            stats.AddBoonBonus(
                boon.instantStatBonus
            );
        }

        // 대시 거리, 지속시간, 쿨타임, 대시 횟수
        if (boon.dashEffect != null &&
            boon.dashEffect.enabled &&
            boon.dashEffect.statBonus != null)
        {
            stats.AddBoonBonus(
                boon.dashEffect.statBonus
            );
        }

        // 작두 타기 효과는 별도 실행 컴포넌트에서 관리
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
        BoonData boon)
    {
        if (boon == null)
        {
            return;
        }

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

    private void RebuildCaches()
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

            if (boon != null)
            {
                AddToCaches(boon);
            }
        }
    }

    private bool MeetsCategoryRequirements(
        BoonData boon)
    {
        if (boon.categoryRequirements == null)
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

            int requiredCount =
                Mathf.Max(
                    1,
                    requirement.requiredCount
                );

            if (GetCategoryCount(
                    requirement.category
                ) <
                requiredCount)
            {
                return false;
            }
        }

        return true;
    }

    private bool MeetsRequiredBoonIds(
        BoonData boon)
    {
        if (boon.requiredBoonIds == null)
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
        string boonId)
    {
        return
            GetBoonStackCount(boonId) >
            0;
    }

    public int GetBoonStackCount(
        string boonId)
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

    // 이전 코드 호환용
    public int GetBoonStack(
        string boonId)
    {
        return
            GetBoonStackCount(boonId);
    }

    public int GetCategoryCount(
        BoonCategory category)
    {
        return categoryCounts.TryGetValue(
            category,
            out int count
        )
            ? count
            : 0;
    }

    public IReadOnlyList<BoonData>
        GetOwnedBoons()
    {
        return ownedBoons;
    }

    // =========================================================
    // 공격 계열 조회
    // =========================================================

    /// <summary>
    /// 모든 보유 득도의 공격속도 증가율을 합산한다.
    /// 0.2는 공격속도 20% 증가다.
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

        return Mathf.Max(
            0f,
            total
        );
    }

    /// <summary>
    /// TempCombatInput 기존 코드 호환용.
    /// </summary>
    public float GetTotalAttackSpeedIncreaseRate()
    {
        return
            GetAttackSpeedIncreaseRate();
    }

    public float GetAttackSpeedMultiplier()
    {
        return
            1f +
            GetAttackSpeedIncreaseRate();
    }

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

        return Mathf.Max(
            0f,
            total
        );
    }

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

        return Mathf.Max(
            0f,
            total
        );
    }

    // =========================================================
    // 수비 계열 조회
    // =========================================================

    public float GetDamageReductionRate()
    {
        float total = 0f;

        for (int i = 0;
             i < ownedBoons.Count;
             i++)
        {
            BoonData boon =
                ownedBoons[i];

            if (boon?.defenseEffect == null)
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
    /// 기존 Player.cs와 호환되는 피해 감소율 조회 메서드.
    /// </summary>
    public float GetTotalDamageReductionRate()
    {
        return GetDamageReductionRate();
    }

    public float CalculateReceivedDamage(
        float originalDamage)
    {
        return Mathf.Max(
            0f,
            originalDamage *
            (1f - GetDamageReductionRate())
        );
    }

    // =========================================================
    // 반사 조회
    // =========================================================

    public void GetReflectEffects(
        BoonTriggerType triggerType,
        List<ReflectEffectData> results)
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
            ReflectEffectData effect =
                ownedBoons[i]
                    ?.reflectEffect;

            if (effect == null ||
                !effect.enabled ||
                effect.triggerType != triggerType)
            {
                continue;
            }

            results.Add(effect);
        }
    }

    public bool HasReflectEffect(
        BoonTriggerType triggerType)
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

    public void GetDebuffEffects(
        BoonTriggerType triggerType,
        List<DebuffEffectData> results)
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
                !effect.enabled ||
                effect.triggerType != triggerType)
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

            if (effect != null &&
                effect.enabled)
            {
                total +=
                    effect.moveSpeedReductionRate;
            }
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

            if (effect != null &&
                effect.enabled)
            {
                total +=
                    effect.attackReductionRate;
            }
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

            if (effect != null &&
                effect.enabled)
            {
                total +=
                    effect.maxHpReductionRate;
            }
        }

        return Mathf.Clamp(
            total,
            0f,
            0.95f
        );
    }

    /// <summary>
    /// 새 런 시작 시 득도 목록과 PlayerStats의 득도 보너스를 초기화한다.
    /// </summary>
    public void ClearAllBoons()
    {
        ResolveReferences();

        ownedBoons.Clear();
        boonStackCounts.Clear();
        categoryCounts.Clear();

        if (stats != null)
        {
            stats.ClearBoonBonuses();
        }

        if (jakduRide != null)
        {
            jakduRide.RemoveCurrentEffect();
        }

        if (showLogs)
        {
            Debug.Log(
                "[BoonInfo] 모든 득도와 득도 스탯 초기화 완료",
                this
            );
        }
    }
}

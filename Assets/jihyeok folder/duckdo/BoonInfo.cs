using System;
using System.Collections.Generic;
using UnityEngine;

public class BoonInfo : MonoBehaviour
{
    [Header("플레이어")]
    [SerializeField]
    private Player player;

    [Header("현재 획득한 득도")]
    [SerializeField]
    private List<BoonData> ownedBoons =
        new List<BoonData>();

    private readonly Dictionary<string, int>
        boonStackCounts =
            new Dictionary<string, int>();

    private readonly Dictionary<BoonCategory, int>
        categoryCounts =
            new Dictionary<BoonCategory, int>();

    public event Action<BoonData> OnBoonAdded;

    private void Awake()
    {
        if (player == null)
        {
            player =
                GetComponent<Player>();
        }

        RebuildCounts();
    }

    public bool CanOffer(BoonData boon)
    {
        if (boon == null ||
            string.IsNullOrWhiteSpace(
                boon.boonId
            ))
        {
            return false;
        }

        int currentStack =
            GetBoonStack(boon.boonId);

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

        /*
         * 일반 득도는 중첩 조건만 통과하면 등장.
         * 전설과 듀오는 아래 조건까지 검사한다.
         */
        if (boon.grade ==
            BoonGrade.Normal)
        {
            return true;
        }

        return MeetsCategoryRequirements(
                   boon.categoryRequirements
               ) &&
               MeetsRequiredBoonIds(
                   boon.requiredBoonIds
               );
    }

    public bool TryAddBoon(BoonData boon)
    {
        if (!CanOffer(boon))
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

        ApplyImmediateStats(boon);

        OnBoonAdded?.Invoke(boon);

        Debug.Log(
            $"[BoonInfo] 득도 획득: " +
            $"{boon.displayName}",
            this
        );

        return true;
    }

    public bool CanOfferLegendary(
        BoonData boon
    )
    {
        return boon != null &&
               boon.grade ==
                   BoonGrade.Legendary &&
               CanOffer(boon);
    }

    public bool CanOfferDuo(
        BoonData boon
    )
    {
        return boon != null &&
               boon.grade ==
                   BoonGrade.Duo &&
               CanOffer(boon);
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

    public bool HasBoon(
        string boonId
    )
    {
        return GetBoonStack(boonId) > 0;
    }

    public IReadOnlyList<BoonData>
        GetOwnedBoons()
    {
        return ownedBoons;
    }

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

            if (GetCategoryCount(
                    requirement.category
                ) <
                Mathf.Max(
                    1,
                    requirement.requiredCount
                ))
            {
                return false;
            }
        }

        return true;
    }

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

    private void ApplyImmediateStats(
        BoonData boon
    )
    {
        if (player == null ||
            player.stats == null)
        {
            return;
        }

        player.stats.AddBoonBonus(
            boon.instantStatBonus
        );

        if (boon.dashEffect != null &&
            boon.dashEffect.enabled)
        {
            player.stats.AddBoonBonus(
                boon.dashEffect.statBonus
            );
        }
    }

    private void RebuildCounts()
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
            GetBoonStack(boonId) + 1;
    }

    private void IncrementCategory(
        BoonCategory category
    )
    {
        categoryCounts[category] =
            GetCategoryCount(category) + 1;
    }
}

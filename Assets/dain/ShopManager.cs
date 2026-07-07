using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 슬롯 종류.
/// </summary>
public enum ShopSlotType
{
    HealthRestore,  // 슬롯 1: 체력 회복 (고정)
    Boon,           // 득도 선택
    MaxHpIncrease,  // 최대 체력 증가
}

/// <summary>
/// 상점 슬롯 하나의 데이터.
/// </summary>
[Serializable]
public class ShopSlotData
{
    public ShopSlotType slotType;
    public int price;
    public BoonCategory boonCategory;
    public bool purchased;
}

/// <summary>
/// 상점 방 전체를 관리한다.
///
/// [슬롯 구성]
///   슬롯 0 - 체력 회복  125골드 (고정)
///   슬롯 1 - 득도       150골드 (타입 무작위)
///   슬롯 2 - 득도 or 최대체력  150골드 (슬롯1과 다른 타입)
///
/// [사용 방법]
///   1. 씬에 빈 GameObject를 만들고 이 컴포넌트를 붙인다.
///   2. statues 배열에 동상 오브젝트에 붙은 ShopStatueInteractable 3개를 연결한다.
///   3. boonDatabase, boonRewardUI를 Inspector에서 연결한다.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("가격")]
    [SerializeField] private int slot0Price = 125;
    [SerializeField] private int slot1Price = 150;
    [SerializeField] private int slot2Price = 150;

    [Header("수치 (미확정 - 추후 조정)")]
    [Tooltip("슬롯0: 체력 회복량")]
    [SerializeField] private int healthRestoreAmount = 30;
    [Tooltip("슬롯2(최대체력): 증가량")]
    [SerializeField] private int maxHpIncreaseAmount = 20;

    [Header("득도 데이터")]
    [SerializeField] private BoonDatabase boonDatabase;
    [Tooltip("BoonDatabase가 비어있을 때 사용할 Resources 경로")]
    [SerializeField] private string resourcesPath = "Boons";

    [Header("득도 선택 UI")]
    [SerializeField] private BoonRewardUI boonRewardUI;

    [Header("동상 (슬롯 순서대로)")]
    [SerializeField] private ShopStatueInteractable[] statues =
        new ShopStatueInteractable[3];

    private readonly List<BoonData> allBoons =
        new List<BoonData>();

    private ShopSlotData[] slots;

    // ───────── 업데이트 ─────────

    private void Update()
    {
        // Time.timeScale = 0이어도 Update는 실행됨
        // BoonRewardUI가 열려있을 때 ESC로 취소
        if (boonRewardUI != null &&
            boonRewardUI.IsOpen &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            boonRewardUI.Cancel();
        }
    }

    // ───────── 초기화 ─────────

    private void Awake()
    {
        LoadBoonData();
        SetupSlots();
        InitStatues();
        ResolveUI();
    }

    private void LoadBoonData()
    {
        allBoons.Clear();

        if (boonDatabase != null &&
            boonDatabase.Boons != null)
        {
            allBoons.AddRange(boonDatabase.Boons);
        }

        if (allBoons.Count == 0)
        {
            BoonData[] loaded =
                Resources.LoadAll<BoonData>(resourcesPath);
            if (loaded != null)
                allBoons.AddRange(loaded);
        }

        if (allBoons.Count == 0)
        {
            Debug.LogWarning(
                "[ShopManager] 득도 데이터가 없습니다. " +
                "BoonDatabase를 Inspector에 연결하세요.",
                this
            );
        }
    }

    private void SetupSlots()
    {
        slots = new ShopSlotData[3];

        // 슬롯 0: 체력 회복 (고정)
        slots[0] = new ShopSlotData
        {
            slotType = ShopSlotType.HealthRestore,
            price = slot0Price,
        };

        // 슬롯 1: 무작위 타입 득도
        BoonCategory cat1 = GetRandomCategory();
        slots[1] = new ShopSlotData
        {
            slotType = ShopSlotType.Boon,
            price = slot1Price,
            boonCategory = cat1,
        };

        // 슬롯 2: 최대체력 or 득도 (슬롯1과 다른 타입)
        bool slot2IsBoon = UnityEngine.Random.value < 0.5f;
        if (slot2IsBoon)
        {
            slots[2] = new ShopSlotData
            {
                slotType = ShopSlotType.Boon,
                price = slot2Price,
                boonCategory = GetRandomCategoryExcluding(cat1),
            };
        }
        else
        {
            slots[2] = new ShopSlotData
            {
                slotType = ShopSlotType.MaxHpIncrease,
                price = slot2Price,
            };
        }
    }

    private void InitStatues()
    {
        for (int i = 0; i < statues.Length; i++)
        {
            if (statues[i] != null)
                statues[i].Initialize(this, i);
        }
    }

    private void ResolveUI()
    {
        if (boonRewardUI != null) return;

        boonRewardUI =
            FindFirstObjectByType<BoonRewardUI>(
                FindObjectsInactive.Include
            );

        if (boonRewardUI == null)
        {
            Debug.LogError(
                "[ShopManager] BoonRewardUI를 찾지 못했습니다.",
                this
            );
        }
    }

    // ───────── 외부 인터페이스 ─────────

    /// <summary>
    /// ShopStatueInteractable이 E키 입력 시 호출한다.
    /// </summary>
    public void OnStatueInteract(
        int slotIndex,
        Player player,
        PlayerInteraction playerInteraction)
    {
        if (slots == null ||
            slotIndex < 0 ||
            slotIndex >= slots.Length)
        {
            return;
        }

        ShopSlotData slot = slots[slotIndex];
        if (slot == null || slot.purchased) return;

        switch (slot.slotType)
        {
            case ShopSlotType.HealthRestore:
                PurchaseHealthRestore(slot, player);
                break;

            case ShopSlotType.Boon:
                PurchaseBoon(slot, player, playerInteraction);
                break;

            case ShopSlotType.MaxHpIncrease:
                PurchaseMaxHp(slot, player);
                break;
        }
    }

    /// <summary>
    /// 슬롯 데이터를 읽기 전용으로 반환한다.
    /// ShopStatueInteractable의 CanInteract에서 사용한다.
    /// </summary>
    public ShopSlotData GetSlot(int index)
    {
        if (slots == null ||
            index < 0 ||
            index >= slots.Length)
        {
            return null;
        }
        return slots[index];
    }

    // ───────── 구매 처리 ─────────

    /// <summary>
    /// 슬롯 0 - 체력 회복.
    /// 골드 차감 후 즉시 회복한다.
    /// </summary>
    private void PurchaseHealthRestore(
        ShopSlotData slot,
        Player player)
    {
        if (!player.money.TrySpend(slot.price))
        {
            Debug.Log(
                "[ShopManager] 골드 부족 - 체력 회복 구매 실패"
            );
            return;
        }

        player.HealHp(healthRestoreAmount);
        slot.purchased = true;

        Debug.Log(
            $"[ShopManager] 체력 회복 구매 완료 " +
            $"(+{healthRestoreAmount} HP / -{slot.price}G)"
        );
    }

    /// <summary>
    /// 슬롯 1·2 - 득도 선택.
    /// BoonRewardUI를 열고, 선택 확정 시 골드를 차감한다.
    /// 취소하면 골드를 차감하지 않고 다시 상호작용할 수 있다.
    /// </summary>
    private void PurchaseBoon(
        ShopSlotData slot,
        Player player,
        PlayerInteraction playerInteraction)
    {
        BoonInfo boonInfo =
            player.GetComponent<BoonInfo>() ??
            player.GetComponentInChildren<BoonInfo>(true);

        if (boonInfo == null)
        {
            Debug.LogError(
                "[ShopManager] Player에 BoonInfo가 없습니다.",
                player
            );
            return;
        }

        if (boonRewardUI == null)
        {
            ResolveUI();
            if (boonRewardUI == null) return;
        }

        List<BoonData> choices =
            BuildBoonChoices(slot.boonCategory, boonInfo);

        if (choices.Count == 0)
        {
            Debug.LogWarning(
                $"[ShopManager] {slot.boonCategory} 계열에서 " +
                "선택 가능한 득도가 없습니다."
            );
            return;
        }

        // SetInteractionBlocked 사용하지 않음
        // BoonRewardUI 자체가 Time.timeScale=0으로 게임을 멈추고
        // ESC로 취소 시 Update()에서 boonRewardUI.Cancel() 호출

        bool opened = boonRewardUI.Open(
            choices,
            (selectedBoon) =>
            {
                if (selectedBoon == null) return;

                if (!player.money.TrySpend(slot.price))
                {
                    Debug.Log(
                        "[ShopManager] 골드 부족 - 득도 구매 실패"
                    );
                    return;
                }

                if (!boonInfo.TryAddBoon(selectedBoon))
                {
                    player.money.AddMoney(slot.price);
                    return;
                }

                slot.purchased = true;

                Debug.Log(
                    $"[ShopManager] 득도 구매 완료 " +
                    $"({selectedBoon.displayName} / -{slot.price}G)"
                );
            },
            () => { /* 취소 - 골드 차감 없음 */ }
        );

        if (!opened)
        {
            Debug.LogWarning(
                "[ShopManager] BoonRewardUI.Open이 실패했습니다."
            );
        }
    }

    /// <summary>
    /// 슬롯 2 - 최대 체력 증가.
    /// 골드 차감 후 즉시 적용한다.
    /// </summary>
    private void PurchaseMaxHp(
        ShopSlotData slot,
        Player player)
    {
        if (!player.money.TrySpend(slot.price))
        {
            Debug.Log(
                "[ShopManager] 골드 부족 - 최대 체력 증가 구매 실패"
            );
            return;
        }

        PlayerStatValues bonus = new PlayerStatValues();
        bonus.maxHp = maxHpIncreaseAmount;
        player.stats.AddBoonBonus(bonus);

        slot.purchased = true;

        Debug.Log(
            $"[ShopManager] 최대 체력 증가 구매 완료 " +
            $"(+{maxHpIncreaseAmount} MaxHP / -{slot.price}G)"
        );
    }

    // ───────── 득도 선택지 생성 ─────────

    private List<BoonData> BuildBoonChoices(
        BoonCategory category,
        BoonInfo boonInfo)
    {
        List<BoonData> available =
            new List<BoonData>();

        for (int i = 0; i < allBoons.Count; i++)
        {
            BoonData boon = allBoons[i];
            if (boon == null) continue;
            if (boon.category != category) continue;
            if (!boonInfo.CanOffer(boon)) continue;
            available.Add(boon);
        }

        // Fisher-Yates 셔플로 최대 3개 추출
        int resultCount =
            Mathf.Min(3, available.Count);

        List<BoonData> result =
            new List<BoonData>(resultCount);

        for (int i = 0; i < resultCount; i++)
        {
            int rand =
                UnityEngine.Random.Range(i, available.Count);

            BoonData tmp = available[i];
            available[i] = available[rand];
            available[rand] = tmp;

            result.Add(available[i]);
        }

        return result;
    }

    // ───────── 유틸리티 ─────────

    private BoonCategory GetRandomCategory()
    {
        BoonCategory[] all =
        {
            BoonCategory.Attack,
            BoonCategory.Defense,
            BoonCategory.Mobility,
            BoonCategory.Debuff,
        };
        return all[UnityEngine.Random.Range(0, all.Length)];
    }

    private BoonCategory GetRandomCategoryExcluding(
        BoonCategory exclude)
    {
        List<BoonCategory> pool =
            new List<BoonCategory>
            {
                BoonCategory.Attack,
                BoonCategory.Defense,
                BoonCategory.Mobility,
                BoonCategory.Debuff,
            };
        pool.Remove(exclude);
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }
}

using System;
using UnityEngine;

/// <summary>
/// 상점 슬롯 종류.
/// </summary>
public enum ShopSlotType
{
    HealthRestore,  // 체력 회복
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
/// 상점(작두점) 방 전체를 관리한다.
///
/// [슬롯 구성]
///   슬롯 0 - 체력 회복     125골드 (고정) → ShopItemOrb
///   슬롯 1 - 최대체력 증가  150골드 (고정) → ShopItemOrb
///   슬롯 2 - 득도          150골드 (카테고리 랜덤) → ShopBoonOrb
///
/// [사용 방법]
///   1. 씬에 빈 GameObject를 만들고 이 컴포넌트를 붙인다.
///   2. itemOrbs[0] = 체력회복 탁자 오브, itemOrbs[1] = 최대체력 탁자 오브 연결.
///   3. boonOrb = 득도 탁자 오브 연결.
///   4. boonRewardUI를 Inspector에서 연결한다.
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
    [Tooltip("슬롯1: 최대체력 증가량")]
    [SerializeField] private int maxHpIncreaseAmount = 20;

    [Header("득도 선택 UI")]
    [SerializeField] private BoonRewardUI boonRewardUI;

    [Header("탁자 오브 (슬롯 순서대로)")]
    [Tooltip("슬롯0=체력회복, 슬롯1=최대체력")]
    [SerializeField] private ShopItemOrb[] itemOrbs = new ShopItemOrb[2];

    [Tooltip("득도 슬롯 (카테고리 랜덤)")]
    [SerializeField] private ShopBoonOrb boonOrb;

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
        SetupSlots();
        InitItemOrbs();
        InitBoonOrb();
        ResolveUI();
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

        // 슬롯 1: 최대체력 증가 (고정)
        slots[1] = new ShopSlotData
        {
            slotType = ShopSlotType.MaxHpIncrease,
            price = slot1Price,
        };

        // 슬롯 2: 득도 - 방 등장 시 카테고리 랜덤 결정
        slots[2] = new ShopSlotData
        {
            slotType = ShopSlotType.Boon,
            price = slot2Price,
            boonCategory = GetRandomCategory(),
        };
    }

    private void InitItemOrbs()
    {
        for (int i = 0; i < itemOrbs.Length && i < slots.Length; i++)
        {
            if (itemOrbs[i] != null)
                itemOrbs[i].Prepare(this, slots[i].slotType, slots[i].price);
        }
    }

    private void InitBoonOrb()
    {
        if (boonOrb != null)
            boonOrb.Prepare(slots[2].boonCategory, slots[2].price);
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
    /// ShopItemOrb가 E키 입력 시 호출한다.
    /// </summary>
    public void OnItemOrbInteract(
        ShopSlotType slotType,
        int price,
        Player player,
        ShopItemOrb orb)
    {
        switch (slotType)
        {
            case ShopSlotType.HealthRestore:
                if (!player.money.TrySpend(price))
                {
                    Debug.Log("[ShopManager] 골드 부족 - 체력 회복 구매 실패");
                    return;
                }
                player.HealHp(healthRestoreAmount);
                orb.SetPurchased();
                Debug.Log($"[ShopManager] 체력 회복 (+{healthRestoreAmount} HP / -{price}G)");
                break;

            case ShopSlotType.MaxHpIncrease:
                if (!player.money.TrySpend(price))
                {
                    Debug.Log("[ShopManager] 골드 부족 - 최대 체력 증가 구매 실패");
                    return;
                }
                PlayerStatValues bonus = new PlayerStatValues();
                bonus.maxHp = maxHpIncreaseAmount;
                player.stats.AddBoonBonus(bonus);
                orb.SetPurchased();
                Debug.Log($"[ShopManager] 최대 체력 증가 (+{maxHpIncreaseAmount} MaxHP / -{price}G)");
                break;
        }
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
}

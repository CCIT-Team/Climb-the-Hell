using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점(작두점) 탁자 위에 배치되는 득도 오브젝트.
///
/// 사용 흐름:
///   1. ShopManager.Awake에서 Prepare(category, price) 호출
///   2. 해당 카테고리 프리팹을 자식으로 Instantiate
///   3. 플레이어가 범위 진입 → E키 → 3선택지 BoonRewardUI 오픈
///   4. 선택 완료 → 골드 차감 → 득도 획득 → 오브 비활성화
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopBoonOrb : InteractableBase
{
    [Header("득도 선택 UI")]
    [SerializeField] private BoonRewardUI boonRewardUI;

    [Header("득도 데이터")]
    [SerializeField] private BoonDatabase boonDatabase;
    [Tooltip("BoonDatabase가 비어 있을 때 검색할 Resources 폴더")]
    [SerializeField] private string resourcesPath = "Boons";

    [Header("계열별 프리팹 (없으면 defaultPrefab 사용)")]
    [SerializeField] private GameObject defaultPrefab;
    [SerializeField] private GameObject attackPrefab;
    [SerializeField] private GameObject defensePrefab;
    [SerializeField] private GameObject mobilityPrefab;
    [SerializeField] private GameObject debuffPrefab;

    [Header("설정")]
    [SerializeField] private int price = 150;
    [Range(1, 3)]
    [SerializeField] private int choiceCount = 3;

    // ───────── 내부 상태 ─────────

    private readonly List<BoonData> allBoons =
        new List<BoonData>();

    private readonly List<BoonData> availableBoons =
        new List<BoonData>(32);

    private readonly List<BoonData> selectedChoices =
        new List<BoonData>(3);

    private BoonCategory targetCategory = BoonCategory.None;
    private GameObject activeVisual;

    private Player currentPlayer;
    private BoonInfo currentBoonInfo;
    private PlayerInteraction currentPlayerInteraction;

    private bool ready;
    private bool purchased;

    // ───────── 초기화 ─────────

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        LoadBoonData();
        ResolveUI();
    }

    // ───────── 외부 인터페이스 ─────────

    /// <summary>
    /// ShopManager가 방 시작 시 호출한다.
    /// 카테고리에 맞는 프리팹을 Instantiate하고 상호작용을 활성화한다.
    /// </summary>
    public void Prepare(BoonCategory category, int orbPrice)
    {
        targetCategory = category;
        price = orbPrice;
        ready = false;
        purchased = false;

        BuildCategoryVisual(category);

        ready = true;
    }

    // ───────── 상호작용 ─────────

    public override bool CanInteract()
    {
        return
            ready &&
            !purchased &&
            targetCategory != BoonCategory.None &&
            currentPlayer != null &&
            currentBoonInfo != null;
    }

    public override void Interact(Player player)
    {
        if (!CanInteract() || player != currentPlayer)
            return;

        CreateChoices();

        if (selectedChoices.Count == 0)
        {
            Debug.LogWarning(
                $"[ShopBoonOrb] {targetCategory} 계열에서 " +
                "선택 가능한 득도가 없습니다."
            );
            return;
        }

        if (boonRewardUI == null)
        {
            ResolveUI();
            if (boonRewardUI == null) return;
        }

        bool opened = boonRewardUI.Open(
            selectedChoices,
            HandleSelected,
            HandleCancelled
        );

        if (!opened)
        {
            Debug.LogWarning("[ShopBoonOrb] BoonRewardUI.Open 실패");
        }
    }

    // ───────── 선택 콜백 ─────────

    private void HandleSelected(BoonData selectedBoon)
    {
        if (selectedBoon == null || currentBoonInfo == null)
            return;

        if (currentPlayer != null &&
            !currentPlayer.money.TrySpend(price))
        {
            Debug.Log("[ShopBoonOrb] 골드 부족 - 득도 구매 실패");
            return;
        }

        if (!currentBoonInfo.TryAddBoon(selectedBoon))
        {
            if (currentPlayer != null)
                currentPlayer.money.AddMoney(price);
            return;
        }

        Debug.Log(
            $"[ShopBoonOrb] 득도 구매 완료 " +
            $"({selectedBoon.displayName} / -{price}G)"
        );

        SetPurchased();
    }

    private void HandleCancelled()
    {
        // 취소 시 골드 차감 없이 다시 상호작용 가능
    }

    private void SetPurchased()
    {
        purchased = true;

        if (currentPlayerInteraction != null)
            currentPlayerInteraction.UnregisterInteractable(this);

        if (activeVisual != null)
            activeVisual.SetActive(false);

        gameObject.SetActive(false);
    }

    // ───────── 트리거 ─────────

    private void OnTriggerEnter(Collider other)
    {
        if (!ready || purchased) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        BoonInfo boonInfo =
            player.GetComponent<BoonInfo>() ??
            player.GetComponentInChildren<BoonInfo>(true);

        PlayerInteraction interaction =
            player.GetComponent<PlayerInteraction>() ??
            player.GetComponentInChildren<PlayerInteraction>(true);

        if (boonInfo == null || interaction == null) return;

        currentPlayer = player;
        currentBoonInfo = boonInfo;
        currentPlayerInteraction = interaction;
        interaction.RegisterInteractable(this);
    }

    private void OnTriggerExit(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null || player != currentPlayer) return;

        if (currentPlayerInteraction != null)
            currentPlayerInteraction.UnregisterInteractable(this);

        currentPlayer = null;
        currentBoonInfo = null;
        currentPlayerInteraction = null;
    }

    private void OnDisable()
    {
        if (currentPlayerInteraction != null)
            currentPlayerInteraction.UnregisterInteractable(this);

        currentPlayer = null;
        currentBoonInfo = null;
        currentPlayerInteraction = null;
    }

    // ───────── 비주얼 ─────────

    private void BuildCategoryVisual(BoonCategory category)
    {
        if (activeVisual != null)
        {
            Destroy(activeVisual);
            activeVisual = null;
        }

        GameObject prefab = GetPrefab(category);
        if (prefab == null) return;

        activeVisual = Instantiate(prefab, transform);
        activeVisual.transform.localPosition = Vector3.zero;
        activeVisual.transform.localRotation = Quaternion.identity;

        // 비주얼 전용 - child Collider는 모두 비활성화
        Collider[] childColliders =
            activeVisual.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < childColliders.Length; i++)
            childColliders[i].enabled = false;
    }

    private GameObject GetPrefab(BoonCategory category)
    {
        switch (category)
        {
            case BoonCategory.Attack:
                return attackPrefab != null ? attackPrefab : defaultPrefab;
            case BoonCategory.Defense:
                return defensePrefab != null ? defensePrefab : defaultPrefab;
            case BoonCategory.Mobility:
                return mobilityPrefab != null ? mobilityPrefab : defaultPrefab;
            case BoonCategory.Debuff:
                return debuffPrefab != null ? debuffPrefab : defaultPrefab;
            default:
                return defaultPrefab;
        }
    }

    // ───────── 득도 선택지 생성 ─────────

    private void CreateChoices()
    {
        availableBoons.Clear();
        selectedChoices.Clear();

        for (int i = 0; i < allBoons.Count; i++)
        {
            BoonData boon = allBoons[i];
            if (boon == null ||
                boon.category != targetCategory ||
                !currentBoonInfo.CanOffer(boon))
            {
                continue;
            }
            availableBoons.Add(boon);
        }

        int count = Mathf.Min(choiceCount, availableBoons.Count);

        for (int i = 0; i < count; i++)
        {
            int rand = UnityEngine.Random.Range(i, availableBoons.Count);
            BoonData tmp = availableBoons[i];
            availableBoons[i] = availableBoons[rand];
            availableBoons[rand] = tmp;
            selectedChoices.Add(availableBoons[i]);
        }
    }

    // ───────── 유틸 ─────────

    private void LoadBoonData()
    {
        allBoons.Clear();

        if (boonDatabase != null && boonDatabase.Boons != null)
            allBoons.AddRange(boonDatabase.Boons);

        if (allBoons.Count == 0)
        {
            BoonData[] loaded = Resources.LoadAll<BoonData>(resourcesPath);
            if (loaded != null) allBoons.AddRange(loaded);
        }

        if (allBoons.Count == 0)
        {
            Debug.LogWarning(
                "[ShopBoonOrb] 득도 데이터가 없습니다. " +
                "BoonDatabase를 Inspector에 연결하세요.",
                this
            );
        }
    }

    private void ResolveUI()
    {
        if (boonRewardUI != null) return;

        boonRewardUI = FindFirstObjectByType<BoonRewardUI>(
            FindObjectsInactive.Include
        );

        if (boonRewardUI == null)
        {
            Debug.LogError(
                "[ShopBoonOrb] BoonRewardUI를 찾지 못했습니다.",
                this
            );
        }
    }
}

using UnityEngine;

/// <summary>
/// 상점(작두점) 탁자 위에 배치되는 아이템 오브젝트.
/// 체력 회복(슬롯0) 또는 최대체력 증가(슬롯1) 슬롯에 사용한다.
///
/// ShopManager.Awake에서 Prepare() 호출 → prefab 생성 → E키 → 즉시 효과 적용.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopItemOrb : InteractableBase
{
    [Header("아이템 프리팹 (비주얼 전용)")]
    [SerializeField] private GameObject itemPrefab;

    private ShopManager shopManager;
    private ShopSlotType slotType;
    private int price;

    private GameObject activeVisual;
    private Player currentPlayer;
    private PlayerInteraction currentPlayerInteraction;
    private bool purchased;

    // ───────── 초기화 ─────────

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    // ───────── 외부 인터페이스 ─────────

    /// <summary>
    /// ShopManager가 방 시작 시 호출한다.
    /// </summary>
    public void Prepare(ShopManager manager, ShopSlotType type, int orbPrice)
    {
        shopManager = manager;
        slotType = type;
        price = orbPrice;
        purchased = false;

        BuildVisual();
    }

    /// <summary>
    /// 구매 완료 시 ShopManager가 호출한다.
    /// </summary>
    public void SetPurchased()
    {
        purchased = true;

        if (currentPlayerInteraction != null)
            currentPlayerInteraction.UnregisterInteractable(this);

        if (activeVisual != null)
            activeVisual.SetActive(false);

        gameObject.SetActive(false);
    }

    // ───────── 상호작용 ─────────

    public override bool CanInteract()
    {
        return !purchased && shopManager != null && currentPlayer != null;
    }

    public override void Interact(Player player)
    {
        if (!CanInteract() || player != currentPlayer) return;
        shopManager.OnItemOrbInteract(slotType, price, player, this);
    }

    // ───────── 트리거 ─────────

    private void OnTriggerEnter(Collider other)
    {
        if (purchased) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        PlayerInteraction interaction =
            player.GetComponent<PlayerInteraction>() ??
            player.GetComponentInChildren<PlayerInteraction>(true);
        if (interaction == null) return;

        currentPlayer = player;
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
        currentPlayerInteraction = null;
    }

    private void OnDisable()
    {
        if (currentPlayerInteraction != null)
            currentPlayerInteraction.UnregisterInteractable(this);

        currentPlayer = null;
        currentPlayerInteraction = null;
    }

    // ───────── 비주얼 ─────────

    private void BuildVisual()
    {
        if (activeVisual != null)
        {
            Destroy(activeVisual);
            activeVisual = null;
        }

        if (itemPrefab == null) return;

        activeVisual = Instantiate(itemPrefab, transform);
        activeVisual.transform.localPosition = Vector3.zero;
        activeVisual.transform.localRotation = Quaternion.identity;

        // 비주얼 전용 - child Collider 비활성화
        Collider[] childColliders =
            activeVisual.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < childColliders.Length; i++)
            childColliders[i].enabled = false;
    }
}

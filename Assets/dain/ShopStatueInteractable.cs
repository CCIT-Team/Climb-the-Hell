using UnityEngine;

/// <summary>
/// 상점 동상 하나에 붙이는 상호작용 컴포넌트.
/// 플레이어가 범위 안에서 E키를 누르면 ShopManager에 알린다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopStatueInteractable : InteractableBase
{
    [Header("상점 연결")]
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private int slotIndex;

    private Player currentPlayer;
    private PlayerInteraction currentPlayerInteraction;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    /// <summary>
    /// ShopManager가 생성 시 호출해 슬롯 인덱스를 설정한다.
    /// </summary>
    public void Initialize(ShopManager manager, int index)
    {
        shopManager = manager;
        slotIndex = index;
    }

    private void OnTriggerEnter(Collider other)
    {
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

    public override bool CanInteract()
    {
        if (shopManager == null || currentPlayer == null) return false;

        ShopSlotData slot = shopManager.GetSlot(slotIndex);
        return slot != null && !slot.purchased;
    }

    public override void Interact(Player player)
    {
        if (!CanInteract()) return;
        shopManager.OnStatueInteract(slotIndex, player, currentPlayerInteraction);
    }

    private void OnDisable()
    {
        if (currentPlayerInteraction != null)
            currentPlayerInteraction.UnregisterInteractable(this);
        currentPlayer = null;
        currentPlayerInteraction = null;
    }
}

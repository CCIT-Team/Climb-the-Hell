using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 상점 아이템 하나를 관리한다.
///
/// 사용 위치:
/// - 체력 증가 프리팹
/// - 체력 회복 프리팹
/// - 득도 보상 프리팹
///
/// 흐름:
/// 1. 플레이어가 Trigger 안에 들어오면 PlayerInteraction에 등록된다.
/// 2. E키를 누르면 플레이어 골드를 검사한다.
/// 3. 골드가 부족하면 "골드가 부족합니다" 월드 텍스트를 띄운다.
/// 4. 골드가 충분하면 가격만큼 차감하고 효과를 적용한다.
/// 5. 득도 보상은 기존 BoonRewardUI를 열고, 선택이 끝났을 때 골드를 차감한 뒤 BoonInfo.TryAddBoon으로 획득한다.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class ShopPurchaseInteractable : InteractableBase
{
    public enum ShopItemType
    {
        MaxHpIncrease,
        Heal,
        BoonReward
    }

    [Header("상점 아이템 종류")]
    [SerializeField]
    private ShopItemType itemType =
        ShopItemType.Heal;

    [Header("가격")]
    [Min(0)]
    [SerializeField]
    private int price = 30;

    [Header("구매 후 처리")]
    [Tooltip("체크하면 한 번 구매한 뒤 다시 구매할 수 없다.")]
    [SerializeField]
    private bool purchaseOnce = true;

    [Tooltip("구매 후 프리팹 오브젝트를 꺼버린다.")]
    [SerializeField]
    private bool hideObjectAfterPurchase = true;

    [Header("체력 증가")]
    [Min(1)]
    [SerializeField]
    private int maxHpIncreaseAmount = 25;

    [Header("체력 회복")]
    [SerializeField]
    private bool healToFull;

    [Min(1)]
    [SerializeField]
    private int healAmount = 30;

    [Tooltip("체력이 가득 차 있으면 회복 아이템을 구매하지 못하게 한다.")]
    [SerializeField]
    private bool blockHealWhenHpFull = true;

    [Header("득도 보상")]
    [Range(1, 3)]
    [SerializeField]
    private int boonChoiceCount = 3;

    [Tooltip("None이면 Attack, Defense, Mobility, Debuff 전체에서 후보를 뽑는다.")]
    [SerializeField]
    private BoonCategory boonCategory =
        BoonCategory.None;

    [SerializeField]
    private BoonRewardUI boonRewardUI;

    [SerializeField]
    private BoonDatabase boonDatabase;

    [Tooltip("BoonDatabase가 비어 있을 때 검색할 Resources 폴더")]
    [SerializeField]
    private string resourcesPath = "Boons";

    [Header("월드 안내 문구")]
    [SerializeField]
    private string notEnoughGoldMessage =
        "골드가 부족합니다";

    [SerializeField]
    private string hpFullMessage =
        "이미 체력이 가득합니다";

    [SerializeField]
    private string noBoonMessage =
        "획득 가능한 득도가 없습니다";

    [SerializeField]
    private TextMeshPro messageTextPrefab;

    [SerializeField]
    private Vector3 messageOffset =
        new Vector3(0f, 1.6f, 0f);

    [Min(0.1f)]
    [SerializeField]
    private float messageDuration = 0.8f;

    [Min(0f)]
    [SerializeField]
    private float messageRiseDistance = 0.45f;

    [Header("디버그")]
    [SerializeField]
    private bool showLogs = true;

    private readonly List<BoonData> allBoons =
        new List<BoonData>();

    private readonly List<BoonData> availableBoons =
        new List<BoonData>(32);

    private readonly List<BoonData> selectedChoices =
        new List<BoonData>(3);

    private readonly HashSet<Collider> playerColliders =
        new HashSet<Collider>();

    private Collider triggerCollider;
    private Rigidbody rigidBody;

    private Player currentPlayer;
    private PlayerInteraction currentPlayerInteraction;
    private BoonInfo currentBoonInfo;

    private bool purchased;
    private bool selectionInProgress;

    private Coroutine messageRoutine;
    private TextMeshPro activeMessageText;

    private void Awake()
    {
        NormalizeMessages();
        SetupPhysics();
        ResolveUI();
        LoadBoonData();
    }

    private void OnValidate()
    {
        NormalizeMessages();
        SetupPhysics();
    }

    private void SetupPhysics()
    {
        if (triggerCollider == null)
        {
            triggerCollider =
                GetComponent<Collider>();
        }

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        if (rigidBody == null)
        {
            rigidBody =
                GetComponent<Rigidbody>();
        }

        if (rigidBody != null)
        {
            rigidBody.isKinematic = true;
            rigidBody.useGravity = false;
            rigidBody.detectCollisions = true;
        }
    }

    private void ResolveUI()
    {
        if (boonRewardUI == null)
        {
            boonRewardUI =
                FindFirstObjectByType<BoonRewardUI>(
                    FindObjectsInactive.Include
                );
        }
    }

    private void LoadBoonData()
    {
        allBoons.Clear();

        if (boonDatabase != null &&
            boonDatabase.Boons != null)
        {
            for (int i = 0;
                 i < boonDatabase.Boons.Count;
                 i++)
            {
                BoonData boon =
                    boonDatabase.Boons[i];

                if (boon != null &&
                    !allBoons.Contains(boon))
                {
                    allBoons.Add(boon);
                }
            }
        }

        if (allBoons.Count > 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(resourcesPath))
        {
            return;
        }

        BoonData[] loadedBoons =
            Resources.LoadAll<BoonData>(
                resourcesPath
            );

        for (int i = 0;
             i < loadedBoons.Length;
             i++)
        {
            BoonData boon =
                loadedBoons[i];

            if (boon != null &&
                !allBoons.Contains(boon))
            {
                allBoons.Add(boon);
            }
        }
    }

    public override bool CanInteract()
    {
        if (!isActiveAndEnabled ||
            selectionInProgress)
        {
            return false;
        }

        if (purchaseOnce && purchased)
        {
            return false;
        }

        return currentPlayer != null &&
               currentPlayerInteraction != null;
    }

    public override void Interact(
        Player player)
    {
        if (!CanInteract() ||
            player == null ||
            player != currentPlayer)
        {
            return;
        }

        switch (itemType)
        {
            case ShopItemType.MaxHpIncrease:
                TryBuyMaxHp(player);
                break;

            case ShopItemType.Heal:
                TryBuyHeal(player);
                break;

            case ShopItemType.BoonReward:
                TryOpenBoonShop(player);
                break;
        }
    }

    private void TryBuyMaxHp(
        Player player)
    {
        if (!TrySpendGold(player))
        {
            ShowMessage(notEnoughGoldMessage);
            return;
        }

        PlayerStatValues bonus =
            new PlayerStatValues();

        bonus.maxHp =
            maxHpIncreaseAmount;

        player.stats.AddBoonBonus(bonus);
        player.RefreshHpUI();

        if (showLogs)
        {
            Debug.Log(
                $"[ShopPurchase] 최대 체력 증가 구매 / " +
                $"가격={price}, 증가량={maxHpIncreaseAmount}, " +
                $"현재 골드={player.money.CurrentMoney}",
                this
            );
        }

        CompletePurchase();
    }

    private void TryBuyHeal(
        Player player)
    {
        if (blockHealWhenHpFull &&
            player.stats.CurrentHp >=
            player.stats.MaxHp)
        {
            ShowMessage(hpFullMessage);
            return;
        }

        if (!TrySpendGold(player))
        {
            ShowMessage(notEnoughGoldMessage);
            return;
        }

        int amount =
            healToFull
                ? player.stats.MaxHp -
                  player.stats.CurrentHp
                : healAmount;

        player.HealHp(amount);

        if (showLogs)
        {
            Debug.Log(
                $"[ShopPurchase] 체력 회복 구매 / " +
                $"가격={price}, 회복량={amount}, " +
                $"현재 골드={player.money.CurrentMoney}",
                this
            );
        }

        CompletePurchase();
    }

    private void TryOpenBoonShop(
        Player player)
    {
        ResolveUI();
        LoadBoonData();

        if (boonRewardUI == null)
        {
            Debug.LogError(
                "[ShopPurchase] BoonRewardUI가 연결되지 않았습니다.",
                this
            );

            return;
        }

        if (player.money == null ||
            !player.money.CanSpend(price))
        {
            ShowMessage(notEnoughGoldMessage);
            return;
        }

        if (currentBoonInfo == null)
        {
            currentBoonInfo =
                player.GetComponent<BoonInfo>();

            if (currentBoonInfo == null)
            {
                currentBoonInfo =
                    player.GetComponentInChildren<BoonInfo>(
                        true
                    );
            }
        }

        if (currentBoonInfo == null)
        {
            Debug.LogError(
                "[ShopPurchase] Player에 BoonInfo가 없습니다.",
                player
            );

            return;
        }

        CreateBoonChoices();

        if (selectedChoices.Count == 0)
        {
            ShowMessage(noBoonMessage);
            return;
        }

        bool opened =
            boonRewardUI.Open(
                selectedChoices,
                HandleBoonSelected,
                HandleBoonCancelled
            );

        if (!opened)
        {
            return;
        }

        selectionInProgress = true;

        if (currentPlayerInteraction != null)
        {
            currentPlayerInteraction
                .SetInteractionBlocked(true);
        }
    }

    private bool TrySpendGold(
        Player player)
    {
        if (player == null ||
            player.money == null)
        {
            return false;
        }

        return player.money.TrySpend(price);
    }

    private void NormalizeMessages()
    {
        notEnoughGoldMessage =
            "Not enough gold";

        hpFullMessage =
            "HP is already full";

        noBoonMessage =
            "No boons available";
    }

    private void CreateBoonChoices()
    {
        availableBoons.Clear();
        selectedChoices.Clear();

        if (currentBoonInfo == null)
        {
            return;
        }

        for (int i = 0;
             i < allBoons.Count;
             i++)
        {
            BoonData boon =
                allBoons[i];

            if (boon == null)
            {
                continue;
            }

            if (!IsAllowedBoonCategory(boon.category))
            {
                continue;
            }

            if (!currentBoonInfo.CanOffer(boon))
            {
                continue;
            }

            availableBoons.Add(boon);
        }

        int resultCount =
            Mathf.Min(
                boonChoiceCount,
                availableBoons.Count
            );

        for (int i = 0;
             i < resultCount;
             i++)
        {
            int randomIndex =
                UnityEngine.Random.Range(
                    i,
                    availableBoons.Count
                );

            BoonData temp =
                availableBoons[i];

            availableBoons[i] =
                availableBoons[randomIndex];

            availableBoons[randomIndex] =
                temp;

            selectedChoices.Add(
                availableBoons[i]
            );
        }
    }

    private bool IsAllowedBoonCategory(
        BoonCategory category)
    {
        if (boonCategory != BoonCategory.None)
        {
            return category == boonCategory;
        }

        return category == BoonCategory.Attack ||
               category == BoonCategory.Defense ||
               category == BoonCategory.Mobility ||
               category == BoonCategory.Debuff;
    }

    private void HandleBoonSelected(
        BoonData selectedBoon)
    {
        selectionInProgress = false;

        if (selectedBoon == null ||
            currentPlayer == null ||
            currentBoonInfo == null)
        {
            UnblockInteraction();
            return;
        }

        if (currentPlayer.money == null ||
            !currentPlayer.money.TrySpend(price))
        {
            ShowMessage(notEnoughGoldMessage);
            UnblockInteraction();
            return;
        }

        bool added =
            currentBoonInfo.TryAddBoon(
                selectedBoon
            );

        if (!added)
        {
            currentPlayer.money.AddMoney(price);

            Debug.LogWarning(
                $"[ShopPurchase] 득도 구매 실패로 골드를 환불했습니다. " +
                $"득도={selectedBoon.displayName}",
                this
            );

            UnblockInteraction();
            return;
        }

        if (showLogs)
        {
            Debug.Log(
                $"[ShopPurchase] 득도 구매 완료 / " +
                $"득도={selectedBoon.displayName}, " +
                $"가격={price}, " +
                $"현재 골드={currentPlayer.money.CurrentMoney}",
                this
            );
        }

        CompletePurchase();
    }

    private void HandleBoonCancelled()
    {
        selectionInProgress = false;
        UnblockInteraction();

        if (currentPlayerInteraction != null &&
            playerColliders.Count > 0 &&
            CanInteract())
        {
            currentPlayerInteraction
                .RegisterInteractable(this);
        }
    }

    private void CompletePurchase()
    {
        purchased = true;
        selectionInProgress = false;

        ClearPlayerReference(true);

        if (hideObjectAfterPurchase)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(
        Collider other)
    {
        TryRegisterPlayer(other);
    }

    private void OnTriggerStay(
        Collider other)
    {
        TryRegisterPlayer(other);
    }

    private void TryRegisterPlayer(
        Collider other)
    {
        if (other == null ||
            (purchaseOnce && purchased))
        {
            return;
        }

        Player player =
            other.GetComponentInParent<Player>();

        if (player == null)
        {
            return;
        }

        if (currentPlayer != null &&
            currentPlayer != player)
        {
            return;
        }

        playerColliders.Add(other);

        if (currentPlayer == player &&
            currentPlayerInteraction != null)
        {
            return;
        }

        PlayerInteraction interaction =
            player.GetComponent<PlayerInteraction>();

        if (interaction == null)
        {
            interaction =
                player.GetComponentInChildren<PlayerInteraction>(
                    true
                );
        }

        if (interaction == null)
        {
            Debug.LogError(
                "[ShopPurchase] Player에 PlayerInteraction이 없습니다.",
                player
            );

            return;
        }

        currentPlayer = player;
        currentPlayerInteraction =
            interaction;

        currentBoonInfo =
            player.GetComponent<BoonInfo>();

        if (currentBoonInfo == null)
        {
            currentBoonInfo =
                player.GetComponentInChildren<BoonInfo>(
                    true
                );
        }

        currentPlayerInteraction
            .RegisterInteractable(this);
    }

    private void OnTriggerExit(
        Collider other)
    {
        Player player =
            other.GetComponentInParent<Player>();

        if (player == null ||
            player != currentPlayer)
        {
            return;
        }

        playerColliders.Remove(other);

        if (playerColliders.Count > 0)
        {
            return;
        }

        ClearPlayerReference(false);
    }

    private void ClearPlayerReference(
        bool unblockInteraction)
    {
        if (currentPlayerInteraction != null)
        {
            currentPlayerInteraction
                .UnregisterInteractable(this);

            if (unblockInteraction)
            {
                currentPlayerInteraction
                    .SetInteractionBlocked(false);
            }
        }

        playerColliders.Clear();

        currentPlayer = null;
        currentPlayerInteraction = null;
        currentBoonInfo = null;
    }

    private void UnblockInteraction()
    {
        if (currentPlayerInteraction != null)
        {
            currentPlayerInteraction
                .SetInteractionBlocked(false);
        }
    }

    private void ShowMessage(
        string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        if (activeMessageText != null)
        {
            Destroy(activeMessageText.gameObject);
            activeMessageText = null;
        }

        activeMessageText =
            CreateMessageText(message);

        if (activeMessageText == null)
        {
            return;
        }

        messageRoutine =
            StartCoroutine(
                MessageRoutine(activeMessageText)
            );
    }

    private TextMeshPro CreateMessageText(
        string message)
    {
        TextMeshPro text;

        if (messageTextPrefab != null)
        {
            text =
                Instantiate(
                    messageTextPrefab,
                    transform.position + messageOffset,
                    Quaternion.identity
                );
        }
        else
        {
            GameObject textObject =
                new GameObject(
                    "ShopMessageText"
                );

            textObject.transform.position =
                transform.position + messageOffset;

            text =
                textObject.AddComponent<TextMeshPro>();

            text.alignment =
                TextAlignmentOptions.Center;

            text.fontSize = 3f;
        }

        text.text = message;

        return text;
    }

    private IEnumerator MessageRoutine(
        TextMeshPro text)
    {
        Vector3 startPosition =
            transform.position + messageOffset;

        Vector3 endPosition =
            startPosition +
            Vector3.up * messageRiseDistance;

        float elapsed = 0f;

        while (elapsed < messageDuration)
        {
            if (text == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;

            float ratio =
                Mathf.Clamp01(
                    elapsed / messageDuration
                );

            text.transform.position =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    ratio
                );

            Camera camera =
                Camera.main;

            if (camera != null)
            {
                text.transform.rotation =
                    Quaternion.LookRotation(
                        text.transform.position -
                        camera.transform.position
                    );
            }

            yield return null;
        }

        if (text != null)
        {
            Destroy(text.gameObject);
        }

        activeMessageText = null;
        messageRoutine = null;
    }
}

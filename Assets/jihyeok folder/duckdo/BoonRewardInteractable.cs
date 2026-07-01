using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 상호작용하면
/// 하이라키에 미리 만들어둔 보상 UI를 연다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BoonRewardInteractable : InteractableBase
{
    [Header("득도 데이터베이스")]
    [SerializeField]
    private BoonDatabase boonDatabase;

    [Header("보상 UI")]
    [Tooltip("하이라키에 있는 BoonRewardUI를 직접 연결")]
    [SerializeField]
    private BoonRewardUI rewardUI;

    [Header("보상 설정")]
    [Range(1, 3)]
    [SerializeField]
    private int choiceCount = 3;

    [SerializeField]
    private bool disableAfterSelection = true;

    // 현재 등장 가능한 득도
    private readonly List<BoonData> availableBoons =
        new List<BoonData>();

    // UI에 보여줄 득도
    private readonly List<BoonData> selectedChoices =
        new List<BoonData>();

    // 플레이어 Collider가 여러 개일 때 처리
    private readonly HashSet<Collider> playerColliders =
        new HashSet<Collider>();

    private Player currentPlayer;
    private BoonInfo currentBoonInfo;
    private PlayerInteraction currentInteraction;

    private bool rewardUsed;
    private bool selectionOpen;

    private void Awake()
    {
        Collider triggerCollider =
            GetComponent<Collider>();

        triggerCollider.isTrigger = true;

        if (boonDatabase == null)
        {
            Debug.LogError(
                "[BoonRewardInteractable] BoonDatabase를 연결하세요.",
                this
            );
        }

        if (rewardUI == null)
        {
            Debug.LogError(
                "[BoonRewardInteractable] " +
                "하이라키의 BoonRewardUI를 연결하세요.",
                this
            );
        }
    }

    public override bool CanInteract()
    {
        return !rewardUsed &&
               !selectionOpen &&
               boonDatabase != null &&
               boonDatabase.Count > 0 &&
               rewardUI != null &&
               !rewardUI.IsOpen &&
               currentPlayer != null &&
               currentBoonInfo != null &&
               currentInteraction != null;
    }

    public override void Interact(
        Player player
    )
    {
        if (!CanInteract())
        {
            return;
        }

        if (player == null ||
            player != currentPlayer)
        {
            return;
        }

        CreateChoices();

        if (selectedChoices.Count == 0)
        {
            Debug.LogWarning(
                "[BoonRewardInteractable] " +
                "현재 등장 가능한 득도가 없습니다.",
                this
            );

            return;
        }

        bool opened =
            rewardUI.Open(
                selectedChoices,
                HandleBoonSelected,
                HandleSelectionCancelled
            );

        if (!opened)
        {
            return;
        }

        selectionOpen = true;

        currentInteraction
            .SetInteractionBlocked(true);
    }

    private void CreateChoices()
    {
        availableBoons.Clear();
        selectedChoices.Clear();

        if (boonDatabase == null ||
            currentBoonInfo == null)
        {
            return;
        }

        List<BoonData> databaseBoons =
            boonDatabase.Boons;

        if (databaseBoons == null)
        {
            return;
        }

        for (int i = 0;
             i < databaseBoons.Count;
             i++)
        {
            BoonData boon =
                databaseBoons[i];

            if (boon == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    boon.boonId
                ))
            {
                continue;
            }

            // 중첩, 전설, 듀오 조건 검사
            if (currentBoonInfo.CanOffer(boon))
            {
                availableBoons.Add(boon);
            }
        }

        int resultCount =
            Mathf.Min(
                choiceCount,
                availableBoons.Count
            );

        // 중복 없이 랜덤 선택
        for (int i = 0;
             i < resultCount;
             i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    availableBoons.Count
                );

            selectedChoices.Add(
                availableBoons[randomIndex]
            );

            availableBoons.RemoveAt(
                randomIndex
            );
        }
    }

    private void HandleBoonSelected(
        BoonData selectedBoon
    )
    {
        selectionOpen = false;

        currentInteraction?
            .SetInteractionBlocked(false);

        if (selectedBoon == null ||
            currentBoonInfo == null)
        {
            return;
        }

        bool added =
            currentBoonInfo.TryAddBoon(
                selectedBoon
            );

        if (!added)
        {
            Debug.LogWarning(
                $"[BoonRewardInteractable] " +
                $"{selectedBoon.displayName} 획득 실패",
                this
            );

            return;
        }

        rewardUsed = true;

        Debug.Log(
            $"[BoonRewardInteractable] " +
            $"{selectedBoon.displayName} 획득 완료",
            this
        );

        currentInteraction?
            .UnregisterInteractable(this);

        if (disableAfterSelection)
        {
            gameObject.SetActive(false);
        }
    }

    private void HandleSelectionCancelled()
    {
        selectionOpen = false;

        currentInteraction?
            .SetInteractionBlocked(false);

        if (currentInteraction != null &&
            playerColliders.Count > 0 &&
            !rewardUsed)
        {
            currentInteraction
                .RegisterInteractable(this);
        }
    }

    private void OnTriggerEnter(
        Collider other
    )
    {
        if (rewardUsed)
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

        // 같은 플레이어의 다른 Collider 진입
        if (currentPlayer == player)
        {
            return;
        }

        BoonInfo boonInfo =
            player.GetComponent<BoonInfo>();

        PlayerInteraction interaction =
            player.GetComponent<PlayerInteraction>();

        if (boonInfo == null)
        {
            Debug.LogError(
                "[BoonRewardInteractable] " +
                "Player에 BoonInfo가 없습니다.",
                player
            );

            playerColliders.Remove(other);
            return;
        }

        if (interaction == null)
        {
            Debug.LogError(
                "[BoonRewardInteractable] " +
                "Player에 PlayerInteraction이 없습니다.",
                player
            );

            playerColliders.Remove(other);
            return;
        }

        if (rewardUI == null)
        {
            Debug.LogError(
                "[BoonRewardInteractable] " +
                "Reward UI가 연결되지 않았습니다.",
                this
            );

            playerColliders.Remove(other);
            return;
        }

        currentPlayer = player;
        currentBoonInfo = boonInfo;
        currentInteraction = interaction;

        currentInteraction
            .RegisterInteractable(this);
    }

    private void OnTriggerExit(
        Collider other
    )
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

        currentInteraction?
            .UnregisterInteractable(this);

        // UI가 열린 중에는 선택 콜백에 참조가 필요함
        if (!selectionOpen)
        {
            ClearPlayerReferences();
        }
    }

    private void ClearPlayerReferences()
    {
        currentPlayer = null;
        currentBoonInfo = null;
        currentInteraction = null;
    }

    private void OnDisable()
    {
        currentInteraction?
            .UnregisterInteractable(this);

        currentInteraction?
            .SetInteractionBlocked(false);

        playerColliders.Clear();
        selectionOpen = false;

        ClearPlayerReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        choiceCount =
            Mathf.Clamp(
                choiceCount,
                1,
                3
            );
    }
#endif
}
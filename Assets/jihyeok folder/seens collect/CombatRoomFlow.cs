using UnityEngine;

/// <summary>
/// 전투방 진행 관리.
/// 몬스터 전멸 -> 꺼져 있던 보상 오브젝트 활성화
/// -> E키로 득도 획득 -> 보상 오브젝트 비활성화
/// -> 다음 방 문 개방.
/// </summary>
public class CombatRoomFlow : MonoBehaviour
{
    [Header("필수 연결")]
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private BoonRewardInteractable boonReward;
    [SerializeField] private RoomChoiceGenerator roomChoiceGenerator;

    private bool battleClearHandled;
    private bool rewardCompleted;

    private void Awake()
    {
        // 전투 시작 전에는 보상 오브젝트 전체를 꺼둔다.
        if (boonReward != null)
        {
            boonReward.gameObject.SetActive(false);
        }

        // 전투 시작 전에는 문을 사용할 수 없게 한다.
        if (roomChoiceGenerator != null)
        {
            roomChoiceGenerator.HideDoors();
        }
    }

    private void OnEnable()
    {
        if (monsterSpawner == null)
        {
            return;
        }

        // 중복 구독 방지.
        monsterSpawner.OnAllPhasesCleared -= HandleBattleCleared;
        monsterSpawner.OnAllPhasesCleared += HandleBattleCleared;
    }

    private void Start()
    {
        ValidateReferences();

        // 이벤트 연결 전에 이미 클리어된 예외 상황 보정.
        if (monsterSpawner != null && monsterSpawner.IsCleared)
        {
            HandleBattleCleared();
        }
    }

    private void OnDisable()
    {
        if (monsterSpawner != null)
        {
            monsterSpawner.OnAllPhasesCleared -= HandleBattleCleared;
        }
    }

    private void HandleBattleCleared()
    {
        if (battleClearHandled)
        {
            return;
        }

        battleClearHandled = true;

        Debug.Log(
            "[CombatRoomFlow] 전투 클리어 이벤트 수신",
            this
        );

        RunFlowManager manager = RunFlowManager.Instance;

        if (manager == null || !manager.IsRunActive)
        {
            Debug.LogError(
                "[CombatRoomFlow] 진행 중인 런이 없습니다. " +
                "로비를 거쳐 전투방으로 진입해야 합니다.",
                this
            );

            return;
        }

        BoonCategory rewardCategory =
            manager.CurrentRewardCategory;

        Debug.Log(
            $"[CombatRoomFlow] 현재 득도 보상 태그: {rewardCategory}",
            this
        );

        // 전투방인데 태그가 없다면 보상을 생략하고 문을 연다.
        if (rewardCategory == BoonCategory.None)
        {
            Debug.LogWarning(
                "[CombatRoomFlow] 보상 태그가 None이므로 문을 바로 엽니다.",
                this
            );

            OpenNextDoors();
            return;
        }

        if (boonReward == null)
        {
            Debug.LogError(
                "[CombatRoomFlow] Boon Reward가 연결되지 않아 문을 바로 엽니다.",
                this
            );

            manager.ConsumeCurrentRewardCategory();
            OpenNextDoors();
            return;
        }

        // 추가: 이 시점에 플레이어를 확정 짓는다.
        Transform playerTransform = ResolvePlayerTransform();

        // 꺼져 있던 보상 오브젝트 전체를 몬스터 전멸 후 켠다.
        boonReward.gameObject.SetActive(true);

        // 활성화된 보상에 계열과 완료 콜백, 플레이어 위치를 전달한다.
        boonReward.Prepare(
            rewardCategory,
            HandleRewardCompleted,
            playerTransform
        );

        Debug.Log(
            "[CombatRoomFlow] 보상 오브젝트 ON",
            this
        );
    }

    private Transform ResolvePlayerTransform()
    {
        Player player = FindFirstObjectByType<Player>();

        if (player == null)
        {
            Debug.LogWarning(
                "[CombatRoomFlow] Player를 찾지 못해 보상이 기존 위치에 생성됩니다.",
                this
            );

            return null;
        }

        return player.transform;
    }

    private void HandleRewardCompleted()
    {
        if (rewardCompleted)
        {
            return;
        }

        rewardCompleted = true;

        Debug.Log(
            "[CombatRoomFlow] 득도 선택 완료",
            this
        );

        RunFlowManager manager = RunFlowManager.Instance;

        if (manager != null)
        {
            manager.ConsumeCurrentRewardCategory();
        }

        // 보상을 획득했으므로 오브젝트 전체를 다시 끈다.
        if (boonReward != null)
        {
            boonReward.gameObject.SetActive(false);
        }

        OpenNextDoors();
    }

    private void OpenNextDoors()
    {
        if (roomChoiceGenerator == null)
        {
            Debug.LogError(
                "[CombatRoomFlow] Room Choice Generator가 연결되지 않았습니다.",
                this
            );

            return;
        }

        roomChoiceGenerator.GenerateAndOpenDoors();

        Debug.Log(
            "[CombatRoomFlow] 다음 방 문 개방",
            this
        );
    }

    private void ValidateReferences()
    {
        if (monsterSpawner == null)
        {
            Debug.LogError(
                "[CombatRoomFlow] Monster Spawner가 연결되지 않았습니다.",
                this
            );
        }

        if (boonReward == null)
        {
            Debug.LogError(
                "[CombatRoomFlow] Boon Reward가 연결되지 않았습니다.",
                this
            );
        }

        if (roomChoiceGenerator == null)
        {
            Debug.LogError(
                "[CombatRoomFlow] Room Choice Generator가 연결되지 않았습니다.",
                this
            );
        }
    }
}
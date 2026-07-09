using UnityEngine;

/// <summary>
/// 전투방 흐름:
///
/// 씬 시작:
/// RoomChoiceGenerator가 직접 실행용 런을 준비하고
/// 문 위 목적지 프리팹을 표시한다.
///
/// 몬스터 전멸:
/// 플레이어 위치 위에 득도 보상을 떨어뜨린다.
///
/// 득도 선택 완료:
/// 이미 표시된 문을 개방한다.
/// </summary>
public class CombatRoomFlow : MonoBehaviour
{
    [Header("필수 연결")]
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private BoonRewardInteractable boonReward;
    [SerializeField] private RoomChoiceGenerator roomChoiceGenerator;

    [Header("보정")]
    [Tooltip("이벤트를 놓쳐도 MonsterSpawner.IsCleared를 검사")]
    [SerializeField] private bool useClearStateFallback = true;
    [SerializeField] private bool disableTrapsWhenCleared = true;
    [SerializeField] private GameObject[] extraTrapObjects;

    private bool battleHandled;
    private bool rewardCompleted;

    private void Awake()
    {
        // RoomChoiceGenerator가 더 이른 실행 순서에서
        // 직접 실행용 런을 초기화하지만 한 번 더 보정한다.
        if (roomChoiceGenerator != null)
        {
            roomChoiceGenerator
                .EnsureDirectPlayInitialized();
        }

        if (boonReward != null)
        {
            boonReward.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (monsterSpawner == null)
        {
            return;
        }

        monsterSpawner.OnAllPhasesCleared -=
            HandleBattleCleared;

        monsterSpawner.OnAllPhasesCleared +=
            HandleBattleCleared;
    }

    private void Start()
    {
        ValidateReferences();

        if (monsterSpawner != null &&
            monsterSpawner.IsCleared)
        {
            HandleBattleCleared();
        }
    }

    private void LateUpdate()
    {
        if (!useClearStateFallback ||
            battleHandled ||
            monsterSpawner == null ||
            !monsterSpawner.IsCleared)
        {
            return;
        }

        HandleBattleCleared();
    }

    private void OnDisable()
    {
        if (monsterSpawner != null)
        {
            monsterSpawner.OnAllPhasesCleared -=
                HandleBattleCleared;
        }
    }

    private void HandleBattleCleared()
    {
        if (battleHandled)
        {
            return;
        }

        RunFlowManager manager =
            RunFlowManager.Instance;

        if (manager == null ||
            !manager.IsRunActive)
        {
            bool initialized =
                roomChoiceGenerator != null &&
                roomChoiceGenerator
                    .EnsureDirectPlayInitialized();

            manager =
                RunFlowManager.Instance;

            if (!initialized ||
                manager == null ||
                !manager.IsRunActive)
            {
                // 직접 실행 테스트 때문에 Error Pause가 걸리지 않도록
                // 예상 가능한 상태는 Warning으로만 남긴다.
                Debug.LogWarning(
                    "[CombatRoomFlow] 런 초기화 실패로 " +
                    "보상과 문 개방 처리를 건너뜁니다. " +
                    "RoomChoiceGenerator의 Direct Play Room Graph를 확인하세요.",
                    this
                );

                return;
            }
        }

        // 런이 확인된 뒤에만 중복 처리 방지 상태를 확정한다.
        battleHandled = true;
        DisableRoomTraps();

        BoonCategory category =
            manager.CurrentRewardCategory;

        if (category == BoonCategory.None)
        {
            OpenNextDoors();
            return;
        }

        if (boonReward == null)
        {
            Debug.LogWarning(
                "[CombatRoomFlow] 보상 오브젝트가 없어 문만 개방합니다.",
                this
            );

            manager
                .ConsumeCurrentRewardCategory();

            OpenNextDoors();
            return;
        }

        boonReward.gameObject
            .SetActive(true);

        boonReward.Prepare(
            category,
            HandleRewardCompleted
        );
    }

    private void HandleRewardCompleted()
    {
        if (rewardCompleted)
        {
            return;
        }

        rewardCompleted = true;

        RunFlowManager manager =
            RunFlowManager.Instance;

        if (manager != null)
        {
            manager
                .ConsumeCurrentRewardCategory();
        }

        if (boonReward != null)
        {
            boonReward.gameObject
                .SetActive(false);
        }

        OpenNextDoors();
    }

    private void OpenNextDoors()
    {
        if (roomChoiceGenerator == null)
        {
            Debug.LogWarning(
                "[CombatRoomFlow] RoomChoiceGenerator가 없어 문을 열 수 없습니다.",
                this
            );

            return;
        }

        roomChoiceGenerator
            .OpenPreparedDoors();
    }

    private void DisableRoomTraps()
    {
        if (!disableTrapsWhenCleared)
        {
            return;
        }

        StopTrapComponents<Spike>();
        StopTrapComponents<Lava>();
        StopTrapComponents<LavaPool>();
        StopTrapComponents<LavaCouldron>();
        StopTrapComponents<WarningCircle>();

        if (extraTrapObjects == null)
        {
            return;
        }

        for (int i = 0; i < extraTrapObjects.Length; i++)
        {
            if (extraTrapObjects[i] != null)
            {
                StopTrapObject(extraTrapObjects[i]);
            }
        }
    }

    private void StopTrapComponents<T>()
        where T : Component
    {
        T[] traps =
            FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < traps.Length; i++)
        {
            T trap =
                traps[i];

            if (trap == null ||
                trap.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            StopTrapComponent(trap);
        }
    }

    private void StopTrapObject(
        GameObject trapObject)
    {
        if (trapObject == null)
        {
            return;
        }

        Spike[] spikes =
            trapObject.GetComponentsInChildren<Spike>(true);
        Lava[] lavas =
            trapObject.GetComponentsInChildren<Lava>(true);
        LavaPool[] lavaPools =
            trapObject.GetComponentsInChildren<LavaPool>(true);
        LavaCouldron[] lavaCouldrons =
            trapObject.GetComponentsInChildren<LavaCouldron>(true);
        WarningCircle[] warningCircles =
            trapObject.GetComponentsInChildren<WarningCircle>(true);

        for (int i = 0; i < spikes.Length; i++)
        {
            StopTrapComponent(spikes[i]);
        }

        for (int i = 0; i < lavas.Length; i++)
        {
            StopTrapComponent(lavas[i]);
        }

        for (int i = 0; i < lavaPools.Length; i++)
        {
            StopTrapComponent(lavaPools[i]);
        }

        for (int i = 0; i < lavaCouldrons.Length; i++)
        {
            StopTrapComponent(lavaCouldrons[i]);
        }

        for (int i = 0; i < warningCircles.Length; i++)
        {
            StopTrapComponent(warningCircles[i]);
        }
    }

    private void StopTrapComponent(
        Component component)
    {
        if (component == null)
        {
            return;
        }

        Spike spike =
            component as Spike;

        if (spike != null)
        {
            spike.StopTrap();
            return;
        }

        Lava lava =
            component as Lava;

        if (lava != null)
        {
            lava.StopTrap();
            return;
        }

        LavaPool lavaPool =
            component as LavaPool;

        if (lavaPool != null)
        {
            lavaPool.StopTrap();
            return;
        }

        LavaCouldron lavaCouldron =
            component as LavaCouldron;

        if (lavaCouldron != null)
        {
            lavaCouldron.StopTrap();
            return;
        }

        WarningCircle warningCircle =
            component as WarningCircle;

        if (warningCircle != null)
        {
            warningCircle.enabled = false;
        }
    }

    private void ValidateReferences()
    {
        if (monsterSpawner == null)
        {
            Debug.LogWarning(
                "[CombatRoomFlow] MonsterSpawner가 연결되지 않았습니다.",
                this
            );
        }

        if (boonReward == null)
        {
            Debug.LogWarning(
                "[CombatRoomFlow] BoonRewardInteractable이 연결되지 않았습니다.",
                this
            );
        }

        if (roomChoiceGenerator == null)
        {
            Debug.LogWarning(
                "[CombatRoomFlow] RoomChoiceGenerator가 연결되지 않았습니다.",
                this
            );
        }
    }
}

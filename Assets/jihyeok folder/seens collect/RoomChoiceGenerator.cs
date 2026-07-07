using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 첫 프레임에 다음 방 경로를 만들고
/// 각 문 위에 목적지 프리팹을 표시한다.
///
/// Combat 씬을 직접 실행한 경우에는
/// 테스트용 RunFlow 상태를 먼저 생성한다.
/// </summary>
[DefaultExecutionOrder(-500)]
public class RoomChoiceGenerator : MonoBehaviour
{
    [Header("문")]
    [SerializeField] private DungeonDoor leftDoor;
    [SerializeField] private DungeonDoor rightDoor;

    [Header("Combat 씬 직접 실행")]
    [Tooltip("체크하면 Combat 씬을 직접 Play해도 테스트 런을 자동 생성")]
    [SerializeField] private bool allowDirectScenePlay = true;

    [Tooltip("직접 실행할 때 사용할 RoomGraphData")]
    [SerializeField] private RoomGraphData directPlayRoomGraph;

    [Tooltip("직접 실행할 때 현재 전투방 보상 계열")]
    [SerializeField] private BoonCategory directPlayRewardCategory =
        BoonCategory.Attack;

    [Tooltip("직접 실행할 때 시작 층")]
    [Min(1)]
    [SerializeField] private int directPlayFloor = 1;

    [Header("보스 문")]
    [Tooltip("보스 진입 시 왼쪽 문 하나만 사용할지 여부")]
    [SerializeField] private bool useSingleDoorForBoss = true;

    [Header("초기화 재시도")]
    [Tooltip("다른 오브젝트 초기화가 늦을 때 재시도할 프레임 수")]
    [Min(1)]
    [SerializeField] private int prepareRetryFrames = 30;

    [Header("디버그")]
    [SerializeField] private bool showLogs = true;

    private readonly List<RoomRouteOption>
        preparedRoutes =
            new List<RoomRouteOption>(2);

    private bool prepared;
    private bool doorsOpened;

    private bool leftConfigured;
    private bool rightConfigured;

    private void Awake()
    {
        EnsureDirectPlayInitialized();

        // 목적지 표시는 유지하면서 문 이동만 잠근다.
        if (leftDoor != null)
        {
            leftDoor.Lock();
        }

        if (rightDoor != null)
        {
            rightDoor.Lock();
        }
    }

    private IEnumerator Start()
    {
        for (int frame = 0;
             frame < prepareRetryFrames;
             frame++)
        {
            if (PrepareDoorChoices(false))
            {
                yield break;
            }

            EnsureDirectPlayInitialized();

            yield return null;
        }

        PrepareDoorChoices(true);
    }

    /// <summary>
    /// 정상 런이 없을 때에만 테스트 런을 생성한다.
    /// 정상적인 Title -> Lobby 흐름에는 영향을 주지 않는다.
    /// </summary>
    public bool EnsureDirectPlayInitialized()
    {
        RunFlowManager manager =
            RunFlowManager.Instance;

        if (manager == null)
        {
            return false;
        }

        if (manager.IsRunActive)
        {
            return true;
        }

        if (!allowDirectScenePlay)
        {
            return false;
        }

        return manager
            .InitializeDirectCombatScene(
                directPlayRoomGraph,
                directPlayRewardCategory,
                directPlayFloor
            );
    }

    public bool PrepareDoorChoices()
    {
        return PrepareDoorChoices(true);
    }

    private bool PrepareDoorChoices(
        bool logFailure)
    {
        if (prepared)
        {
            return true;
        }

        RunFlowManager manager =
            RunFlowManager.Instance;

        if (manager == null)
        {
            if (logFailure)
            {
                Debug.LogWarning(
                    "[RoomChoiceGenerator] RunFlowManager가 없습니다.",
                    this
                );
            }

            return false;
        }

        if (!manager.IsRunActive &&
            !EnsureDirectPlayInitialized())
        {
            if (logFailure)
            {
                Debug.LogWarning(
                    "[RoomChoiceGenerator] 런 초기화에 실패했습니다. " +
                    "Direct Play Room Graph를 확인하세요.",
                    this
                );
            }

            return false;
        }

        List<RoomRouteOption> routes =
            manager.GenerateNextRoutes(2);

        if (routes == null ||
            routes.Count == 0)
        {
            if (logFailure)
            {
                Debug.LogWarning(
                    "[RoomChoiceGenerator] 다음 방 경로가 생성되지 않았습니다.",
                    this
                );
            }

            return false;
        }

        preparedRoutes.Clear();
        preparedRoutes.AddRange(routes);

        prepared = true;
        doorsOpened = false;

        ConfigurePreparedDoors();

        if (showLogs)
        {
            Debug.Log(
                "[RoomChoiceGenerator] 문 위 프리팹 생성 완료",
                this
            );
        }

        return true;
    }

    public void OpenPreparedDoors()
    {
        if (doorsOpened)
        {
            return;
        }

        if (!prepared &&
            !PrepareDoorChoices(true))
        {
            return;
        }

        doorsOpened = true;

        if (leftConfigured &&
            leftDoor != null)
        {
            leftDoor.Open();
        }

        if (rightConfigured &&
            rightDoor != null)
        {
            rightDoor.Open();
        }
    }

    public void GenerateAndOpenDoors()
    {
        OpenPreparedDoors();
    }

    /// <summary>
    /// 문 이동만 잠근다.
    /// 문 위 목적지 프리팹은 유지한다.
    /// </summary>
    public void HideDoors()
    {
        if (leftDoor != null)
        {
            leftDoor.Lock();
        }

        if (rightDoor != null)
        {
            rightDoor.Lock();
        }

        doorsOpened = false;
    }

    public void ClearPreparedChoices()
    {
        prepared = false;
        doorsOpened = false;

        leftConfigured = false;
        rightConfigured = false;

        preparedRoutes.Clear();

        if (leftDoor != null)
        {
            leftDoor.ClearRoute();
        }

        if (rightDoor != null)
        {
            rightDoor.ClearRoute();
        }
    }

    private void ConfigurePreparedDoors()
    {
        leftConfigured = false;
        rightConfigured = false;

        if (leftDoor != null)
        {
            if (preparedRoutes.Count > 0)
            {
                leftDoor.Configure(
                    preparedRoutes[0]
                );

                leftConfigured = true;
            }
            else
            {
                leftDoor.ClearRoute();
            }
        }

        if (rightDoor == null)
        {
            return;
        }

        RoomRouteOption firstRoute =
            preparedRoutes.Count > 0
                ? preparedRoutes[0]
                : null;

        bool isBossRoute =
            firstRoute != null &&
            firstRoute.TargetRoom != null &&
            firstRoute.TargetRoom.RoomType ==
                RoomType.Boss;

        if (isBossRoute)
        {
            if (useSingleDoorForBoss)
            {
                rightDoor.ClearRoute();
                rightConfigured = false;
            }
            else
            {
                rightDoor.Configure(
                    firstRoute
                );

                rightConfigured = true;
            }

            return;
        }

        if (preparedRoutes.Count >= 2)
        {
            rightDoor.Configure(
                preparedRoutes[1]
            );

            rightConfigured = true;
        }
        else
        {
            rightDoor.ClearRoute();
            rightConfigured = false;
        }
    }
}

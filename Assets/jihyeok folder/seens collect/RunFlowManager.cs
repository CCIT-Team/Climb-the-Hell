using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 런의 층, 현재 방, 그래프 선택, 미등장 보정,
/// Loading 씬을 통한 이동을 관리한다.
///
/// Player 생성/이동은 PlayerSceneMover가 담당한다.
/// RunFlowManager는 Loading 씬으로 보내기 전에 PlayerSceneMover에게 Player 보호만 요청한다.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class RunFlowManager : MonoBehaviour
{
    private static RunFlowManager instance;
    private static bool instanceWasAutoCreated;

    public static RunFlowManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance =
                    FindFirstObjectByType<RunFlowManager>();
            }

            if (instance == null)
            {
                GameObject managerObject =
                    new GameObject(
                        nameof(RunFlowManager)
                    );

                instance =
                    managerObject.AddComponent<RunFlowManager>();

                instanceWasAutoCreated = true;

                Debug.LogWarning(
                    "[RunFlowManager] 씬에 RunFlowManager가 없어 임시 매니저를 생성했습니다. " +
                    "정상 구조에서는 Title 씬에 설정된 RunFlowManager를 배치하세요.",
                    instance
                );
            }

            return instance;
        }
    }

    [Header("방 그래프")]
    [SerializeField]
    private RoomGraphData roomGraph;

    [Header("공통 씬")]
    [SerializeField]
    private string titleSceneName = "Title";

    [SerializeField]
    private string loadingSceneName = "Loading";

    [SerializeField]
    private string lobbySceneName = "Lobby";

    [Header("층 설정")]
    [Min(2)]
    [SerializeField]
    private int bossFloor = 10;

    [Header("1층 전투 보상")]
    [Tooltip("1층은 이전 문 선택이 없으므로 전투 보상 계열을 무작위로 정한다.")]
    [SerializeField]
    private bool randomizeFirstRewardCategory = true;

    [SerializeField]
    private BoonCategory fixedFirstRewardCategory =
        BoonCategory.Attack;

    [Header("디버그")]
    [SerializeField]
    private bool printSelectionLog = true;

    [Tooltip("Play Mode에서 다음 방 후보 수치를 확인할 수 있다.")]
    [SerializeField]
    private List<RoomDebugStat> debugCandidateStats =
        new List<RoomDebugStat>();

    private readonly Dictionary<string, RoomRuntimeStat> runtimeStats =
        new Dictionary<string, RoomRuntimeStat>();

    private RoomNodeData pendingRoom;
    private string pendingSceneName;
    private RoomType pendingRoomType;
    private BoonCategory pendingRewardCategory;
    private int pendingFloor;

    public RoomGraphData RoomGraph => roomGraph;

    public int CurrentFloor
    {
        get;
        private set;
    }

    public RoomNodeData CurrentRoom
    {
        get;
        private set;
    }

    public RoomNodeData PreviousRoom
    {
        get;
        private set;
    }

    public RoomType CurrentRoomType
    {
        get;
        private set;
    } = RoomType.None;

    public BoonCategory CurrentRewardCategory
    {
        get;
        private set;
    } = BoonCategory.None;

    public bool IsRunActive
    {
        get;
        private set;
    }

    public bool IsTransitioning
    {
        get;
        private set;
    }

    public bool HasPendingDestination
    {
        get;
        private set;
    }

    public int BossFloor => bossFloor;

    public IReadOnlyList<RoomDebugStat> DebugCandidateStats =>
        debugCandidateStats;

    private void Awake()
    {
        if (instance != null &&
            instance != this)
        {
            bool currentIsConfigured =
                roomGraph != null;

            bool oldInstanceIsTemporary =
                instanceWasAutoCreated ||
                instance.roomGraph == null;

            if (oldInstanceIsTemporary &&
                currentIsConfigured)
            {
                if (instance != null)
                {
                    Destroy(instance.gameObject);
                }

                instance = this;
                instanceWasAutoCreated = false;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            instance = this;
            instanceWasAutoCreated = false;
        }

        DontDestroyOnLoad(gameObject);

        BuildRuntimeStats();
    }

    public bool GoToLobby()
    {
        ResetRun();

        PlayerSpawnContext.SetSpawnId("Default");

        return RequestStaticScene(
            lobbySceneName,
            RoomType.Lobby
        );
    }

    public bool ReturnToTitle()
    {
        ResetRun();

        PlayerSpawnContext.SetSpawnId("Default");

        return RequestStaticScene(
            titleSceneName,
            RoomType.Title
        );
    }

    /// <summary>
    /// 로비에서 정상적인 새 런을 시작한다.
    /// Loading 씬을 거쳐 1층 전투방으로 이동한다.
    /// </summary>
    public bool StartNewRun()
    {
        if (!ValidateGraph())
        {
            return false;
        }

        ResetRun();

        IsRunActive = true;

        BoonCategory firstCategory =
            randomizeFirstRewardCategory
                ? GetRandomBoonCategory()
                : fixedFirstRewardCategory;

        RoomRouteOption firstRoute =
            new RoomRouteOption(
                roomGraph.FirstCombatRoom,
                1,
                firstCategory
            );

        PlayerSpawnContext.SetSpawnId("Default");

        bool requested =
            RequestRoute(firstRoute);

        if (!requested)
        {
            IsRunActive = false;
        }

        return requested;
    }

    /// <summary>
    /// Unity에서 Combat 씬을 직접 Play했을 때만
    /// 씬 이동 없이 테스트용 런 상태를 만든다.
    ///
    /// 이미 정상 런이 진행 중이면 아무것도 변경하지 않는다.
    /// </summary>
    public bool InitializeDirectCombatScene(
        RoomGraphData fallbackRoomGraph,
        BoonCategory testRewardCategory,
        int testFloor = 1
    )
    {
        if (IsRunActive)
        {
            return true;
        }

        if (roomGraph == null &&
            fallbackRoomGraph != null)
        {
            roomGraph = fallbackRoomGraph;
            BuildRuntimeStats();
        }

        if (roomGraph == null)
        {
            Debug.LogWarning(
                "[RunFlowManager] 직접 실행용 Room Graph가 없습니다. " +
                "RoomChoiceGenerator의 Direct Play Room Graph를 연결하세요.",
                this
            );

            return false;
        }

        if (!ValidateGraph())
        {
            Debug.LogWarning(
                "[RunFlowManager] Room Graph 검증 실패로 " +
                "직접 실행용 런을 만들 수 없습니다.",
                this
            );

            return false;
        }

        string activeSceneName =
            SceneManager
                .GetActiveScene()
                .name;

        RoomNodeData activeRoom =
            FindRoomBySceneName(
                activeSceneName
            );

        if (activeRoom == null ||
            activeRoom.RoomType != RoomType.Combat)
        {
            activeRoom =
                roomGraph.FirstCombatRoom;

            Debug.LogWarning(
                $"[RunFlowManager] 현재 씬 '{activeSceneName}'과 " +
                "일치하는 Combat RoomNode를 찾지 못했습니다. " +
                $"테스트용 현재 방으로 " +
                $"'{activeRoom.DisplayName}'을 사용합니다.",
                this
            );
        }

        ResetRun();

        IsRunActive = true;
        IsTransitioning = false;

        CurrentRoom = activeRoom;
        CurrentRoomType = RoomType.Combat;

        CurrentFloor =
            Mathf.Clamp(
                testFloor,
                1,
                Mathf.Max(
                    1,
                    bossFloor - 1
                )
            );

        CurrentRewardCategory =
            testRewardCategory == BoonCategory.None
                ? GetRandomBoonCategory()
                : testRewardCategory;

        RoomRuntimeStat currentStat =
            GetOrCreateStat(
                CurrentRoom
            );

        currentStat.visitedCount++;
        currentStat.lastVisitedFloor =
            CurrentFloor;

        Debug.Log(
            $"[RunFlowManager] Combat 씬 직접 실행용 런 초기화 / " +
            $"씬={activeSceneName}, " +
            $"현재 방={CurrentRoom.DisplayName}, " +
            $"층={CurrentFloor}, " +
            $"보상={CurrentRewardCategory}",
            this
        );

        return true;
    }

    private RoomNodeData FindRoomBySceneName(
        string sceneName
    )
    {
        if (roomGraph == null ||
            roomGraph.AllRooms == null ||
            string.IsNullOrWhiteSpace(sceneName))
        {
            return null;
        }

        IReadOnlyList<RoomNodeData> rooms =
            roomGraph.AllRooms;

        for (int i = 0;
             i < rooms.Count;
             i++)
        {
            RoomNodeData room =
                rooms[i];

            if (room == null)
            {
                continue;
            }

            if (string.Equals(
                    room.SceneName,
                    sceneName,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return room;
            }
        }

        return null;
    }

    /// <summary>
    /// 문에서 선택한 경로를 Loading 씬으로 전달한다.
    /// </summary>
    public bool RequestRoute(
        RoomRouteOption route
    )
    {
        if (route == null ||
            !route.IsValid)
        {
            Debug.LogError(
                "[RunFlowManager] 유효하지 않은 방 경로입니다.",
                this
            );

            return false;
        }

        if (IsTransitioning)
        {
            return false;
        }

        RoomNodeData target =
            route.TargetRoom;

        if (!CanLoadScene(target.SceneName))
        {
            Debug.LogError(
                $"[RunFlowManager] '{target.SceneName}' 씬을 불러올 수 없습니다. " +
                "Build Profiles의 Scene List를 확인하세요.",
                target
            );

            return false;
        }

        pendingRoom = target;
        pendingSceneName = target.SceneName;
        pendingRoomType = target.RoomType;

        pendingRewardCategory =
            target.RoomType == RoomType.Combat
                ? route.RewardCategory
                : BoonCategory.None;

        pendingFloor = route.TargetFloor;

        return BeginLoading();
    }

    /// <summary>
    /// 현재 방 완료 후 문에 들어갈 다음 경로를 생성한다.
    /// </summary>
    public List<RoomRouteOption> GenerateNextRoutes(
        int requestedCount = 2
    )
    {
        List<RoomRouteOption> routes =
            new List<RoomRouteOption>();

        if (!ValidateGraph() ||
            !IsRunActive)
        {
            return routes;
        }

        int nextFloor =
            CurrentFloor + 1;

        if (nextFloor >= bossFloor)
        {
            routes.Add(
                new RoomRouteOption(
                    roomGraph.BossRoom,
                    bossFloor,
                    BoonCategory.None
                )
            );

            debugCandidateStats.Clear();

            return routes;
        }

        List<WeightedCandidate> eligibleCandidates =
            BuildEligibleCandidates(
                nextFloor
            );

        if (eligibleCandidates.Count == 0)
        {
            Debug.LogError(
                $"[RunFlowManager] {nextFloor}층에 등장 가능한 다음 방이 없습니다.",
                this
            );

            return routes;
        }

        int selectionCount =
            Mathf.Min(
                Mathf.Max(
                    1,
                    requestedCount
                ),
                eligibleCandidates.Count
            );

        List<RoomNodeData> selectedRooms =
            new List<RoomNodeData>(
                selectionCount
            );

        for (int i = 0;
             i < selectionCount;
             i++)
        {
            WeightedCandidate selected =
                SelectCandidate(
                    eligibleCandidates
                );

            if (selected == null ||
                selected.room == null)
            {
                break;
            }

            selectedRooms.Add(
                selected.room
            );

            eligibleCandidates.Remove(
                selected
            );
        }

        UpdateOfferStats(
            selectedRooms,
            nextFloor
        );

        List<BoonCategory> categories =
            CreateShuffledCategories();

        int categoryIndex = 0;

        for (int i = 0;
             i < selectedRooms.Count;
             i++)
        {
            RoomNodeData room =
                selectedRooms[i];

            BoonCategory category =
                BoonCategory.None;

            if (room.RoomType == RoomType.Combat)
            {
                category =
                    categories[
                        categoryIndex %
                        categories.Count
                    ];

                categoryIndex++;
            }

            routes.Add(
                new RoomRouteOption(
                    room,
                    nextFloor,
                    category
                )
            );
        }

        if (printSelectionLog)
        {
            PrintGeneratedRoutes(
                routes
            );
        }

        return routes;
    }

    public string GetPendingSceneName()
    {
        if (HasPendingDestination &&
            CanLoadScene(pendingSceneName))
        {
            return pendingSceneName;
        }

        Debug.LogError(
            "[RunFlowManager] 저장된 목적지 씬이 없습니다.",
            this
        );

        return string.Empty;
    }

    public bool CommitPendingDestination()
    {
        if (!HasPendingDestination)
        {
            IsTransitioning = false;
            return false;
        }

        PreviousRoom = CurrentRoom;
        CurrentRoom = pendingRoom;
        CurrentFloor = pendingFloor;
        CurrentRoomType = pendingRoomType;

        CurrentRewardCategory =
            pendingRoomType == RoomType.Combat
                ? pendingRewardCategory
                : BoonCategory.None;

        if (CurrentRoom != null &&
            IsRunActive)
        {
            RoomRuntimeStat stat =
                GetOrCreateStat(
                    CurrentRoom
                );

            stat.visitedCount++;
            stat.lastVisitedFloor =
                CurrentFloor;
        }

        ClearPending();

        IsTransitioning = false;

        return true;
    }

    public void ConsumeCurrentRewardCategory()
    {
        CurrentRewardCategory =
            BoonCategory.None;
    }

    public RoomRuntimeStat GetRuntimeStat(
        RoomNodeData room
    )
    {
        return GetOrCreateStat(room);
    }

    public string GetDebugText()
    {
        StringBuilder builder =
            new StringBuilder(512);

        builder.AppendLine(
            $"층: {CurrentFloor}/{bossFloor}"
        );

        builder.AppendLine(
            $"현재 방: " +
            $"{(CurrentRoom != null ? CurrentRoom.DisplayName : "없음")}"
        );

        builder.AppendLine(
            $"방 타입: {CurrentRoomType}"
        );

        builder.AppendLine(
            $"전투 보상 태그: {CurrentRewardCategory}"
        );

        builder.AppendLine();
        builder.AppendLine(
            "다음 방 후보 가중치"
        );

        for (int i = 0;
             i < debugCandidateStats.Count;
             i++)
        {
            RoomDebugStat stat =
                debugCandidateStats[i];

            builder.Append(
                stat.eligible
                    ? "[가능] "
                    : "[제외] "
            );

            builder.Append(
                stat.roomName
            );

            builder.Append(
                $" / 기본 {stat.baseWeight:0.##}"
            );

            builder.Append(
                $" / 보정 +{stat.pityBonus:0.##}"
            );

            builder.Append(
                $" / 최종 {stat.finalWeight:0.##}"
            );

            builder.Append(
                $" / 미등장 {stat.missedOfferCount}"
            );

            if (!stat.eligible &&
                !string.IsNullOrWhiteSpace(stat.reason))
            {
                builder.Append(
                    $" / {stat.reason}"
                );
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    public void ResetRun()
    {
        IsRunActive = false;
        CurrentFloor = 0;
        CurrentRoom = null;
        PreviousRoom = null;
        CurrentRoomType = RoomType.None;
        CurrentRewardCategory = BoonCategory.None;
        IsTransitioning = false;

        ClearPending();
        BuildRuntimeStats();
        debugCandidateStats.Clear();
    }

    private bool RequestStaticScene(
        string sceneName,
        RoomType roomType
    )
    {
        if (IsTransitioning)
        {
            return false;
        }

        if (!CanLoadScene(sceneName))
        {
            Debug.LogError(
                $"[RunFlowManager] '{sceneName}' 씬을 불러올 수 없습니다.",
                this
            );

            return false;
        }

        pendingRoom = null;
        pendingSceneName = sceneName;
        pendingRoomType = roomType;
        pendingRewardCategory = BoonCategory.None;
        pendingFloor = 0;

        return BeginLoading();
    }

    private bool BeginLoading()
    {
        if (!CanLoadScene(loadingSceneName))
        {
            Debug.LogError(
                $"[RunFlowManager] Loading 씬 '{loadingSceneName}'을 불러올 수 없습니다.",
                this
            );

            ClearPending();

            return false;
        }

        HasPendingDestination = true;
        IsTransitioning = true;
        Time.timeScale = 1f;

        if (PlayerSceneMover.Instance != null)
        {
            PlayerSceneMover.Instance
                .PreparePlayerForSceneTransition();
        }
        else
        {
            Debug.LogWarning(
                "[RunFlowManager] PlayerSceneMover가 없어 Player 보호 처리를 건너뜁니다.",
                this
            );
        }

        SceneManager.LoadScene(
            loadingSceneName
        );

        return true;
    }

    private List<WeightedCandidate> BuildEligibleCandidates(
        int targetFloor
    )
    {
        List<RoomNodeData> sourceRooms =
            GetConnectedRooms();

        List<WeightedCandidate> eligible =
            new List<WeightedCandidate>();

        debugCandidateStats.Clear();

        for (int i = 0;
             i < sourceRooms.Count;
             i++)
        {
            RoomNodeData room =
                sourceRooms[i];

            if (room == null)
            {
                continue;
            }

            string reason;

            bool canAppear =
                IsEligible(
                    room,
                    targetFloor,
                    out reason
                );

            RoomRuntimeStat runtime =
                GetOrCreateStat(room);

            float pityBonus =
                room.UsePityWeight
                    ? runtime.missedOfferCount *
                      room.PityWeightPerMiss
                    : 0f;

            float finalWeight =
                Mathf.Max(
                    0f,
                    room.BaseWeight +
                    pityBonus
                );

            RoomDebugStat debug =
                new RoomDebugStat
                {
                    roomId = room.RoomId,
                    roomName = room.DisplayName,
                    roomType = room.RoomType,
                    eligible =
                        canAppear &&
                        finalWeight > 0f,
                    reason = reason,
                    baseWeight = room.BaseWeight,
                    pityBonus = pityBonus,
                    finalWeight = finalWeight,
                    missedOfferCount =
                        runtime.missedOfferCount,
                    offeredCount =
                        runtime.offeredCount,
                    visitedCount =
                        runtime.visitedCount
                };

            debugCandidateStats.Add(debug);

            if (!canAppear ||
                finalWeight <= 0f)
            {
                continue;
            }

            bool guaranteed =
                room.UsePityWeight &&
                room.GuaranteedAfterMisses > 0 &&
                runtime.missedOfferCount >=
                room.GuaranteedAfterMisses;

            eligible.Add(
                new WeightedCandidate(
                    room,
                    finalWeight,
                    guaranteed,
                    runtime.missedOfferCount
                )
            );
        }

        return eligible;
    }

    private List<RoomNodeData> GetConnectedRooms()
    {
        List<RoomNodeData> result =
            new List<RoomNodeData>();

        if (CurrentRoom != null &&
            CurrentRoom.NextRooms != null &&
            CurrentRoom.NextRooms.Count > 0)
        {
            for (int i = 0;
                 i < CurrentRoom.NextRooms.Count;
                 i++)
            {
                RoomNodeData room =
                    CurrentRoom.NextRooms[i];

                if (room != null &&
                    !result.Contains(room))
                {
                    result.Add(room);
                }
            }

            return result;
        }

        if (roomGraph != null &&
            roomGraph.AllRooms != null)
        {
            for (int i = 0;
                 i < roomGraph.AllRooms.Count;
                 i++)
            {
                RoomNodeData room =
                    roomGraph.AllRooms[i];

                if (room != null &&
                    !result.Contains(room))
                {
                    result.Add(room);
                }
            }
        }

        return result;
    }

    private bool IsEligible(
        RoomNodeData room,
        int targetFloor,
        out string reason
    )
    {
        reason = string.Empty;

        if (!room.CanAppearRandomly)
        {
            reason = "랜덤 등장 비활성화";
            return false;
        }

        if (room.RoomType == RoomType.Boss)
        {
            reason = "보스는 10층 고정";
            return false;
        }

        if (targetFloor < room.MinimumFloor ||
            targetFloor > room.MaximumFloor)
        {
            reason = "등장 층 범위 밖";
            return false;
        }

        if (!room.AllowImmediateRepeat &&
            CurrentRoom == room)
        {
            reason = "직전 방 반복 금지";
            return false;
        }

        if (PreviousRoom == room)
        {
            reason = "Previous room excluded once";
            return false;
        }

        RoomRuntimeStat stat =
            GetOrCreateStat(room);

        if (room.MaximumVisitsPerRun > 0 &&
            stat.visitedCount >= room.MaximumVisitsPerRun)
        {
            reason = "런 최대 방문 횟수 도달";
            return false;
        }

        if (room.MinimumFloorGap > 0 &&
            stat.lastVisitedFloor >= 0)
        {
            int floorGap =
                targetFloor -
                stat.lastVisitedFloor;

            if (floorGap < room.MinimumFloorGap)
            {
                reason = "최소 등장 간격 미달";
                return false;
            }
        }

        return true;
    }

    private WeightedCandidate SelectCandidate(
        List<WeightedCandidate> candidates
    )
    {
        if (candidates == null ||
            candidates.Count == 0)
        {
            return null;
        }

        List<WeightedCandidate> guaranteed =
            new List<WeightedCandidate>();

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            if (candidates[i].guaranteed)
            {
                guaranteed.Add(candidates[i]);
            }
        }

        if (guaranteed.Count > 0)
        {
            WeightedCandidate best =
                guaranteed[0];

            for (int i = 1;
                 i < guaranteed.Count;
                 i++)
            {
                WeightedCandidate current =
                    guaranteed[i];

                if (current.missCount > best.missCount ||
                    (
                        current.missCount == best.missCount &&
                        current.weight > best.weight
                    ))
                {
                    best = current;
                }
            }

            return best;
        }

        float totalWeight = 0f;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            totalWeight +=
                Mathf.Max(
                    0f,
                    candidates[i].weight
                );
        }

        if (totalWeight <= 0f)
        {
            return candidates[
                Random.Range(
                    0,
                    candidates.Count
                )
            ];
        }

        float randomValue =
            Random.Range(
                0f,
                totalWeight
            );

        float accumulated = 0f;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            accumulated +=
                Mathf.Max(
                    0f,
                    candidates[i].weight
                );

            if (randomValue <= accumulated)
            {
                return candidates[i];
            }
        }

        return candidates[
            candidates.Count - 1
        ];
    }

    private void UpdateOfferStats(
        List<RoomNodeData> selectedRooms,
        int offeredFloor
    )
    {
        HashSet<RoomNodeData> selected =
            new HashSet<RoomNodeData>(
                selectedRooms
            );

        for (int i = 0;
             i < debugCandidateStats.Count;
             i++)
        {
            RoomDebugStat debug =
                debugCandidateStats[i];

            if (!debug.eligible)
            {
                continue;
            }

            RoomNodeData room =
                roomGraph.FindRoom(
                    debug.roomId
                );

            if (room == null)
            {
                continue;
            }

            RoomRuntimeStat stat =
                GetOrCreateStat(room);

            if (selected.Contains(room))
            {
                stat.offeredCount++;
                stat.lastOfferedFloor =
                    offeredFloor;

                stat.missedOfferCount = 0;
            }
            else if (room.UsePityWeight)
            {
                stat.missedOfferCount++;
            }

            debug.missedOfferCount =
                stat.missedOfferCount;

            debug.offeredCount =
                stat.offeredCount;

            debug.visitedCount =
                stat.visitedCount;
        }
    }

    private List<BoonCategory> CreateShuffledCategories()
    {
        List<BoonCategory> categories =
            new List<BoonCategory>
            {
                BoonCategory.Attack,
                BoonCategory.Defense,
                BoonCategory.Mobility,
                BoonCategory.Debuff
            };

        for (int i = categories.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            BoonCategory temporary =
                categories[i];

            categories[i] =
                categories[randomIndex];

            categories[randomIndex] =
                temporary;
        }

        return categories;
    }

    private BoonCategory GetRandomBoonCategory()
    {
        switch (Random.Range(0, 4))
        {
            case 0:
                return BoonCategory.Attack;

            case 1:
                return BoonCategory.Defense;

            case 2:
                return BoonCategory.Mobility;

            default:
                return BoonCategory.Debuff;
        }
    }

    private RoomRuntimeStat GetOrCreateStat(
        RoomNodeData room
    )
    {
        if (room == null)
        {
            return new RoomRuntimeStat("NULL");
        }

        RoomRuntimeStat stat;

        if (!runtimeStats.TryGetValue(
                room.RoomId,
                out stat))
        {
            stat =
                new RoomRuntimeStat(
                    room.RoomId
                );

            runtimeStats.Add(
                room.RoomId,
                stat
            );
        }

        return stat;
    }

    private void BuildRuntimeStats()
    {
        runtimeStats.Clear();

        if (roomGraph == null ||
            roomGraph.AllRooms == null)
        {
            return;
        }

        for (int i = 0;
             i < roomGraph.AllRooms.Count;
             i++)
        {
            RoomNodeData room =
                roomGraph.AllRooms[i];

            if (room == null ||
                string.IsNullOrWhiteSpace(room.RoomId))
            {
                continue;
            }

            runtimeStats[room.RoomId] =
                new RoomRuntimeStat(
                    room.RoomId
                );
        }
    }

    private bool ValidateGraph()
    {
        if (roomGraph == null)
        {
            Debug.LogError(
                "[RunFlowManager] Room Graph가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        return roomGraph.ValidateGraph();
    }

    private bool CanLoadScene(
        string sceneName
    )
    {
        return
            !string.IsNullOrWhiteSpace(sceneName) &&
            Application.CanStreamedLevelBeLoaded(
                sceneName
            );
    }

    private void ClearPending()
    {
        pendingRoom = null;
        pendingSceneName = string.Empty;
        pendingRoomType = RoomType.None;
        pendingRewardCategory = BoonCategory.None;
        pendingFloor = 0;
        HasPendingDestination = false;
    }

    private void PrintGeneratedRoutes(
        List<RoomRouteOption> routes
    )
    {
        StringBuilder builder =
            new StringBuilder();

        builder.AppendLine(
            $"[RunFlowManager] {CurrentFloor + 1}층 문 생성"
        );

        for (int i = 0;
             i < routes.Count;
             i++)
        {
            RoomRouteOption route =
                routes[i];

            builder.AppendLine(
                $"{i + 1}. " +
                $"{route.TargetRoom.DisplayName} / " +
                $"{route.TargetRoom.RoomType} / " +
                $"태그 {route.RewardCategory}"
            );
        }

        Debug.Log(
            builder.ToString(),
            this
        );
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            instanceWasAutoCreated = false;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(titleSceneName))
        {
            titleSceneName = "Title";
        }

        if (string.IsNullOrWhiteSpace(loadingSceneName))
        {
            loadingSceneName = "Loading";
        }

        if (string.IsNullOrWhiteSpace(lobbySceneName))
        {
            lobbySceneName = "Lobby";
        }

        bossFloor =
            Mathf.Max(
                2,
                bossFloor
            );
    }
#endif

    private sealed class WeightedCandidate
    {
        public readonly RoomNodeData room;
        public readonly float weight;
        public readonly bool guaranteed;
        public readonly int missCount;

        public WeightedCandidate(
            RoomNodeData room,
            float weight,
            bool guaranteed,
            int missCount
        )
        {
            this.room = room;
            this.weight = weight;
            this.guaranteed = guaranteed;
            this.missCount = missCount;
        }
    }
}

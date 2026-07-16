using System.Collections;
using UnityEngine;

/// <summary>
/// 문 하나의 경로 표시와 개방을 관리한다.
///
/// - Configure 호출 시 목적지 표시 프리팹을 생성한다.
/// - Destination Spawn Point의 월드 위치에 프리팹을 생성한다.
/// - 문이 열리면 Scene Trigger를 활성화한다.
/// - 플레이어가 들어오면 RunFlowManager에 다음 방 이동을 요청한다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class DungeonDoor : MonoBehaviour
{
    [Header("문 회전")]
    [SerializeField]
    private Transform doorPivot;

    [SerializeField]
    private DoorSceneTrigger sceneTrigger;

    [Tooltip("문이 열릴 Y축 각도")]
    [SerializeField]
    private float openAngle = -90f;

    [Min(0f)]
    [SerializeField]
    private float openDuration = 1.2f;

    [Min(0f)]
    [SerializeField]
    private float closeDuration = 0.8f;

    [Min(0f)]
    [SerializeField]
    private float triggerEnableDelay = 0.2f;

    [Header("다음 씬 Player Spawn ID")]
    [Tooltip(
        "다음 씬에 있는 PlayerSpawnPoint의 Spawn ID와 같아야 합니다.\n" +
        "보통 Default로 통일하면 됩니다."
    )]
    [SerializeField]
    private string targetSpawnId = "Default";

    [Header("Jakdu Entry Cost")]
    [Min(0)]
    [SerializeField]
    private int jakduEntryHpCost = 10;

    [Min(0)]
    [SerializeField]
    private int minimumHpAfterJakduEntry = 1;

    [Header("목적지 프리팹 생성 기준점")]
    [Tooltip(
        "Hierarchy에 있는 빈 오브젝트 또는 Quad의 Transform을 연결하세요.\n" +
        "목적지 프리팹은 이 Transform의 월드 위치와 회전에 생성됩니다."
    )]
    [SerializeField]
    private Transform destinationSpawnPoint;

    [Header("전투방 계열 프리팹")]
    [Tooltip("계열별 프리팹이 비어 있을 때 사용하는 기본 전투방 프리팹")]
    [SerializeField]
    private GameObject combatRoomPrefab;

    [SerializeField]
    private GameObject attackCombatPrefab;

    [SerializeField]
    private GameObject defenseCombatPrefab;

    [SerializeField]
    private GameObject mobilityCombatPrefab;

    [SerializeField]
    private GameObject debuffCombatPrefab;

    [Header("비전투방 프리팹")]
    [SerializeField]
    private GameObject rewardRoomPrefab;

    [SerializeField]
    private GameObject shopRoomPrefab;

    [SerializeField]
    private GameObject jakduRoomPrefab;

    [SerializeField]
    private GameObject eventRoomPrefab;

    [SerializeField]
    private GameObject bossRoomPrefab;

    [Header("디버그")]
    [SerializeField]
    private bool showLogs = true;

    private Quaternion closedRotation;
    private Quaternion openedRotation;

    private RoomRouteOption route;
    private GameObject currentDestinationVisual;

    private bool doorUnlocked;
    private bool opened;
    private bool animating;
    private bool changingScene;

    private Coroutine doorRoutine;

    public bool HasValidRoute
    {
        get
        {
            return route != null &&
                   route.IsValid &&
                   route.TargetRoom != null;
        }
    }

    private void Awake()
    {
        InitializeDoorRotation();
        InitializeSceneTrigger();
    }

    private void InitializeDoorRotation()
    {
        if (doorPivot == null)
        {
            Debug.LogError(
                "[DungeonDoor] Door Pivot이 연결되지 않았습니다.",
                this
            );

            return;
        }

        closedRotation = doorPivot.localRotation;

        openedRotation =
            closedRotation *
            Quaternion.Euler(
                0f,
                openAngle,
                0f
            );

        doorPivot.localRotation = closedRotation;
    }

    private void InitializeSceneTrigger()
    {
        if (sceneTrigger == null)
        {
            Debug.LogWarning(
                "[DungeonDoor] Scene Trigger가 연결되지 않았습니다.",
                this
            );

            return;
        }

        sceneTrigger.Initialize(this);
        sceneTrigger.SetTriggerEnabled(false);
    }

    public void Configure(RoomRouteOption newRoute)
    {
        route = newRoute;

        doorUnlocked = false;
        opened = false;
        animating = false;
        changingScene = false;

        StopDoorRoutine();

        if (doorPivot != null)
        {
            doorPivot.localRotation = closedRotation;
        }

        if (sceneTrigger != null)
        {
            sceneTrigger.SetTriggerEnabled(false);
        }

        if (!HasValidRoute)
        {
            DestroyDestinationVisual();

            Debug.LogError(
                "[DungeonDoor] 유효하지 않은 경로가 전달되었습니다.",
                this
            );

            return;
        }

        RefreshDestinationVisual();

        if (showLogs)
        {
            Debug.Log(
                $"[DungeonDoor] 문 경로 설정 완료 / " +
                $"방={route.TargetRoom.DisplayName}, " +
                $"타입={route.TargetRoom.RoomType}, " +
                $"태그={route.RewardCategory}, " +
                $"Spawn ID={targetSpawnId}",
                this
            );
        }
    }

    public void Lock()
    {
        doorUnlocked = false;
        opened = false;
        animating = false;
        changingScene = false;

        StopDoorRoutine();

        if (sceneTrigger != null)
        {
            sceneTrigger.SetTriggerEnabled(false);
        }

        if (doorPivot != null)
        {
            doorPivot.localRotation = closedRotation;
        }
    }

    public void SetDoorEnabled(bool value)
    {
        if (!value)
        {
            Lock();
            return;
        }

        doorUnlocked = HasValidRoute;
    }

    public void ClearRoute()
    {
        route = null;

        doorUnlocked = false;
        opened = false;
        animating = false;
        changingScene = false;

        StopDoorRoutine();

        if (sceneTrigger != null)
        {
            sceneTrigger.SetTriggerEnabled(false);
        }

        if (doorPivot != null)
        {
            doorPivot.localRotation = closedRotation;
        }

        DestroyDestinationVisual();
    }

    public void Open()
    {
        if (!HasValidRoute)
        {
            Debug.LogWarning(
                "[DungeonDoor] 경로가 없어서 문을 열 수 없습니다.",
                this
            );

            return;
        }

        if (opened ||
            animating)
        {
            return;
        }

        doorUnlocked = true;
        opened = true;

        if (currentDestinationVisual == null)
        {
            RefreshDestinationVisual();
        }

        StopDoorRoutine();

        doorRoutine =
            StartCoroutine(OpenRoutine());
    }

    public bool TryEnter(Player player)
    {
        if (player == null)
        {
            return false;
        }

        if (!doorUnlocked ||
            !opened ||
            animating ||
            changingScene ||
            !HasValidRoute)
        {
            return false;
        }

        changingScene = true;

        if (sceneTrigger != null)
        {
            sceneTrigger.SetTriggerEnabled(false);
        }

        StopDoorRoutine();

        doorRoutine =
            StartCoroutine(CloseAndMoveRoutine());

        return true;
    }

    private IEnumerator OpenRoutine()
    {
        yield return RotateDoor(
            openedRotation,
            openDuration
        );

        if (triggerEnableDelay > 0f)
        {
            yield return new WaitForSeconds(
                triggerEnableDelay
            );
        }

        if (sceneTrigger != null &&
            !changingScene)
        {
            sceneTrigger.SetTriggerEnabled(true);
        }

        doorRoutine = null;
    }

    private IEnumerator CloseAndMoveRoutine()
    {
        yield return RotateDoor(
            closedRotation,
            closeDuration
        );

        doorRoutine = null;

        RunFlowManager manager =
            RunFlowManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[DungeonDoor] RunFlowManager가 없습니다.",
                this
            );

            changingScene = false;
            opened = false;

            Open();
            yield break;
        }

        PlayerSpawnContext.SetSpawnId(targetSpawnId);

        int consumedJakduEntryHp =
            ApplyJakduEntryCost();

        bool requested =
            manager.RequestRoute(route);

        if (requested)
        {
            yield break;
        }

        PlayerSpawnContext.Clear();

        RefundJakduEntryCost(
            consumedJakduEntryHp
        );

        Debug.LogError(
            "[DungeonDoor] 다음 방 이동 요청에 실패했습니다.",
            this
        );

        changingScene = false;
        opened = false;

        Open();
    }

    private IEnumerator RotateDoor(
        Quaternion targetRotation,
        float duration
    )
    {
        if (doorPivot == null)
        {
            yield break;
        }

        animating = true;

        Quaternion startRotation =
            doorPivot.localRotation;

        if (duration <= 0f)
        {
            doorPivot.localRotation =
                targetRotation;

            animating = false;

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float ratio =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float smoothRatio =
                ratio *
                ratio *
                (3f - 2f * ratio);

            doorPivot.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    smoothRatio
                );

            yield return null;
        }

        doorPivot.localRotation =
            targetRotation;

        animating = false;
    }

    private void RefreshDestinationVisual()
    {
        DestroyDestinationVisual();

        if (!HasValidRoute)
        {
            return;
        }

        if (destinationSpawnPoint == null)
        {
            Debug.LogError(
                "[DungeonDoor] Destination Spawn Point가 비어 있습니다. Hierarchy에 있는 위치 기준 오브젝트를 연결하세요.",
                this
            );

            return;
        }

        GameObject selectedPrefab =
            GetDestinationPrefab();

        if (selectedPrefab == null)
        {
            Debug.LogError(
                $"[DungeonDoor] 목적지 프리팹이 연결되지 않았습니다. " +
                $"방 타입={route.TargetRoom.RoomType}, " +
                $"보상 계열={route.RewardCategory}",
                this
            );

            return;
        }

        Vector3 worldSpawnPosition =
            destinationSpawnPoint.position;

        Quaternion worldSpawnRotation =
            destinationSpawnPoint.rotation;

        currentDestinationVisual =
            Instantiate(
                selectedPrefab,
                worldSpawnPosition,
                worldSpawnRotation
            );

        currentDestinationVisual.name =
            $"{selectedPrefab.name}_Destination";

        currentDestinationVisual.transform.SetParent(
            destinationSpawnPoint,
            true
        );

        currentDestinationVisual.transform.SetPositionAndRotation(
            worldSpawnPosition,
            worldSpawnRotation
        );

        DisableDestinationColliders();

        currentDestinationVisual.SetActive(true);

        if (showLogs)
        {
            Debug.Log(
                $"[DungeonDoor] 목적지 프리팹 생성 완료\n" +
                $"기준점 이름={destinationSpawnPoint.name}\n" +
                $"기준점 월드 위치={destinationSpawnPoint.position}\n" +
                $"생성된 월드 위치={currentDestinationVisual.transform.position}\n" +
                $"프리팹={selectedPrefab.name}",
                currentDestinationVisual
            );
        }
    }

    private void DisableDestinationColliders()
    {
        if (currentDestinationVisual == null)
        {
            return;
        }

        Collider[] colliders =
            currentDestinationVisual
                .GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private GameObject GetDestinationPrefab()
    {
        if (!HasValidRoute)
        {
            return null;
        }

        RoomType roomType =
            route.TargetRoom.RoomType;

        switch (roomType)
        {
            case RoomType.Combat:
                return GetCombatPrefab(
                    route.RewardCategory
                );

            case RoomType.Reward:
                return rewardRoomPrefab;

            case RoomType.Shop:
                return shopRoomPrefab;

            case RoomType.Jakdu:
                return jakduRoomPrefab;

            case RoomType.Event:
                return eventRoomPrefab;

            case RoomType.Boss:
                return bossRoomPrefab;

            default:
                return null;
        }
    }

    private int ApplyJakduEntryCost()
    {
        if (jakduEntryHpCost <= 0 ||
            route == null ||
            route.TargetRoom == null ||
            route.TargetRoom.RoomType != RoomType.Jakdu)
        {
            return 0;
        }

        Player player =
            PlayerSceneMover.Instance != null
                ? PlayerSceneMover.Instance.CurrentPlayer
                : null;

        if (player == null)
        {
            player =
                FindFirstObjectByType<Player>(
                    FindObjectsInactive.Include
                );
        }

        if (player == null)
        {
            Debug.LogWarning(
                "[DungeonDoor] Jakdu entry HP cost skipped because Player was not found.",
                this
            );

            return 0;
        }

        int consumed =
            player.ConsumeHp(
                jakduEntryHpCost,
                minimumHpAfterJakduEntry
            );

        if (showLogs)
        {
            Debug.Log(
                $"[DungeonDoor] Jakdu entry HP cost consumed: {consumed}",
                player
            );
        }

        return consumed;
    }

    private void RefundJakduEntryCost(
        int amount
    )
    {
        if (amount <= 0)
        {
            return;
        }

        Player player =
            PlayerSceneMover.Instance != null
                ? PlayerSceneMover.Instance.CurrentPlayer
                : null;

        if (player == null)
        {
            player =
                FindFirstObjectByType<Player>(
                    FindObjectsInactive.Include
                );
        }

        if (player == null)
        {
            return;
        }

        player.HealHp(amount);
    }

    private GameObject GetCombatPrefab(
        BoonCategory category
    )
    {
        switch (category)
        {
            case BoonCategory.Attack:
                return attackCombatPrefab != null
                    ? attackCombatPrefab
                    : combatRoomPrefab;

            case BoonCategory.Defense:
                return defenseCombatPrefab != null
                    ? defenseCombatPrefab
                    : combatRoomPrefab;

            case BoonCategory.Mobility:
                return mobilityCombatPrefab != null
                    ? mobilityCombatPrefab
                    : combatRoomPrefab;

            case BoonCategory.Debuff:
                return debuffCombatPrefab != null
                    ? debuffCombatPrefab
                    : combatRoomPrefab;

            default:
                return combatRoomPrefab;
        }
    }

    private void DestroyDestinationVisual()
    {
        if (currentDestinationVisual == null)
        {
            return;
        }

        Destroy(currentDestinationVisual);

        currentDestinationVisual = null;
    }

    private void StopDoorRoutine()
    {
        if (doorRoutine == null)
        {
            return;
        }

        StopCoroutine(doorRoutine);

        doorRoutine = null;
        animating = false;
    }

    private void OnDisable()
    {
        StopDoorRoutine();

        if (sceneTrigger != null)
        {
            sceneTrigger.SetTriggerEnabled(false);
        }
    }

    private void OnDestroy()
    {
        DestroyDestinationVisual();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(targetSpawnId))
        {
            targetSpawnId = "Default";
        }
    }
#endif
}

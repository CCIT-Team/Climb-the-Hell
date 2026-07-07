using System.Collections;
using UnityEngine;

/// <summary>
/// 문 하나의 경로 표시와 개방을 관리한다.
///
/// 핵심:
/// - Configure가 호출되면 문 위 목적지 프리팹을 즉시 생성한다.
/// - 목적지 프리팹은 문이 잠겨 있어도 계속 보인다.
/// - Open이 호출될 때만 문 회전과 이동 Trigger가 활성화된다.
/// - 별도의 DestinationAnchor 오브젝트는 필요 없다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class DungeonDoor : MonoBehaviour
{
    [Header("문 회전")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private DoorSceneTrigger sceneTrigger;

    [Tooltip("문이 열릴 Y축 각도")]
    [SerializeField] private float openAngle = -90f;

    [Min(0f)]
    [SerializeField] private float openDuration = 1.2f;

    [Min(0f)]
    [SerializeField] private float closeDuration = 0.8f;

    [Min(0f)]
    [SerializeField] private float triggerEnableDelay = 0.2f;

    [Header("문 위 프리팹 위치")]
    [Tooltip("DungeonDoor 오브젝트 기준 로컬 위치")]
    [SerializeField] private Vector3 destinationLocalPosition =
        new Vector3(0f, 3f, 0f);

    [Tooltip("생성된 목적지 프리팹의 로컬 회전 보정")]
    [SerializeField] private Vector3 destinationLocalEulerAngles =
        Vector3.zero;

    [Tooltip("프리팹 원래 크기에 곱할 값")]
    [Min(0.01f)]
    [SerializeField] private float destinationScaleMultiplier = 1f;

    [Header("전투방 계열 프리팹")]
    [Tooltip("계열 프리팹이 비어 있을 때 사용할 기본 전투방 프리팹")]
    [SerializeField] private GameObject combatRoomPrefab;

    [SerializeField] private GameObject attackCombatPrefab;
    [SerializeField] private GameObject defenseCombatPrefab;
    [SerializeField] private GameObject mobilityCombatPrefab;
    [SerializeField] private GameObject debuffCombatPrefab;

    [Header("비전투방 프리팹")]
    [SerializeField] private GameObject rewardRoomPrefab;
    [SerializeField] private GameObject shopRoomPrefab;
    [SerializeField] private GameObject jakduRoomPrefab;
    [SerializeField] private GameObject eventRoomPrefab;
    [SerializeField] private GameObject bossRoomPrefab;

    [Header("디버그")]
    [SerializeField] private bool showLogs = true;

    private Quaternion closedRotation;
    private Quaternion openedRotation;

    private RoomRouteOption route;
    private GameObject currentDestinationVisual;

    private bool doorUnlocked;
    private bool opened;
    private bool animating;
    private bool changingScene;

    private Coroutine doorRoutine;

    public bool HasValidRoute =>
        route != null &&
        route.IsValid;

    private void Awake()
    {
        if (doorPivot == null)
        {
            Debug.LogError(
                "[DungeonDoor] Door Pivot이 연결되지 않았습니다.",
                this
            );

            return;
        }

        closedRotation =
            doorPivot.localRotation;

        openedRotation =
            closedRotation *
            Quaternion.Euler(
                0f,
                openAngle,
                0f
            );

        doorPivot.localRotation =
            closedRotation;

        if (sceneTrigger != null)
        {
            sceneTrigger.Initialize(this);
            sceneTrigger.SetTriggerEnabled(false);
        }
    }

    /// <summary>
    /// 다음 방 경로를 배정하고 문 위 프리팹을 즉시 생성한다.
    /// 이 시점에는 문 이동만 잠겨 있다.
    /// </summary>
    public void Configure(
        RoomRouteOption newRoute)
    {
        route = newRoute;

        doorUnlocked = false;
        opened = false;
        animating = false;
        changingScene = false;

        StopDoorRoutine();

        if (doorPivot != null)
        {
            doorPivot.localRotation =
                closedRotation;
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
                $"[DungeonDoor] 문 위 표시 생성 / " +
                $"방={route.TargetRoom.DisplayName}, " +
                $"타입={route.TargetRoom.RoomType}, " +
                $"태그={route.RewardCategory}",
                this
            );
        }
    }

    /// <summary>
    /// 문 이동만 잠근다.
    /// 문 위에 생성된 프리팹은 절대 숨기지 않는다.
    /// </summary>
    public void Lock()
    {
        doorUnlocked = false;
        opened = false;
        changingScene = false;

        StopDoorRoutine();

        if (sceneTrigger != null)
        {
            sceneTrigger.SetTriggerEnabled(false);
        }

        if (doorPivot != null)
        {
            doorPivot.localRotation =
                closedRotation;
        }
    }

    /// <summary>
    /// 이전 코드 호환용.
    /// false면 문만 잠그고, true면 경로가 있을 때 문을 열 수 있는 상태로 둔다.
    /// 목적지 프리팹은 건드리지 않는다.
    /// </summary>
    public void SetDoorEnabled(
        bool value)
    {
        if (!value)
        {
            Lock();
            return;
        }

        doorUnlocked =
            HasValidRoute;
    }

    /// <summary>
    /// 사용하지 않는 문의 경로와 표시를 전부 제거한다.
    /// </summary>
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
            doorPivot.localRotation =
                closedRotation;
        }

        DestroyDestinationVisual();
    }

    /// <summary>
    /// 방 완료 후 문을 연다.
    /// 문 위 표시가 없으면 같은 경로로 다시 생성한다.
    /// </summary>
    public void Open()
    {
        if (!HasValidRoute ||
            opened ||
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
            StartCoroutine(
                OpenRoutine()
            );
    }

    /// <summary>
    /// DoorSceneTrigger가 플레이어 진입 시 호출한다.
    /// </summary>
    public bool TryEnter(
        Player player)
    {
        if (player == null ||
            !doorUnlocked ||
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
            StartCoroutine(
                CloseAndMoveRoutine()
            );

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

        bool requested =
            manager != null &&
            manager.RequestRoute(route);

        if (requested)
        {
            yield break;
        }

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
        float duration)
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

        GameObject selectedPrefab =
            GetDestinationPrefab();

        if (selectedPrefab == null)
        {
            Debug.LogError(
                $"[DungeonDoor] 연결된 목적지 프리팹이 없습니다. " +
                $"타입={route.TargetRoom.RoomType}, " +
                $"태그={route.RewardCategory}",
                this
            );

            return;
        }

        // 별도의 Anchor 없이 DungeonDoor 바로 아래에 생성한다.
        currentDestinationVisual =
            Instantiate(
                selectedPrefab,
                transform
            );

        currentDestinationVisual.name =
            $"{selectedPrefab.name}_Destination";

        Transform visualTransform =
            currentDestinationVisual.transform;

        visualTransform.localPosition =
            destinationLocalPosition;

        visualTransform.localRotation =
            Quaternion.Euler(
                destinationLocalEulerAngles
            );

        visualTransform.localScale *=
            destinationScaleMultiplier;

        // 문양용 프리팹이 문 충돌이나 플레이어 판정을 방해하지 않게 한다.
        Collider[] colliders =
            currentDestinationVisual
                .GetComponentsInChildren<Collider>(true);

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            colliders[i].enabled = false;
        }

        currentDestinationVisual.SetActive(true);
    }

    private GameObject GetDestinationPrefab()
    {
        if (!HasValidRoute ||
            route.TargetRoom == null)
        {
            return null;
        }

        RoomType roomType =
            route.TargetRoom.RoomType;

        if (roomType == RoomType.Combat)
        {
            return GetCombatPrefab(
                route.RewardCategory
            );
        }

        switch (roomType)
        {
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

    private GameObject GetCombatPrefab(
        BoonCategory category)
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
    }

    private void OnDisable()
    {
        StopDoorRoutine();

        if (sceneTrigger != null)
        {
            sceneTrigger.SetTriggerEnabled(false);
        }

        // 문 위 프리팹은 경로가 유지되는 동안 삭제하지 않는다.
    }
}

using System.Collections;
using UnityEngine;

/// <summary>
/// 씬에 이미 배치된 문을 관리한다.
///
/// 문 위 아이콘 Quad는 씬 로드 직후부터 계속 표시한다.
/// 문 상태와 상관없이 아이콘을 끄지 않으며,
/// 경로가 설정되면 Material만 교체한다.
/// </summary>
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

    [Header("문 위 아이콘 Quad")]
    [Tooltip(
        "씬 시작부터 계속 표시할 문 위 Quad 오브젝트"
    )]
    [SerializeField]
    private GameObject routeIconQuad;

    [Tooltip(
        "Route Icon Quad의 MeshRenderer. 비워두면 자동 탐색"
    )]
    [SerializeField]
    private MeshRenderer routeIconRenderer;

    [Header("전투방 득도 태그 Material")]
    [SerializeField]
    private Material attackMaterial;

    [SerializeField]
    private Material defenseMaterial;

    [SerializeField]
    private Material mobilityMaterial;

    [SerializeField]
    private Material debuffMaterial;

    [Header("비전투방 아이콘 Material")]
    [SerializeField]
    private Material rewardRoomMaterial;

    [SerializeField]
    private Material shopRoomMaterial;

    [SerializeField]
    private Material jakduRoomMaterial;

    [SerializeField]
    private Material eventRoomMaterial;

    [SerializeField]
    private Material bossRoomMaterial;

    private Quaternion closedRotation;
    private Quaternion openedRotation;

    private RoomRouteOption route;

    private bool doorEnabled;
    private bool opened;
    private bool animating;
    private bool changingScene;

    private Coroutine doorRoutine;

    private void Reset()
    {
        FindIconRenderer();
    }

    private void Awake()
    {
        if (doorPivot == null)
        {
            Debug.LogError(
                "[DungeonDoor] Door Pivot이 연결되지 않았습니다.",
                this
            );

            enabled = false;
            return;
        }

        FindIconRenderer();

        closedRotation =
            doorPivot.localRotation;

        openedRotation =
            closedRotation *
            Quaternion.Euler(
                0f,
                openAngle,
                0f
            );

        if (sceneTrigger != null)
        {
            sceneTrigger.Initialize(this);
            sceneTrigger.SetTriggerEnabled(false);
        }

        /*
         * 아이콘은 씬 로드 직후부터 항상 표시한다.
         * Inspector에서 설정한 기본 Material도 그대로 보인다.
         */
        KeepIconVisible();
    }

    /// <summary>
    /// RoomChoiceGenerator가 다음 방 정보를 전달한다.
    /// 아이콘은 끄지 않고 Material만 교체한다.
    /// </summary>
    public void Configure(
        RoomRouteOption newRoute
    )
    {
        route = newRoute;

        opened = false;
        animating = false;
        changingScene = false;

        if (doorRoutine != null)
        {
            StopCoroutine(doorRoutine);
            doorRoutine = null;
        }

        if (doorPivot != null)
        {
            doorPivot.localRotation =
                closedRotation;
        }

        doorEnabled =
            route != null &&
            route.IsValid;

        if (sceneTrigger != null)
        {
            sceneTrigger.SetTriggerEnabled(false);
        }

        /*
         * 경로가 잘못되어도 아이콘은 숨기지 않는다.
         * 기존 Material과 표시 상태를 유지한다.
         */
        if (!doorEnabled)
        {
            KeepIconVisible();

            Debug.LogWarning(
                "[DungeonDoor] 유효하지 않은 경로가 전달되었습니다.",
                this
            );

            return;
        }

        ApplyCurrentRouteMaterial();

        Debug.Log(
            $"[DungeonDoor] 경로 설정 완료 / " +
            $"방: {route.TargetRoom.name} / " +
            $"태그: {route.RewardCategory}",
            this
        );
    }

    /// <summary>
    /// 문 사용 가능 여부를 설정한다.
    /// 이 값은 아이콘 표시에는 영향을 주지 않는다.
    /// </summary>
    public void SetDoorEnabled(
        bool value
    )
    {
        doorEnabled = value;

        if (value)
        {
            KeepIconVisible();
            return;
        }

        opened = false;
        changingScene = false;

        if (doorRoutine != null)
        {
            StopCoroutine(doorRoutine);
            doorRoutine = null;
        }

        if (sceneTrigger != null)
        {
            sceneTrigger.SetTriggerEnabled(false);
        }

        if (doorPivot != null)
        {
            doorPivot.localRotation =
                closedRotation;
        }

        /*
         * 문을 비활성 상태로 바꿔도
         * 아이콘은 계속 표시한다.
         */
        KeepIconVisible();
    }

    /// <summary>
    /// 아이콘을 유지한 상태로 문을 연다.
    /// </summary>
    public void Open()
    {
        if (!doorEnabled ||
            route == null ||
            !route.IsValid ||
            opened ||
            animating)
        {
            KeepIconVisible();
            return;
        }

        opened = true;

        ApplyCurrentRouteMaterial();

        if (doorRoutine != null)
        {
            StopCoroutine(doorRoutine);
        }

        doorRoutine =
            StartCoroutine(
                OpenRoutine()
            );
    }

    /// <summary>
    /// DoorSceneTrigger가 플레이어 진입 시 호출한다.
    /// </summary>
    public bool TryEnter(
        Player player
    )
    {
        if (player == null ||
            !doorEnabled ||
            !opened ||
            animating ||
            changingScene ||
            route == null ||
            !route.IsValid)
        {
            return false;
        }

        changingScene = true;

        if (sceneTrigger != null)
        {
            sceneTrigger.SetTriggerEnabled(false);
        }

        if (doorRoutine != null)
        {
            StopCoroutine(doorRoutine);
        }

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

        KeepIconVisible();

        doorRoutine = null;
    }

    private IEnumerator CloseAndMoveRoutine()
    {
        /*
         * 문이 닫히는 동안에도 아이콘은 유지한다.
         */
        KeepIconVisible();

        yield return RotateDoor(
            closedRotation,
            closeDuration
        );

        doorRoutine = null;

        bool requestSucceeded =
            RunFlowManager
                .Instance
                .RequestRoute(route);

        if (requestSucceeded)
        {
            yield break;
        }

        Debug.LogError(
            "[DungeonDoor] 다음 방 이동 요청에 실패했습니다.",
            this
        );

        changingScene = false;
        opened = false;

        KeepIconVisible();
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

    /// <summary>
    /// 현재 경로에 맞는 Material만 교체한다.
    /// 아이콘 오브젝트는 절대 끄지 않는다.
    /// </summary>
    private void ApplyCurrentRouteMaterial()
    {
        KeepIconVisible();

        if (route == null ||
            route.TargetRoom == null)
        {
            return;
        }

        if (routeIconRenderer == null)
        {
            Debug.LogError(
                "[DungeonDoor] Route Icon Quad에 MeshRenderer가 없습니다.",
                this
            );

            return;
        }

        Material selectedMaterial =
            GetRouteMaterial();

        if (selectedMaterial == null)
        {
            Debug.LogWarning(
                $"[DungeonDoor] 적용할 Material이 없습니다. " +
                $"방 타입: {route.TargetRoom.RoomType}, " +
                $"태그: {route.RewardCategory}",
                this
            );

            return;
        }

        /*
         * Material 복제 생성을 막기 위해
         * sharedMaterial로 교체한다.
         */
        routeIconRenderer.sharedMaterial =
            selectedMaterial;
    }

    /// <summary>
    /// 아이콘 Quad와 Renderer를 항상 활성 상태로 유지한다.
    /// </summary>
    private void KeepIconVisible()
    {
        if (routeIconQuad == null)
        {
            return;
        }

        if (!routeIconQuad.activeSelf)
        {
            routeIconQuad.SetActive(true);
        }

        FindIconRenderer();

        if (routeIconRenderer != null)
        {
            routeIconRenderer.enabled =
                true;
        }
    }

    private void FindIconRenderer()
    {
        if (routeIconQuad == null ||
            routeIconRenderer != null)
        {
            return;
        }

        routeIconRenderer =
            routeIconQuad
                .GetComponent<MeshRenderer>();

        if (routeIconRenderer == null)
        {
            routeIconRenderer =
                routeIconQuad
                    .GetComponentInChildren<
                        MeshRenderer>(true);
        }
    }

    private Material GetRouteMaterial()
    {
        RoomType roomType =
            route.TargetRoom.RoomType;

        if (roomType ==
            RoomType.Combat)
        {
            return GetBoonMaterial(
                route.RewardCategory
            );
        }

        return GetRoomMaterial(
            roomType
        );
    }

    private Material GetBoonMaterial(
        BoonCategory category
    )
    {
        switch (category)
        {
            case BoonCategory.Attack:
                return attackMaterial;

            case BoonCategory.Defense:
                return defenseMaterial;

            case BoonCategory.Mobility:
                return mobilityMaterial;

            case BoonCategory.Debuff:
                return debuffMaterial;

            default:
                Debug.LogWarning(
                    "[DungeonDoor] 전투방인데 득도 태그가 None입니다.",
                    this
                );

                return null;
        }
    }

    private Material GetRoomMaterial(
        RoomType roomType
    )
    {
        switch (roomType)
        {
            case RoomType.Reward:
                return rewardRoomMaterial;

            case RoomType.Shop:
                return shopRoomMaterial;

            case RoomType.Jakdu:
                return jakduRoomMaterial;

            case RoomType.Event:
                return eventRoomMaterial;

            case RoomType.Boss:
                return bossRoomMaterial;

            default:
                return null;
        }
    }

    private void OnEnable()
    {
        /*
         * 씬 로드 또는 오브젝트 재활성화 시에도
         * 아이콘을 즉시 다시 표시한다.
         */
        KeepIconVisible();
    }

    private void OnDisable()
    {
        if (doorRoutine != null)
        {
            StopCoroutine(doorRoutine);
            doorRoutine = null;
        }

        if (sceneTrigger != null)
        {
            sceneTrigger.SetTriggerEnabled(false);
        }

        /*
         * 여기서도 아이콘을 끄는 코드는 없다.
         */
    }
}

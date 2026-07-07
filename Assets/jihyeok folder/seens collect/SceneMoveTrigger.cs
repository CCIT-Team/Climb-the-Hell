using UnityEngine;

/// <summary>
/// Player가 Trigger에 들어오면 지정한 씬으로 이동시킨다.
///
/// 기존 DungeonDoor 또는 DoorSceneTrigger와 충돌하지 않도록
/// 별도 이름으로 만든 단순 씬 이동 Trigger다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SceneMoveTrigger : MonoBehaviour
{
    [Header("이동할 씬")]
    [Tooltip("Build Profiles에 등록된 씬 이름")]
    [SerializeField]
    private string targetSceneName;

    [Header("새 씬의 스폰 지점")]
    [Tooltip(
        "새 씬에 있는 PlayerSpawnPoint의 Spawn ID와 같아야 합니다."
    )]
    [SerializeField]
    private string targetSpawnId = "Default";

    [Header("시작 설정")]
    [Tooltip("게임 시작 시 Trigger를 바로 활성화할지 여부")]
    [SerializeField]
    private bool activeOnStart = true;

    [Header("디버그")]
    [SerializeField]
    private bool showLogs = true;

    private Collider triggerCollider;
    private bool requested;

    private void Awake()
    {
        triggerCollider =
            GetComponent<Collider>();

        triggerCollider.isTrigger = true;
        triggerCollider.enabled =
            activeOnStart;
    }

    /// <summary>
    /// 문이 열렸을 때 Trigger를 활성화할 수 있다.
    /// </summary>
    public void SetTriggerEnabled(
        bool value
    )
    {
        if (triggerCollider == null)
        {
            triggerCollider =
                GetComponent<Collider>();
        }

        triggerCollider.enabled =
            value;

        if (value)
        {
            requested = false;
        }
    }

    private void OnTriggerEnter(
        Collider other
    )
    {
        if (requested)
        {
            return;
        }

        Player player =
            other.GetComponentInParent<Player>();

        if (player == null)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError(
                "[SceneMoveTrigger] GameManager가 없습니다.",
                this
            );

            return;
        }

        requested =
            GameManager.Instance.ChangeScene(
                targetSceneName,
                targetSpawnId
            );

        if (!requested)
        {
            return;
        }

        SetTriggerEnabled(false);

        if (showLogs)
        {
            Debug.Log(
                $"[SceneMoveTrigger] 씬 이동 요청 / " +
                $"씬={targetSceneName}, " +
                $"Spawn ID={targetSpawnId}",
                this
            );
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(targetSpawnId))
        {
            targetSpawnId = "Default";
        }

        Collider foundCollider =
            GetComponent<Collider>();

        if (foundCollider != null)
        {
            foundCollider.isTrigger = true;
        }
    }
#endif
}

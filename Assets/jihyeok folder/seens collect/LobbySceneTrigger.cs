using UnityEngine;

/// <summary>
/// 로비 출구 Trigger.
/// 플레이어가 들어오면 1층 전투방을 시작한다.
/// MeshCollider가 아닌 BoxCollider 전용이다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class LobbySceneTrigger : MonoBehaviour
{
    private BoxCollider triggerCollider;
    private bool isChangingScene;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isChangingScene)
        {
            return;
        }

        Player player =
            other.GetComponentInParent<Player>();

        if (player == null)
        {
            return;
        }

        isChangingScene = true;

        bool requested =
            RunFlowManager.Instance.StartNewRun();

        if (!requested)
        {
            isChangingScene = false;
            return;
        }

        triggerCollider.enabled = false;
    }
}

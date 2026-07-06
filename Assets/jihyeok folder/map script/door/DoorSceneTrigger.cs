using UnityEngine;

/// <summary>
/// 문 안쪽 플레이어 진입 판정.
/// 오목 MeshCollider 오류를 막기 위해 BoxCollider만 사용한다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class DoorSceneTrigger : MonoBehaviour
{
    private DungeonDoor owner;
    private BoxCollider triggerCollider;
    private bool isProcessing;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
    }

    public void Initialize(DungeonDoor doorOwner)
    {
        owner = doorOwner;
    }

    public void SetTriggerEnabled(bool value)
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<BoxCollider>();
        }

        triggerCollider.enabled = value;

        if (!value)
        {
            isProcessing = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isProcessing || owner == null)
        {
            return;
        }

        Player player =
            other.GetComponentInParent<Player>();

        if (player == null)
        {
            return;
        }

        if (owner.TryEnter(player))
        {
            isProcessing = true;
        }
    }
}

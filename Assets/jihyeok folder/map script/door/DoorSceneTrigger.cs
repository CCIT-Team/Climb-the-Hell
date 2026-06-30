using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorSceneTrigger : MonoBehaviour
{
    private DungeonDoor dungeonDoor;
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider =
            GetComponent<Collider>();

        triggerCollider.isTrigger = true;
    }

    public void Initialize(DungeonDoor owner)
    {
        dungeonDoor = owner;
    }

    public void SetTriggerEnabled(bool value)
    {
        if (triggerCollider == null)
        {
            triggerCollider =
                GetComponent<Collider>();
        }

        triggerCollider.enabled = value;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            $"[DoorSceneTrigger] 진입 감지: {other.gameObject.name}",
            other.gameObject
        );

        Player player =
            other.GetComponentInParent<Player>();

        if (player == null)
        {
            return;
        }

        if (dungeonDoor == null)
        {
            Debug.LogError(
                "[DoorSceneTrigger] DungeonDoor가 연결되지 않았습니다.",
                this
            );

            return;
        }

        dungeonDoor.TryChangeScene(player);
    }
}
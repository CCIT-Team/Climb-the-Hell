using UnityEngine;

/// <summary>
/// 씬 이동 후 Player가 나타날 위치.
/// 씬마다 하나 이상 배치한다.
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [Header("스폰 지점 ID")]
    [Tooltip(
        "문에서 요청한 Spawn ID와 같은 지점을 사용합니다.\n" +
        "예: Default, Entrance, LeftDoor, BossEntrance"
    )]
    [SerializeField]
    private string spawnId = "Default";

    public string SpawnId => spawnId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(spawnId))
        {
            spawnId = "Default";
        }
    }
#endif

    private void OnDrawGizmos()
    {
        Gizmos.color =
            Color.green;

        Gizmos.DrawSphere(
            transform.position,
            0.3f
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            transform.forward * 1.5f
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            0.5f
        );
    }
}

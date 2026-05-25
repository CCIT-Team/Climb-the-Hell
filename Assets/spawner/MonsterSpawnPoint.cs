using UnityEngine;

public class MonsterSpawnPoint : MonoBehaviour
{
    public bool canSpawn = true;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
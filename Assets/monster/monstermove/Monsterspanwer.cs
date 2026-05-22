using UnityEngine;
using UnityEngine.AI;

public class MonsterSpawner : MonoBehaviour
{
    [Header("Spawn Target")]
    public GameObject monsterPrefab;
    public Transform player;

    [Header("Spawn Setting")]
    public int spawnCount = 10;
    public float spawnRadius = 8f;
    public float spawnInterval = 0.3f;

    [Header("Spawn Check")]
    public float checkRadius = 1f;
    public LayerMask blockLayer;

    [Header("Spawn Option")]
    public bool spawnOnStart = true;

    private int spawnedCount;

    void Start()
    {
        if (spawnOnStart)
        {
            StartCoroutine(SpawnMonsters());
        }
    }

    System.Collections.IEnumerator SpawnMonsters()
    {
        spawnedCount = 0;

        while (spawnedCount < spawnCount)
        {
            SpawnOneMonster();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnOneMonster()
    {
        // 최대 20번까지 생성 위치 탐색
        for (int i = 0; i < 20; i++)
        {
            Vector3 randomPos =
                transform.position +
                Random.insideUnitSphere * spawnRadius;

            randomPos.y = transform.position.y;

            // 주변에 오브젝트 존재 검사
            bool blocked =
                Physics.CheckSphere(
                    randomPos,
                    checkRadius,
                    blockLayer);

            // NavMesh 위인지 검사
            NavMeshHit hit;

            bool onNavMesh =
                NavMesh.SamplePosition(
                    randomPos,
                    out hit,
                    2f,
                    NavMesh.AllAreas);

            // 공간이 비어있고 NavMesh 위면 생성
            if (!blocked && onNavMesh)
            {
                GameObject monster =
                    Instantiate(
                        monsterPrefab,
                        hit.position,
                        Quaternion.identity);

                testmonsterai ai =
                    monster.GetComponent<testmonsterai>();

                if (ai != null)
                {
                    ai.player = player;
                }

                spawnedCount++;

                return;
            }
        }

        Debug.Log("생성 가능한 위치를 찾지 못함");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            spawnRadius);

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            checkRadius);
    }
}
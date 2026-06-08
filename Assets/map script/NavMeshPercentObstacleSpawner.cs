using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshBFSObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public GameObject[] obstaclePrefabs;

    [Header("Spawn")]
    public int samplePointCount = 300;

    [Range(0f, 1f)]
    public float obstaclePercent = 0.15f;

    [Header("Safe Zone")]
    public Transform playerSpawnPoint;
    public float playerSafeRadius = 3f;

    public Vector3 roomCenter;
    public float centerSafeRadius = 2f;

    [Header("Obstacle Rule")]
    public float minObstacleDistance = 2f;

    [Header("BFS Connectivity")]
    public bool useBFSConnectivityCheck = true;
    public float gridCellSize = 1.5f;

    [Range(0f, 1f)]
    public float minReachableRatio = 0.9f;

    private List<Vector3> candidatePoints = new List<Vector3>();
    private List<Vector3> placedPositions = new List<Vector3>();
    private List<GameObject> spawnedObjects = new List<GameObject>();

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        Clear();

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            Debug.LogError("Obstacle Prefabs 비어있음");
            return;
        }

        FindPointsFromNavMesh();

        if (candidatePoints.Count == 0)
        {
            Debug.LogError("NavMesh 후보 포인트 0개");
            return;
        }

        int targetCount = Mathf.RoundToInt(candidatePoints.Count * obstaclePercent);
        Shuffle(candidatePoints);

        int placedCount = 0;

        foreach (Vector3 point in candidatePoints)
        {
            if (placedCount >= targetCount)
                break;

            if (!CanPlaceObstacle(point))
                continue;

            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject obj = Instantiate(
                prefab,
                point,
                rot,
                transform
            );

            spawnedObjects.Add(obj);
            placedPositions.Add(point);

            Physics.SyncTransforms();

            if (useBFSConnectivityCheck && !CheckBFSConnectivity())
            {
                spawnedObjects.Remove(obj);
                placedPositions.Remove(point);
                Destroy(obj);

                Physics.SyncTransforms();
                continue;
            }

            placedCount++;
        }

        Debug.Log($"후보: {candidatePoints.Count}, 목표: {targetCount}, 실제 배치: {placedCount}");
    }

    void FindPointsFromNavMesh()
    {
        candidatePoints.Clear();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        if (triangulation.vertices.Length == 0 || triangulation.indices.Length == 0)
        {
            Debug.LogError("NavMesh 삼각형 데이터 없음. Bake 확인");
            return;
        }

        for (int i = 0; i < samplePointCount; i++)
        {
            int triIndex = Random.Range(0, triangulation.indices.Length / 3) * 3;

            Vector3 a = triangulation.vertices[triangulation.indices[triIndex]];
            Vector3 b = triangulation.vertices[triangulation.indices[triIndex + 1]];
            Vector3 c = triangulation.vertices[triangulation.indices[triIndex + 2]];

            Vector3 point = RandomPointInTriangle(a, b, c);

            if (Vector3.Distance(point, roomCenter) < centerSafeRadius)
                continue;

            if (playerSpawnPoint != null &&
                Vector3.Distance(point, playerSpawnPoint.position) < playerSafeRadius)
                continue;

            candidatePoints.Add(point);
        }

        Debug.Log("후보 포인트 수: " + candidatePoints.Count);
    }

    Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;

        return (1 - r1) * a + r1 * (1 - r2) * b + r1 * r2 * c;
    }

    bool CanPlaceObstacle(Vector3 pos)
    {
        foreach (Vector3 placed in placedPositions)
        {
            if (Vector3.Distance(pos, placed) < minObstacleDistance)
                return false;
        }

        return true;
    }

    bool CheckBFSConnectivity()
    {
        if (playerSpawnPoint == null)
            return true;

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        if (triangulation.vertices.Length == 0)
            return false;

        Bounds bounds = new Bounds(triangulation.vertices[0], Vector3.zero);

        foreach (Vector3 v in triangulation.vertices)
            bounds.Encapsulate(v);

        int width = Mathf.CeilToInt(bounds.size.x / gridCellSize);
        int height = Mathf.CeilToInt(bounds.size.z / gridCellSize);

        bool[,] walkable = new bool[width, height];
        bool[,] visited = new bool[width, height];

        int totalWalkable = 0;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 worldPos = GridToWorld(bounds, x, z);

                bool onNavMesh = NavMesh.SamplePosition(
                    worldPos,
                    out NavMeshHit hit,
                    gridCellSize * 0.6f,
                    NavMesh.AllAreas
                );

                if (!onNavMesh)
                {
                    walkable[x, z] = false;
                    continue;
                }

                bool blockedByObstacle = IsBlockedBySpawnedObstacle(hit.position);

                if (blockedByObstacle)
                {
                    walkable[x, z] = false;
                    continue;
                }

                walkable[x, z] = true;
                totalWalkable++;
            }
        }

        if (totalWalkable == 0)
            return false;

        Vector2Int startCell = WorldToGrid(bounds, playerSpawnPoint.position);

        if (!IsInside(startCell, width, height))
            return false;

        if (!walkable[startCell.x, startCell.y])
        {
            Vector2Int nearest = FindNearestWalkableCell(walkable, startCell, width, height);

            if (nearest.x == -1)
                return false;

            startCell = nearest;
        }

        int reachableCount = BFS(walkable, visited, startCell, width, height);

        float ratio = (float)reachableCount / totalWalkable;

        Debug.Log($"BFS 연결성: {reachableCount}/{totalWalkable} = {ratio}");

        return ratio >= minReachableRatio;
    }

    bool IsBlockedBySpawnedObstacle(Vector3 cellPos)
    {
        Vector3 checkPos = cellPos + Vector3.up * 0.5f;
        Vector3 halfSize = new Vector3(
            gridCellSize * 0.45f,
            1f,
            gridCellSize * 0.45f
        );

        Bounds cellBounds = new Bounds(checkPos, halfSize * 2f);

        foreach (GameObject obj in spawnedObjects)
        {
            if (obj == null)
                continue;

            Collider[] colliders = obj.GetComponentsInChildren<Collider>();

            foreach (Collider col in colliders)
            {
                if (col == null)
                    continue;

                if (col.isTrigger)
                    continue;

                if (cellBounds.Intersects(col.bounds))
                    return true;
            }
        }

        return false;
    }

    Vector3 GridToWorld(Bounds bounds, int x, int z)
    {
        return new Vector3(
            bounds.min.x + x * gridCellSize + gridCellSize * 0.5f,
            bounds.center.y,
            bounds.min.z + z * gridCellSize + gridCellSize * 0.5f
        );
    }

    Vector2Int WorldToGrid(Bounds bounds, Vector3 pos)
    {
        int x = Mathf.FloorToInt((pos.x - bounds.min.x) / gridCellSize);
        int z = Mathf.FloorToInt((pos.z - bounds.min.z) / gridCellSize);

        return new Vector2Int(x, z);
    }

    bool IsInside(Vector2Int cell, int width, int height)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }

    Vector2Int FindNearestWalkableCell(bool[,] walkable, Vector2Int start, int width, int height)
    {
        int maxRadius = Mathf.Max(width, height);

        for (int r = 1; r < maxRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    Vector2Int cell = new Vector2Int(start.x + dx, start.y + dz);

                    if (!IsInside(cell, width, height))
                        continue;

                    if (walkable[cell.x, cell.y])
                        return cell;
                }
            }
        }

        return new Vector2Int(-1, -1);
    }

    int BFS(bool[,] walkable, bool[,] visited, Vector2Int start, int width, int height)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        int count = 0;

        Vector2Int[] dirs =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            count++;

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = current + dir;

                if (!IsInside(next, width, height))
                    continue;

                if (visited[next.x, next.y])
                    continue;

                if (!walkable[next.x, next.y])
                    continue;

                visited[next.x, next.y] = true;
                queue.Enqueue(next);
            }
        }

        return count;
    }

    void Shuffle(List<Vector3> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            Vector3 temp = list[i];
            list[i] = list[rand];
            list[rand] = temp;
        }
    }

    public void Clear()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedObjects.Clear();
        placedPositions.Clear();
        candidatePoints.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(roomCenter, centerSafeRadius);

        if (playerSpawnPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(playerSpawnPoint.position, playerSafeRadius);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

public class HadesStyleRandomMapMaker_Fixed : MonoBehaviour
{
    enum CellType
    {
        Void = -1,      // 바닥 없음
        Empty = 0,      // 바닥 있음
        Obstacle = 1    // 장애물
    }

    [Header("Map Size")]
    public int width = 31;
    public int height = 23;
    public float cellSize = 4f;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject[] obstaclePrefabs;
    public GameObject wallPrefab;

    [Header("Player")]
    public Transform player;
    public int safeRadius = 3;
    public bool movePlayerToSpawn = true;

    [Header("Room Shape")]
    [Range(0.45f, 0.95f)]
    public float roomFill = 0.72f;
    [Range(0f, 0.45f)]
    public float edgeNoise = 0.28f;
    public int smoothingCount = 2;

    [Header("Obstacle Settings")]
    public int randomObstacleCount = 52;
    public int cornerObstacleCount = 56;
    public int obstacleFootprint = 1;
    public int obstaclePadding = 1;
    [Range(0f, 0.6f)]
    public float maxObstacleDensity = 0.38f;

    [Header("Evaluation")]
    public int maxRetry = 80;
    public int minFloorCells = 150;
    public int minFreeCellsAroundPlayer = 25;

    private int[,] grid;
    private Vector2Int playerSpawn;
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    void Start()
    {
        GenerateValidMap();
    }

    [ContextMenu("Generate Random Room")]
    public void GenerateValidMap()
    {
        for (int i = 0; i < maxRetry; i++)
        {
            ClearMap();
            CreateRandomRoomShape();
            SetPlayerSpawnToCenterFloor();
            ClearSafeArea();
            AddCornerObstacles();
            AddRandomObstacles();

            if (EvaluateMap())
            {
                BuildMap();
                MovePlayerToSpawn();
                Debug.Log("랜덤 방 생성 성공");
                return;
            }
        }

        Debug.LogWarning("조건 만족 맵 생성 실패. width/height를 키우거나 obstacleFootprint, obstacleCount를 줄여보세요.");
    }

    void CreateRandomRoomShape()
    {
        grid = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = (int)CellType.Void;
            }
        }

        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);

        float radiusX = width * roomFill * 0.5f;
        float radiusY = height * roomFill * 0.5f;

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                float dx = (x - center.x) / radiusX;
                float dy = (y - center.y) / radiusY;

                float ellipseValue = dx * dx + dy * dy;

                // 외곽을 울퉁불퉁하게 만들기 위한 좌표 기반 노이즈
                float noise = Mathf.PerlinNoise(x * 0.23f + Random.value * 10f, y * 0.23f + Random.value * 10f);
                float edgeCut = Mathf.Lerp(-edgeNoise, edgeNoise, noise);

                if (ellipseValue < 1f + edgeCut)
                {
                    grid[x, y] = (int)CellType.Empty;
                }
            }
        }

        // 셀룰러 오토마타 느낌으로 외곽을 조금 정리
        for (int i = 0; i < smoothingCount; i++)
        {
            SmoothRoomShape();
        }
    }

    void SmoothRoomShape()
    {
        int[,] nextGrid = (int[,])grid.Clone();

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                int floorNeighbors = CountFloorNeighbors8(x, y);

                if (grid[x, y] == (int)CellType.Empty)
                {
                    if (floorNeighbors <= 2)
                        nextGrid[x, y] = (int)CellType.Void;
                }
                else
                {
                    if (floorNeighbors >= 6)
                        nextGrid[x, y] = (int)CellType.Empty;
                }
            }
        }

        grid = nextGrid;
    }

    int CountFloorNeighbors8(int x, int y)
    {
        int count = 0;

        for (int ox = -1; ox <= 1; ox++)
        {
            for (int oy = -1; oy <= 1; oy++)
            {
                if (ox == 0 && oy == 0)
                    continue;

                Vector2Int p = new Vector2Int(x + ox, y + oy);

                if (IsInside(p) && grid[p.x, p.y] != (int)CellType.Void)
                    count++;
            }
        }

        return count;
    }

    void SetPlayerSpawnToCenterFloor()
    {
        Vector2Int center = new Vector2Int(width / 2, height / 2);

        // 중심 근처는 무조건 바닥으로 보정
        for (int x = center.x - safeRadius; x <= center.x + safeRadius; x++)
        {
            for (int y = center.y - safeRadius; y <= center.y + safeRadius; y++)
            {
                Vector2Int p = new Vector2Int(x, y);
                if (IsInside(p))
                    grid[x, y] = (int)CellType.Empty;
            }
        }

        playerSpawn = center;
    }

    void ClearSafeArea()
    {
        for (int x = playerSpawn.x - safeRadius; x <= playerSpawn.x + safeRadius; x++)
        {
            for (int y = playerSpawn.y - safeRadius; y <= playerSpawn.y + safeRadius; y++)
            {
                Vector2Int p = new Vector2Int(x, y);
                if (IsInside(p))
                    grid[x, y] = (int)CellType.Empty;
            }
        }
    }

    void AddCornerObstacles()
    {
        TryPlaceObstaclesNearCorner(true, true, cornerObstacleCount / 4);
        TryPlaceObstaclesNearCorner(false, true, cornerObstacleCount / 4);
        TryPlaceObstaclesNearCorner(true, false, cornerObstacleCount / 4);
        TryPlaceObstaclesNearCorner(false, false, cornerObstacleCount / 4);
    }

    void TryPlaceObstaclesNearCorner(bool left, bool bottom, int count)
    {
        int placed = 0;
        int attempts = 0;
        int maxAttempts = count * 30;

        while (placed < count && attempts < maxAttempts)
        {
            attempts++;

            int x = left ? Random.Range(1, width / 3) : Random.Range(width * 2 / 3, width - 1);
            int y = bottom ? Random.Range(1, height / 3) : Random.Range(height * 2 / 3, height - 1);

            if (TryPlaceObstacleLarge(x, y))
                placed++;
        }
    }

    void AddRandomObstacles()
    {
        int placed = 0;
        int attempts = 0;
        int maxAttempts = randomObstacleCount * 40;

        while (placed < randomObstacleCount && attempts < maxAttempts)
        {
            attempts++;

            Vector2Int p = GetRandomFloorCell();

            if (IsNearCenter(p.x, p.y, safeRadius + 2))
                continue;

            if (TryPlaceObstacleLarge(p.x, p.y))
                placed++;
        }
    }

    Vector2Int GetRandomFloorCell()
    {
        for (int i = 0; i < 200; i++)
        {
            int x = Random.Range(1, width - 1);
            int y = Random.Range(1, height - 1);

            if (grid[x, y] == (int)CellType.Empty)
                return new Vector2Int(x, y);
        }

        return playerSpawn;
    }

    bool TryPlaceObstacleLarge(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);

        if (!IsInside(pos))
            return false;

        if (grid[x, y] != (int)CellType.Empty)
            return false;

        if (Vector2Int.Distance(pos, playerSpawn) <= safeRadius + 1)
            return false;

        int checkRadius = obstacleFootprint + obstaclePadding;

        for (int ox = -checkRadius; ox <= checkRadius; ox++)
        {
            for (int oy = -checkRadius; oy <= checkRadius; oy++)
            {
                Vector2Int check = new Vector2Int(x + ox, y + oy);

                if (!IsInside(check))
                    return false;

                if (grid[check.x, check.y] != (int)CellType.Empty)
                    return false;
            }
        }

        // 실제로 장애물이 차지하는 영역 표시
        for (int ox = -obstacleFootprint; ox <= obstacleFootprint; ox++)
        {
            for (int oy = -obstacleFootprint; oy <= obstacleFootprint; oy++)
            {
                Vector2Int occupy = new Vector2Int(x + ox, y + oy);

                if (IsInside(occupy))
                    grid[occupy.x, occupy.y] = (int)CellType.Obstacle;
            }
        }

        return true;
    }

    bool EvaluateMap()
    {
        if (CountCells((int)CellType.Empty) < minFloorCells)
            return false;

        if (!CheckConnectivity())
            return false;

        if (!CheckFreeSpaceAroundPlayer())
            return false;

        if (!CheckObstacleDensity())
            return false;

        return true;
    }

    bool CheckConnectivity()
    {
        if (grid[playerSpawn.x, playerSpawn.y] == (int)CellType.Void)
            return false;

        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(playerSpawn);
        visited[playerSpawn.x, playerSpawn.y] = true;

        int reachableCount = 0;
        int floorCount = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == (int)CellType.Empty)
                    floorCount++;
            }
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            reachableCount++;

            foreach (Vector2Int dir in GetDirs())
            {
                Vector2Int next = current + dir;

                if (!IsInside(next))
                    continue;

                if (visited[next.x, next.y])
                    continue;

                if (grid[next.x, next.y] != (int)CellType.Empty)
                    continue;

                visited[next.x, next.y] = true;
                queue.Enqueue(next);
            }
        }

        return reachableCount == floorCount;
    }

    bool CheckFreeSpaceAroundPlayer()
    {
        int freeCount = 0;

        for (int x = playerSpawn.x - safeRadius; x <= playerSpawn.x + safeRadius; x++)
        {
            for (int y = playerSpawn.y - safeRadius; y <= playerSpawn.y + safeRadius; y++)
            {
                Vector2Int p = new Vector2Int(x, y);

                if (!IsInside(p))
                    continue;

                if (grid[x, y] == (int)CellType.Empty)
                    freeCount++;
            }
        }

        return freeCount >= minFreeCellsAroundPlayer;
    }

    bool CheckObstacleDensity()
    {
        int obstacleCells = CountCells((int)CellType.Obstacle);
        int floorOrObstacleCells = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != (int)CellType.Void)
                    floorOrObstacleCells++;
            }
        }

        if (floorOrObstacleCells == 0)
            return false;

        float density = (float)obstacleCells / floorOrObstacleCells;
        return density <= maxObstacleDensity;
    }

    int CountCells(int type)
    {
        int count = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == type)
                    count++;
            }
        }

        return count;
    }

    void BuildMap()
    {
        // 바닥 생성: Empty와 Obstacle 칸 모두 밑에 바닥은 깔림
        if (floorPrefab != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] == (int)CellType.Void)
                        continue;

                    SpawnReturn(floorPrefab, GridToWorld(x, y));
                }
            }
        }

        // 장애물 생성: 장애물 영역의 중심처럼 보이는 칸에만 프리팹 생성
        if (obstaclePrefabs != null && obstaclePrefabs.Length > 0)
        {
            bool[,] spawnedObstacle = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != (int)CellType.Obstacle)
                        continue;

                    if (spawnedObstacle[x, y])
                        continue;

                    GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                    GameObject obj = SpawnReturn(prefab, GridToWorld(x, y));

                    obj.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);

                    int markRadius = obstacleFootprint * 2 + obstaclePadding;
                    for (int ox = -markRadius; ox <= markRadius; ox++)
                    {
                        for (int oy = -markRadius; oy <= markRadius; oy++)
                        {
                            Vector2Int p = new Vector2Int(x + ox, y + oy);
                            if (IsInside(p))
                                spawnedObstacle[p.x, p.y] = true;
                        }
                    }
                }
            }
        }

        // 외곽 벽 생성 선택 사항
        if (wallPrefab != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] == (int)CellType.Void)
                        continue;

                    foreach (Vector2Int dir in GetDirs())
                    {
                        Vector2Int n = new Vector2Int(x, y) + dir;
                        if (!IsInside(n) || grid[n.x, n.y] == (int)CellType.Void)
                        {
                            Vector3 wallPos = GridToWorld(x, y) + new Vector3(dir.x, 0f, dir.y) * (cellSize * 0.5f);
                            SpawnReturn(wallPrefab, wallPos);
                        }
                    }
                }
            }
        }
    }

    void MovePlayerToSpawn()
    {
        if (!movePlayerToSpawn || player == null)
            return;

        Vector3 spawnPos = GridToWorld(playerSpawn.x, playerSpawn.y);
        player.position = spawnPos + Vector3.up * 0.2f;
    }

    GameObject SpawnReturn(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return null;

        GameObject obj = Instantiate(prefab, position, Quaternion.identity, transform);
        spawnedObjects.Add(obj);
        return obj;
    }

    Vector3 GridToWorld(int x, int y)
    {
        // 맵 중심이 월드 원점에 오도록 보정
        float worldX = (x - (width - 1) * 0.5f) * cellSize;
        float worldZ = (y - (height - 1) * 0.5f) * cellSize;

        return new Vector3(worldX, 0f, worldZ);
    }

    bool IsInside(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    bool IsNearCenter(int x, int y, int radius)
    {
        Vector2Int center = new Vector2Int(width / 2, height / 2);
        return Vector2Int.Distance(new Vector2Int(x, y), center) < radius;
    }

    Vector2Int[] GetDirs()
    {
        return new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };
    }

    public void ClearMap()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(spawnedObjects[i]);
                else
                    DestroyImmediate(spawnedObjects[i]);
            }
        }

        spawnedObjects.Clear();
    }
}

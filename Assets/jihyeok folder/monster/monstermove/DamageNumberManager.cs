using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bridson 방식 포아송 디스크 샘플링으로
/// 최소 거리가 확보된 데미지 숫자 위치를 한 번 생성한다.
/// </summary>
public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance
    {
        get;
        private set;
    }

    [Header("데미지 숫자 프리팹")]
    [SerializeField]
    private DamageNumber3D damageNumberPrefab;

    [Header("표시 설정")]
    [Tooltip("호출자가 색을 지정하지 않았을 때 사용하는 기본 데미지 색상")]
    [SerializeField]
    private Color normalColor = Color.white;

    [SerializeField]
    private Color criticalColor = Color.yellow;

    [SerializeField]
    private Color healColor = new Color(0.4f, 1f, 0.4f);

    [SerializeField]
    private Color goldColor = new Color(1f, 0.84f, 0.1f);

    [SerializeField]
    private Color flowerColor = new Color(1f, 0.35f, 0.75f);

    [Min(0.001f)]
    [SerializeField]
    private float normalScale = 0.1f;

    [Min(0.001f)]
    [SerializeField]
    private float criticalScale = 0.15f;

    [Header("포아송 디스크 샘플링")]
    [Tooltip("숫자가 퍼질 가로 범위")]
    [Min(0.1f)]
    [SerializeField]
    private float sampleWidth = 2f;

    [Tooltip("숫자가 퍼질 세로 범위")]
    [Min(0.1f)]
    [SerializeField]
    private float sampleHeight = 1.2f;

    [Tooltip("샘플 위치 사이 최소 거리")]
    [Min(0.05f)]
    [SerializeField]
    private float minimumDistance = 0.4f;

    [Tooltip("한 활성 점에서 후보를 검사하는 횟수")]
    [Range(1, 50)]
    [SerializeField]
    private int candidateAttempts = 20;

    private readonly List<Vector2> poissonOffsets =
        new List<Vector2>();

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        GeneratePoissonOffsets();
    }

    /// <summary>
    /// 지정된 슬롯 번호에 해당하는 포아송 위치에
    /// 데미지 숫자를 생성한다.
    /// overrideColor를 지정하지 않으면 normalColor를 사용한다.
    /// isCritical이 true면 overrideColor보다 criticalColor가 우선한다.
    /// </summary>
    public void ShowDamage(
        int damage,
        Vector3 basePosition,
        int slotIndex,
        bool isCritical = false,
        Color? overrideColor = null
    )
    {
        if (damage <= 0 ||
            damageNumberPrefab == null)
        {
            return;
        }

        if (poissonOffsets.Count == 0)
        {
            GeneratePoissonOffsets();
        }

        int safeIndex =
            Mathf.Abs(slotIndex) %
            poissonOffsets.Count;

        Vector2 offset =
            poissonOffsets[safeIndex];

        Vector3 spawnPosition =
            basePosition +
            new Vector3(
                offset.x,
                offset.y,
                0f
            );

        DamageNumber3D number =
            Instantiate(
                damageNumberPrefab,
                spawnPosition,
                Quaternion.identity
            );

        Color resolvedColor =
            isCritical
                ? criticalColor
                : (overrideColor ?? normalColor);

        number.Initialize(
            damage,
            resolvedColor,
            isCritical
                ? criticalScale
                : normalScale,
            isCritical
        );
    }

    /// <summary>
    /// 지정된 슬롯 번호에 해당하는 포아송 위치에
    /// 회복량 숫자를 생성한다.
    /// </summary>
    public void ShowHeal(
        int amount,
        Vector3 basePosition,
        int slotIndex
    )
    {
        if (amount <= 0 ||
            damageNumberPrefab == null)
        {
            return;
        }

        if (poissonOffsets.Count == 0)
        {
            GeneratePoissonOffsets();
        }

        int safeIndex =
            Mathf.Abs(slotIndex) %
            poissonOffsets.Count;

        Vector2 offset =
            poissonOffsets[safeIndex];

        Vector3 spawnPosition =
            basePosition +
            new Vector3(
                offset.x,
                offset.y,
                0f
            );

        DamageNumber3D number =
            Instantiate(
                damageNumberPrefab,
                spawnPosition,
                Quaternion.identity
            );

        number.Initialize(
            amount,
            healColor,
            normalScale,
            false,
            "+"
        );
    }

    public void ShowGold(
        int amount,
        Vector3 basePosition,
        int slotIndex
    )
    {
        ShowPositive(
            amount,
            basePosition,
            slotIndex,
            goldColor,
            "+",
            "G"
        );
    }

    public void ShowFlower(
        int amount,
        Vector3 basePosition,
        int slotIndex
    )
    {
        ShowPositive(
            amount,
            basePosition,
            slotIndex,
            flowerColor,
            "+",
            "F"
        );
    }

    private void ShowPositive(
        int amount,
        Vector3 basePosition,
        int slotIndex,
        Color color,
        string prefix,
        string suffix
    )
    {
        if (amount <= 0 ||
            damageNumberPrefab == null)
        {
            return;
        }

        if (poissonOffsets.Count == 0)
        {
            GeneratePoissonOffsets();
        }

        int safeIndex =
            Mathf.Abs(slotIndex) %
            poissonOffsets.Count;

        Vector2 offset =
            poissonOffsets[safeIndex];

        Vector3 spawnPosition =
            basePosition +
            new Vector3(
                offset.x,
                offset.y,
                0f
            );

        DamageNumber3D number =
            Instantiate(
                damageNumberPrefab,
                spawnPosition,
                Quaternion.identity
            );

        number.Initialize(
            amount,
            color,
            normalScale,
            false,
            prefix,
            suffix
        );
    }

    /// <summary>
    /// 현재 생성된 포아송 슬롯 개수를 반환한다.
    /// </summary>
    public int GetSlotCount()
    {
        return Mathf.Max(
            1,
            poissonOffsets.Count
        );
    }

    /// <summary>
    /// Bridson 포아송 디스크 샘플링.
    /// Awake에서 한 번만 실행된다.
    /// </summary>
    private void GeneratePoissonOffsets()
    {
        poissonOffsets.Clear();

        float cellSize =
            minimumDistance /
            Mathf.Sqrt(2f);

        int gridWidth =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    sampleWidth / cellSize
                )
            );

        int gridHeight =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    sampleHeight / cellSize
                )
            );

        int[,] grid =
            new int[
                gridWidth,
                gridHeight
            ];

        List<Vector2> points =
            new List<Vector2>();

        List<Vector2> activePoints =
            new List<Vector2>();

        Vector2 firstPoint =
            new Vector2(
                sampleWidth * 0.5f,
                sampleHeight * 0.5f
            );

        points.Add(firstPoint);
        activePoints.Add(firstPoint);

        AddToGrid(
            firstPoint,
            0,
            grid,
            cellSize
        );

        while (activePoints.Count > 0)
        {
            int activeIndex =
                Random.Range(
                    0,
                    activePoints.Count
                );

            Vector2 activePoint =
                activePoints[activeIndex];

            bool foundPoint = false;

            for (int i = 0;
                 i < candidateAttempts;
                 i++)
            {
                float angle =
                    Random.value *
                    Mathf.PI *
                    2f;

                float distance =
                    Random.Range(
                        minimumDistance,
                        minimumDistance * 2f
                    );

                Vector2 candidate =
                    activePoint +
                    new Vector2(
                        Mathf.Cos(angle),
                        Mathf.Sin(angle)
                    ) * distance;

                if (!IsInsideArea(candidate))
                {
                    continue;
                }

                if (!IsValidCandidate(
                        candidate,
                        points,
                        grid,
                        cellSize
                    ))
                {
                    continue;
                }

                int newPointIndex =
                    points.Count;

                points.Add(candidate);
                activePoints.Add(candidate);

                AddToGrid(
                    candidate,
                    newPointIndex,
                    grid,
                    cellSize
                );

                foundPoint = true;
                break;
            }

            if (!foundPoint)
            {
                activePoints.RemoveAt(
                    activeIndex
                );
            }
        }

        Vector2 center =
            new Vector2(
                sampleWidth * 0.5f,
                sampleHeight * 0.5f
            );

        for (int i = 0;
             i < points.Count;
             i++)
        {
            poissonOffsets.Add(
                points[i] - center
            );
        }

        // 중앙에 가까운 위치부터 사용
        poissonOffsets.Sort(
            (a, b) =>
                a.sqrMagnitude.CompareTo(
                    b.sqrMagnitude
                )
        );
    }

    private bool IsInsideArea(
        Vector2 point
    )
    {
        return
            point.x >= 0f &&
            point.x < sampleWidth &&
            point.y >= 0f &&
            point.y < sampleHeight;
    }

    private bool IsValidCandidate(
        Vector2 candidate,
        List<Vector2> points,
        int[,] grid,
        float cellSize
    )
    {
        int cellX =
            Mathf.FloorToInt(
                candidate.x / cellSize
            );

        int cellY =
            Mathf.FloorToInt(
                candidate.y / cellSize
            );

        int startX =
            Mathf.Max(
                0,
                cellX - 2
            );

        int endX =
            Mathf.Min(
                grid.GetLength(0) - 1,
                cellX + 2
            );

        int startY =
            Mathf.Max(
                0,
                cellY - 2
            );

        int endY =
            Mathf.Min(
                grid.GetLength(1) - 1,
                cellY + 2
            );

        float minimumDistanceSquared =
            minimumDistance *
            minimumDistance;

        for (int x = startX;
             x <= endX;
             x++)
        {
            for (int y = startY;
                 y <= endY;
                 y++)
            {
                int pointIndex =
                    grid[x, y] - 1;

                if (pointIndex < 0)
                {
                    continue;
                }

                if ((candidate -
                     points[pointIndex])
                    .sqrMagnitude < minimumDistanceSquared)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void AddToGrid(
        Vector2 point,
        int pointIndex,
        int[,] grid,
        float cellSize
    )
    {
        int cellX =
            Mathf.FloorToInt(
                point.x / cellSize
            );

        int cellY =
            Mathf.FloorToInt(
                point.y / cellSize
            );

        grid[cellX, cellY] =
            pointIndex + 1;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

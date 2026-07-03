using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BoonDatabase의 실제 데이터 구조를 그래프로 시각화한다.
///
/// 실제 BoonData 하나당 정점 하나를 생성한다.
///
/// requiredBoonIds:
/// 특정 일반 득도 ─────────→ 전설·듀오 득도
///
/// categoryRequirements:
/// 같은 계열의 모든 일반 득도
///     └→ 계열 조건 정점 ─→ 전설·듀오 득도
///
/// 계열 조건 정점 내부의 점 개수는 requiredCount를 의미한다.
/// </summary>
public class BoonGraphVisualizer : MonoBehaviour
{
    [Header("기존 득도 데이터")]

    [SerializeField]
    private BoonDatabase boonDatabase;

    [Header("그래프 생성 영역")]

    [Tooltip("그래프 전용 빈 UI 오브젝트를 연결")]
    [SerializeField]
    private RectTransform graphContent;

    [Header("노드 크기")]

    [Tooltip("일반 득도 아이콘 크기")]
    [SerializeField]
    private Vector2 normalNodeSize =
        new Vector2(58f, 58f);

    [Tooltip("전설·듀오 아이콘 크기")]
    [SerializeField]
    private Vector2 specialNodeSize =
        new Vector2(78f, 78f);

    [Tooltip("공격 3개 등의 계열 조건 노드 크기")]
    [SerializeField]
    private Vector2 conditionNodeSize =
        new Vector2(52f, 52f);

    [Header("화면 배치")]

    [Min(0f)]
    [SerializeField]
    private float horizontalPadding = 55f;

    [Min(0f)]
    [SerializeField]
    private float verticalPadding = 45f;

    [Range(0.1f, 1f)]
    [Tooltip("그래프 전체 크기")]
    [SerializeField]
    private float graphScale = 0.85f;

    [Min(1f)]
    [Tooltip("특수 득도 주변의 조건 노드 간격")]
    [SerializeField]
    private float conditionSpacing = 62f;

    [Header("아이콘")]

    [Min(0f)]
    [SerializeField]
    private float iconPadding = 5f;

    [Header("연결선")]

    [Min(1f)]
    [SerializeField]
    private float lineThickness = 2.5f;

    [Tooltip("특정 득도를 직접 요구하는 연결선")]
    [SerializeField]
    private Color directRequirementLineColor =
        new Color(0.9f, 0.9f, 0.9f, 0.9f);

    [Tooltip("일반 득도에서 계열 조건으로 이어지는 선")]
    [SerializeField]
    private Color categoryCandidateLineColor =
        new Color(0.6f, 0.6f, 0.6f, 0.35f);

    [Tooltip("계열 조건에서 특수 득도로 이어지는 선")]
    [SerializeField]
    private Color conditionResultLineColor =
        new Color(1f, 0.72f, 0.2f, 0.95f);

    [Header("일반 득도 배경")]

    [SerializeField]
    private Color attackColor =
        new Color(0.72f, 0.2f, 0.18f, 1f);

    [SerializeField]
    private Color defenseColor =
        new Color(0.18f, 0.42f, 0.75f, 1f);

    [SerializeField]
    private Color mobilityColor =
        new Color(0.18f, 0.65f, 0.35f, 1f);

    [SerializeField]
    private Color debuffColor =
        new Color(0.52f, 0.22f, 0.7f, 1f);

    [SerializeField]
    private Color defaultNodeColor =
        new Color(0.25f, 0.25f, 0.25f, 1f);

    [Header("특수 득도 배경")]

    [SerializeField]
    private Color legendaryColor =
        new Color(0.8f, 0.55f, 0.12f, 1f);

    [SerializeField]
    private Color duoColor =
        new Color(0.45f, 0.2f, 0.7f, 1f);

    [Header("계열 조건 배경")]

    [SerializeField]
    private Color conditionBackgroundColor =
        new Color(0.12f, 0.12f, 0.12f, 1f);

    [SerializeField]
    private Color conditionDotColor =
        new Color(1f, 1f, 1f, 1f);

    [Header("생성 설정")]

    [SerializeField]
    private bool rebuildOnEnable = true;

    [SerializeField]
    private bool printDebugLog = true;

    /*
     * 실제 그래프 정점이다.
     *
     * Key:
     * BoonData의 boonId
     *
     * Value:
     * 해당 BoonData를 표현하는 UI 정점
     */
    private readonly Dictionary<string, RectTransform>
        boonNodeById =
            new Dictionary<string, RectTransform>();

    /*
     * ID로 실제 BoonData를 찾기 위한 검색용 Dictionary.
     */
    private readonly Dictionary<string, BoonData>
        boonById =
            new Dictionary<string, BoonData>();

    /*
     * 각 계열에 속한 실제 일반 득도 목록.
     */
    private readonly Dictionary<
        BoonCategory,
        List<BoonData>
    > normalBoonsByCategory =
        new Dictionary<
            BoonCategory,
            List<BoonData>
        >();

    /*
     * 전설·듀오 득도 목록.
     */
    private readonly List<BoonData>
        specialBoons =
            new List<BoonData>();

    private Coroutine rebuildCoroutine;

    private void OnEnable()
    {
        if (!rebuildOnEnable)
        {
            return;
        }

        if (rebuildCoroutine != null)
        {
            StopCoroutine(rebuildCoroutine);
        }

        /*
         * UI 레이아웃이 계산된 다음 프레임에 생성한다.
         * GraphContent 크기가 0으로 읽히는 문제를 막는다.
         */
        rebuildCoroutine =
            StartCoroutine(
                RebuildAfterLayout()
            );
    }

    private IEnumerator RebuildAfterLayout()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        RebuildGraph();

        rebuildCoroutine = null;
    }

    /// <summary>
    /// 현재 BoonDatabase를 읽어 그래프를 다시 생성한다.
    /// </summary>
    [ContextMenu("Rebuild Boon Graph")]
    public void RebuildGraph()
    {
        if (!ValidateReferences())
        {
            return;
        }

        ClearGraph();
        BuildDataStructure();

        Canvas.ForceUpdateCanvases();

        float width =
            graphContent.rect.width;

        float height =
            graphContent.rect.height;

        if (width <= 1f ||
            height <= 1f)
        {
            Debug.LogError(
                "[BoonGraphVisualizer] " +
                "GraphContent의 크기가 0입니다. " +
                "RectTransform을 화면 전체 Stretch로 설정하세요.",
                graphContent
            );

            return;
        }

        CreateActualBoonNodes(
            width,
            height
        );

        CreateRequirementGraph();

        if (printDebugLog)
        {
            Debug.Log(
                $"[BoonGraphVisualizer] 그래프 생성 완료\n" +
                $"실제 득도 정점: {boonNodeById.Count}\n" +
                $"전설·듀오 정점: {specialBoons.Count}",
                this
            );
        }
    }

    private bool ValidateReferences()
    {
        if (boonDatabase == null)
        {
            Debug.LogError(
                "[BoonGraphVisualizer] " +
                "BoonDatabase가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (graphContent == null)
        {
            Debug.LogError(
                "[BoonGraphVisualizer] " +
                "GraphContent가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        return true;
    }

    /// <summary>
    /// BoonDatabase의 List를 그래프 생성에 적합한
    /// Dictionary와 분류 목록으로 변환한다.
    /// </summary>
    private void BuildDataStructure()
    {
        boonById.Clear();
        normalBoonsByCategory.Clear();
        specialBoons.Clear();

        List<BoonData> boons =
            boonDatabase.Boons;

        if (boons == null)
        {
            return;
        }

        for (int i = 0;
             i < boons.Count;
             i++)
        {
            BoonData boon =
                boons[i];

            if (boon == null ||
                string.IsNullOrWhiteSpace(
                    boon.boonId
                ))
            {
                continue;
            }

            if (boonById.ContainsKey(
                    boon.boonId
                ))
            {
                Debug.LogError(
                    $"[BoonGraphVisualizer] " +
                    $"중복 Boon ID: {boon.boonId}",
                    boon
                );

                continue;
            }

            boonById.Add(
                boon.boonId,
                boon
            );

            if (boon.grade ==
                BoonGrade.Normal)
            {
                RegisterNormalBoon(
                    boon
                );
            }
            else if (
                boon.grade ==
                    BoonGrade.Legendary ||
                boon.grade ==
                    BoonGrade.Duo
            )
            {
                specialBoons.Add(
                    boon
                );
            }
        }
    }

    private void RegisterNormalBoon(
        BoonData boon
    )
    {
        if (!normalBoonsByCategory.TryGetValue(
                boon.category,
                out List<BoonData> categoryList
            ))
        {
            categoryList =
                new List<BoonData>();

            normalBoonsByCategory.Add(
                boon.category,
                categoryList
            );
        }

        categoryList.Add(
            boon
        );
    }

    /// <summary>
    /// 데이터베이스의 모든 실제 BoonData를
    /// 각각 하나의 UI 정점으로 생성한다.
    /// </summary>
    private void CreateActualBoonNodes(
        float contentWidth,
        float contentHeight
    )
    {
        float scaledNormalWidth =
            normalNodeSize.x *
            graphScale;

        float scaledNormalHeight =
            normalNodeSize.y *
            graphScale;

        float scaledSpecialWidth =
            specialNodeSize.x *
            graphScale;

        float scaledSpecialHeight =
            specialNodeSize.y *
            graphScale;

        /*
         * 왼쪽 영역에는 일반 득도를 계열별 열로 배치한다.
         */
        BoonCategory[] categories =
        {
            BoonCategory.Attack,
            BoonCategory.Defense,
            BoonCategory.Mobility,
            BoonCategory.Debuff
        };

        float leftBoundary =
            -contentWidth * 0.5f +
            horizontalPadding;

        float normalAreaWidth =
            contentWidth * 0.54f;

        float categoryColumnWidth =
            normalAreaWidth /
            categories.Length;

        float top =
            contentHeight * 0.5f -
            verticalPadding;

        float bottom =
            -contentHeight * 0.5f +
            verticalPadding;

        float usableHeight =
            top - bottom;

        for (int categoryIndex = 0;
             categoryIndex < categories.Length;
             categoryIndex++)
        {
            BoonCategory category =
                categories[categoryIndex];

            if (!normalBoonsByCategory.TryGetValue(
                    category,
                    out List<BoonData> categoryBoons
                ))
            {
                continue;
            }

            float x =
                leftBoundary +
                categoryColumnWidth *
                (categoryIndex + 0.5f);

            float spacing =
                categoryBoons.Count <= 1
                    ? 0f
                    : usableHeight /
                      (categoryBoons.Count - 1);

            for (int i = 0;
                 i < categoryBoons.Count;
                 i++)
            {
                float y =
                    categoryBoons.Count == 1
                        ? 0f
                        : top -
                          spacing * i;

                BoonData boon =
                    categoryBoons[i];

                RectTransform node =
                    CreateBoonNode(
                        boon,
                        new Vector2(x, y),
                        new Vector2(
                            scaledNormalWidth,
                            scaledNormalHeight
                        ),
                        GetCategoryColor(
                            boon.category
                        )
                    );

                boonNodeById.Add(
                    boon.boonId,
                    node
                );
            }
        }

        /*
         * None 계열 일반 득도가 있다면
         * 왼쪽 가장자리 중앙에 배치한다.
         */
        if (normalBoonsByCategory.TryGetValue(
                BoonCategory.None,
                out List<BoonData> noneBoons
            ))
        {
            float spacing =
                noneBoons.Count <= 1
                    ? 0f
                    : usableHeight /
                      (noneBoons.Count - 1);

            for (int i = 0;
                 i < noneBoons.Count;
                 i++)
            {
                float y =
                    noneBoons.Count == 1
                        ? 0f
                        : top -
                          spacing * i;

                RectTransform node =
                    CreateBoonNode(
                        noneBoons[i],
                        new Vector2(
                            leftBoundary,
                            y
                        ),
                        new Vector2(
                            scaledNormalWidth,
                            scaledNormalHeight
                        ),
                        defaultNodeColor
                    );

                boonNodeById.Add(
                    noneBoons[i].boonId,
                    node
                );
            }
        }

        /*
         * 오른쪽에는 실제 전설·듀오 득도를 배치한다.
         */
        float specialX =
            contentWidth * 0.5f -
            horizontalPadding -
            scaledSpecialWidth *
            0.5f;

        float specialSpacing =
            specialBoons.Count <= 1
                ? 0f
                : usableHeight /
                  (specialBoons.Count - 1);

        for (int i = 0;
             i < specialBoons.Count;
             i++)
        {
            BoonData specialBoon =
                specialBoons[i];

            float y =
                specialBoons.Count == 1
                    ? 0f
                    : top -
                      specialSpacing * i;

            Color color =
                specialBoon.grade ==
                BoonGrade.Duo
                    ? duoColor
                    : legendaryColor;

            RectTransform node =
                CreateBoonNode(
                    specialBoon,
                    new Vector2(
                        specialX,
                        y
                    ),
                    new Vector2(
                        scaledSpecialWidth,
                        scaledSpecialHeight
                    ),
                    color
                );

            boonNodeById.Add(
                specialBoon.boonId,
                node
            );
        }
    }

    /// <summary>
    /// 실제 requiredBoonIds와 categoryRequirements를
    /// 그래프의 간선 및 조건 정점으로 변환한다.
    /// </summary>
    private void CreateRequirementGraph()
    {
        for (int specialIndex = 0;
             specialIndex < specialBoons.Count;
             specialIndex++)
        {
            BoonData specialBoon =
                specialBoons[specialIndex];

            if (!boonNodeById.TryGetValue(
                    specialBoon.boonId,
                    out RectTransform specialNode
                ))
            {
                continue;
            }

            /*
             * 특정 득도 ID 조건은 실제 정점끼리 바로 연결한다.
             */
            CreateDirectRequirementEdges(
                specialBoon,
                specialNode
            );

            /*
             * 계열 개수 조건은 중간 조건 정점을 생성한다.
             */
            CreateCategoryRequirementNodes(
                specialBoon,
                specialNode
            );
        }
    }

    private void CreateDirectRequirementEdges(
        BoonData specialBoon,
        RectTransform specialNode
    )
    {
        if (specialBoon.requiredBoonIds ==
            null)
        {
            return;
        }

        for (int i = 0;
             i <
             specialBoon
                 .requiredBoonIds
                 .Count;
             i++)
        {
            RequiredBoonId requirement =
                specialBoon
                    .requiredBoonIds[i];

            if (requirement == null ||
                string.IsNullOrWhiteSpace(
                    requirement.boonId
                ))
            {
                continue;
            }

            if (!boonNodeById.TryGetValue(
                    requirement.boonId,
                    out RectTransform requiredNode
                ))
            {
                Debug.LogWarning(
                    $"[BoonGraphVisualizer] " +
                    $"필수 득도 ID를 찾지 못했습니다: " +
                    $"{requirement.boonId}",
                    specialBoon
                );

                continue;
            }

            CreateLine(
                requiredNode,
                specialNode,
                directRequirementLineColor,
                lineThickness * graphScale
            );
        }
    }

    private void CreateCategoryRequirementNodes(
        BoonData specialBoon,
        RectTransform specialNode
    )
    {
        if (specialBoon.categoryRequirements ==
            null ||
            specialBoon.categoryRequirements.Count ==
            0)
        {
            return;
        }

        int validConditionCount = 0;

        for (int i = 0;
             i <
             specialBoon
                 .categoryRequirements
                 .Count;
             i++)
        {
            BoonRequirement requirement =
                specialBoon
                    .categoryRequirements[i];

            if (requirement != null &&
                requirement.category !=
                BoonCategory.None)
            {
                validConditionCount++;
            }
        }

        if (validConditionCount == 0)
        {
            return;
        }

        float firstY =
            specialNode.anchoredPosition.y +
            (validConditionCount - 1) *
            conditionSpacing *
            graphScale *
            0.5f;

        float conditionX =
            specialNode.anchoredPosition.x -
            145f * graphScale;

        int conditionIndex = 0;

        for (int i = 0;
             i <
             specialBoon
                 .categoryRequirements
                 .Count;
             i++)
        {
            BoonRequirement requirement =
                specialBoon
                    .categoryRequirements[i];

            if (requirement == null ||
                requirement.category ==
                BoonCategory.None)
            {
                continue;
            }

            Vector2 conditionPosition =
                new Vector2(
                    conditionX,
                    firstY -
                    conditionIndex *
                    conditionSpacing *
                    graphScale
                );

            RectTransform conditionNode =
                CreateConditionNode(
                    specialBoon,
                    requirement,
                    conditionPosition
                );

            /*
             * 해당 계열의 실제 일반 득도 정점들을
             * 계열 조건 정점으로 연결한다.
             *
             * 이 선들은 "이 계열의 후보들"을 의미한다.
             */
            if (normalBoonsByCategory.TryGetValue(
                    requirement.category,
                    out List<BoonData> categoryBoons
                ))
            {
                for (int boonIndex = 0;
                     boonIndex <
                     categoryBoons.Count;
                     boonIndex++)
                {
                    BoonData candidateBoon =
                        categoryBoons[boonIndex];

                    if (!boonNodeById.TryGetValue(
                            candidateBoon.boonId,
                            out RectTransform candidateNode
                        ))
                    {
                        continue;
                    }

                    CreateLine(
                        candidateNode,
                        conditionNode,
                        categoryCandidateLineColor,
                        lineThickness *
                        graphScale
                    );
                }
            }

            /*
             * 조건 정점에서 실제 전설·듀오 정점으로 연결한다.
             */
            CreateLine(
                conditionNode,
                specialNode,
                conditionResultLineColor,
                lineThickness *
                1.4f *
                graphScale
            );

            conditionIndex++;
        }
    }

    /// <summary>
    /// 실제 BoonData 아이콘 노드를 생성한다.
    /// </summary>
    private RectTransform CreateBoonNode(
        BoonData boon,
        Vector2 position,
        Vector2 size,
        Color backgroundColor
    )
    {
        GameObject nodeObject =
            new GameObject(
                $"BoonNode_{boon.boonId}",
                typeof(RectTransform),
                typeof(Image)
            );

        nodeObject.transform.SetParent(
            graphContent,
            false
        );

        RectTransform nodeRect =
            nodeObject.GetComponent<
                RectTransform
            >();

        SetupRect(
            nodeRect,
            position,
            size
        );

        Image background =
            nodeObject.GetComponent<Image>();

        background.color =
            backgroundColor;

        background.raycastTarget =
            false;

        if (boon.icon != null)
        {
            CreateIcon(
                nodeRect,
                boon.icon
            );
        }
        else
        {
            Debug.LogWarning(
                $"[BoonGraphVisualizer] " +
                $"{boon.displayName}의 Icon이 비어 있습니다.",
                boon
            );
        }

        return nodeRect;
    }

    /// <summary>
    /// 공격 3개 등의 계열 조건을 나타내는 중간 정점.
    ///
    /// 내부의 작은 점 개수가 requiredCount를 의미한다.
    /// </summary>
    private RectTransform CreateConditionNode(
        BoonData specialBoon,
        BoonRequirement requirement,
        Vector2 position
    )
    {
        GameObject nodeObject =
            new GameObject(
                $"Condition_" +
                $"{specialBoon.boonId}_" +
                $"{requirement.category}",
                typeof(RectTransform),
                typeof(Image)
            );

        nodeObject.transform.SetParent(
            graphContent,
            false
        );

        RectTransform nodeRect =
            nodeObject.GetComponent<
                RectTransform
            >();

        SetupRect(
            nodeRect,
            position,
            conditionNodeSize *
            graphScale
        );

        Image background =
            nodeObject.GetComponent<Image>();

        background.color =
            conditionBackgroundColor;

        background.raycastTarget =
            false;

        /*
         * 바깥 테두리 역할을 하는 계열 색상 오브젝트.
         */
        CreateConditionBorder(
            nodeRect,
            GetCategoryColor(
                requirement.category
            )
        );

        CreateRequiredCountDots(
            nodeRect,
            Mathf.Max(
                1,
                requirement.requiredCount
            )
        );

        return nodeRect;
    }

    private void CreateConditionBorder(
        RectTransform parent,
        Color color
    )
    {
        GameObject borderObject =
            new GameObject(
                "CategoryBorder",
                typeof(RectTransform),
                typeof(Image)
            );

        borderObject.transform.SetParent(
            parent,
            false
        );

        RectTransform borderRect =
            borderObject.GetComponent<
                RectTransform
            >();

        borderRect.anchorMin =
            Vector2.zero;

        borderRect.anchorMax =
            Vector2.one;

        borderRect.offsetMin =
            Vector2.zero;

        borderRect.offsetMax =
            Vector2.zero;

        Image borderImage =
            borderObject.GetComponent<Image>();

        borderImage.color =
            color;

        borderImage.raycastTarget =
            false;

        /*
         * 가운데에 작은 어두운 패널을 올려
         * 테두리처럼 보이게 만든다.
         */
        GameObject innerObject =
            new GameObject(
                "Inner",
                typeof(RectTransform),
                typeof(Image)
            );

        innerObject.transform.SetParent(
            borderRect,
            false
        );

        RectTransform innerRect =
            innerObject.GetComponent<
                RectTransform
            >();

        innerRect.anchorMin =
            Vector2.zero;

        innerRect.anchorMax =
            Vector2.one;

        float borderSize =
            4f * graphScale;

        innerRect.offsetMin =
            new Vector2(
                borderSize,
                borderSize
            );

        innerRect.offsetMax =
            new Vector2(
                -borderSize,
                -borderSize
            );

        Image innerImage =
            innerObject.GetComponent<Image>();

        innerImage.color =
            conditionBackgroundColor;

        innerImage.raycastTarget =
            false;
    }

    /// <summary>
    /// requiredCount를 글자 대신 작은 점으로 표시한다.
    /// </summary>
    private void CreateRequiredCountDots(
        RectTransform parent,
        int count
    )
    {
        int safeCount =
            Mathf.Clamp(
                count,
                1,
                12
            );

        int columns =
            Mathf.CeilToInt(
                Mathf.Sqrt(
                    safeCount
                )
            );

        int rows =
            Mathf.CeilToInt(
                safeCount /
                (float)columns
            );

        float areaWidth =
            conditionNodeSize.x *
            graphScale *
            0.58f;

        float areaHeight =
            conditionNodeSize.y *
            graphScale *
            0.58f;

        float dotSize =
            Mathf.Min(
                areaWidth /
                Mathf.Max(
                    1,
                    columns * 1.5f
                ),

                areaHeight /
                Mathf.Max(
                    1,
                    rows * 1.5f
                )
            );

        dotSize =
            Mathf.Max(
                3f,
                dotSize
            );

        float spacingX =
            columns <= 1
                ? 0f
                : areaWidth /
                  (columns - 1);

        float spacingY =
            rows <= 1
                ? 0f
                : areaHeight /
                  (rows - 1);

        float startX =
            -areaWidth * 0.5f;

        float startY =
            areaHeight * 0.5f;

        for (int i = 0;
             i < safeCount;
             i++)
        {
            int row =
                i / columns;

            int column =
                i % columns;

            GameObject dotObject =
                new GameObject(
                    $"CountDot_{i + 1}",
                    typeof(RectTransform),
                    typeof(Image)
                );

            dotObject.transform.SetParent(
                parent,
                false
            );

            RectTransform dotRect =
                dotObject.GetComponent<
                    RectTransform
                >();

            dotRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            dotRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            dotRect.pivot =
                new Vector2(0.5f, 0.5f);

            dotRect.sizeDelta =
                new Vector2(
                    dotSize,
                    dotSize
                );

            dotRect.anchoredPosition =
                new Vector2(
                    columns == 1
                        ? 0f
                        : startX +
                          spacingX *
                          column,

                    rows == 1
                        ? 0f
                        : startY -
                          spacingY *
                          row
                );

            Image dotImage =
                dotObject.GetComponent<Image>();

            dotImage.color =
                conditionDotColor;

            dotImage.raycastTarget =
                false;
        }
    }

    private void CreateIcon(
        RectTransform parent,
        Sprite icon
    )
    {
        GameObject iconObject =
            new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(Image)
            );

        iconObject.transform.SetParent(
            parent,
            false
        );

        RectTransform iconRect =
            iconObject.GetComponent<
                RectTransform
            >();

        iconRect.anchorMin =
            Vector2.zero;

        iconRect.anchorMax =
            Vector2.one;

        iconRect.offsetMin =
            new Vector2(
                iconPadding,
                iconPadding
            );

        iconRect.offsetMax =
            new Vector2(
                -iconPadding,
                -iconPadding
            );

        Image iconImage =
            iconObject.GetComponent<Image>();

        iconImage.sprite =
            icon;

        iconImage.preserveAspect =
            true;

        iconImage.raycastTarget =
            false;
    }

    /// <summary>
    /// 실제 그래프 간선을 그린다.
    /// </summary>
    private void CreateLine(
        RectTransform from,
        RectTransform to,
        Color color,
        float thickness
    )
    {
        Vector2 fromCenter =
            from.anchoredPosition;

        Vector2 toCenter =
            to.anchoredPosition;

        Vector2 direction =
            toCenter - fromCenter;

        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        Vector2 normalized =
            direction.normalized;

        /*
         * 선이 노드 중심까지 들어가지 않고
         * 노드 가장자리에서 시작하도록 조정한다.
         */
        float fromRadius =
            Mathf.Max(
                from.sizeDelta.x,
                from.sizeDelta.y
            ) * 0.48f;

        float toRadius =
            Mathf.Max(
                to.sizeDelta.x,
                to.sizeDelta.y
            ) * 0.48f;

        Vector2 start =
            fromCenter +
            normalized *
            fromRadius;

        Vector2 end =
            toCenter -
            normalized *
            toRadius;

        Vector2 lineDirection =
            end - start;

        float distance =
            lineDirection.magnitude;

        if (distance <= 0f)
        {
            return;
        }

        GameObject lineObject =
            new GameObject(
                "BoonGraphEdge",
                typeof(RectTransform),
                typeof(Image)
            );

        lineObject.transform.SetParent(
            graphContent,
            false
        );

        RectTransform lineRect =
            lineObject.GetComponent<
                RectTransform
            >();

        lineRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        lineRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        lineRect.pivot =
            new Vector2(0.5f, 0.5f);

        lineRect.anchoredPosition =
            (start + end) * 0.5f;

        lineRect.sizeDelta =
            new Vector2(
                distance,
                Mathf.Max(
                    1f,
                    thickness
                )
            );

        float angle =
            Mathf.Atan2(
                lineDirection.y,
                lineDirection.x
            ) * Mathf.Rad2Deg;

        lineRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        Image lineImage =
            lineObject.GetComponent<Image>();

        lineImage.color =
            color;

        lineImage.raycastTarget =
            false;

        /*
         * 모든 연결선을 실제 정점 뒤에 배치한다.
         */
        lineObject.transform.SetAsFirstSibling();
    }

    private void SetupRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size
    )
    {
        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;

        rect.localScale =
            Vector3.one;
    }

    private Color GetCategoryColor(
        BoonCategory category
    )
    {
        switch (category)
        {
            case BoonCategory.Attack:
                return attackColor;

            case BoonCategory.Defense:
                return defenseColor;

            case BoonCategory.Mobility:
                return mobilityColor;

            case BoonCategory.Debuff:
                return debuffColor;

            default:
                return defaultNodeColor;
        }
    }

    /// <summary>
    /// 기존에 생성된 그래프 오브젝트를 전부 제거한다.
    ///
    /// GraphContent에는 다른 UI를 넣지 않는 것이 좋다.
    /// </summary>
    [ContextMenu("Clear Boon Graph")]
    public void ClearGraph()
    {
        boonNodeById.Clear();

        if (graphContent == null)
        {
            return;
        }

        for (int i =
                 graphContent.childCount - 1;
             i >= 0;
             i--)
        {
            GameObject child =
                graphContent
                    .GetChild(i)
                    .gameObject;

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }
}
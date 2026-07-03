using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 득도 관계를 점 기반 그래프로 시각화한다.
///
/// 배치:
/// 상단 = 전설 / 듀오 득도
/// 중단 = 계열 개수 조건
/// 하단 = 일반 득도
///
/// 표시:
/// 일반 득도 = 작은 원형 점
/// 전설 득도 = 큰 원형 점
/// 듀오 득도 = 큰 원형 점
/// 계열 조건 = 마름모형 점
/// 순환 노드 = 빨간색 점
///
/// requiredBoonIds:
/// 실제 위상 정렬 간선으로 사용
///
/// categoryRequirements:
/// 위상 정렬에는 포함하지 않고
/// 중간 조건 노드로만 표시
/// </summary>
public class BoonTopologicalGraphVisualizer : MonoBehaviour
{
    [Header("데이터")]

    [SerializeField]
    private BoonDatabase boonDatabase;

    [Header("그래프 영역")]

    [Tooltip("Scroll View의 Content 같은 그래프 전용 RectTransform")]
    [SerializeField]
    private RectTransform graphContent;

    [Header("점 크기")]

    [Min(6f)]
    [SerializeField]
    private float normalDotSize = 22f;

    [Min(8f)]
    [SerializeField]
    private float specialDotSize = 34f;

    [Min(6f)]
    [SerializeField]
    private float conditionDotSize = 24f;

    [Header("배치 간격")]

    [Min(25f)]
    [SerializeField]
    private float horizontalSpacing = 85f;

    [Min(80f)]
    [SerializeField]
    private float verticalSpacing = 190f;

    [Min(20f)]
    [SerializeField]
    private float outerPadding = 80f;

    [Header("이름 표시")]

    [Tooltip("점 아래에 득도 이름 표시")]
    [SerializeField]
    private bool showBoonNames = true;

    [Tooltip("조건 점 아래에 조건 내용 표시")]
    [SerializeField]
    private bool showConditionNames = true;

    [Min(8f)]
    [SerializeField]
    private float labelFontSize = 15f;

    [SerializeField]
    private Vector2 labelSize =
        new Vector2(150f, 42f);

    [Header("연결선")]

    [Min(1f)]
    [SerializeField]
    private float lineThickness = 2f;

    [Min(3f)]
    [SerializeField]
    private float arrowSize = 9f;

    [SerializeField]
    private Color dependencyLineColor =
        new Color(0.9f, 0.9f, 0.9f, 0.75f);

    [SerializeField]
    private Color categoryLineColor =
        new Color(1f, 0.72f, 0.2f, 0.75f);

    [SerializeField]
    private Color categoryCandidateLineColor =
        new Color(1f, 0.72f, 0.2f, 0.2f);

    [Header("점 색상")]

    [SerializeField]
    private Color attackColor =
        new Color(0.8f, 0.22f, 0.2f, 1f);

    [SerializeField]
    private Color defenseColor =
        new Color(0.2f, 0.48f, 0.85f, 1f);

    [SerializeField]
    private Color mobilityColor =
        new Color(0.2f, 0.72f, 0.38f, 1f);

    [SerializeField]
    private Color debuffColor =
        new Color(0.62f, 0.28f, 0.78f, 1f);

    [SerializeField]
    private Color noneColor =
        new Color(0.55f, 0.55f, 0.55f, 1f);

    [SerializeField]
    private Color legendaryColor =
        new Color(1f, 0.7f, 0.12f, 1f);

    [SerializeField]
    private Color duoColor =
        new Color(0.68f, 0.32f, 0.9f, 1f);

    [SerializeField]
    private Color conditionColor =
        new Color(1f, 0.82f, 0.25f, 1f);

    [SerializeField]
    private Color cycleColor =
        new Color(1f, 0.1f, 0.1f, 1f);

    [Header("생성 설정")]

    [SerializeField]
    private bool rebuildOnEnable = true;

    [SerializeField]
    private bool showCategoryConditions = true;

    [SerializeField]
    private bool printDebugLog = true;

    /*
     * 득도 ID -> 생성된 점 노드
     */
    private readonly Dictionary<string, RectTransform>
        nodeByBoonId =
            new Dictionary<string, RectTransform>();

    /*
     * 위상 정렬에 실패한 순환 노드 ID
     */
    private readonly HashSet<string>
        cycleBoonIds =
            new HashSet<string>();

    private Coroutine rebuildCoroutine;

    private void OnEnable()
    {
        if (!rebuildOnEnable)
        {
            return;
        }

        if (rebuildCoroutine != null)
        {
            StopCoroutine(
                rebuildCoroutine
            );
        }

        rebuildCoroutine =
            StartCoroutine(
                RebuildNextFrame()
            );
    }

    private IEnumerator RebuildNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        RebuildGraph();

        rebuildCoroutine = null;
    }

    /// <summary>
    /// 현재 BoonDatabase를 읽어
    /// 점 기반 그래프를 다시 만든다.
    /// </summary>
    [ContextMenu("Rebuild Dot Graph")]
    public void RebuildGraph()
    {
        if (!ValidateReferences())
        {
            return;
        }

        ClearGraph();

        bool sortedSuccessfully =
            boonDatabase.TryBuildTopologicalLevels(
                out Dictionary<string, int> levelByBoonId,
                out List<BoonData> sortedBoons,
                out List<BoonData> cycleBoons
            );

        RegisterCycleBoons(
            cycleBoons
        );

        List<BoonData> normalBoons =
            new List<BoonData>();

        List<BoonData> specialBoons =
            new List<BoonData>();

        SplitBoonsByGrade(
            normalBoons,
            specialBoons
        );

        SortBoons(
            normalBoons,
            levelByBoonId
        );

        SortBoons(
            specialBoons,
            levelByBoonId
        );

        ResizeGraphContent(
            normalBoons.Count,
            specialBoons.Count
        );

        CreateNormalDots(
            normalBoons
        );

        CreateSpecialDots(
            specialBoons
        );

        /*
         * 연결선은 점보다 먼저 뒤쪽에 배치된다.
         */
        CreateDependencyEdges();

        if (showCategoryConditions)
        {
            CreateCategoryConditionDots();
        }

        BringDotsToFront();

        if (printDebugLog)
        {
            Debug.Log(
                "[BoonTopologicalGraphVisualizer] " +
                $"점 그래프 생성 완료\n" +
                $"일반 득도: {normalBoons.Count}\n" +
                $"전설/듀오: {specialBoons.Count}\n" +
                $"순환 노드: {cycleBoonIds.Count}\n" +
                $"위상 정렬 성공: {sortedSuccessfully}",
                this
            );
        }
    }

    private bool ValidateReferences()
    {
        if (boonDatabase == null)
        {
            Debug.LogError(
                "[BoonTopologicalGraphVisualizer] " +
                "BoonDatabase가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (graphContent == null)
        {
            Debug.LogError(
                "[BoonTopologicalGraphVisualizer] " +
                "GraphContent가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        return true;
    }

    private void RegisterCycleBoons(
        List<BoonData> cycleBoons
    )
    {
        cycleBoonIds.Clear();

        if (cycleBoons == null)
        {
            return;
        }

        for (int i = 0;
             i < cycleBoons.Count;
             i++)
        {
            BoonData boon =
                cycleBoons[i];

            if (boon == null ||
                string.IsNullOrWhiteSpace(
                    boon.boonId
                ))
            {
                continue;
            }

            cycleBoonIds.Add(
                boon.boonId
            );
        }
    }

    private void SplitBoonsByGrade(
        List<BoonData> normalBoons,
        List<BoonData> specialBoons
    )
    {
        List<BoonData> allBoons =
            boonDatabase.Boons;

        if (allBoons == null)
        {
            return;
        }

        for (int i = 0;
             i < allBoons.Count;
             i++)
        {
            BoonData boon =
                allBoons[i];

            if (boon == null ||
                string.IsNullOrWhiteSpace(
                    boon.boonId
                ))
            {
                continue;
            }

            if (boon.grade ==
                BoonGrade.Normal)
            {
                normalBoons.Add(
                    boon
                );
            }
            else
            {
                specialBoons.Add(
                    boon
                );
            }
        }
    }

    private void SortBoons(
        List<BoonData> boons,
        Dictionary<string, int> levelByBoonId
    )
    {
        boons.Sort(
            (left, right) =>
            {
                int leftLevel =
                    GetLevel(
                        left,
                        levelByBoonId
                    );

                int rightLevel =
                    GetLevel(
                        right,
                        levelByBoonId
                    );

                int levelCompare =
                    leftLevel.CompareTo(
                        rightLevel
                    );

                if (levelCompare != 0)
                {
                    return levelCompare;
                }

                int categoryCompare =
                    left.category.CompareTo(
                        right.category
                    );

                if (categoryCompare != 0)
                {
                    return categoryCompare;
                }

                return string.Compare(
                    left.displayName,
                    right.displayName,
                    System.StringComparison.Ordinal
                );
            }
        );
    }

    private int GetLevel(
        BoonData boon,
        Dictionary<string, int> levelByBoonId
    )
    {
        if (boon == null ||
            levelByBoonId == null)
        {
            return 0;
        }

        return levelByBoonId.TryGetValue(
            boon.boonId,
            out int level
        )
            ? level
            : 0;
    }

    private void ResizeGraphContent(
        int normalCount,
        int specialCount
    )
    {
        int largestRow =
            Mathf.Max(
                1,
                normalCount,
                specialCount
            );

        float width =
            outerPadding * 2f +
            largestRow *
            horizontalSpacing;

        float height =
            outerPadding * 2f +
            verticalSpacing * 2f +
            labelSize.y * 2f +
            specialDotSize +
            normalDotSize;

        graphContent.sizeDelta =
            new Vector2(
                Mathf.Max(
                    graphContent.sizeDelta.x,
                    width
                ),
                Mathf.Max(
                    graphContent.sizeDelta.y,
                    height
                )
            );
    }

    private void CreateNormalDots(
        List<BoonData> normalBoons
    )
    {
        float y =
            -graphContent.rect.height * 0.5f +
            outerPadding +
            labelSize.y +
            normalDotSize;

        CreateDotRow(
            normalBoons,
            y,
            normalDotSize
        );
    }

    private void CreateSpecialDots(
        List<BoonData> specialBoons
    )
    {
        float y =
            graphContent.rect.height * 0.5f -
            outerPadding -
            labelSize.y -
            specialDotSize;

        CreateDotRow(
            specialBoons,
            y,
            specialDotSize
        );
    }

    private void CreateDotRow(
        List<BoonData> boons,
        float y,
        float dotSize
    )
    {
        if (boons == null ||
            boons.Count == 0)
        {
            return;
        }

        float totalWidth =
            Mathf.Max(
                0,
                boons.Count - 1
            ) *
            horizontalSpacing;

        float startX =
            -totalWidth * 0.5f;

        for (int i = 0;
             i < boons.Count;
             i++)
        {
            BoonData boon =
                boons[i];

            Vector2 position =
                new Vector2(
                    startX +
                    i * horizontalSpacing,
                    y
                );

            RectTransform dot =
                CreateBoonDot(
                    boon,
                    position,
                    dotSize
                );

            nodeByBoonId.Add(
                boon.boonId,
                dot
            );
        }
    }

    private RectTransform CreateBoonDot(
        BoonData boon,
        Vector2 position,
        float dotSize
    )
    {
        GameObject dotObject =
            new GameObject(
                $"BoonDot_{boon.boonId}",
                typeof(RectTransform),
                typeof(Image)
            );

        dotObject.transform.SetParent(
            graphContent,
            false
        );

        RectTransform dotRect =
            dotObject.GetComponent<
                RectTransform
            >();

        SetupRect(
            dotRect,
            position,
            new Vector2(
                dotSize,
                dotSize
            )
        );

        Image dotImage =
            dotObject.GetComponent<Image>();

        /*
         * 기본 UI Image는 사각형이므로,
         * 둥근 Sprite가 없더라도 작은 크기로 사용하면
         * 점처럼 보이게 구성된다.
         *
         * 원형 Sprite를 넣고 싶다면
         * 아래 sprite 필드를 추가해 연결해도 된다.
         */
        dotImage.color =
            GetBoonColor(
                boon
            );

        dotImage.raycastTarget =
            false;

        if (showBoonNames)
        {
            CreateLabel(
                dotRect,
                boon.displayName,
                false
            );
        }

        return dotRect;
    }

    private void CreateLabel(
        RectTransform parent,
        string text,
        bool isCondition
    )
    {
        GameObject labelObject =
            new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        labelObject.transform.SetParent(
            parent,
            false
        );

        RectTransform labelRect =
            labelObject.GetComponent<
                RectTransform
            >();

        labelRect.anchorMin =
            new Vector2(0.5f, 0f);

        labelRect.anchorMax =
            new Vector2(0.5f, 0f);

        labelRect.pivot =
            new Vector2(0.5f, 1f);

        labelRect.anchoredPosition =
            new Vector2(
                0f,
                -8f
            );

        labelRect.sizeDelta =
            labelSize;

        TextMeshProUGUI label =
            labelObject.GetComponent<
                TextMeshProUGUI
            >();

        label.text = text;

        label.fontSize =
            isCondition
                ? labelFontSize - 1f
                : labelFontSize;

        label.alignment =
            TextAlignmentOptions.Top;

        label.enableWordWrapping = true;
        label.raycastTarget = false;
    }

    /// <summary>
    /// requiredBoonIds를 방향 간선으로 표시한다.
    /// </summary>
    private void CreateDependencyEdges()
    {
        List<BoonData> allBoons =
            boonDatabase.Boons;

        if (allBoons == null)
        {
            return;
        }

        for (int i = 0;
             i < allBoons.Count;
             i++)
        {
            BoonData dependentBoon =
                allBoons[i];

            if (dependentBoon == null ||
                dependentBoon.requiredBoonIds ==
                null)
            {
                continue;
            }

            if (!nodeByBoonId.TryGetValue(
                    dependentBoon.boonId,
                    out RectTransform dependentDot
                ))
            {
                continue;
            }

            for (int j = 0;
                 j <
                 dependentBoon
                     .requiredBoonIds
                     .Count;
                 j++)
            {
                RequiredBoonId requirement =
                    dependentBoon
                        .requiredBoonIds[j];

                if (requirement == null ||
                    string.IsNullOrWhiteSpace(
                        requirement.boonId
                    ))
                {
                    continue;
                }

                if (!nodeByBoonId.TryGetValue(
                        requirement.boonId,
                        out RectTransform requiredDot
                    ))
                {
                    continue;
                }

                CreateArrow(
                    requiredDot,
                    dependentDot,
                    dependencyLineColor
                );
            }
        }
    }

    /// <summary>
    /// 계열 개수 조건을 중간의 마름모 점으로 만든다.
    /// </summary>
    private void CreateCategoryConditionDots()
    {
        List<BoonData> allBoons =
            boonDatabase.Boons;

        if (allBoons == null)
        {
            return;
        }

        for (int i = 0;
             i < allBoons.Count;
             i++)
        {
            BoonData targetBoon =
                allBoons[i];

            if (targetBoon == null ||
                targetBoon.grade ==
                BoonGrade.Normal ||
                targetBoon.categoryRequirements ==
                null)
            {
                continue;
            }

            if (!nodeByBoonId.TryGetValue(
                    targetBoon.boonId,
                    out RectTransform targetDot
                ))
            {
                continue;
            }

            List<BoonRequirement> validRequirements =
                new List<BoonRequirement>();

            for (int j = 0;
                 j <
                 targetBoon
                     .categoryRequirements
                     .Count;
                 j++)
            {
                BoonRequirement requirement =
                    targetBoon
                        .categoryRequirements[j];

                if (requirement == null ||
                    requirement.category ==
                    BoonCategory.None)
                {
                    continue;
                }

                validRequirements.Add(
                    requirement
                );
            }

            float totalWidth =
                Mathf.Max(
                    0,
                    validRequirements.Count - 1
                ) *
                (conditionDotSize + 45f);

            float startX =
                targetDot.anchoredPosition.x -
                totalWidth * 0.5f;

            for (int j = 0;
                 j < validRequirements.Count;
                 j++)
            {
                BoonRequirement requirement =
                    validRequirements[j];

                Vector2 position =
                    new Vector2(
                        startX +
                        j *
                        (conditionDotSize + 45f),
                        0f
                    );

                RectTransform conditionDot =
                    CreateConditionDot(
                        targetBoon,
                        requirement,
                        position
                    );

                CreateCategoryCandidateEdges(
                    requirement.category,
                    conditionDot
                );

                CreateArrow(
                    conditionDot,
                    targetDot,
                    categoryLineColor
                );
            }
        }
    }

    private RectTransform CreateConditionDot(
        BoonData targetBoon,
        BoonRequirement requirement,
        Vector2 position
    )
    {
        GameObject dotObject =
            new GameObject(
                $"ConditionDot_" +
                $"{targetBoon.boonId}_" +
                $"{requirement.category}",
                typeof(RectTransform),
                typeof(Image)
            );

        dotObject.transform.SetParent(
            graphContent,
            false
        );

        RectTransform dotRect =
            dotObject.GetComponent<
                RectTransform
            >();

        SetupRect(
            dotRect,
            position,
            new Vector2(
                conditionDotSize,
                conditionDotSize
            )
        );

        /*
         * 45도 회전시켜 마름모로 표시한다.
         */
        dotRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                45f
            );

        Image dotImage =
            dotObject.GetComponent<Image>();

        dotImage.color =
            conditionColor;

        dotImage.raycastTarget =
            false;

        if (showConditionNames)
        {
            string labelText =
                $"{requirement.category} " +
                $"≥ {Mathf.Max(1, requirement.requiredCount)}";

            CreateConditionLabel(
                dotRect,
                labelText
            );
        }

        return dotRect;
    }

    private void CreateConditionLabel(
        RectTransform parent,
        string text
    )
    {
        GameObject labelObject =
            new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        labelObject.transform.SetParent(
            parent,
            false
        );

        RectTransform labelRect =
            labelObject.GetComponent<
                RectTransform
            >();

        labelRect.anchorMin =
            new Vector2(0.5f, 0f);

        labelRect.anchorMax =
            new Vector2(0.5f, 0f);

        labelRect.pivot =
            new Vector2(0.5f, 1f);

        labelRect.anchoredPosition =
            new Vector2(
                0f,
                -12f
            );

        labelRect.sizeDelta =
            labelSize;

        /*
         * 부모가 45도 회전되어 있으므로
         * 글자는 반대 방향으로 돌려 수평을 유지한다.
         */
        labelRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                -45f
            );

        TextMeshProUGUI label =
            labelObject.GetComponent<
                TextMeshProUGUI
            >();

        label.text = text;
        label.fontSize = labelFontSize - 1f;

        label.alignment =
            TextAlignmentOptions.Top;

        label.enableWordWrapping = true;
        label.raycastTarget = false;
    }

    private void CreateCategoryCandidateEdges(
        BoonCategory category,
        RectTransform conditionDot
    )
    {
        List<BoonData> allBoons =
            boonDatabase.Boons;

        for (int i = 0;
             i < allBoons.Count;
             i++)
        {
            BoonData boon =
                allBoons[i];

            if (boon == null ||
                boon.grade !=
                BoonGrade.Normal ||
                boon.category != category)
            {
                continue;
            }

            if (!nodeByBoonId.TryGetValue(
                    boon.boonId,
                    out RectTransform normalDot
                ))
            {
                continue;
            }

            CreateArrow(
                normalDot,
                conditionDot,
                categoryCandidateLineColor
            );
        }
    }

    private void CreateArrow(
        RectTransform from,
        RectTransform to,
        Color color
    )
    {
        Vector2 fromCenter =
            from.anchoredPosition;

        Vector2 toCenter =
            to.anchoredPosition;

        Vector2 direction =
            toCenter - fromCenter;

        if (direction.sqrMagnitude <
            0.001f)
        {
            return;
        }

        Vector2 normalized =
            direction.normalized;

        float fromRadius =
            Mathf.Max(
                from.sizeDelta.x,
                from.sizeDelta.y
            ) * 0.5f;

        float toRadius =
            Mathf.Max(
                to.sizeDelta.x,
                to.sizeDelta.y
            ) * 0.5f;

        Vector2 start =
            fromCenter +
            normalized *
            fromRadius;

        Vector2 end =
            toCenter -
            normalized *
            toRadius;

        CreateLine(
            start,
            end,
            color
        );

        CreateArrowHead(
            end,
            normalized,
            color
        );
    }

    private void CreateLine(
        Vector2 start,
        Vector2 end,
        Color color
    )
    {
        Vector2 direction =
            end - start;

        float distance =
            direction.magnitude;

        if (distance <= 0.001f)
        {
            return;
        }

        GameObject lineObject =
            new GameObject(
                "GraphEdge",
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
                lineThickness
            );

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;

        lineRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        Image lineImage =
            lineObject.GetComponent<Image>();

        lineImage.color = color;
        lineImage.raycastTarget = false;

        lineObject.transform.SetAsFirstSibling();
    }

    private void CreateArrowHead(
        Vector2 end,
        Vector2 direction,
        Color color
    )
    {
        GameObject arrowObject =
            new GameObject(
                "ArrowHead",
                typeof(RectTransform),
                typeof(Image)
            );

        arrowObject.transform.SetParent(
            graphContent,
            false
        );

        RectTransform arrowRect =
            arrowObject.GetComponent<
                RectTransform
            >();

        arrowRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        arrowRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        arrowRect.pivot =
            new Vector2(0.5f, 0.5f);

        arrowRect.anchoredPosition =
            end;

        arrowRect.sizeDelta =
            new Vector2(
                arrowSize,
                arrowSize
            );

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;

        arrowRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle - 45f
            );

        Image arrowImage =
            arrowObject.GetComponent<Image>();

        arrowImage.color = color;
        arrowImage.raycastTarget = false;

        arrowObject.transform.SetAsFirstSibling();
    }

    private Color GetBoonColor(
        BoonData boon
    )
    {
        if (cycleBoonIds.Contains(
                boon.boonId
            ))
        {
            return cycleColor;
        }

        if (boon.grade ==
            BoonGrade.Legendary)
        {
            return legendaryColor;
        }

        if (boon.grade ==
            BoonGrade.Duo)
        {
            return duoColor;
        }

        switch (boon.category)
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
                return noneColor;
        }
    }

    private void BringDotsToFront()
    {
        foreach (RectTransform dot
                 in nodeByBoonId.Values)
        {
            if (dot != null)
            {
                dot.SetAsLastSibling();
            }
        }
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

    [ContextMenu("Clear Dot Graph")]
    public void ClearGraph()
    {
        nodeByBoonId.Clear();
        cycleBoonIds.Clear();

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

            child.SetActive(false);

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

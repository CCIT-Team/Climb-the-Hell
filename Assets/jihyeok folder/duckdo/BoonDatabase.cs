using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BoonDatabase",
    menuName = "Game/Boon/Boon Database"
)]
public class BoonDatabase : ScriptableObject
{
    [Header("게임에 등장하는 모든 득도")]

    [SerializeField]
    private List<BoonData> boons =
        new List<BoonData>();

    /*
     * 기존 BoonRewardInteractable 등에서
     * List<BoonData>로 직접 사용하고 있으므로
     * 반환형을 List<BoonData>로 유지한다.
     */
    public List<BoonData> Boons
    {
        get
        {
            return boons;
        }
    }

    public int Count
    {
        get
        {
            if (boons == null)
            {
                return 0;
            }

            return boons.Count;
        }
    }

    /// <summary>
    /// 고유 ID를 이용해 득도를 찾는다.
    /// </summary>
    public BoonData GetBoonById(
        string boonId
    )
    {
        if (boons == null ||
            string.IsNullOrWhiteSpace(
                boonId
            ))
        {
            return null;
        }

        for (int i = 0;
             i < boons.Count;
             i++)
        {
            BoonData boon =
                boons[i];

            if (boon == null)
            {
                continue;
            }

            if (boon.boonId ==
                boonId)
            {
                return boon;
            }
        }

        return null;
    }

    /// <summary>
    /// requiredBoonIds를 방향 간선으로 사용해
    /// 칸(Kahn) 알고리즘 기반 위상 정렬을 수행한다.
    ///
    /// 간선 방향:
    /// 필수 득도 -> 해당 득도를 요구하는 득도
    ///
    /// categoryRequirements는
    /// 특정 노드를 요구하는 조건이 아니라
    /// 특정 계열을 N개 이상 보유해야 하는 조건이므로
    /// 위상 정렬에는 포함하지 않는다.
    /// </summary>
    public bool TryTopologicalSort(
        out List<BoonData> sortedBoons,
        out List<BoonData> cycleBoons
    )
    {
        sortedBoons =
            new List<BoonData>();

        cycleBoons =
            new List<BoonData>();

        bool graphBuilt =
            TryBuildDependencyGraph(
                out Dictionary<string, BoonData>
                    boonById,

                out Dictionary<string, List<string>>
                    outgoingEdges,

                out Dictionary<string, int>
                    indegree
            );

        if (!graphBuilt)
        {
            return false;
        }

        /*
         * 진입 차수가 0인 노드부터 처리한다.
         */
        Queue<string> zeroIndegreeQueue =
            new Queue<string>();

        foreach (KeyValuePair<string, int>
                 pair in indegree)
        {
            if (pair.Value == 0)
            {
                zeroIndegreeQueue.Enqueue(
                    pair.Key
                );
            }
        }

        while (zeroIndegreeQueue.Count > 0)
        {
            string currentId =
                zeroIndegreeQueue.Dequeue();

            sortedBoons.Add(
                boonById[currentId]
            );

            List<string> nextIds =
                outgoingEdges[currentId];

            for (int i = 0;
                 i < nextIds.Count;
                 i++)
            {
                string nextId =
                    nextIds[i];

                indegree[nextId]--;

                if (indegree[nextId] == 0)
                {
                    zeroIndegreeQueue.Enqueue(
                        nextId
                    );
                }
            }
        }

        /*
         * 전체 노드 수보다 처리된 노드가 적다면
         * 순환 관계가 존재한다.
         */
        if (sortedBoons.Count !=
            boonById.Count)
        {
            foreach (KeyValuePair<string, int>
                     pair in indegree)
            {
                if (pair.Value > 0)
                {
                    cycleBoons.Add(
                        boonById[pair.Key]
                    );
                }
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// 위상 정렬과 함께 각 득도의 깊이를 계산한다.
    ///
    /// 선행 조건이 없는 득도는 Level 0,
    /// 해당 득도를 요구하는 후속 득도는
    /// Level 1 이상이 된다.
    ///
    /// 그래프 시각화 배치에 사용한다.
    /// </summary>
    public bool TryBuildTopologicalLevels(
        out Dictionary<string, int>
            levelByBoonId,

        out List<BoonData>
            sortedBoons,

        out List<BoonData>
            cycleBoons
    )
    {
        levelByBoonId =
            new Dictionary<string, int>();

        sortedBoons =
            new List<BoonData>();

        cycleBoons =
            new List<BoonData>();

        bool graphBuilt =
            TryBuildDependencyGraph(
                out Dictionary<string, BoonData>
                    boonById,

                out Dictionary<string, List<string>>
                    outgoingEdges,

                out Dictionary<string, int>
                    indegree
            );

        if (!graphBuilt)
        {
            return false;
        }

        Queue<string> zeroIndegreeQueue =
            new Queue<string>();

        /*
         * 모든 노드의 기본 레벨을 0으로 설정한다.
         */
        foreach (string boonId
                 in boonById.Keys)
        {
            levelByBoonId[boonId] = 0;

            if (indegree[boonId] == 0)
            {
                zeroIndegreeQueue.Enqueue(
                    boonId
                );
            }
        }

        while (zeroIndegreeQueue.Count > 0)
        {
            string currentId =
                zeroIndegreeQueue.Dequeue();

            sortedBoons.Add(
                boonById[currentId]
            );

            List<string> nextIds =
                outgoingEdges[currentId];

            for (int i = 0;
                 i < nextIds.Count;
                 i++)
            {
                string nextId =
                    nextIds[i];

                /*
                 * 여러 선행 조건이 있을 경우
                 * 가장 깊은 경로를 기준으로 레벨을 잡는다.
                 */
                levelByBoonId[nextId] =
                    Mathf.Max(
                        levelByBoonId[nextId],
                        levelByBoonId[currentId] + 1
                    );

                indegree[nextId]--;

                if (indegree[nextId] == 0)
                {
                    zeroIndegreeQueue.Enqueue(
                        nextId
                    );
                }
            }
        }

        /*
         * 모든 노드가 처리되지 않았다면
         * 순환 관계가 존재한다.
         */
        if (sortedBoons.Count !=
            boonById.Count)
        {
            int fallbackLevel = 0;

            foreach (int level
                     in levelByBoonId.Values)
            {
                fallbackLevel =
                    Mathf.Max(
                        fallbackLevel,
                        level
                    );
            }

            fallbackLevel++;

            foreach (KeyValuePair<string, int>
                     pair in indegree)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                cycleBoons.Add(
                    boonById[pair.Key]
                );

                /*
                 * 순환 노드도 시각화할 수 있도록
                 * 마지막 레벨에 임시 배치한다.
                 */
                levelByBoonId[pair.Key] =
                    fallbackLevel;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// 위상 정렬에 필요한 그래프 구조를 만든다.
    ///
    /// 생성되는 자료:
    /// 1. ID별 BoonData 검색 구조
    /// 2. 정점별 후속 노드 목록
    /// 3. 각 정점의 진입 차수
    /// </summary>
    private bool TryBuildDependencyGraph(
        out Dictionary<string, BoonData>
            boonById,

        out Dictionary<string, List<string>>
            outgoingEdges,

        out Dictionary<string, int>
            indegree
    )
    {
        boonById =
            new Dictionary<string, BoonData>();

        outgoingEdges =
            new Dictionary<string, List<string>>();

        indegree =
            new Dictionary<string, int>();

        if (boons == null)
        {
            return true;
        }

        bool isValid = true;

        /*
         * 1단계:
         * 모든 유효한 득도를 그래프 정점으로 등록한다.
         */
        for (int i = 0;
             i < boons.Count;
             i++)
        {
            BoonData boon =
                boons[i];

            if (boon == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    boon.boonId
                ))
            {
                Debug.LogError(
                    "[BoonDatabase] " +
                    $"{boon.name}의 Boon ID가 비어 있습니다.",
                    boon
                );

                isValid = false;
                continue;
            }

            if (boonById.ContainsKey(
                    boon.boonId
                ))
            {
                Debug.LogError(
                    "[BoonDatabase] " +
                    $"중복된 Boon ID: {boon.boonId}",
                    boon
                );

                isValid = false;
                continue;
            }

            boonById.Add(
                boon.boonId,
                boon
            );

            outgoingEdges.Add(
                boon.boonId,
                new List<string>()
            );

            indegree.Add(
                boon.boonId,
                0
            );
        }

        /*
         * 2단계:
         * requiredBoonIds를 방향 간선으로 변환한다.
         *
         * 예:
         * 공격 강화 1 -> 공격 강화 2
         */
        foreach (KeyValuePair<string, BoonData>
                 pair in boonById)
        {
            BoonData dependentBoon =
                pair.Value;

            if (dependentBoon.requiredBoonIds ==
                null)
            {
                continue;
            }

            /*
             * 하나의 득도 안에서
             * 같은 필수 ID가 중복 등록되는 것을 방지한다.
             */
            HashSet<string> duplicatedEdges =
                new HashSet<string>();

            for (int i = 0;
                 i <
                 dependentBoon
                     .requiredBoonIds
                     .Count;
                 i++)
            {
                RequiredBoonId requirement =
                    dependentBoon
                        .requiredBoonIds[i];

                if (requirement == null ||
                    string.IsNullOrWhiteSpace(
                        requirement.boonId
                    ))
                {
                    continue;
                }

                string requiredId =
                    requirement.boonId;

                if (!boonById.ContainsKey(
                        requiredId
                    ))
                {
                    Debug.LogError(
                        "[BoonDatabase] " +
                        $"{dependentBoon.displayName}이 요구하는 " +
                        $"득도 ID를 찾을 수 없습니다: {requiredId}",
                        dependentBoon
                    );

                    isValid = false;
                    continue;
                }

                if (!duplicatedEdges.Add(
                        requiredId
                    ))
                {
                    Debug.LogWarning(
                        "[BoonDatabase] " +
                        $"{dependentBoon.displayName}의 " +
                        $"필수 득도 ID가 중복되었습니다: {requiredId}",
                        dependentBoon
                    );

                    continue;
                }

                /*
                 * 필수 득도에서 후속 득도로 간선을 연결한다.
                 */
                outgoingEdges[requiredId].Add(
                    dependentBoon.boonId
                );

                /*
                 * 후속 득도의 진입 차수를 증가시킨다.
                 */
                indegree[dependentBoon.boonId]++;
            }
        }

        return isValid;
    }

    /// <summary>
    /// 인스펙터의 컨텍스트 메뉴에서
    /// 위상 정렬 검사를 실행한다.
    /// </summary>
    [ContextMenu("Validate Topological Order")]
    private void ValidateTopologicalOrder()
    {
        bool success =
            TryTopologicalSort(
                out List<BoonData> sortedBoons,
                out List<BoonData> cycleBoons
            );

        if (!success)
        {
            /*
             * 잘못된 ID나 중복 ID 때문에
             * 그래프 생성부터 실패한 경우.
             */
            if (cycleBoons.Count == 0)
            {
                Debug.LogError(
                    "[BoonDatabase] " +
                    "위상 정렬 전 데이터 검증에 실패했습니다.",
                    this
                );

                return;
            }

            string cycleNames =
                string.Empty;

            for (int i = 0;
                 i < cycleBoons.Count;
                 i++)
            {
                if (i > 0)
                {
                    cycleNames += ", ";
                }

                cycleNames +=
                    cycleBoons[i].displayName;
            }

            Debug.LogError(
                "[BoonDatabase] " +
                "득도 선행 조건에 순환 관계가 존재합니다.\n" +
                $"관련 득도: {cycleNames}",
                this
            );

            return;
        }

        string order =
            string.Empty;

        for (int i = 0;
             i < sortedBoons.Count;
             i++)
        {
            if (i > 0)
            {
                order += " -> ";
            }

            order +=
                sortedBoons[i].displayName;
        }

        Debug.Log(
            "[BoonDatabase] " +
            "위상 정렬 검증 성공\n" +
            order,
            this
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (boons == null)
        {
            boons =
                new List<BoonData>();

            return;
        }

        HashSet<string> usedIds =
            new HashSet<string>();

        for (int i = 0;
             i < boons.Count;
             i++)
        {
            BoonData boon =
                boons[i];

            if (boon == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    boon.boonId
                ))
            {
                Debug.LogWarning(
                    $"{boon.name}의 Boon ID가 비어 있습니다.",
                    boon
                );

                continue;
            }

            if (!usedIds.Add(
                    boon.boonId
                ))
            {
                Debug.LogError(
                    $"중복된 Boon ID: {boon.boonId}",
                    boon
                );
            }
        }
    }
#endif
}

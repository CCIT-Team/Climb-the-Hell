using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapGenerator : MonoBehaviour
{
    [Header("프리팹")]
    public Button nodeButtonPrefab;
    public Image lineImagePrefab;

    [Header("부모 오브젝트")]
    public RectTransform nodeParent;
    public RectTransform lineParent;

    [Header("층 설정")]
    public int currentFloor = 1;
    public int maxFloor = 10;

    [Header("위치 설정")]
    public float startY = -120f;
    public float floorGapY = 30f;
    public float nodeGapX = 70f;

    [Header("주변 노드 연결 설정")]
    public int nearNodeRange = 1; // 1이면 바로 주변 노드만 연결

    [Header("크기 설정")]
    public Vector2 nodeSize = new Vector2(25, 25);
    public float lineThickness = 2f;

    private List<List<MapNodes>> floors = new List<List<MapNodes>>();
    private HashSet<string> connectedLineKeys = new HashSet<string>();

    private void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        ClearMap();
        CreateNodes();
        CreateLines();
    }

    private void ClearMap()
    {
        floors.Clear();
        connectedLineKeys.Clear();

        foreach (Transform child in nodeParent)
            Destroy(child.gameObject);

        foreach (Transform child in lineParent)
            Destroy(child.gameObject);
    }

    private void CreateNodes()
    {
        for (int floor = 1; floor <= maxFloor; floor++)
        {
            List<MapNodes> floorNodes = new List<MapNodes>();
            int nodeCount = GetNodeCount(floor);

            for (int i = 0; i < nodeCount; i++)
            {
                Button button = Instantiate(nodeButtonPrefab, nodeParent);
                RectTransform rect = button.GetComponent<RectTransform>();

                float y = startY + (floor - 1) * floorGapY;
                float x = (i - (nodeCount - 1) / 2f) * nodeGapX;
                x += Random.Range(-15f, 15f);

                rect.anchoredPosition = new Vector2(x, y);
                rect.sizeDelta = nodeSize;

                MapNodes node = button.GetComponent<MapNodes>();
                node.Init(floor, GetSceneNameByFloor(floor), this);

                floorNodes.Add(node);
            }

            floors.Add(floorNodes);
        }
    }

    private void CreateLines()
    {
        connectedLineKeys.Clear();

        for (int floorIndex = 0; floorIndex < floors.Count - 1; floorIndex++)
        {
            List<MapNodes> currentFloorNodes = floors[floorIndex];
            List<MapNodes> nextFloorNodes = floors[floorIndex + 1];

            for (int i = 0; i < currentFloorNodes.Count; i++)
            {
                List<MapNodes> nearNodes = GetNearNodes(i, nextFloorNodes);
                MapNodes toNode = nearNodes[Random.Range(0, nearNodes.Count)];
                TryConnect(currentFloorNodes[i], toNode);
            }

            for (int i = 0; i < nextFloorNodes.Count; i++)
            {
                if (!HasIncomingLine(currentFloorNodes, nextFloorNodes[i]))
                {
                    List<MapNodes> nearFromNodes = GetNearNodes(i, currentFloorNodes);
                    MapNodes fromNode = nearFromNodes[Random.Range(0, nearFromNodes.Count)];
                    TryConnect(fromNode, nextFloorNodes[i]);
                }
            }

            int extraLineCount = GetExtraLineCount(floorIndex + 1);

            for (int i = 0; i < extraLineCount; i++)
            {
                int randomFromIndex = Random.Range(0, currentFloorNodes.Count);
                List<MapNodes> nearNodes = GetNearNodes(randomFromIndex, nextFloorNodes);

                MapNodes fromNode = currentFloorNodes[randomFromIndex];
                MapNodes toNode = nearNodes[Random.Range(0, nearNodes.Count)];

                TryConnect(fromNode, toNode);
            }
        }
    }

private List<MapNodes> GetNearNodes(int baseIndex, List<MapNodes> targetNodes)
{
    List<MapNodes> nearNodes = new List<MapNodes>();

    if (targetNodes == null || targetNodes.Count == 0)
        return nearNodes;

    int clampedBaseIndex = Mathf.Clamp(baseIndex, 0, targetNodes.Count - 1);

    int startIndex = Mathf.Max(0, clampedBaseIndex - nearNodeRange);
    int endIndex = Mathf.Min(targetNodes.Count - 1, clampedBaseIndex + nearNodeRange);

    for (int i = startIndex; i <= endIndex; i++)
    {
        nearNodes.Add(targetNodes[i]);
    }

    return nearNodes;
}
    private bool HasIncomingLine(List<MapNodes> currentFloorNodes, MapNodes toNode)
    {
        foreach (MapNodes fromNode in currentFloorNodes)
        {
            if (connectedLineKeys.Contains(GetLineKey(fromNode, toNode)))
                return true;
        }

        return false;
    }

    private void TryConnect(MapNodes fromNode, MapNodes toNode)
    {
        string key = GetLineKey(fromNode, toNode);

        if (connectedLineKeys.Contains(key))
            return;

        connectedLineKeys.Add(key);

        DrawLine(
            fromNode.GetComponent<RectTransform>(),
            toNode.GetComponent<RectTransform>()
        );
    }

    private string GetLineKey(MapNodes fromNode, MapNodes toNode)
    {
        return fromNode.GetInstanceID() + "_" + toNode.GetInstanceID();
    }

    private void DrawLine(RectTransform from, RectTransform to)
    {
        Image line = Instantiate(lineImagePrefab, lineParent);
        RectTransform lineRect = line.GetComponent<RectTransform>();

        Vector2 start = from.anchoredPosition;
        Vector2 end = to.anchoredPosition;

        Vector2 direction = end - start;
        float distance = direction.magnitude;

        lineRect.anchoredPosition = start + direction / 2f;
        lineRect.sizeDelta = new Vector2(distance, lineThickness);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineRect.rotation = Quaternion.Euler(0, 0, angle);
    }

    private int GetNodeCount(int floor)
    {
        if (floor == 1)
            return 1;

        if (floor == maxFloor)
            return 1;

        if (floor <= 3)
            return Random.Range(1, 3);

        if (floor <= 6)
            return Random.Range(2, 4);

        return Random.Range(3, 5);
    }

    private int GetExtraLineCount(int floor)
    {
        if (floor <= 3)
            return Random.Range(0, 2);

        if (floor <= 6)
            return Random.Range(1, 3);

        return Random.Range(1, 4);
    }

    private string GetSceneNameByFloor(int floor)
    {
        return "mixseen";
    }
}
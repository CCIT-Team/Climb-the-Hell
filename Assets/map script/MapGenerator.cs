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

    [Header("랜덤 간선 확률")]
    public float extraLineChance = 0.25f;

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

            foreach (MapNodes fromNode in currentFloorNodes)
            {
                MapNodes randomToNode = nextFloorNodes[Random.Range(0, nextFloorNodes.Count)];
                TryConnect(fromNode, randomToNode);
            }

            foreach (MapNodes toNode in nextFloorNodes)
            {
                bool hasIncoming = false;

                foreach (MapNodes fromNode in currentFloorNodes)
                {
                    if (connectedLineKeys.Contains(GetLineKey(fromNode, toNode)))
                    {
                        hasIncoming = true;
                        break;
                    }
                }

                if (!hasIncoming)
                {
                    MapNodes randomFromNode = currentFloorNodes[Random.Range(0, currentFloorNodes.Count)];
                    TryConnect(randomFromNode, toNode);
                }
            }

            foreach (MapNodes fromNode in currentFloorNodes)
            {
                foreach (MapNodes toNode in nextFloorNodes)
                {
                    if (Random.value < extraLineChance)
                        TryConnect(fromNode, toNode);
                }
            }
        }
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

        if (floor == 9 || floor == 10)
            return 1;

        return Random.Range(2, 4);
    }

    private string GetSceneNameByFloor(int floor)
    {
        return "mixseen";
    }
}
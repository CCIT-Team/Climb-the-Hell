using System.Collections.Generic;
using System.IO;
using UnityEngine;






public class MapMaker : MonoBehaviour
{
    [Header("Map Setting")]
    public int mapWidth = 30;
    public int mapHeight = 30;
    public int obstacleCount = 20;

    [Header("Prefab")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject obstaclePrefab;
    public GameObject playerStartPrefab;

    [Header("Parent")]
    public Transform mapParent;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<MapObjectData> savedObjects = new List<MapObjectData>();

    private int currentMapIndex = 0;

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap() // 맵 생성을 위해 함수들을 불러옴
    {
        ClearMap();

        currentMapIndex++;
        savedObjects.Clear(); // 맵 클리어 

        CreateFloor();
        CreateWalls();
        CreatePlayerStart();
        CreateObstacles();

        Debug.Log("맵 생성 완료");
    }

    void ClearMap()
    {
        foreach (GameObject obj in spawnedObjects) // 하나
        {
            if (obj != null)
            {
                Destroy(obj); // 오브젝트 디스트로이??? < 어 시발 이거 아닌데
            }
        }

        spawnedObjects.Clear();
    }

    void CreateFloor() // 오브젝트 소환
    {
        GameObject floor = Instantiate(
            floorPrefab,
            Vector3.zero,
            Quaternion.identity,
            mapParent
        );

        floor.transform.localScale = new Vector3(mapWidth, 1, mapHeight);

        AddObjectData("Floor", floor.transform);
        spawnedObjects.Add(floor);
    }

    void CreateWalls()
    {
        float halfX = mapWidth / 2f;
        float halfZ = mapHeight / 2f;

        CreateWall("Wall_North", new Vector3(0, 1, halfZ), new Vector3(mapWidth, 2, 1));
        CreateWall("Wall_South", new Vector3(0, 1, -halfZ), new Vector3(mapWidth, 2, 1));
        CreateWall("Wall_East", new Vector3(halfX, 1, 0), new Vector3(1, 2, mapHeight));
        CreateWall("Wall_West", new Vector3(-halfX, 1, 0), new Vector3(1, 2, mapHeight));
    }

    void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = Instantiate(
            wallPrefab,
            position,
            Quaternion.identity,
            mapParent
        );

        wall.name = name;
        wall.transform.localScale = scale;

        AddObjectData(name, wall.transform);
        spawnedObjects.Add(wall);
    }

    void CreatePlayerStart()
    {
        GameObject start = Instantiate(
            playerStartPrefab,
            Vector3.zero,
            Quaternion.identity,
            mapParent
        );

        start.name = "PlayerStart";

        AddObjectData("PlayerStart", start.transform);
        spawnedObjects.Add(start);
    }

    void CreateObstacles()
    {
        for (int i = 0; i < obstacleCount; i++)
        {
            Vector3 pos = GetRandomPosition();

            GameObject obstacle = Instantiate(
                obstaclePrefab,
                pos,
                Quaternion.Euler(0, Random.Range(0, 360), 0),
                mapParent
            );

            obstacle.name = "Obstacle_" + i;

            float randomScale = Random.Range(0.8f, 1.8f);
            obstacle.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

            AddObjectData(obstacle.name, obstacle.transform);
            spawnedObjects.Add(obstacle);
        }
    }

    Vector3 GetRandomPosition()
    {
        float x = Random.Range(-mapWidth / 2f + 3f, mapWidth / 2f - 3f);
        float z = Random.Range(-mapHeight / 2f + 3f, mapHeight / 2f - 3f);

        return new Vector3(x, 0, z);
    }

    void AddObjectData(string type, Transform tr)
    {
        MapObjectData data = new MapObjectData();

        data.type = type;

        data.position = new Vector3Dataa(
            tr.position.x,
            tr.position.y,
            tr.position.z
        );

        data.rotation = new Vector3Dataa(
            tr.eulerAngles.x,
            tr.eulerAngles.y,
            tr.eulerAngles.z
        );

        data.scale = new Vector3Dataa(
            tr.localScale.x,
            tr.localScale.y,
            tr.localScale.z
        );

        savedObjects.Add(data);
    }

    void SaveMap() // 맵 json 파일로 저장
    {
        MapSaveData saveData = new MapSaveData();

        saveData.mapName = "Map_" + currentMapIndex;
        saveData.width = mapWidth;
        saveData.height = mapHeight;
        saveData.objects = savedObjects;

        string json = JsonUtility.ToJson(saveData, true);

        string folderPath = Application.dataPath + "/GeneratedMaps";

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = folderPath + "/" + saveData.mapName + ".json";

        File.WriteAllText(filePath, json);

        Debug.Log("맵 저장 완료: " + filePath);
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = 24;

        if (GUI.Button(new Rect(20, 20, 160, 60), "통과", style))
        {
            Debug.Log("현재 맵 통과");
        }

        if (GUI.Button(new Rect(20, 90, 160, 60), "실패", style))
        {
            Debug.Log("현재 맵 실패 → 새 맵 생성");
            GenerateMap();
        }

        if (GUI.Button(new Rect(20, 160, 160, 60), "저장", style))
        {
            SaveMap();
        }
    }
}


using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapNodes : MonoBehaviour
{
    public int floor;
    public string targetSceneName;

    private MapGenerator mapGenerator;
    private Button button;

    public void Init(int floorValue, string sceneName, MapGenerator generator)
    {
        floor = floorValue;
        targetSceneName = sceneName;
        mapGenerator = generator;

        button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(MoveToScene);

        RefreshState();
    }

    public void RefreshState()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.interactable = floor == mapGenerator.currentFloor + 1;
    }

    private void MoveToScene()
    {
        if (floor != mapGenerator.currentFloor + 1)
            return;

        mapGenerator.currentFloor = floor;
        SceneManager.LoadScene(targetSceneName);
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNodeButton : MonoBehaviour
{
    public string targetSceneName = "mixseen";

    public void MoveToScene()
    {
        SceneManager.LoadScene(targetSceneName);
    }
}
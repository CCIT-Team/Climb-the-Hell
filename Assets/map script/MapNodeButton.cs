using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNodeButton : MonoBehaviour
{
    public string targetSceneName = "BattleScene_01";

    public void MoveToScene()
    {
        SceneManager.LoadScene(targetSceneName);
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMoveTrigger : MonoBehaviour
{
    [Header("이동할 씬 이름")]
    [SerializeField] private string targetSceneName;

    [Header("중복 이동 방지")]
    [SerializeField] private bool isChangingScene;

    private void OnTriggerEnter(Collider other)
    {
        if (isChangingScene)
            return;

        Player player = other.GetComponentInParent<Player>();

        if (player == null && !other.CompareTag("Player"))
            return;

        MoveScene();
    }

    private void MoveScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning(
                "[SceneMoveTrigger] 이동할 씬 이름이 설정되지 않았습니다."
            );
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError(
                $"[SceneMoveTrigger] '{targetSceneName}' 씬을 찾을 수 없습니다."
            );
            return;
        }

        isChangingScene = true;

        SceneManager.LoadScene(targetSceneName);
    }
}
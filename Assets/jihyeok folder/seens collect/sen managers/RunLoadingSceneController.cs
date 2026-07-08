using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loading 씬에서 RunFlowManager가 저장한 목적지 씬을 로드한다.
/// 로드 완료 후 PlayerSceneMover가 Player를 생성/이동한다.
/// </summary>
public class RunLoadingSceneController : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField]
    private string defaultSpawnId = "Default";

    [Header("로딩 설정")]
    [Min(0f)]
    [SerializeField]
    private float minimumLoadingTime = 0.3f;

    [Header("디버그")]
    [SerializeField]
    private bool showLogs = true;

    private bool loading;

    private void Start()
    {
        if (loading)
        {
            return;
        }

        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        loading = true;

        DontDestroyOnLoad(gameObject);

        RunFlowManager manager =
            RunFlowManager.Instance;

        if (manager == null ||
            !manager.HasPendingDestination)
        {
            Debug.LogError(
                "[RunLoadingSceneController] RunFlowManager에 목적지 씬이 없습니다.",
                this
            );

            Destroy(gameObject);
            yield break;
        }

        string targetSceneName =
            manager.GetPendingSceneName();

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError(
                "[RunLoadingSceneController] 목적지 씬 이름이 비어 있습니다.",
                this
            );

            Destroy(gameObject);
            yield break;
        }

        string spawnId =
            PlayerSpawnContext.ConsumeSpawnId();

        if (string.IsNullOrWhiteSpace(spawnId))
        {
            spawnId = defaultSpawnId;
        }

        float startTime =
            Time.realtimeSinceStartup;

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                targetSceneName,
                LoadSceneMode.Single
            );

        if (operation == null)
        {
            Debug.LogError(
                $"[RunLoadingSceneController] 씬 로드 실패: {targetSceneName}",
                this
            );

            Destroy(gameObject);
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }

        float elapsed =
            Time.realtimeSinceStartup -
            startTime;

        float remain =
            minimumLoadingTime -
            elapsed;

        if (remain > 0f)
        {
            yield return new WaitForSecondsRealtime(
                remain
            );
        }

        yield return null;

        Scene loadedScene =
            SceneManager.GetSceneByName(
                targetSceneName
            );

        if (loadedScene.IsValid() &&
            loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(
                loadedScene
            );
        }
        else
        {
            Debug.LogError(
                $"[RunLoadingSceneController] 로드된 씬을 찾지 못했습니다: {targetSceneName}",
                this
            );
        }

        manager.CommitPendingDestination();

        bool moved = false;

        if (PlayerSceneMover.Instance != null)
        {
            moved =
                PlayerSceneMover.Instance
                    .SpawnOrMovePlayerToActiveScene(spawnId);
        }
        else
        {
            Debug.LogError(
                "[RunLoadingSceneController] PlayerSceneMover가 없습니다. Title 씬에 PlayerSceneMover를 배치하세요.",
                this
            );
        }

        if (showLogs)
        {
            Debug.Log(
                $"[RunLoadingSceneController] 씬 이동 완료 / " +
                $"씬={targetSceneName}, " +
                $"Spawn ID={spawnId}, " +
                $"Player 이동={moved}",
                this
            );
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(defaultSpawnId))
        {
            defaultSpawnId = "Default";
        }
    }
#endif
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// RunFlowManager가 저장한 목적지 씬을 비동기로 로드한다.
/// Loading 씬에 하나만 배치한다.
/// </summary>
public class LoadingSceneController : MonoBehaviour
{
    [Header("로딩 UI")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("로딩 시간")]
    [Min(0f)]
    [Tooltip("실제 씬 로딩이 끝나더라도 로딩 화면을 유지할 최소 시간")]
    [SerializeField] private float minimumDisplayTime = 2f;

    private IEnumerator Start()
    {
        // 이전 씬에서 시간이 정지되어 있을 가능성 방지
        Time.timeScale = 1f;

        RunFlowManager manager = RunFlowManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[LoadingSceneController] RunFlowManager를 찾을 수 없습니다.",
                this);

            yield break;
        }

        string targetSceneName =
            manager.GetPendingSceneName();

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError(
                "[LoadingSceneController] 로드할 목적지 씬이 설정되지 않았습니다.",
                this);

            yield break;
        }

        SetProgress(0f);

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                targetSceneName,
                LoadSceneMode.Single);

        if (operation == null)
        {
            Debug.LogError(
                $"[LoadingSceneController] '{targetSceneName}' 씬 로드에 실패했습니다.",
                this);

            yield break;
        }

        // 로드가 완료되어도 바로 씬을 전환하지 않는다.
        operation.allowSceneActivation = false;

        float startedTime = Time.unscaledTime;

        while (true)
        {
            float elapsedTime =
                Time.unscaledTime - startedTime;

            // Unity 비동기 로딩은 씬 활성화 전까지 progress가 0.9에서 멈춘다.
            float sceneLoadProgress =
                Mathf.Clamp01(operation.progress / 0.9f);

            // 최소 로딩 시간 기준 진행률
            float timeProgress;

            if (minimumDisplayTime <= 0f)
            {
                timeProgress = 1f;
            }
            else
            {
                timeProgress =
                    Mathf.Clamp01(
                        elapsedTime / minimumDisplayTime);
            }

            // 실제 로딩과 최소 시간 중 더 느린 쪽을 진행률로 사용한다.
            float displayedProgress =
                Mathf.Min(
                    sceneLoadProgress,
                    timeProgress);

            SetProgress(displayedProgress);

            bool sceneLoadCompleted =
                operation.progress >= 0.9f;

            bool minimumTimeCompleted =
                elapsedTime >= minimumDisplayTime;

            if (sceneLoadCompleted &&
                minimumTimeCompleted)
            {
                break;
            }

            yield return null;
        }

        SetProgress(1f);

        // 목적지 정보를 확정한 후 씬 활성화
        manager.CommitPendingDestination();

        operation.allowSceneActivation = true;
    }

    private void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);

        if (progressSlider != null)
        {
            progressSlider.value = value;
        }

        if (progressText != null)
        {
            progressText.text =
                $"{value * 100f:0}%";
        }
    }
}
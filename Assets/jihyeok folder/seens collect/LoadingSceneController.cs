using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider loadingSlider;

    [Header("Loading")]
    [SerializeField] private float minimumLoadingTime = 5f;

    [Tooltip("슬라이더가 이동하는 속도")]
    [SerializeField] private float sliderSpeed = 0.5f;

    private void Start()
    {
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        string sceneName = SceneLoader.nextSceneName;

        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = "lobby";
        }

        if (loadingSlider != null)
        {
            loadingSlider.minValue = 0f;
            loadingSlider.maxValue = 1f;
            loadingSlider.value = 0f;
        }

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName);

        operation.allowSceneActivation = false;

        float timer = 0f;
        float displayedProgress = 0f;

        while (true)
        {
            timer += Time.deltaTime;

            // Unity 비동기 로딩 진행도는 0.9에서 멈춤
            float realProgress =
                Mathf.Clamp01(operation.progress / 0.9f);

            // 최소 로딩 시간 기준 진행도
            float timeProgress =
                Mathf.Clamp01(timer / minimumLoadingTime);

            /*
             * 씬 로딩이 빨라도 최소 로딩 시간에 맞춰 이동하고,
             * 실제 로딩이 느리면 실제 진행도 이상으로 올라가지 않음.
             */
            float targetProgress =
                Mathf.Min(realProgress, timeProgress);

            displayedProgress = Mathf.MoveTowards(
                displayedProgress,
                targetProgress,
                sliderSpeed * Time.deltaTime
            );

            if (loadingSlider != null)
            {
                loadingSlider.value = displayedProgress;
            }

            bool sceneLoaded = operation.progress >= 0.9f;
            bool minimumTimePassed = timer >= minimumLoadingTime;
            bool sliderFinished = displayedProgress >= 0.99f;

            if (sceneLoaded &&
                minimumTimePassed &&
                sliderFinished)
            {
                if (loadingSlider != null)
                {
                    loadingSlider.value = 1f;
                }

                yield return new WaitForSeconds(0.2f);

                operation.allowSceneActivation = true;
                break;
            }

            yield return null;
        }
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    public Slider loadingSlider;
    public float minimumLoadingTime = 5f;

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

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        operation.allowSceneActivation = false;

        float timer = 0f;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (loadingSlider != null)
            {
                loadingSlider.value = progress;
            }

            if (operation.progress >= 0.9f && timer >= minimumLoadingTime)
            {
                loadingSlider.value = 1f;
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
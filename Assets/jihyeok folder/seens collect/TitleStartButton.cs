using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleStartButton : MonoBehaviour
{
    public string loadingSceneName = "Loading";

    [Header("Fade")]
    public Image blackPanel;
    public float fadeTime = 1f;

    private bool isChangingScene = false;

    public void StartGame()
    {
        if (isChangingScene) return;

        StartCoroutine(FadeOutAndLoadLoadingScene());
    }

    private IEnumerator FadeOutAndLoadLoadingScene()
    {
        isChangingScene = true;

        blackPanel.gameObject.SetActive(true);

        Color color = blackPanel.color;
        color.a = 0f;
        blackPanel.color = color;

        float time = 0f;

        while (time < fadeTime)
        {
            time += Time.deltaTime;

            float alpha = time / fadeTime;

            color = blackPanel.color;
            color.a = alpha;
            blackPanel.color = color;

            yield return null;
        }

        color = blackPanel.color;
        color.a = 1f;
        blackPanel.color = color;

        SceneManager.LoadScene(loadingSceneName);
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleStartButton : MonoBehaviour
{
    public string lobbySceneName = "lobby";

    [Header("Fade")]
    public Image blackPanel;
    public float fadeTime = 1f;

    private bool isChangingScene = false;

    public void StartGame()
    {
        if (isChangingScene) return;

        StartCoroutine(FadeOutAndLoadScene());
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        isChangingScene = true;

        blackPanel.gameObject.SetActive(true);

        float time = 0f;

        while (time < fadeTime)
        {
            time += Time.deltaTime;

            float alpha = time / fadeTime;

            Color color = blackPanel.color;
            color.a = alpha;
            blackPanel.color = color;

            yield return null;
        }

        SceneManager.LoadScene(lobbySceneName);
    }
}
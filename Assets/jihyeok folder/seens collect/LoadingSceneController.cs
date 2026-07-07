using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// RunFlowManager가 저장한 목적지 씬을 비동기로 로드한다.
/// 검은 화면(CanvasGroup)이 씬 전환 전/후로 페이드 인·아웃된다.
/// 씬 전환 시 파괴되지 않도록 DontDestroyOnLoad로 유지된 뒤,
/// 페이드 아웃이 끝나면 스스로 파괴된다.
/// Loading 씬에 하나만 배치한다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class LoadingSceneController : MonoBehaviour
{
    [Header("검은 화면")]
    [SerializeField] private CanvasGroup blackScreen;

    [Header("타이밍")]
    [Min(0f)] [SerializeField] private float fadeInDuration = 0.4f;
    [Min(0f)] [SerializeField] private float fadeOutDuration = 0.4f;
    [Min(0f)]
    [Tooltip("실제 씬 로딩이 끝나더라도 검은 화면을 유지할 최소 시간 (페이드 인/아웃 제외)")]
    [SerializeField] private float minimumDisplayTime = 1f;

    private void Awake()
    {
        // 씬 전환 이후에도 이 오브젝트(검은 화면)가 살아남아야
        // "다음 씬 위에서 걷히는" 페이드 아웃이 성립한다.
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        Time.timeScale = 1f;

        RunFlowManager manager = RunFlowManager.Instance;

        if (manager == null)
        {
            Debug.LogError("[LoadingSceneController] RunFlowManager를 찾을 수 없습니다.", this);
            Destroy(gameObject);
            yield break;
        }

        string targetSceneName = manager.GetPendingSceneName();

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("[LoadingSceneController] 로드할 목적지 씬이 설정되지 않았습니다.", this);
            Destroy(gameObject);
            yield break;
        }

        // 1) 완전한 검은 화면으로 페이드 인
        yield return Fade(0f, 1f, fadeInDuration);

        // 2) 목적지 씬 비동기 로드 (활성화는 보류)
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);

        if (operation == null)
        {
            Debug.LogError($"[LoadingSceneController] '{targetSceneName}' 씬 로드에 실패했습니다.", this);
            Destroy(gameObject);
            yield break;
        }

        operation.allowSceneActivation = false;

        float startedTime = Time.unscaledTime;

        while (true)
        {
            bool sceneLoadCompleted = operation.progress >= 0.9f;
            bool minimumTimeCompleted =
                Time.unscaledTime - startedTime >= minimumDisplayTime;

            if (sceneLoadCompleted && minimumTimeCompleted)
            {
                break;
            }

            yield return null;
        }

        // 3) 목적지 확정 후 씬 활성화 (화면은 여전히 검은 상태)
        manager.CommitPendingDestination();
        operation.allowSceneActivation = true;

        // 활성화 직후 한 프레임 대기 → 새 씬의 Awake/Start가
        // 최소한 한 번 돌아간 뒤 페이드 아웃을 시작한다.
        // (카메라·라이팅 초기화 전에 걷히면 초기 프레임이 깨져 보일 수 있음)
        yield return null;

        // 4) 새 씬 위에서 검은 화면을 걷어낸다
        yield return Fade(1f, 0f, fadeOutDuration);

        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (blackScreen == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            blackScreen.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            blackScreen.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        blackScreen.alpha = to;
    }
}
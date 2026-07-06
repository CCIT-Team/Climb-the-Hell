using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 타이틀 시작 버튼.
/// 마우스를 올리면 투명도가 반복해서 변하고,
/// 클릭하면 Title -> Loading -> Lobby 순서로 이동한다.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class TitleStartButton :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("화면 전환")]
    [SerializeField] private Image blackPanel;

    [Min(0f)]
    [SerializeField] private float fadeTime = 0.7f;

    [Header("마우스 반짝임")]
    [Range(0f, 1f)]
    [SerializeField] private float minimumAlpha = 0.55f;

    [Range(0f, 1f)]
    [SerializeField] private float maximumAlpha = 1f;

    [Min(0.1f)]
    [SerializeField] private float blinkSpeed = 3f;

    private Button button;
    private CanvasGroup canvasGroup;

    private bool isPointerOver;
    private bool isChangingScene;

    private void Awake()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        if (!isPointerOver || isChangingScene)
        {
            return;
        }

        /*
         * Sin 값을 이용해 투명도를
         * minimumAlpha와 maximumAlpha 사이에서 반복한다.
         */
        float wave =
            (Mathf.Sin(
                Time.unscaledTime * blinkSpeed
            ) + 1f) * 0.5f;

        canvasGroup.alpha =
            Mathf.Lerp(
                minimumAlpha,
                maximumAlpha,
                wave
            );
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (isChangingScene)
        {
            return;
        }

        isPointerOver = true;
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        isPointerOver = false;
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Button의 OnClick에 연결한다.
    /// </summary>
    public void StartGame()
    {
        if (isChangingScene)
        {
            return;
        }

        StartCoroutine(StartRoutine());
    }

    private IEnumerator StartRoutine()
    {
        isChangingScene = true;
        isPointerOver = false;

        canvasGroup.alpha = 1f;

        if (button != null)
        {
            button.interactable = false;
        }

        if (blackPanel != null)
        {
            blackPanel.gameObject.SetActive(true);

            Color color = blackPanel.color;
            color.a = 0f;
            blackPanel.color = color;

            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;

                color.a =
                    fadeTime <= 0f
                        ? 1f
                        : Mathf.Clamp01(
                            elapsed / fadeTime
                        );

                blackPanel.color = color;

                yield return null;
            }

            color.a = 1f;
            blackPanel.color = color;
        }

        RunFlowManager manager =
            RunFlowManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[TitleStartButton] " +
                "RunFlowManager가 없습니다.",
                this
            );

            RestoreButton();
            yield break;
        }

        bool requested =
            manager.GoToLobby();

        if (!requested)
        {
            RestoreButton();
        }
    }

    private void RestoreButton()
    {
        isChangingScene = false;

        canvasGroup.alpha = 1f;

        if (button != null)
        {
            button.interactable = true;
        }

        if (blackPanel != null)
        {
            Color color = blackPanel.color;
            color.a = 0f;
            blackPanel.color = color;

            blackPanel.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        isPointerOver = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }
}
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 보상 선택 화면의 목록바 하나를 관리한다.
/// 득도 표시, 마우스 오버 투명도 반복 효과,
/// 클릭 선택 전달을 담당한다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BoonRewardSlot : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("보상 표시")]

    [Tooltip("BoonData의 아이콘을 표시할 Image")]
    [SerializeField]
    private Image rewardImage;

    [Tooltip("득도 이름을 표시할 TMP Text")]
    [SerializeField]
    private TextMeshProUGUI titleText;

    [Tooltip("득도 설명. 설명 UI가 없다면 비워도 됨")]
    [SerializeField]
    private TextMeshProUGUI descriptionText;

    [Header("마우스 오버 투명도 효과")]

    [Range(0.5f, 1f)]
    [Tooltip("마우스를 올렸을 때 가장 흐려지는 투명도")]
    [SerializeField]
    private float hoverMinAlpha = 0.82f;

    [Range(0.5f, 1f)]
    [Tooltip("마우스를 올렸을 때 가장 선명해지는 투명도")]
    [SerializeField]
    private float hoverMaxAlpha = 1f;

    [Min(0.1f)]
    [Tooltip("투명도가 한 번 왕복하는 속도")]
    [SerializeField]
    private float hoverPulseSpeed = 2.5f;

    [Range(0.5f, 1f)]
    [Tooltip("마우스가 없을 때 기본 투명도")]
    [SerializeField]
    private float normalAlpha = 0.95f;

    private CanvasGroup canvasGroup;

    // 현재 슬롯에 표시된 득도
    private BoonData currentBoon;

    /*
     * 클릭하면 선택된 슬롯과 득도 데이터를
     * BoonRewardUI로 전달한다.
     */
    private Action<BoonRewardSlot, BoonData>
        selectedCallback;

    private Coroutine hoverRoutine;

    private bool selectable;
    private bool pointerInside;

    private void Awake()
    {
        InitializeReferences();

        SetAlpha(normalAlpha);
    }

    /// <summary>
    /// CanvasGroup 참조를 확보한다.
    /// </summary>
    private bool InitializeReferences()
    {
        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            Debug.LogError(
                "[BoonRewardSlot] " +
                "슬롯 루트에 CanvasGroup이 없습니다.",
                this
            );

            return false;
        }

        return true;
    }

    /// <summary>
    /// 슬롯에 득도 데이터를 표시한다.
    /// </summary>
    public void Setup(
        BoonData boon,
        Action<BoonRewardSlot, BoonData> onSelected
    )
    {
        if (!InitializeReferences())
        {
            return;
        }

        StopHoverRoutine();

        currentBoon = boon;
        selectedCallback = onSelected;

        selectable =
            boon != null;

        pointerInside = false;

        gameObject.SetActive(selectable);

        if (!selectable)
        {
            return;
        }

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        SetAlpha(normalAlpha);

        if (rewardImage != null)
        {
            rewardImage.sprite =
                boon.icon;

            rewardImage.enabled =
                boon.icon != null;
        }
        else
        {
            Debug.LogError(
                "[BoonRewardSlot] " +
                "Reward Image가 연결되지 않았습니다.",
                this
            );
        }

        if (titleText != null)
        {
            titleText.text =
                boon.GetRewardTitle();
        }

        if (descriptionText != null)
        {
            descriptionText.text =
                boon.GetRewardDescription();
        }
    }

    /// <summary>
    /// 선택되지 않은 슬롯을 즉시 숨긴다.
    /// </summary>
    public void HideImmediately()
    {
        selectable = false;
        pointerInside = false;

        StopHoverRoutine();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 선택된 슬롯을 선명한 상태로 유지한다.
    /// </summary>
    public void ShowSelectedEffect()
    {
        selectable = false;
        pointerInside = false;

        StopHoverRoutine();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        SetAlpha(1f);
    }

    /// <summary>
    /// 슬롯을 초기화하고 숨긴다.
    /// </summary>
    public void Clear()
    {
        selectable = false;
        pointerInside = false;

        currentBoon = null;
        selectedCallback = null;

        StopHoverRoutine();

        if (rewardImage != null)
        {
            rewardImage.sprite = null;
            rewardImage.enabled = false;
        }

        if (titleText != null)
        {
            titleText.text =
                string.Empty;
        }

        if (descriptionText != null)
        {
            descriptionText.text =
                string.Empty;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        SetAlpha(normalAlpha);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 마우스가 슬롯 위에 들어왔을 때 호출된다.
    /// </summary>
    public void OnPointerEnter(
        PointerEventData eventData
    )
    {
        if (!selectable)
        {
            return;
        }

        pointerInside = true;

        StartHoverRoutine();
    }

    /// <summary>
    /// 마우스가 슬롯에서 나갔을 때 호출된다.
    /// </summary>
    public void OnPointerExit(
        PointerEventData eventData
    )
    {
        pointerInside = false;

        StopHoverRoutine();

        if (selectable)
        {
            SetAlpha(normalAlpha);
        }
    }

    /// <summary>
    /// 슬롯을 클릭했을 때 선택 정보를 UI에 전달한다.
    /// </summary>
    public void OnPointerClick(
        PointerEventData eventData
    )
    {
        if (!selectable ||
            currentBoon == null)
        {
            return;
        }

        selectable = false;
        pointerInside = false;

        StopHoverRoutine();

        SetAlpha(1f);

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        selectedCallback?.Invoke(
            this,
            currentBoon
        );
    }

    /// <summary>
    /// 마우스 오버 반복 효과를 시작한다.
    /// </summary>
    private void StartHoverRoutine()
    {
        StopHoverRoutine();

        hoverRoutine =
            StartCoroutine(
                HoverPulseRoutine()
            );
    }

    /// <summary>
    /// 0.82~1.0 사이에서 투명도를 반복 변경한다.
    /// </summary>
    private IEnumerator HoverPulseRoutine()
    {
        float elapsed = 0f;

        while (pointerInside &&
               selectable)
        {
            elapsed +=
                Time.unscaledDeltaTime *
                hoverPulseSpeed;

            /*
             * Sin 값은 -1~1로 움직이므로
             * 0~1 범위로 변환한다.
             */
            float normalized =
                (Mathf.Sin(elapsed) + 1f) *
                0.5f;

            float alpha =
                Mathf.Lerp(
                    hoverMinAlpha,
                    hoverMaxAlpha,
                    normalized
                );

            SetAlpha(alpha);

            yield return null;
        }

        hoverRoutine = null;
    }

    /// <summary>
    /// 실행 중인 마우스 오버 코루틴을 중단한다.
    /// </summary>
    private void StopHoverRoutine()
    {
        if (hoverRoutine == null)
        {
            return;
        }

        StopCoroutine(hoverRoutine);

        hoverRoutine = null;
    }

    private void SetAlpha(
        float alpha
    )
    {
        if (!InitializeReferences())
        {
            return;
        }

        canvasGroup.alpha =
            Mathf.Clamp01(alpha);
    }

    private void OnDisable()
    {
        pointerInside = false;

        StopHoverRoutine();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        hoverMinAlpha =
            Mathf.Clamp(
                hoverMinAlpha,
                0.5f,
                1f
            );

        hoverMaxAlpha =
            Mathf.Clamp(
                hoverMaxAlpha,
                hoverMinAlpha,
                1f
            );

        normalAlpha =
            Mathf.Clamp(
                normalAlpha,
                hoverMinAlpha,
                1f
            );

        hoverPulseSpeed =
            Mathf.Max(
                0.1f,
                hoverPulseSpeed
            );
    }
#endif
}
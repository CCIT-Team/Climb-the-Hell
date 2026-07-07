using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 일반 득도와 작두점이 함께 사용하는 3개 선택 UI.
///
/// 기존 Open 함수는 그대로 유지하고,
/// 작두점용 제목과 취소 불가 옵션을 받는 오버로드를 추가한다.
/// </summary>
public class BoonRewardUI : MonoBehaviour
{
    private static BoonRewardUI instance;

    private Canvas canvas;
    private GameObject panel;
    private GameObject closeButtonObject;

    private TextMeshProUGUI rewardTitleText;

    private readonly BoonRewardSlot[]
        slots =
            new BoonRewardSlot[3];

    private Action<BoonData>
        selectedCallback;

    private Action
        cancelledCallback;

    private bool isOpen;
    private bool isBuilt;
    private bool currentAllowCancel = true;

    private float previousTimeScale = 1f;
    private bool previousCursorVisible;

    private CursorLockMode
        previousCursorLockMode;

    public static BoonRewardUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance =
                    FindFirstObjectByType<
                        BoonRewardUI
                    >();
            }

            if (instance == null)
            {
                GameObject root =
                    new GameObject(
                        "RuntimeBoonRewardUI"
                    );

                instance =
                    root.AddComponent<
                        BoonRewardUI
                    >();
            }

            return instance;
        }
    }

    public bool IsOpen =>
        isOpen;

    private void Awake()
    {
        if (instance != null &&
            instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        BuildUI();
        EnsureEventSystem();
    }

    /// <summary>
    /// 기존 일반 득도 호출과 호환되는 함수.
    /// </summary>
    public bool Open(
        IReadOnlyList<BoonData> choices,
        Action<BoonData> onSelected,
        Action onCancelled)
    {
        return Open(
            choices,
            onSelected,
            onCancelled,
            "득도 선택",
            true
        );
    }

    /// <summary>
    /// 작두점에서 제목 변경과 취소 금지를 사용할 수 있는 함수.
    /// </summary>
    public bool Open(
        IReadOnlyList<BoonData> choices,
        Action<BoonData> onSelected,
        Action onCancelled,
        string windowTitle,
        bool allowCancel)
    {
        if (isOpen ||
            choices == null ||
            choices.Count == 0)
        {
            return false;
        }

        if (!isBuilt)
        {
            BuildUI();
        }

        selectedCallback =
            onSelected;

        cancelledCallback =
            onCancelled;

        currentAllowCancel =
            allowCancel;

        rewardTitleText.text =
            string.IsNullOrWhiteSpace(
                windowTitle
            )
                ? "득도 선택"
                : windowTitle;

        closeButtonObject.SetActive(
            currentAllowCancel
        );

        int visibleCount =
            Mathf.Min(
                choices.Count,
                slots.Length
            );

        for (int i = 0;
             i < slots.Length;
             i++)
        {
            if (i < visibleCount)
            {
                slots[i].Setup(
                    choices[i],
                    HandleSelected
                );
            }
            else
            {
                slots[i].Clear();
            }
        }

        previousTimeScale =
            Time.timeScale;

        previousCursorVisible =
            Cursor.visible;

        previousCursorLockMode =
            Cursor.lockState;

        panel.SetActive(true);
        isOpen = true;

        Time.timeScale = 0f;

        Cursor.visible = true;

        Cursor.lockState =
            CursorLockMode.None;

        return true;
    }

    public void Cancel()
    {
        if (!isOpen ||
            !currentAllowCancel)
        {
            return;
        }

        Action callback =
            cancelledCallback;

        CloseInternal();

        callback?.Invoke();
    }

    /// <summary>
    /// 예외 상황에서만 외부가 강제로 닫을 때 사용한다.
    /// 취소 콜백은 호출하지 않는다.
    /// </summary>
    public void ForceClose()
    {
        if (!isOpen)
        {
            return;
        }

        CloseInternal();
    }

    private void HandleSelected(
        BoonData boon)
    {
        if (!isOpen ||
            boon == null)
        {
            return;
        }

        Action<BoonData> callback =
            selectedCallback;

        CloseInternal();

        callback?.Invoke(boon);
    }

    private void CloseInternal()
    {
        isOpen = false;

        Time.timeScale =
            previousTimeScale;

        Cursor.visible =
            previousCursorVisible;

        Cursor.lockState =
            previousCursorLockMode;

        for (int i = 0;
             i < slots.Length;
             i++)
        {
            slots[i]?.Clear();
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }

        selectedCallback = null;
        cancelledCallback = null;
        currentAllowCancel = true;
    }

    private void BuildUI()
    {
        if (isBuilt)
        {
            return;
        }

        canvas =
            GetComponent<Canvas>();

        if (canvas == null)
        {
            canvas =
                gameObject.AddComponent<
                    Canvas
                >();
        }

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder = 5000;

        CanvasScaler scaler =
            GetComponent<CanvasScaler>();

        if (scaler == null)
        {
            scaler =
                gameObject.AddComponent<
                    CanvasScaler
                >();
        }

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode
                .ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        scaler.matchWidthOrHeight =
            0.5f;

        if (GetComponent<
                GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<
                GraphicRaycaster
            >();
        }

        CreatePanel();
        CreateTitle();
        CreateSlots();
        CreateCloseButton();

        panel.SetActive(false);

        isBuilt = true;
    }

    private void CreatePanel()
    {
        panel =
            new GameObject(
                "BoonRewardPanel",
                typeof(RectTransform)
            );

        RectTransform panelRect =
            panel.GetComponent<
                RectTransform
            >();

        panelRect.SetParent(
            transform,
            false
        );

        panelRect.anchorMin =
            Vector2.zero;

        panelRect.anchorMax =
            Vector2.one;

        panelRect.offsetMin =
            Vector2.zero;

        panelRect.offsetMax =
            Vector2.zero;

        Image dim =
            panel.AddComponent<Image>();

        dim.color =
            new Color(
                0f,
                0f,
                0f,
                0.8f
            );
    }

    private void CreateTitle()
    {
        RectTransform panelRect =
            panel.GetComponent<
                RectTransform
            >();

        rewardTitleText =
            CreateText(
                "RewardTitle",
                panelRect,
                "득도 선택",
                48f,
                FontStyles.Bold,
                TextAlignmentOptions.Center
            );

        RectTransform titleRect =
            rewardTitleText
                .rectTransform;

        titleRect.anchorMin =
            new Vector2(0.2f, 1f);

        titleRect.anchorMax =
            new Vector2(0.8f, 1f);

        titleRect.pivot =
            new Vector2(0.5f, 1f);

        titleRect.sizeDelta =
            new Vector2(0f, 80f);

        titleRect.anchoredPosition =
            new Vector2(0f, -38f);
    }

    private void CreateSlots()
    {
        RectTransform panelRect =
            panel.GetComponent<
                RectTransform
            >();

        for (int i = 0;
             i < slots.Length;
             i++)
        {
            GameObject slotObject =
                new GameObject(
                    $"BoonSlot_{i + 1}",
                    typeof(RectTransform)
                );

            BoonRewardSlot slot =
                slotObject.AddComponent<
                    BoonRewardSlot
                >();

            slot.Build(
                panelRect,
                i
            );

            slots[i] = slot;
        }
    }

    private void CreateCloseButton()
    {
        RectTransform panelRect =
            panel.GetComponent<
                RectTransform
            >();

        closeButtonObject =
            new GameObject(
                "CloseButton",
                typeof(RectTransform)
            );

        RectTransform closeRect =
            closeButtonObject
                .GetComponent<
                    RectTransform
                >();

        closeRect.SetParent(
            panelRect,
            false
        );

        closeRect.anchorMin =
            new Vector2(0.5f, 0f);

        closeRect.anchorMax =
            new Vector2(0.5f, 0f);

        closeRect.pivot =
            new Vector2(0.5f, 0f);

        closeRect.sizeDelta =
            new Vector2(220f, 60f);

        closeRect.anchoredPosition =
            new Vector2(0f, 30f);

        Image closeImage =
            closeButtonObject
                .AddComponent<Image>();

        closeImage.color =
            new Color(
                0.18f,
                0.18f,
                0.2f,
                1f
            );

        Button closeButton =
            closeButtonObject
                .AddComponent<Button>();

        closeButton.targetGraphic =
            closeImage;

        closeButton.onClick.AddListener(
            Cancel
        );

        TextMeshProUGUI closeText =
            CreateText(
                "Text",
                closeRect,
                "닫기",
                26f,
                FontStyles.Bold,
                TextAlignmentOptions.Center
            );

        RectTransform textRect =
            closeText.rectTransform;

        textRect.anchorMin =
            Vector2.zero;

        textRect.anchorMax =
            Vector2.one;

        textRect.offsetMin =
            Vector2.zero;

        textRect.offsetMax =
            Vector2.zero;
    }

    private static TextMeshProUGUI
        CreateText(
            string objectName,
            RectTransform parent,
            string content,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
    {
        GameObject textObject =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        RectTransform rect =
            textObject.GetComponent<
                RectTransform
            >();

        rect.SetParent(
            parent,
            false
        );

        TextMeshProUGUI text =
            textObject.AddComponent<
                TextMeshProUGUI
            >();

        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<
                EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject =
            new GameObject(
                "EventSystem"
            );

        eventSystemObject.AddComponent<
            EventSystem
        >();

        eventSystemObject.AddComponent<
            InputSystemUIInputModule
        >();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (isOpen)
        {
            Time.timeScale =
                previousTimeScale;

            Cursor.visible =
                previousCursorVisible;

            Cursor.lockState =
                previousCursorLockMode;
        }
    }
}

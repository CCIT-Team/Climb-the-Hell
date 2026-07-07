using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 일반 득도와 작두점이 함께 사용하는 보상 카드.
/// </summary>
public class BoonRewardSlot :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private static readonly Color
        NormalBackgroundColor =
            new Color(
                0.08f,
                0.08f,
                0.1f,
                0.96f
            );

    private static readonly Color
        JakduBackgroundColor =
            new Color(
                0.12f,
                0.045f,
                0.055f,
                0.98f
            );

    private Image background;
    private Image iconImage;
    private Image highlight;

    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descriptionText;

    private Button button;

    private BoonData currentBoon;
    private Action<BoonData> onSelected;

    private bool selectable;

    public void Build(
        RectTransform parent,
        int index)
    {
        RectTransform rect =
            GetComponent<RectTransform>();

        if (rect == null)
        {
            Debug.LogError(
                "[BoonRewardSlot] RectTransform이 필요합니다.",
                this
            );

            return;
        }

        rect.SetParent(
            parent,
            false
        );

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.sizeDelta =
            new Vector2(360f, 540f);

        rect.anchoredPosition =
            new Vector2(
                (index - 1) * 400f,
                -5f
            );

        background =
            gameObject.AddComponent<Image>();

        background.color =
            NormalBackgroundColor;

        button =
            gameObject.AddComponent<Button>();

        button.targetGraphic =
            background;

        button.onClick.AddListener(
            HandleClick
        );

        CreateHighlight(rect);
        CreateIcon(rect);
        CreateTexts(rect);

        gameObject.SetActive(false);
    }

    private void CreateHighlight(
        RectTransform parent)
    {
        RectTransform highlightRect =
            CreateUIObject(
                "Highlight",
                parent
            );

        highlightRect.anchorMin =
            Vector2.zero;

        highlightRect.anchorMax =
            Vector2.one;

        highlightRect.offsetMin =
            new Vector2(-8f, -8f);

        highlightRect.offsetMax =
            new Vector2(8f, 8f);

        highlight =
            highlightRect.gameObject
                .AddComponent<Image>();

        highlight.color =
            new Color(
                1f,
                0.78f,
                0.2f,
                0.35f
            );

        highlight.raycastTarget = false;

        highlight.gameObject
            .SetActive(false);
    }

    private void CreateIcon(
        RectTransform parent)
    {
        RectTransform iconRect =
            CreateUIObject(
                "Icon",
                parent
            );

        iconRect.anchorMin =
            new Vector2(0.5f, 1f);

        iconRect.anchorMax =
            new Vector2(0.5f, 1f);

        iconRect.pivot =
            new Vector2(0.5f, 1f);

        iconRect.sizeDelta =
            new Vector2(200f, 200f);

        iconRect.anchoredPosition =
            new Vector2(0f, -28f);

        iconImage =
            iconRect.gameObject
                .AddComponent<Image>();

        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
    }

    private void CreateTexts(
        RectTransform parent)
    {
        titleText =
            CreateText(
                "Title",
                parent,
                29f,
                FontStyles.Bold,
                TextAlignmentOptions.Center
            );

        RectTransform titleRect =
            titleText.rectTransform;

        titleRect.anchorMin =
            new Vector2(0.05f, 1f);

        titleRect.anchorMax =
            new Vector2(0.95f, 1f);

        titleRect.pivot =
            new Vector2(0.5f, 1f);

        titleRect.sizeDelta =
            new Vector2(0f, 58f);

        titleRect.anchoredPosition =
            new Vector2(0f, -238f);

        descriptionText =
            CreateText(
                "Description",
                parent,
                20f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft
            );

        RectTransform descriptionRect =
            descriptionText.rectTransform;

        descriptionRect.anchorMin =
            new Vector2(0.075f, 0.055f);

        descriptionRect.anchorMax =
            new Vector2(0.925f, 0.44f);

        descriptionRect.offsetMin =
            Vector2.zero;

        descriptionRect.offsetMax =
            Vector2.zero;

        descriptionText.enableWordWrapping =
            true;

        descriptionText.richText = true;
    }

    public void Setup(
        BoonData boon,
        Action<BoonData> selectedCallback)
    {
        currentBoon = boon;

        onSelected =
            selectedCallback;

        selectable =
            boon != null;

        gameObject.SetActive(
            selectable
        );

        if (!selectable)
        {
            return;
        }

        iconImage.sprite =
            boon.icon;

        iconImage.enabled =
            boon.icon != null;

        titleText.text =
            boon.GetRewardTitle();

        descriptionText.text =
            boon.GetContextRewardDescription();

        background.color =
            boon.IsJakduPoint
                ? JakduBackgroundColor
                : NormalBackgroundColor;

        button.interactable = true;

        highlight.gameObject
            .SetActive(false);
    }

    public void Clear()
    {
        currentBoon = null;
        onSelected = null;
        selectable = false;

        if (button != null)
        {
            button.interactable = false;
        }

        if (highlight != null)
        {
            highlight.gameObject
                .SetActive(false);
        }

        gameObject.SetActive(false);
    }

    private void HandleClick()
    {
        if (!selectable ||
            currentBoon == null)
        {
            return;
        }

        selectable = false;
        button.interactable = false;

        onSelected?.Invoke(
            currentBoon
        );
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (selectable &&
            highlight != null)
        {
            highlight.gameObject
                .SetActive(true);
        }
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        if (highlight != null)
        {
            highlight.gameObject
                .SetActive(false);
        }
    }

    private static RectTransform
        CreateUIObject(
            string objectName,
            RectTransform parent)
    {
        GameObject child =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        RectTransform rect =
            child.GetComponent<
                RectTransform
            >();

        rect.SetParent(
            parent,
            false
        );

        return rect;
    }

    private static TextMeshProUGUI
        CreateText(
            string objectName,
            RectTransform parent,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
    {
        RectTransform rect =
            CreateUIObject(
                objectName,
                parent
            );

        TextMeshProUGUI text =
            rect.gameObject
                .AddComponent<
                    TextMeshProUGUI
                >();

        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }
}

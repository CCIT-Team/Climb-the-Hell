using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TraitRow : MonoBehaviour
{
    [Header("Trait Data")]
    [SerializeField]
    private TraitData trait;

    [Header("UI")]
    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text levelText;

    [SerializeField]
    private TMP_Text priceText;

    [SerializeField]
    [FormerlySerializedAs("effectText")]
    private TMP_Text stateText;

    [SerializeField]
    [FormerlySerializedAs("upgradeButton")]
    private Button buyButton;

    private TraitManager traitManager;
    private bool buttonBound;

    private void Awake()
    {
        CacheUI();
        BindButton();
    }

    public void Setup(
        TraitData newTrait,
        TraitManager manager
    )
    {
        trait = newTrait;
        traitManager = manager;

        CacheUI();
        BindButton();
        Refresh();
    }

    public void SetManager(
        TraitManager manager
    )
    {
        traitManager = manager;

        CacheUI();
        BindButton();
    }

    public void Refresh()
    {
        CacheUI();

        if (traitManager == null)
        {
            ResolveManager();
        }

        if (trait == null)
        {
            SetText(nameText, "No Trait");
            SetText(levelText, "-");
            SetText(priceText, "-");
            SetText(stateText, "No Data");

            if (buyButton != null)
            {
                buyButton.interactable = false;
            }

            return;
        }

        int level = 0;
        int maxLevel =
            Mathf.Max(0, trait.maxLevel);

        if (traitManager != null)
        {
            level =
                traitManager.GetTraitLevel(trait);
        }

        bool isMaxLevel =
            level >= maxLevel;

        SetText(
            nameText,
            trait.type.ToString()
        );

        SetText(
            levelText,
            $"{level} / {maxLevel}"
        );

        if (isMaxLevel)
        {
            SetText(priceText, "MAX");
            SetText(stateText, "Max Level");

            if (buyButton != null)
            {
                buyButton.interactable = false;
            }

            return;
        }

        int price = 0;
        bool canBuy = false;

        if (traitManager != null)
        {
            price =
                traitManager.GetPrice(
                    trait,
                    level
                );

            canBuy =
                traitManager.CanBuyTrait(trait);
        }

        SetText(
            priceText,
            $"{price} Petals"
        );

        SetText(
            stateText,
            canBuy ? "Available" : "Need Petals"
        );

        if (buyButton != null)
        {
            buyButton.interactable = canBuy;
        }
    }

    private void Buy()
    {
        if (traitManager == null)
        {
            ResolveManager();
        }

        if (traitManager == null ||
            trait == null)
        {
            return;
        }

        bool bought =
            traitManager.BuyTrait(trait);

        if (bought)
        {
            Refresh();
        }
    }

    private void ResolveManager()
    {
        if (GameManager.Instance != null)
        {
            traitManager =
                GameManager.Instance.traitManager;
        }

        if (traitManager == null)
        {
            traitManager =
                FindObjectOfType<TraitManager>(true);
        }
    }

    private void CacheUI()
    {
        if (buyButton == null)
        {
            buyButton =
                GetComponentInChildren<Button>(true);
        }

        TMP_Text[] texts =
            GetComponentsInChildren<TMP_Text>(true);

        if (nameText == null &&
            texts.Length > 0)
        {
            nameText = texts[0];
        }

        if (levelText == null &&
            texts.Length > 1)
        {
            levelText = texts[1];
        }

        if (priceText == null &&
            texts.Length > 2)
        {
            priceText = texts[2];
        }

        if (stateText == null &&
            texts.Length > 3)
        {
            stateText = texts[3];
        }
    }

    private void BindButton()
    {
        if (buyButton == null ||
            buttonBound)
        {
            return;
        }

        buyButton.onClick.RemoveListener(Buy);
        buyButton.onClick.AddListener(Buy);

        buttonBound = true;
    }

    private void SetText(
        TMP_Text target,
        string value
    )
    {
        if (target == null)
        {
            return;
        }

        target.text = value;
    }
}

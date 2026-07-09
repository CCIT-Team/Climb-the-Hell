using TMPro;
using UnityEngine;

public class TraitUI : UIBase
{
    [Header("References")]
    [SerializeField]
    private TraitManager traitManager;

    [SerializeField]
    private TraitPanel traitPanel;

    [SerializeField]
    private TMP_Text currencyText;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Init(
        TraitManager manager
    )
    {
        traitManager = manager;

        ResolveReferences();

        if (traitPanel != null)
        {
            traitPanel.Init(traitManager);
        }
    }

    public void RefreshUI()
    {
        ResolveReferences();

        if (traitManager != null)
        {
            traitManager.EnsureReferences();
        }

        if (traitPanel == null)
        {
            Debug.LogWarning(
                "[TraitUI] TraitPanel is missing, so the UI refresh was skipped.",
                this
            );
            return;
        }

        RefreshCurrencyText();

        traitPanel.Init(traitManager);
        traitPanel.Refresh();
    }

    private void RefreshCurrencyText()
    {
        if (currencyText == null)
        {
            return;
        }

        int flowerLeaf =
            traitManager != null
                ? traitManager.GetTraitCurrency()
                : 0;

        currencyText.text =
            $"{flowerLeaf} Petals";
    }

    private void ResolveReferences()
    {
        if (traitManager == null &&
            GameManager.Instance != null)
        {
            traitManager =
                GameManager.Instance.traitManager;
        }

        if (traitManager == null)
        {
            traitManager =
                FindObjectOfType<TraitManager>(true);
        }

        if (traitPanel == null)
        {
            traitPanel =
                GetComponentInChildren<TraitPanel>(true);
        }

        if (traitPanel == null)
        {
            traitPanel =
                FindObjectOfType<TraitPanel>(true);
        }

        if (currencyText == null)
        {
            TMP_Text[] texts =
                GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0;
                 i < texts.Length;
                 i++)
            {
                if (texts[i] != null &&
                    texts[i].name == "CurrencyText")
                {
                    currencyText = texts[i];
                    break;
                }
            }
        }
    }
}

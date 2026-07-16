using UnityEngine;

// 특성 UI 최상위 클래스
public class TraitUI : UIBase
{
    [Header("참조")]
    [SerializeField]
    private TraitManager traitManager;

    [SerializeField]
    private TraitPanel traitPanel;

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
                "[TraitUI] TraitPanel이 없어 UI 갱신을 건너뜁니다.",
                this
            );
            return;
        }

        traitPanel.Init(traitManager);
        traitPanel.Refresh();
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
    }
}
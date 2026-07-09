using UnityEngine;

// 특성 Row 여러 개를 관리하는 패널
public class TraitPanel : UIBase
{
    [Header("참조")]
    [SerializeField]
    private TraitManager traitManager;

    [Header("Row 목록")]
    [SerializeField]
    private TraitRow[] rows;

    private void Awake()
    {
        CacheRows();
    }

    public void Init(
        TraitManager manager
    )
    {
        traitManager = manager;

        ResolveManager();
        CacheRows();
        SetupRows();
    }

    public void Refresh()
    {
        ResolveManager();
        CacheRows();
        SetupRows();

        if (rows == null)
        {
            return;
        }

        for (int i = 0; i < rows.Length; i++)
        {
            TraitRow row = rows[i];

            if (row == null)
            {
                continue;
            }

            row.Refresh();
        }
    }

    private void ResolveManager()
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

        if (traitManager != null)
        {
            traitManager.EnsureReferences();
        }
    }

    private void CacheRows()
    {
        if (rows == null ||
            rows.Length == 0)
        {
            rows =
                GetComponentsInChildren<TraitRow>(true);
        }
    }

    private void SetupRows()
    {
        if (traitManager == null ||
            rows == null)
        {
            return;
        }

        for (int i = 0; i < rows.Length; i++)
        {
            TraitRow row = rows[i];

            if (row == null)
            {
                continue;
            }

            row.SetManager(traitManager);
        }
    }
}
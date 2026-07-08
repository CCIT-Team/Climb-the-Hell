using UnityEngine;

// 허브 씬 초기화 매니저
public class HubManager : MonoBehaviour
{
    [Header("특성 시스템")]
    public TraitManager traitManager;
    public TraitUI traitUI;

    [Header("시작 설정")]
    [SerializeField]
    private bool initOnStart = true;

    private bool initialized;

    private void Start()
    {
        if (initOnStart)
        {
            Init();
        }
    }

    public void Init()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        ResolveReferences();

        if (traitManager == null)
        {
            Debug.LogError(
                "[HubManager] TraitManager가 없습니다. Hub 씬 또는 GameManager에 연결하세요.",
                this
            );
            return;
        }

        traitManager.EnsureReferences();
        traitManager.LoadTraits();

        if (traitUI == null)
        {
            Debug.LogWarning(
                "[HubManager] TraitUI가 없습니다. 특성 UI 갱신을 건너뜁니다.",
                this
            );
            return;
        }

        traitUI.Init(traitManager);
        traitUI.RefreshUI();
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

        if (traitUI == null)
        {
            traitUI =
                FindObjectOfType<TraitUI>(true);
        }
    }
}
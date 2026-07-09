using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체를 관리하는 중앙 매니저.
///
/// 역할:
/// - 저장/불러오기
/// - 영구 재화
/// - TraitManager / RunManager / UIManager / HubManager / ResultUI 참조 갱신
///
/// 주의:
/// - Player 생성, Player 씬 이동, PlayerSpawnPoint 배치는 PlayerSceneMover가 담당한다.
/// - GameManager는 Player를 직접 이동시키지 않는다.
/// - 기존 코드 호환을 위해 Player 이동 관련 래퍼 함수만 남겨둔다.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("기존 매니저")]
    public SaveManager saveManager;
    public TraitManager traitManager;
    public RunManager runManager;
    public UIManager uiManager;
    public HubManager hubManager;
    public ResultUI resultUI;

    [Header("영구 재화")]
    public MoneyData permanentMoney =
        new MoneyData();

    [Header("테스트 재화")]
    [Tooltip(
        "체크하면 게임 시작 후 영구 재화를 Test Money Amount로 설정합니다.\n" +
        "실제 빌드에서는 체크를 해제하세요."
    )]
    [SerializeField]
    private bool useTestMoney = true;

    [Min(0)]
    [SerializeField]
    private int testMoneyAmount = 1000000;

    [Header("디버그")]
    [SerializeField]
    private bool showLogs = true;

    public Player CurrentPlayer
    {
        get
        {
            return PlayerSceneMover.Instance != null
                ? PlayerSceneMover.Instance.CurrentPlayer
                : null;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        RefreshSceneReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        RefreshSceneReferences();

        if (saveManager != null)
        {
            saveManager.Load();
        }
        else
        {
            Debug.LogWarning(
                "[GameManager] SaveManager가 연결되지 않았습니다.",
                this
            );
        }

        if (useTestMoney)
        {
            permanentMoney.SetMoney(
                testMoneyAmount
            );
        }

        RefreshSceneReferences();
    }

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode
    )
    {
        RefreshSceneReferences();

        if (showLogs)
        {
            Debug.Log(
                $"[GameManager] 씬 참조 갱신 완료 / Scene={scene.name}",
                this
            );
        }
    }

    public void AddPermanentMoney(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        permanentMoney.AddMoney(amount);
    }

    public bool TrySpendPermanentMoney(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        return permanentMoney.TrySpend(amount);
    }

    public void SaveGame()
    {
        if (traitManager != null)
        {
            traitManager.SaveTraits();
        }
        else
        {
            Debug.LogWarning(
                "[GameManager] TraitManager가 연결되지 않았습니다.",
                this
            );
        }

        if (saveManager == null)
        {
            Debug.LogError(
                "[GameManager] SaveManager가 없어 저장할 수 없습니다.",
                this
            );
            return;
        }

        if (saveManager.saveData == null)
        {
            Debug.LogError(
                "[GameManager] SaveData가 생성되지 않았습니다.",
                this
            );
            return;
        }

        Player currentPlayer =
            CurrentPlayer;

        saveManager.saveData.permanentMoney =
            currentPlayer != null
                ? currentPlayer.FlowerLeaf
                : permanentMoney.CurrentMoney;

        saveManager.Save();
    }

    /// <summary>
    /// 기존 코드 호환용.
    /// 실제 등록은 PlayerSceneMover가 담당한다.
    /// </summary>
    public void RegisterPlayer(Player newPlayer)
    {
        if (newPlayer == null)
        {
            return;
        }

        if (PlayerSceneMover.Instance != null)
        {
            PlayerSceneMover.Instance.RegisterPlayer(newPlayer);
        }

        RefreshSceneReferences();
    }

    /// <summary>
    /// 기존 코드 호환용.
    /// 실제 Player 보호는 PlayerSceneMover가 담당한다.
    /// </summary>
    public void PreparePlayerForSceneTransition()
    {
        if (PlayerSceneMover.Instance != null)
        {
            PlayerSceneMover.Instance.PreparePlayerForSceneTransition();
        }
        else
        {
            Debug.LogWarning(
                "[GameManager] PlayerSceneMover가 없어 Player 보호 처리를 건너뜁니다.",
                this
            );
        }
    }

    /// <summary>
    /// 기존 코드 호환용.
    /// 실제 Player 생성/이동은 PlayerSceneMover가 담당한다.
    /// </summary>
    public bool MoveExistingPlayerToCurrentSceneSpawn(
        string spawnId = "Default"
    )
    {
        if (PlayerSceneMover.Instance == null)
        {
            Debug.LogError(
                "[GameManager] PlayerSceneMover가 없어 Player를 이동시킬 수 없습니다.",
                this
            );
            return false;
        }

        return PlayerSceneMover.Instance
            .SpawnOrMovePlayerToActiveScene(spawnId);
    }

    /// <summary>
    /// 기존 코드 호환용.
    /// </summary>
    public bool SpawnPersistentPlayerAtSpawn(
        string spawnId = "Default"
    )
    {
        return MoveExistingPlayerToCurrentSceneSpawn(spawnId);
    }

    /// <summary>
    /// 현재 씬에 존재하는 씬 전용 매니저와 UI 참조를 다시 잡는다.
    ///
    /// saveManager / traitManager / runManager는 전역에 있을 수도 있으므로
    /// 비어 있을 때 우선 탐색한다.
    ///
    /// uiManager / hubManager / resultUI는 씬마다 달라질 수 있으므로
    /// 씬 로드 때마다 새로 탐색한다.
    /// </summary>
    public void RefreshSceneReferences()
    {
        if (saveManager == null)
        {
            saveManager =
                FindObjectOfType<SaveManager>(true);
        }

        if (traitManager == null)
        {
            traitManager =
                FindObjectOfType<TraitManager>(true);
        }

        if (runManager == null)
        {
            runManager =
                FindObjectOfType<RunManager>(true);
        }

        uiManager =
            FindObjectOfType<UIManager>(true);

        hubManager =
            FindObjectOfType<HubManager>(true);

        resultUI =
            FindObjectOfType<ResultUI>(true);

        Player currentPlayer =
            CurrentPlayer;

        if (traitManager != null)
        {
            traitManager.player =
                currentPlayer;
        }

        if (runManager != null)
        {
            runManager.player =
                currentPlayer;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (Instance == this)
        {
            Instance = null;
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체를 관리하는 중앙 매니저.
///
/// 기존 기능:
/// - 저장/불러오기
/// - 특성 관리
/// - 런 진행 관리
/// - UI 관리
/// - 허브 관리
/// - 결과 UI
/// - 영구 재화
///
/// 추가 기능:
/// - Player를 씬 이동 후에도 유지
/// - 새 씬의 PlayerSpawnPoint 위치로 이동
/// - 씬 이동 중 플레이어 조작 차단
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

    [Header("플레이어 유지")]
    [Tooltip(
        "시작 씬의 Player를 연결하세요.\n" +
        "비워두면 현재 씬에서 자동으로 찾습니다."
    )]
    [SerializeField]
    private Player player;

    [Header("씬 이동")]
    [Min(0f)]
    [Tooltip("씬 로드 완료 후 최소 대기 시간")]
    [SerializeField]
    private float minimumLoadingTime = 0.3f;

    [Tooltip(
        "요청한 Spawn ID를 찾지 못했을 때 " +
        "첫 번째 PlayerSpawnPoint를 사용할지 여부"
    )]
    [SerializeField]
    private bool useFirstSpawnAsFallback = true;

    [Header("씬 이동 디버그")]
    [SerializeField]
    private bool showSceneLogs = true;

    private bool isChangingScene;
    private string pendingSpawnId = "Default";
    private Coroutine sceneRoutine;

    public bool IsChangingScene =>
        isChangingScene;

    public Player CurrentPlayer =>
        player;

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

        FindPlayer();

        if (player != null)
        {
            KeepPlayerBetweenScenes();
        }
    }

    private void Start()
    {
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
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
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

        saveManager.saveData.permanentMoney =
            permanentMoney.CurrentMoney;

        saveManager.Save();
    }

    public void RegisterPlayer(Player newPlayer)
    {
        if (newPlayer == null)
        {
            return;
        }

        player = newPlayer;
        KeepPlayerBetweenScenes();
    }

    private void FindPlayer()
    {
        if (player != null)
        {
            return;
        }

        player =
            FindObjectOfType<Player>(true);

        if (player != null)
        {
            KeepPlayerBetweenScenes();
        }
    }

    private void KeepPlayerBetweenScenes()
    {
        if (player == null)
        {
            return;
        }

        if (player.transform.parent != null)
        {
            player.transform.SetParent(null);
        }

        DontDestroyOnLoad(
            player.gameObject
        );
    }

    public bool ChangeScene(
        string sceneName,
        string spawnId = "Default"
    )
    {
        if (isChangingScene)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(
                "[GameManager] 이동할 씬 이름이 비어 있습니다.",
                this
            );
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"[GameManager] 씬을 불러올 수 없습니다: {sceneName}\n" +
                "Build Settings 또는 Build Profiles에 씬이 등록됐는지 확인하세요.",
                this
            );
            return false;
        }

        pendingSpawnId =
            string.IsNullOrWhiteSpace(spawnId)
                ? "Default"
                : spawnId;

        if (sceneRoutine != null)
        {
            StopCoroutine(sceneRoutine);
        }

        sceneRoutine =
            StartCoroutine(
                ChangeSceneRoutine(sceneName)
            );

        return true;
    }

    private IEnumerator ChangeSceneRoutine(
        string sceneName
    )
    {
        isChangingScene = true;

        FindPlayer();
        SetPlayerControl(false);
        StopPlayerMovement();

        float loadStartTime =
            Time.realtimeSinceStartup;

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(sceneName);

        if (loadOperation == null)
        {
            Debug.LogError(
                $"[GameManager] 씬 로드 요청 실패: {sceneName}",
                this
            );

            FinishFailedSceneChange();
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        float elapsedTime =
            Time.realtimeSinceStartup -
            loadStartTime;

        float remainingTime =
            minimumLoadingTime -
            elapsedTime;

        if (remainingTime > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    remainingTime
                );
        }

        yield return null;

        RemoveDuplicatePlayers();
        FindPlayer();
        RefreshSceneReferences();

        bool moved =
            MovePlayerToSpawnPoint(
                pendingSpawnId
            );

        StopPlayerMovement();
        SetPlayerControl(true);

        isChangingScene = false;
        sceneRoutine = null;

        if (showSceneLogs)
        {
            Debug.Log(
                $"[GameManager] 씬 이동 완료 / " +
                $"씬={sceneName}, " +
                $"Spawn ID={pendingSpawnId}, " +
                $"Player 이동={moved}",
                this
            );
        }
    }

    private bool MovePlayerToSpawnPoint(
        string spawnId
    )
    {
        if (player == null)
        {
            Debug.LogError(
                "[GameManager] 이동시킬 Player가 없습니다.",
                this
            );
            return false;
        }

        PlayerSpawnPoint[] spawnPoints =
            FindObjectsOfType<PlayerSpawnPoint>(true);

        PlayerSpawnPoint selectedPoint =
            null;

        for (int i = 0;
             i < spawnPoints.Length;
             i++)
        {
            PlayerSpawnPoint point =
                spawnPoints[i];

            if (point == null)
            {
                continue;
            }

            if (point.SpawnId == spawnId)
            {
                selectedPoint = point;
                break;
            }
        }

        if (selectedPoint == null &&
            useFirstSpawnAsFallback &&
            spawnPoints.Length > 0)
        {
            selectedPoint =
                spawnPoints[0];

            Debug.LogWarning(
                $"[GameManager] Spawn ID '{spawnId}'를 찾지 못해 " +
                "첫 번째 Spawn Point를 사용합니다.",
                selectedPoint
            );
        }

        if (selectedPoint == null)
        {
            Debug.LogError(
                $"[GameManager] 새 씬에 PlayerSpawnPoint가 없습니다. " +
                $"요청 ID={spawnId}",
                this
            );
            return false;
        }

        TeleportPlayer(
            selectedPoint.transform.position,
            selectedPoint.transform.rotation
        );

        return true;
    }

    private void TeleportPlayer(
        Vector3 targetPosition,
        Quaternion targetRotation
    )
    {
        if (player == null)
        {
            return;
        }

        CharacterController characterController =
            player.GetComponent<CharacterController>();

        bool controllerWasEnabled =
            characterController != null &&
            characterController.enabled;

        if (controllerWasEnabled)
        {
            characterController.enabled = false;
        }

        Rigidbody body =
            player.GetComponent<Rigidbody>();

        if (body != null)
        {
            body.velocity =
                Vector3.zero;

            body.angularVelocity =
                Vector3.zero;

            body.position =
                targetPosition;

            body.rotation =
                targetRotation;
        }

        player.transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );

        Physics.SyncTransforms();

        if (controllerWasEnabled)
        {
            characterController.enabled = true;
        }
    }

    private void StopPlayerMovement()
    {
        if (player == null)
        {
            return;
        }

        Rigidbody body =
            player.GetComponent<Rigidbody>();

        if (body == null)
        {
            return;
        }

        body.velocity =
            Vector3.zero;

        body.angularVelocity =
            Vector3.zero;
    }

    private void SetPlayerControl(bool value)
    {
        if (player == null)
        {
            return;
        }

        PlayerController controller =
            player.GetComponent<PlayerController>();

        if (controller == null)
        {
            controller =
                player.GetComponentInChildren<PlayerController>(
                    true
                );
        }

        if (controller != null)
        {
            controller.enabled = value;
        }
    }

    private void RemoveDuplicatePlayers()
    {
        Player[] players =
            FindObjectsOfType<Player>(true);

        if (players.Length == 0)
        {
            return;
        }

        if (player == null)
        {
            player = players[0];
            KeepPlayerBetweenScenes();
        }

        for (int i = 0;
             i < players.Length;
             i++)
        {
            Player foundPlayer =
                players[i];

            if (foundPlayer == null ||
                foundPlayer == player)
            {
                continue;
            }

            Debug.LogWarning(
                $"[GameManager] 중복 Player 제거: {foundPlayer.name}",
                foundPlayer
            );

            Destroy(
                foundPlayer.gameObject
            );
        }
    }

    private void RefreshSceneReferences()
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

        if (uiManager == null)
        {
            uiManager =
                FindObjectOfType<UIManager>(true);
        }

        if (hubManager == null)
        {
            hubManager =
                FindObjectOfType<HubManager>(true);
        }

        if (resultUI == null)
        {
            resultUI =
                FindObjectOfType<ResultUI>(true);
        }
    }

    private void FinishFailedSceneChange()
    {
        SetPlayerControl(true);

        isChangingScene = false;
        sceneRoutine = null;
    }
}

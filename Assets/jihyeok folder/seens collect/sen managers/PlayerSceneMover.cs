using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Title 씬에 두는 Player 이동 전담 매니저.
///
/// 역할:
/// - Player Prefab 생성
/// - 씬 이동 직전 Player 보호
/// - 목적지 씬 로드 후 Player를 현재 활성 씬으로 편입
/// - PlayerSpawnPoint 위치로 Player 이동
///
/// 구조:
/// - GameManager는 Lobby 씬에 둔다.
/// - PlayerSceneMover는 Title 씬에 둔다.
/// - Player는 각 씬에 미리 배치하지 않는다.
/// </summary>
[DefaultExecutionOrder(-950)]
public class PlayerSceneMover : MonoBehaviour
{
    public static PlayerSceneMover Instance;

    [Header("Player")]
    [Tooltip("현재 사용 중인 Player. 비워두면 자동으로 찾고, 없으면 Player Prefab으로 생성합니다.")]
    [SerializeField]
    private Player player;

    [Tooltip("Title에서 시작할 때 생성할 Player 프리팹")]
    [SerializeField]
    private Player playerPrefab;

    [Header("스폰 설정")]
    [SerializeField]
    private bool useFirstSpawnAsFallback = true;

    [SerializeField]
    private string defaultSpawnId = "Default";

    [Header("디버그")]
    [SerializeField]
    private bool showLogs = true;

    public Player CurrentPlayer => player;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            /*
             * PlayerSceneMover는 Title에서 시작해서
             * Loading, Lobby, CombatRoom까지 살아남아야 한다.
             *
             * 반드시 Title 씬 Hierarchy의 루트 오브젝트에 붙여야 한다.
             */
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ResolveExistingPlayerOnly();
    }

    public void RegisterPlayer(
        Player newPlayer
    )
    {
        if (newPlayer == null)
        {
            return;
        }

        player = newPlayer;

        EnsurePlayerTag(player);
    }

    /// <summary>
    /// RunFlowManager가 Loading 씬으로 넘기기 직전에 호출한다.
    /// 기존 Player가 있으면 씬 로드로 파괴되지 않게 잠깐 보호한다.
    /// Title에서는 아직 Player가 없을 수 있으므로 정상적으로 그냥 넘어간다.
    /// </summary>
    public void PreparePlayerForSceneTransition()
    {
        ResolveExistingPlayerOnly();

        if (player == null)
        {
            if (showLogs)
            {
                Debug.Log(
                    "[PlayerSceneMover] 아직 Player가 없습니다. 목적지 씬에서 Player Prefab을 생성합니다.",
                    this
                );
            }

            return;
        }

        if (player.transform.parent != null)
        {
            player.transform.SetParent(null);
        }

        EnsurePlayerTag(player);
        SetPlayerControl(false);
        StopPlayerMovement();

        DontDestroyOnLoad(player.gameObject);

        if (showLogs)
        {
            Debug.Log(
                "[PlayerSceneMover] Player 씬 이동 보호 완료.",
                player
            );
        }
    }

    /// <summary>
    /// 현재 활성 씬 안의 PlayerSpawnPoint 위치에 Player를 생성하거나 이동한다.
    /// 기존 Player가 있으면 새로 만들지 않고 그대로 이동한다.
    /// </summary>
    public bool SpawnOrMovePlayerToActiveScene(
        string spawnId = "Default"
    )
    {
        string safeSpawnId =
            string.IsNullOrWhiteSpace(spawnId)
                ? defaultSpawnId
                : spawnId;

        Scene activeScene =
            SceneManager.GetActiveScene();

        PlayerSpawnPoint spawnPoint =
            FindSpawnPointInScene(
                activeScene,
                safeSpawnId
            );

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"[PlayerSceneMover] 활성 씬 '{activeScene.name}' 안에서 PlayerSpawnPoint를 찾지 못했습니다. 요청 ID={safeSpawnId}",
                this
            );

            return false;
        }

        ResolveExistingPlayerOnly();

        if (player == null)
        {
            if (playerPrefab == null)
            {
                Debug.LogError(
                    "[PlayerSceneMover] Player가 없고 Player Prefab도 연결되지 않았습니다. Title 씬의 PlayerSceneMover 인스펙터에 Player Prefab을 넣으세요.",
                    this
                );

                return false;
            }

            player =
                Instantiate(
                    playerPrefab,
                    spawnPoint.transform.position,
                    spawnPoint.transform.rotation
                );

            player.name =
                playerPrefab.name;

            if (showLogs)
            {
                Debug.Log(
                    $"[PlayerSceneMover] Player Prefab 생성 완료 / Scene={activeScene.name}, Spawn ID={safeSpawnId}",
                    player
                );
            }
        }

        RemoveDuplicatePlayers();

        if (player.transform.parent != null)
        {
            player.transform.SetParent(null);
        }

        /*
         * 핵심:
         * Player를 DontDestroyOnLoad에 계속 두지 않고,
         * 현재 활성 씬으로 실제 편입시킨다.
         */
        if (player.gameObject.scene != activeScene)
        {
            SceneManager.MoveGameObjectToScene(
                player.gameObject,
                activeScene
            );
        }

        EnsurePlayerTag(player);

        SetPlayerControl(false);
        StopPlayerMovement();

        TeleportPlayer(
            spawnPoint.transform.position,
            spawnPoint.transform.rotation
        );

        spawnPoint.HideAfterSpawn();

        StopPlayerMovement();

        PlayerFeedback feedback =
            player.GetComponent<PlayerFeedback>();

        bool emerging =
            feedback != null &&
            feedback.BeginLobbyEmergeIfRequested();

        if (!emerging)
        {
            SetPlayerControl(true);
        }

        RefreshExternalReferences();

        if (showLogs)
        {
            Debug.Log(
                $"[PlayerSceneMover] Player 씬 배치 완료 / " +
                $"Scene={activeScene.name}, " +
                $"Spawn ID={safeSpawnId}, " +
                $"Spawn Position={spawnPoint.transform.position}, " +
                $"Player Scene={player.gameObject.scene.name}",
                player
            );
        }

        return true;
    }

    private void ResolveExistingPlayerOnly()
    {
        if (player != null)
        {
            return;
        }

        player =
            FindObjectOfType<Player>(true);

        if (player != null)
        {
            EnsurePlayerTag(player);
            return;
        }

        GameObject taggedPlayer = null;

        try
        {
            taggedPlayer =
                GameObject.FindGameObjectWithTag("Player");
        }
        catch
        {
            taggedPlayer = null;
        }

        if (taggedPlayer == null)
        {
            return;
        }

        player =
            taggedPlayer.GetComponent<Player>();

        if (player == null)
        {
            player =
                taggedPlayer.GetComponentInParent<Player>();
        }

        if (player != null)
        {
            EnsurePlayerTag(player);
        }
    }

    private PlayerSpawnPoint FindSpawnPointInScene(
        Scene targetScene,
        string spawnId
    )
    {
        string safeSpawnId =
            string.IsNullOrWhiteSpace(spawnId)
                ? defaultSpawnId
                : spawnId;

        PlayerSpawnPoint[] spawnPoints =
            FindObjectsOfType<PlayerSpawnPoint>(true);

        PlayerSpawnPoint fallbackPoint = null;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            PlayerSpawnPoint point =
                spawnPoints[i];

            if (point == null)
            {
                continue;
            }

            /*
             * 핵심:
             * 현재 활성 씬 안의 SpawnPoint만 사용한다.
             * Lobby, Loading, DontDestroyOnLoad 쪽 SpawnPoint를 잘못 잡지 않게 한다.
             */
            if (point.gameObject.scene != targetScene)
            {
                continue;
            }

            if (fallbackPoint == null)
            {
                fallbackPoint = point;
            }

            if (point.SpawnId == safeSpawnId)
            {
                return point;
            }
        }

        if (fallbackPoint != null &&
            useFirstSpawnAsFallback)
        {
            Debug.LogWarning(
                $"[PlayerSceneMover] 씬 '{targetScene.name}' 안에서 Spawn ID '{safeSpawnId}'를 찾지 못해 첫 번째 PlayerSpawnPoint를 사용합니다.",
                fallbackPoint
            );

            return fallbackPoint;
        }

        return null;
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

        bool characterControllerWasEnabled =
            characterController != null &&
            characterController.enabled;

        if (characterControllerWasEnabled)
        {
            characterController.enabled = false;
        }

        Rigidbody body =
            player.GetComponent<Rigidbody>();

        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = targetPosition;
            body.rotation = targetRotation;
        }

        player.transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );

        Physics.SyncTransforms();

        if (characterControllerWasEnabled)
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

        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    private void SetPlayerControl(
        bool value
    )
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
                player.GetComponentInChildren<PlayerController>(true);
        }

        if (controller != null)
        {
            controller.enabled = value;
        }

#if ENABLE_INPUT_SYSTEM
        PlayerInput input =
            player.GetComponent<PlayerInput>();

        if (input == null)
        {
            input =
                player.GetComponentInChildren<PlayerInput>(true);
        }

        if (input != null)
        {
            input.enabled = value;

            if (value)
            {
                input.ActivateInput();
            }
            else
            {
                input.DeactivateInput();
            }
        }
#endif
    }

    private void EnsurePlayerTag(
        Player targetPlayer
    )
    {
        if (targetPlayer == null)
        {
            return;
        }

        try
        {
            targetPlayer.gameObject.tag = "Player";
        }
        catch
        {
            Debug.LogWarning(
                "[PlayerSceneMover] Player 태그 설정 실패. Unity Tag 목록에 Player 태그가 있는지 확인하세요.",
                targetPlayer
            );
        }
    }

    private void RemoveDuplicatePlayers()
    {
        Player[] players =
            FindObjectsOfType<Player>(true);

        if (players.Length <= 1)
        {
            return;
        }

        if (player == null)
        {
            player = players[0];
            EnsurePlayerTag(player);
        }

        for (int i = 0; i < players.Length; i++)
        {
            Player foundPlayer =
                players[i];

            if (foundPlayer == null ||
                foundPlayer == player)
            {
                continue;
            }

            Debug.LogWarning(
                $"[PlayerSceneMover] 중복 Player 제거: {foundPlayer.name}",
                foundPlayer
            );

            Destroy(foundPlayer.gameObject);
        }
    }

    private void RefreshExternalReferences()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.RefreshSceneReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(defaultSpawnId))
        {
            defaultSpawnId = "Default";
        }
    }
#endif
}

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어와 BoonInfo를 씬 이동 중 유지한다.
/// 로비의 Player 루트에 하나만 붙인다.
/// </summary>
[DefaultExecutionOrder(-900)]
public class RunPlayerPersistence : MonoBehaviour
{
    private static RunPlayerPersistence instance;

    [Header("타이틀 복귀 설정")]
    [SerializeField] private bool destroyInTitleScene = true;
    [SerializeField] private string titleSceneName = "Title";

    private Rigidbody playerRigidbody;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        playerRigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (destroyInTitleScene &&
            scene.name == titleSceneName)
        {
            Destroy(gameObject);
            return;
        }

        PlayerSpawnPoint spawnPoint =
            FindFirstObjectByType<PlayerSpawnPoint>();

        if (spawnPoint == null)
        {
            return;
        }

        Transform spawnTransform = spawnPoint.transform;

        if (playerRigidbody != null)
        {
            playerRigidbody.position =
                spawnTransform.position;

            playerRigidbody.rotation =
                spawnTransform.rotation;

            playerRigidbody.velocity =
                Vector3.zero;

            playerRigidbody.angularVelocity =
                Vector3.zero;
        }
        else
        {
            transform.SetPositionAndRotation(
                spawnTransform.position,
                spawnTransform.rotation);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

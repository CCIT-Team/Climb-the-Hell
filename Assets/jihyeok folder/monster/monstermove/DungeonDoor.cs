using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonDoor : MonoBehaviour
{
    [Header("몬스터 스포너")]
    [SerializeField] private MonsterSpawner monsterSpawner;

    [Header("사라질 문")]
    [Tooltip("클리어 시 사라질 문 모델과 일반 Collider가 들어 있는 오브젝트")]
    [SerializeField] private GameObject doorVisual;

    [Header("씬 이동 영역")]
    [Tooltip("문이 사라진 뒤 활성화할 Trigger Collider")]
    [SerializeField] private Collider sceneChangeTrigger;

    [Header("문 위 표시")]
    [Tooltip("문이 열렸을 때 위쪽에 표시할 오브젝트")]
    [SerializeField] private GameObject destinationObject;

    [Header("이동할 씬")]
    [Tooltip("Trigger에 들어갔을 때 이동할 씬 이름")]
    [SerializeField] private string targetSceneName;

    [Tooltip("클리어 후 Trigger가 켜질 때까지의 시간")]
    [SerializeField] private float activationDelay = 0.5f;

    private bool isOpened;
    private bool canChangeScene;
    private bool isChangingScene;

    private void Awake()
    {
        if (destinationObject != null)
        {
            destinationObject.SetActive(false);
        }

        if (sceneChangeTrigger != null)
        {
            sceneChangeTrigger.isTrigger = true;
            sceneChangeTrigger.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (monsterSpawner != null)
        {
            monsterSpawner.OnAllPhasesCleared += OpenDoor;
        }
    }

    private void Start()
    {
        // 문보다 스포너가 먼저 클리어된 경우
        if (monsterSpawner != null &&
            monsterSpawner.IsCleared)
        {
            OpenDoor();
        }
    }

    private void OnDisable()
    {
        if (monsterSpawner != null)
        {
            monsterSpawner.OnAllPhasesCleared -= OpenDoor;
        }
    }

    private void OpenDoor()
    {
        if (isOpened)
            return;

        isOpened = true;

        // 문 모델과 문을 막는 Collider 제거
        if (doorVisual != null)
        {
            doorVisual.SetActive(false);
        }

        // 문 위 목적지 표시
        if (destinationObject != null)
        {
            destinationObject.SetActive(true);
        }

        StartCoroutine(EnableSceneChangeTrigger());
    }

    private IEnumerator EnableSceneChangeTrigger()
    {
        yield return new WaitForSeconds(activationDelay);

        if (sceneChangeTrigger != null)
        {
            sceneChangeTrigger.enabled = true;
        }

        canChangeScene = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canChangeScene || isChangingScene)
            return;

        // 자식 Collider가 Trigger에 들어오는 경우도 처리
        Player player = other.GetComponentInParent<Player>();

        if (player == null && !other.CompareTag("Player"))
            return;

        ChangeScene();
    }

    private void ChangeScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning(
                "[DungeonDoor] 이동할 씬 이름이 지정되지 않았습니다."
            );
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError(
                $"[DungeonDoor] '{targetSceneName}' 씬을 찾을 수 없습니다. " +
                "Build Profiles의 Scene List를 확인하세요."
            );
            return;
        }

        isChangingScene = true;

        SceneManager.LoadScene(targetSceneName);
    }
}
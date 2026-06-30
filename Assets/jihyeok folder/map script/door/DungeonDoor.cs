using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonDoor : MonoBehaviour
{
    [Header("몬스터 스포너")]
    [SerializeField] private MonsterSpawner monsterSpawner;

    [Header("문 회전축")]
    [Tooltip("문의 왼쪽 경첩 위치에 배치한 빈 오브젝트")]
    [SerializeField] private Transform doorPivot;

    [Header("문 열기 설정")]
    [Tooltip("문이 열릴 때 Y축으로 회전할 각도")]
    [SerializeField] private float openAngle = -90f;

    [Tooltip("문이 열리는 데 걸리는 시간")]
    [SerializeField] private float openDuration = 1.5f;

    [Header("문 닫기 설정")]
    [Tooltip("플레이어가 들어간 뒤 문이 닫히는 시간")]
    [SerializeField] private float closeDuration = 3f;

    [Header("씬 이동 Trigger")]
    [Tooltip("DoorSceneTrigger가 붙어 있는 오브젝트")]
    [SerializeField] private DoorSceneTrigger sceneChangeTrigger;

    [Header("문 위 표시")]
    [SerializeField] private GameObject destinationObject;

    [Header("이동할 씬")]
    [SerializeField] private string targetSceneName;

    [Header("Trigger 활성화 지연")]
    [Tooltip("문이 완전히 열린 뒤 Trigger가 활성화될 때까지의 시간")]
    [SerializeField] private float activationDelay = 0.5f;

    private Quaternion closedRotation;
    private Quaternion openedRotation;

    private bool isOpened;
    private bool isChangingScene;
    private bool isDoorAnimating;

    private Coroutine doorCoroutine;

    private void Awake()
    {
        if (doorPivot != null)
        {
            closedRotation = doorPivot.localRotation;

            openedRotation =
                closedRotation *
                Quaternion.Euler(0f, openAngle, 0f);
        }
        else
        {
            Debug.LogError(
                "[DungeonDoor] Door Pivot이 연결되지 않았습니다.",
                this
            );
        }

        if (destinationObject != null)
        {
            destinationObject.SetActive(false);
        }

        if (sceneChangeTrigger != null)
        {
            sceneChangeTrigger.Initialize(this);
            sceneChangeTrigger.SetTriggerEnabled(false);
        }
        else
        {
            Debug.LogError(
                "[DungeonDoor] Scene Change Trigger가 연결되지 않았습니다.",
                this
            );
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
        if (monsterSpawner == null)
        {
            Debug.LogWarning(
                "[DungeonDoor] MonsterSpawner가 연결되지 않았습니다.",
                this
            );

            return;
        }

        if (monsterSpawner.IsCleared)
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
        if (isOpened ||
            isChangingScene ||
            isDoorAnimating)
        {
            return;
        }

        if (doorPivot == null)
        {
            Debug.LogError(
                "[DungeonDoor] Door Pivot이 없어 문을 열 수 없습니다.",
                this
            );

            return;
        }

        isOpened = true;

        if (destinationObject != null)
        {
            destinationObject.SetActive(true);
        }

        if (doorCoroutine != null)
        {
            StopCoroutine(doorCoroutine);
        }

        doorCoroutine = StartCoroutine(
            OpenDoorRoutine()
        );
    }

    private IEnumerator OpenDoorRoutine()
    {
        yield return RotateDoor(
            openedRotation,
            openDuration
        );

        if (activationDelay > 0f)
        {
            yield return new WaitForSeconds(
                activationDelay
            );
        }

        if (sceneChangeTrigger != null &&
            !isChangingScene)
        {
            sceneChangeTrigger.SetTriggerEnabled(true);
        }

        doorCoroutine = null;
    }

    public void TryChangeScene(Player player)
    {
        if (!isOpened ||
            isChangingScene ||
            isDoorAnimating ||
            player == null)
        {
            return;
        }

        if (!ValidateTargetScene())
        {
            return;
        }

        if (doorCoroutine != null)
        {
            StopCoroutine(doorCoroutine);
        }

        doorCoroutine = StartCoroutine(
            CloseDoorAndChangeScene()
        );
    }

    private IEnumerator CloseDoorAndChangeScene()
    {
        isChangingScene = true;

        if (sceneChangeTrigger != null)
        {
            sceneChangeTrigger.SetTriggerEnabled(false);
        }

        if (destinationObject != null)
        {
            destinationObject.SetActive(false);
        }

        /*
         * 플레이어가 Trigger에 들어온 순간부터
         * closeDuration 동안 문이 닫힌다.
         */
        yield return RotateDoor(
            closedRotation,
            closeDuration
        );

        doorCoroutine = null;

        ChangeScene();
    }

    private IEnumerator RotateDoor(
        Quaternion targetRotation,
        float duration
    )
    {
        if (doorPivot == null)
        {
            yield break;
        }

        isDoorAnimating = true;

        Quaternion startRotation =
            doorPivot.localRotation;

        if (duration <= 0f)
        {
            doorPivot.localRotation =
                targetRotation;

            isDoorAnimating = false;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float ratio = Mathf.Clamp01(
                elapsedTime / duration
            );

            /*
             * 시작과 끝에서 회전 속도를 줄이는 보간
             */
            float smoothRatio =
                ratio *
                ratio *
                (3f - 2f * ratio);

            doorPivot.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    smoothRatio
                );

            yield return null;
        }

        doorPivot.localRotation =
            targetRotation;

        isDoorAnimating = false;
    }

    private bool ValidateTargetScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning(
                "[DungeonDoor] 이동할 씬 이름이 비어 있습니다.",
                this
            );

            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(
                targetSceneName
            ))
        {
            Debug.LogError(
                $"[DungeonDoor] '{targetSceneName}' 씬을 불러올 수 없습니다. " +
                "Build Profiles의 Scene List를 확인하세요.",
                this
            );

            return false;
        }

        return true;
    }

    private void ChangeScene()
    {
        SceneManager.LoadScene(
            targetSceneName
        );
    }
}
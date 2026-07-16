using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 작두방 씬에만 배치하는 흐름 관리자.
///
/// 공용 BoonRewardInteractable이 스스로 방을 판단하지 않고,
/// 이 스크립트가 작두방에서만 정확한 Spawn Point를 전달한다.
/// </summary>
[DefaultExecutionOrder(200)]
public class JakduRoomFlow : MonoBehaviour
{
    [Header("작두 보상")]

    [SerializeField]
    private BoonRewardInteractable boonReward;

    [Tooltip("작두가 최종 착지할 정확한 월드 위치")]
    [SerializeField]
    private Transform rewardSpawnPoint;

    [Header("다음 방 문")]

    [SerializeField]
    private RoomChoiceGenerator roomChoiceGenerator;

    [Header("디버그")]

    [SerializeField]
    private bool showLogs = true;

    [Min(1)]
    [SerializeField]
    private int roomStateWaitFrames = 30;

    private bool initialized;
    private bool completed;

    private IEnumerator Start()
    {
        /*
         * LoadingSceneController가 목적지 정보를 확정한 뒤
         * 현재 방 타입을 읽도록 한 프레임 기다린다.
         */
        for (int i = 0;
             i < roomStateWaitFrames &&
             !IsJakduRoomActive();
             i++)
        {
            yield return null;
        }

        if (!ValidateReferences())
        {
            yield break;
        }

        bool isJakduRoom =
            IsJakduRoomActive();

        if (!isJakduRoom)
        {
            boonReward.HideReward();
            boonReward.gameObject.SetActive(false);

            if (showLogs)
            {
                Debug.Log(
                    "[JakduRoomFlow] 작두방이 아니므로 보상을 생성하지 않습니다.",
                    this
                );
            }

            yield break;
        }

        initialized = true;
        completed = false;

        boonReward.gameObject.SetActive(true);

        /*
         * 문 위 목적지 표시는 미리 만들고
         * 실제 문 이동만 잠근다.
         */
        roomChoiceGenerator.PrepareDoorChoices();
        roomChoiceGenerator.HideDoors();

        /*
         * 플레이어 위치가 아니라 인스펙터에 연결한
         * Spawn Point의 월드 위치를 사용한다.
         */
        boonReward.PrepareAt(
            BoonCategory.Jakdu,
            rewardSpawnPoint.position,
            HandleRewardCompleted
        );

        if (showLogs)
        {
            Debug.Log(
                "[JakduRoomFlow] 작두 보상 생성 완료 / " +
                $"착지 위치={rewardSpawnPoint.position}",
                this
            );
        }
    }

    private void HandleRewardCompleted()
    {
        if (!initialized ||
            completed)
        {
            return;
        }

        completed = true;

        roomChoiceGenerator.OpenPreparedDoors();

        if (showLogs)
        {
            Debug.Log(
                "[JakduRoomFlow] 작두 보상 완료 / 문 개방",
                this
            );
        }
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (boonReward == null)
        {
            Debug.LogError(
                "[JakduRoomFlow] Boon Reward가 연결되지 않았습니다.",
                this
            );

            valid = false;
        }

        if (rewardSpawnPoint == null)
        {
            Debug.LogError(
                "[JakduRoomFlow] Reward Spawn Point가 연결되지 않았습니다.",
                this
            );

            valid = false;
        }

        if (roomChoiceGenerator == null)
        {
            Debug.LogError(
                "[JakduRoomFlow] Room Choice Generator가 연결되지 않았습니다.",
                this
            );

            valid = false;
        }

        return valid;
    }

    private static bool IsJakduRoomActive()
    {
        RunFlowManager manager =
            RunFlowManager.Instance;

        if (manager != null &&
            manager.IsRunActive &&
            manager.CurrentRoomType == RoomType.Jakdu)
        {
            return true;
        }

        string sceneName =
            SceneManager.GetActiveScene().name;

        return
            sceneName.IndexOf(
                "Jakdu",
                System.StringComparison.OrdinalIgnoreCase
            ) >= 0;
    }

    private void OnDisable()
    {
        if (boonReward != null)
        {
            boonReward.HideReward();
        }
    }
}

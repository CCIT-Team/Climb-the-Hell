using System.Collections;
using UnityEngine;

/// <summary>
/// 상점방 전용 흐름.
/// 전투를 기다리지 않고, 씬 시작 후 문을 바로 열어 둔다.
///
/// 사용법:
/// - 상점방 씬에 빈 오브젝트를 만들고 이 스크립트를 붙인다.
/// - RoomChoiceGenerator를 연결한다.
/// - 상점방에서는 CombatRoomFlow를 꺼두거나 제거한다.
/// </summary>
[DefaultExecutionOrder(100)]
public class ShopRoomFlow : MonoBehaviour
{
    [Header("문 선택 생성기")]
    [SerializeField]
    private RoomChoiceGenerator roomChoiceGenerator;

    [Header("직접 열 문")]
    [Tooltip("RoomChoiceGenerator를 쓰지 않는 테스트 씬이면 여기에 DungeonDoor를 직접 넣어도 된다.")]
    [SerializeField]
    private DungeonDoor[] doorsToOpenDirectly;

    [Header("문 열기 재시도")]
    [Min(1)]
    [SerializeField]
    private int retryFrames = 60;

    [Header("디버그")]
    [SerializeField]
    private bool showLogs = true;

    private IEnumerator Start()
    {
        if (roomChoiceGenerator == null)
        {
            roomChoiceGenerator =
                FindFirstObjectByType<RoomChoiceGenerator>();
        }

        if (roomChoiceGenerator != null)
        {
            roomChoiceGenerator
                .EnsureDirectPlayInitialized();

            for (int i = 0;
                 i < retryFrames;
                 i++)
            {
                bool prepared =
                    roomChoiceGenerator
                        .PrepareDoorChoices();

                if (prepared)
                {
                    roomChoiceGenerator
                        .OpenPreparedDoors();

                    if (showLogs)
                    {
                        Debug.Log(
                            "[ShopRoomFlow] 상점방 문을 즉시 개방했습니다.",
                            this
                        );
                    }

                    yield break;
                }

                yield return null;
            }

            roomChoiceGenerator
                .OpenPreparedDoors();

            yield break;
        }

        OpenDirectDoors();
    }

    private void OpenDirectDoors()
    {
        if (doorsToOpenDirectly == null)
        {
            return;
        }

        for (int i = 0;
             i < doorsToOpenDirectly.Length;
             i++)
        {
            DungeonDoor door =
                doorsToOpenDirectly[i];

            if (door == null)
            {
                continue;
            }

            door.Open();
        }

        if (showLogs)
        {
            Debug.Log(
                "[ShopRoomFlow] 직접 연결된 문을 즉시 개방했습니다.",
                this
            );
        }
    }
}

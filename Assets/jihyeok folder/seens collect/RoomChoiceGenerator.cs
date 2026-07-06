using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 방 완료 후 그래프에서 다음 경로를 뽑아
/// 고정된 문 두 개에 배정한다.
/// </summary>
public class RoomChoiceGenerator : MonoBehaviour
{
    [SerializeField] private DungeonDoor leftDoor;
    [SerializeField] private DungeonDoor rightDoor;

    [Tooltip("10층 보스 진입 시 왼쪽 문 하나만 사용할지 여부")]
    [SerializeField] private bool useSingleDoorForBoss = true;

    private bool generated;

    private void Awake()
    {
        HideDoors();
    }

    public void HideDoors()
    {
        generated = false;

        if (leftDoor != null)
        {
            leftDoor.SetDoorEnabled(false);
        }

        if (rightDoor != null)
        {
            rightDoor.SetDoorEnabled(false);
        }
    }

    public void GenerateAndOpenDoors()
    {
        if (generated)
        {
            return;
        }

        generated = true;

        List<RoomRouteOption> routes =
            RunFlowManager.Instance.GenerateNextRoutes(2);

        if (routes.Count == 0)
        {
            generated = false;

            Debug.LogError(
                "[RoomChoiceGenerator] 생성된 다음 방 경로가 없습니다.",
                this);
            return;
        }

        bool isBossRoute =
            routes[0].TargetRoom != null &&
            routes[0].TargetRoom.RoomType == RoomType.Boss;

        if (leftDoor != null)
        {
            leftDoor.Configure(routes[0]);
            leftDoor.Open();
        }

        if (rightDoor == null)
        {
            return;
        }

        if (isBossRoute && !useSingleDoorForBoss)
        {
            rightDoor.Configure(routes[0]);
            rightDoor.Open();
            return;
        }

        if (routes.Count >= 2)
        {
            rightDoor.Configure(routes[1]);
            rightDoor.Open();
        }
        else
        {
            rightDoor.SetDoorEnabled(false);
        }
    }
}

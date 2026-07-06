using System;
using UnityEngine;

/// <summary>
/// 문 하나가 들고 있는 다음 방 선택 정보.
/// 전투방일 때만 RewardCategory가 사용된다.
/// </summary>
[Serializable]
public class RoomRouteOption
{
    [SerializeField] private RoomNodeData targetRoom;
    [SerializeField] private int targetFloor;
    [SerializeField] private BoonCategory rewardCategory;

    public RoomNodeData TargetRoom => targetRoom;
    public int TargetFloor => targetFloor;
    public BoonCategory RewardCategory => rewardCategory;

    public bool IsValid =>
        targetRoom != null &&
        targetFloor > 0 &&
        !string.IsNullOrWhiteSpace(targetRoom.SceneName);

    public RoomRouteOption(
        RoomNodeData targetRoom,
        int targetFloor,
        BoonCategory rewardCategory)
    {
        this.targetRoom = targetRoom;
        this.targetFloor = targetFloor;

        // 득도 태그는 전투방에만 전달한다.
        this.rewardCategory =
            targetRoom != null &&
            targetRoom.RoomType == RoomType.Combat
                ? rewardCategory
                : BoonCategory.None;
    }
}

using System;

/// <summary>
/// 한 런 동안 방 하나가 가진 실시간 통계.
/// </summary>
[Serializable]
public class RoomRuntimeStat
{
    public string roomId;
    public int offeredCount;
    public int visitedCount;
    public int missedOfferCount;
    public int lastOfferedFloor = -1;
    public int lastVisitedFloor = -1;

    public RoomRuntimeStat(string roomId)
    {
        this.roomId = roomId;
    }
}

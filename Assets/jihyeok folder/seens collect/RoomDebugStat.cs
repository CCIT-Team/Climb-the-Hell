using System;

/// <summary>
/// 인스펙터와 디버그 UI에서 확인할 후보 수치.
/// </summary>
[Serializable]
public class RoomDebugStat
{
    public string roomId;
    public string roomName;
    public RoomType roomType;
    public bool eligible;
    public string reason;
    public float baseWeight;
    public float pityBonus;
    public float finalWeight;
    public int missedOfferCount;
    public int offeredCount;
    public int visitedCount;
}

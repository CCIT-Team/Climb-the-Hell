using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 방 그래프.
/// 시작 전투방과 10층 보스방을 지정한다.
/// </summary>
[CreateAssetMenu(
    fileName = "RoomGraph",
    menuName = "Dungeon/Room Graph")]
public class RoomGraphData : ScriptableObject
{
    [Header("핵심 방")]
    [SerializeField] private RoomNodeData firstCombatRoom;
    [SerializeField] private RoomNodeData bossRoom;

    [Header("전체 방 노드")]
    [SerializeField] private List<RoomNodeData> allRooms =
        new List<RoomNodeData>();

    public RoomNodeData FirstCombatRoom => firstCombatRoom;
    public RoomNodeData BossRoom => bossRoom;
    public IReadOnlyList<RoomNodeData> AllRooms => allRooms;

    public RoomNodeData FindRoom(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return null;
        }

        for (int i = 0; i < allRooms.Count; i++)
        {
            RoomNodeData room = allRooms[i];

            if (room != null && room.RoomId == roomId)
            {
                return room;
            }
        }

        return null;
    }

    public bool ValidateGraph()
    {
        if (firstCombatRoom == null)
        {
            Debug.LogError(
                "[RoomGraphData] First Combat Room이 없습니다.",
                this);
            return false;
        }

        if (firstCombatRoom.RoomType != RoomType.Combat)
        {
            Debug.LogError(
                "[RoomGraphData] First Combat Room은 Combat 타입이어야 합니다.",
                firstCombatRoom);
            return false;
        }

        if (bossRoom == null)
        {
            Debug.LogError(
                "[RoomGraphData] Boss Room이 없습니다.",
                this);
            return false;
        }

        if (bossRoom.RoomType != RoomType.Boss)
        {
            Debug.LogError(
                "[RoomGraphData] Boss Room은 Boss 타입이어야 합니다.",
                bossRoom);
            return false;
        }

        HashSet<string> ids = new HashSet<string>();

        for (int i = 0; i < allRooms.Count; i++)
        {
            RoomNodeData room = allRooms[i];

            if (room == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(room.RoomId))
            {
                Debug.LogError(
                    $"[RoomGraphData] {room.name}의 Room ID가 비어 있습니다.",
                    room);
                return false;
            }

            if (!ids.Add(room.RoomId))
            {
                Debug.LogError(
                    $"[RoomGraphData] 중복 Room ID: {room.RoomId}",
                    room);
                return false;
            }

            if (string.IsNullOrWhiteSpace(room.SceneName))
            {
                Debug.LogError(
                    $"[RoomGraphData] {room.RoomId}의 Scene Name이 비어 있습니다.",
                    room);
                return false;
            }
        }

        return true;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 그래프의 정점 하나.
/// 실제 씬, 등장 가중치, 연결 가능한 다음 방을 보관한다.
/// </summary>
[CreateAssetMenu(
    fileName = "RoomNode",
    menuName = "Dungeon/Room Node")]
public class RoomNodeData : ScriptableObject
{
    [Header("식별 정보")]
    [SerializeField] private string roomId;
    [SerializeField] private string displayName;
    [SerializeField] private RoomType roomType = RoomType.Combat;
    [SerializeField] private string sceneName;

    [Header("등장 층")]
    [Min(1)]
    [SerializeField] private int minimumFloor = 1;

    [Min(1)]
    [SerializeField] private int maximumFloor = 9;

    [Header("기본 등장 설정")]
    [Min(0f)]
    [SerializeField] private float baseWeight = 10f;

    [Tooltip("일반 선택 후보로 등장할 수 있는지 여부")]
    [SerializeField] private bool canAppearRandomly = true;

    [Tooltip("직전에 방문한 방과 같은 노드가 바로 다시 나올 수 있는지")]
    [SerializeField] private bool allowImmediateRepeat;

    [Tooltip("마지막 방문 층과 다음 등장 층 사이에 필요한 최소 층 차이")]
    [Min(0)]
    [SerializeField] private int minimumFloorGap;

    [Tooltip("한 런에서 방문 가능한 최대 횟수. 0이면 제한 없음")]
    [Min(0)]
    [SerializeField] private int maximumVisitsPerRun;

    [Header("미등장 보정")]
    [SerializeField] private bool usePityWeight;

    [Tooltip("후보 문에 나오지 않을 때마다 더해지는 가중치")]
    [Min(0f)]
    [SerializeField] private float pityWeightPerMiss = 5f;

    [Tooltip("이 횟수 이상 후보에 나오지 않으면 우선 선택. 0이면 없음")]
    [Min(0)]
    [SerializeField] private int guaranteedAfterMisses;

    [Header("그래프 간선")]
    [Tooltip("이 방을 완료한 뒤 이동 가능한 다음 방 노드들")]
    [SerializeField] private List<RoomNodeData> nextRooms =
        new List<RoomNodeData>();

    public string RoomId => roomId;
    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public RoomType RoomType => roomType;
    public string SceneName => sceneName;
    public int MinimumFloor => minimumFloor;
    public int MaximumFloor => maximumFloor;
    public float BaseWeight => baseWeight;
    public bool CanAppearRandomly => canAppearRandomly;
    public bool AllowImmediateRepeat => allowImmediateRepeat;
    public int MinimumFloorGap => minimumFloorGap;
    public int MaximumVisitsPerRun => maximumVisitsPerRun;
    public bool UsePityWeight => usePityWeight;
    public float PityWeightPerMiss => pityWeightPerMiss;
    public int GuaranteedAfterMisses => guaranteedAfterMisses;
    public IReadOnlyList<RoomNodeData> NextRooms => nextRooms;

    private void OnValidate()
    {
        minimumFloor = Mathf.Max(1, minimumFloor);
        maximumFloor = Mathf.Max(minimumFloor, maximumFloor);
        baseWeight = Mathf.Max(0f, baseWeight);
        pityWeightPerMiss = Mathf.Max(0f, pityWeightPerMiss);

        if (string.IsNullOrWhiteSpace(roomId))
        {
            roomId = name;
        }
    }
}

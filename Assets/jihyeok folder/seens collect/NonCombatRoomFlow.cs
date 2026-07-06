using UnityEngine;

/// <summary>
/// 보상방, 상점방, 작두점, 이벤트방 전용.
/// 득도 보상을 주지 않고 콘텐츠가 끝나면 문만 연다.
/// </summary>
public class NonCombatRoomFlow : MonoBehaviour
{
    [SerializeField] private RoomChoiceGenerator roomChoiceGenerator;

    [Tooltip("테스트용. 실제 게임에서는 끄는 것을 권장")]
    [SerializeField] private bool completeOnStartForTest;

    private bool completed;

    private void Awake()
    {
        if (roomChoiceGenerator != null)
        {
            roomChoiceGenerator.HideDoors();
        }
    }

    private void Start()
    {
        if (completeOnStartForTest)
        {
            CompleteRoom();
        }
    }

    /// <summary>
    /// 상자 수령, 상점 종료, 작두점 결과 완료 시 호출한다.
    /// </summary>
    public void CompleteRoom()
    {
        if (completed)
        {
            return;
        }

        completed = true;

        if (roomChoiceGenerator == null)
        {
            Debug.LogError(
                "[NonCombatRoomFlow] RoomChoiceGenerator가 없습니다.",
                this);
            return;
        }

        roomChoiceGenerator.GenerateAndOpenDoors();
    }
}

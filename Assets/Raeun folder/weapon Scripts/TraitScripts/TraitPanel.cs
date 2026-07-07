using UnityEngine;

// 특성 목록(패널)을 관리하는 클래스
public class TraitPanel : MonoBehaviour
{
    // 특성 데이터를 관리하는 매니저
    public TraitManager traitManager;

    // 특성 UI
    public TraitUI traitUI;

    // 화면에 표시될 모든 특성 Row
    public TraitRow[] rows;

    private void Start()
    {
        // 패널 초기화
        Init();
    }

    // 모든 Row 초기화
    public void Init()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            // 각 Row에 표시할 TraitData와 필요한 참조 전달
            rows[i].Init(
                traitManager.allTraits[i],
                traitManager);
        }
    }

    // 모든 Row 새로고침
    public void Refresh()
    {
        foreach (TraitRow row in rows)
        {
            // 현재 레벨, 가격, 버튼 상태 등을 갱신
            row.Refresh();
        }
    }
}
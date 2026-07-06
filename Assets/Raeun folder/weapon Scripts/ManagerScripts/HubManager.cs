using UnityEngine;

// 허브(상점) 씬을 초기화하는 매니저
public class HubManager : MonoBehaviour
{
    // 특성 데이터를 관리하는 매니저
    public TraitManager traitManager;

    // 특성 UI를 관리하는 클래스
    public TraitUI traitUI;

    // 허브 초기화
    public void Init()
    {
        // 저장된 특성 데이터(레벨, 돈 등) 불러오기
        traitManager.LoadTraits();

        // 불러온 데이터를 바탕으로 UI 새로고침
        traitUI.RefreshUI();
    }

    private void Start()
    {
        // 씬이 시작되면 허브 초기화
        Init();
    }
}
using UnityEngine;

// 게임의 모든 UI를 관리하는 클래스
public class UIManager : MonoBehaviour
{
    [SerializeField]
    // 관리할 UI 목록
    private UIBase[] uiList;

    // 특정 UI 열기
    public void Open(UIBase ui)
    {
        // 현재 열려있는 모든 UI 닫기
        CloseAll();

        // 원하는 UI만 열기
        ui.Open();
    }

    // 모든 UI 닫기
    public void CloseAll()
    {
        foreach (UIBase ui in uiList)
        {
            ui.Close();
        }
    }
}
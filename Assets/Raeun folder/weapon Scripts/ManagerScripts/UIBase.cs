using UnityEngine;

// 모든 UI의 부모 클래스
// 공통적으로 사용하는 열기/닫기 기능을 제공한다.
public class UIBase : MonoBehaviour
{
    // UI 열기
    // 게임 오브젝트를 활성화하여 화면에 표시
    public virtual void Open()
    {
        gameObject.SetActive(true);
    }

    // UI 닫기
    // 게임 오브젝트를 비활성화하여 화면에서 숨김
    public virtual void Close()
    {
        gameObject.SetActive(false);
    }
}
using TMPro;
using UnityEngine;

// 특성(허브) UI를 관리하는 클래스
public class TraitUI : UIBase
{
    [Header("UI")]
    // 현재 보유한 영구 재화 표시
    public TMP_Text currencyText;

    [Header("Reference")]
    // 특성 목록을 관리하는 패널
    public TraitPanel traitPanel;

    // GameManager에 있는 영구 재화 데이터
    private MoneyData PermanentMoney =>
        GameManager.Instance.permanentMoney;

    // UI가 열릴 때 호출
    public override void Open()
    {
        // 부모 클래스의 Open() 실행 (UI 활성화)
        base.Open();

        // UI 최신 정보로 갱신
        RefreshUI();
    }

    // 전체 UI 갱신
    public void RefreshUI()
    {
        // 현재 돈 표시 갱신
        UpdateMoneyText(
            PermanentMoney.CurrentMoney);
    }

    // UI가 활성화될 때
    private void OnEnable()
    {
        // 돈이 변경되면 자동으로 UI 갱신
        GameManager.Instance.permanentMoney.OnMoneyChanged
            += UpdateMoneyText;
    }

    // UI가 비활성화될 때
    private void OnDisable()
    {
        // 이벤트 해제
        GameManager.Instance.permanentMoney.OnMoneyChanged
            -= UpdateMoneyText;
    }

    // 돈 UI 갱신
    private void UpdateMoneyText(int money)
    {
        // 보유한 돈 표시
        currencyText.text = $"{money}p";

        // 돈이 바뀌면 특성 구매 가능 여부도 다시 계산
        traitPanel.Refresh();
    }

    public void OnClickClose()
    {
        Close();
    }
}
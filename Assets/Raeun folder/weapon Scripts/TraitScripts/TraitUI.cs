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

    [SerializeField] private TraitManager traitManager;

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

        traitPanel.Refresh();
    }

    // UI가 활성화될 때
    private void OnEnable()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.permanentMoney.OnMoneyChanged += UpdateMoneyText;

        traitManager.OnTraitChanged += RefreshUI;
    }

    // UI가 비활성화될 때
    private void OnDisable()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.permanentMoney.OnMoneyChanged -= UpdateMoneyText;

        traitManager.OnTraitChanged -= RefreshUI;
    }

    // 돈 UI 갱신
    private void UpdateMoneyText(int money)
    {
        // 보유한 돈 표시
        currencyText.text = $"{money}p";
    }

    public void OnClickClose()
    {
        Close();
    }
}
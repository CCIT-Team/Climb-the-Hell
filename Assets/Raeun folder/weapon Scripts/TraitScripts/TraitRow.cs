using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 특성 하나(한 줄)의 UI를 관리하는 클래스
public class TraitRow : MonoBehaviour
{
    [Header("Manager")]
    // 특성 구매 및 레벨 관리
    public TraitManager traitManager;

    // 전체 특성 UI
    public TraitUI traitUI;

    [Header("Data")]
    // 이 Row가 표시할 특성 데이터
    public TraitData trait;

    [Header("UI")]
    // 특성 이름
    public TMP_Text nameText;

    // 현재 효과 → 다음 레벨 효과
    public TMP_Text effectText;

    // 현재 레벨 표시
    public TMP_Text levelText;

    // 업그레이드 비용
    public TMP_Text priceText;

    // 구매 버튼
    public Button upgradeButton;

    // Row 초기화
    public void Init(
        TraitData traitData,
        TraitManager manager,
        TraitUI ui)
    {
        trait = traitData;
        traitManager = manager;
        traitUI = ui;

        // 초기 UI 갱신
        Refresh();
    }

    // 구매 버튼이 눌렸을 때 호출
    public void Buy()
    {
        traitManager.BuyTrait(trait);
    }

    // 현재 특성 정보를 UI에 표시
    public void Refresh()
    {
        // 현재 특성 레벨
        int level =
            traitManager.GetTraitLevel(trait);

        // 이름 표시
        nameText.text = trait.traitName;

        // 레벨 표시
        levelText.text =
            $"Lv. {level}/{trait.maxLevel}";

        // 현재 효과
        float currentValue =
            trait.valuePerLevel * level;

        // 다음 레벨 효과
        float nextValue =
            trait.valuePerLevel *
            Mathf.Min(
                level + 1,
                trait.maxLevel);

        // 최대 레벨인 경우
        if (level >= trait.maxLevel)
        {
            // 현재 효과만 표시
            effectText.text = GetValueText(currentValue);

            // 가격 대신 MAX 표시
            priceText.text = "MAX";

            // 버튼 비활성화
            upgradeButton.interactable = false;

            return;
        }

        // 현재 효과 → 다음 효과 표시
        effectText.text =
            $"{GetValueText(currentValue)} → {GetValueText(nextValue)}";

        // 현재 레벨의 업그레이드 가격
        int price =
            traitManager.GetPrice(trait, level);

        // 실제 값(5)을 화면에서는 0.5로 표시
        priceText.text = $"{price / 10f:F1}p";

        // 구매 가능한지 확인
        bool canBuy =
            traitManager.CanBuyTrait(trait);

        // 가능하면 버튼 활성화
        upgradeButton.interactable =
            canBuy;

        // 돈이 부족하면 빨간색
        priceText.color =
            canBuy
                ? Color.white
                : Color.red;
    }

    // 특성 종류에 맞게 수치를 문자열로 변환
    private string GetValueText(
        float value)
    {
        switch (trait.type)
        {
            // 치명타 확률(%)
            case TraitType.CritChance:
                return $"{value * 100:F0}%";

            // 치명타 데미지(배율)
            case TraitType.CritDamage:
                return $"X{value:F1}";

            // 골드 획득 배율
            case TraitType.GoldMultiplier:
                return $"X{value:F1}";

            // 나머지는 일반 숫자
            default:
                if (value % 1 == 0)
                {
                    return ((int)value).ToString();
                }

                return value.ToString("F1");
        }
    }
}
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 특성 하나(한 줄)의 UI를 관리하는 클래스
public class TraitRow : MonoBehaviour
{
    [Header("Manager")]
    // 특성 구매 및 레벨 관리
    public TraitManager traitManager;

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
        TraitManager manager)
    {
        trait = traitData;
        traitManager = manager;

        // 버튼 클릭 이벤트 연결
        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(Buy);

        Refresh();
    }

    // 구매 버튼이 눌렸을 때 호출
    public void Buy()
    {
        Debug.Log("구매 버튼 클릭!");

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
        string currentText = GetDisplayValue(level);

        // 다음 레벨 효과
        string nextText =  GetDisplayValue(
            Mathf.Min(level + 1, trait.maxLevel));

        // 최대 레벨인 경우
        if (level >= trait.maxLevel)
        {
            // 현재 효과만 표시
            effectText.text = currentText;

            // 가격 대신 MAX 표시
            priceText.text = "MAX";

            // 버튼 비활성화
            upgradeButton.interactable = false;

            return;
        }

        // 현재 효과 → 다음 효과 표시
        effectText.text =
            $"{currentText} → {nextText}";

        // 현재 레벨의 업그레이드 가격
        int price =
            traitManager.GetPrice(trait, level);

        // 실제 값
        priceText.text = $"{price}";

        // 구매 가능한지 확인
        bool canBuy =
            traitManager.CanBuyTrait(trait);

        // 가능하면 버튼 활성화
        upgradeButton.interactable =
            canBuy;

        // 돈이 부족하면 빨간색
        priceText.color =
            canBuy
                ? Color.black
                : Color.red;
    }

    private string GetDisplayValue(int level)
    {
        switch (trait.type)
        {
            // 시작 골드
            case TraitType.StartGold:
                {
                    int baseValue = 100;

                    int value =
                        Mathf.RoundToInt(
                            baseValue *
                            (1 +
                            trait.valuePerLevel *
                            level));

                    return value.ToString();
                }

            // 체력
            case TraitType.MaxHp:
                {
                    int baseValue =
                        traitManager.player.stats.BaseStats.maxHp;

                    int value =
                        Mathf.RoundToInt(
                            baseValue *
                            (1 +
                            trait.valuePerLevel *
                            level));

                    return value.ToString();
                }

            // 공격력
            case TraitType.Attack:
                {
                    int baseValue =
                        traitManager.player.stats.BaseStats.attack;

                    int value =
                        Mathf.RoundToInt(
                            baseValue *
                            (1 +
                            trait.valuePerLevel *
                            level));

                    return value.ToString();
                }

            // 치확
            case TraitType.CritChance:
                {
                    float baseValue =
                        traitManager.player.stats.BaseStats.criticalChance;

                    float value =
                        baseValue *
                        (1 +
                        trait.valuePerLevel *
                        level);

                    return $"{value * 100:F0}%";
                }

            // 치피
            case TraitType.CritDamage:
                {
                    float baseValue =
                        traitManager.player.stats.BaseStats.criticalMultiplier;

                    float value =
                        baseValue *
                        (1 +
                        trait.valuePerLevel *
                        level);

                    return $"{value:F1}";
                }

            // 골드 획득량
            case TraitType.GoldMultiplier:
                {
                    float baseValue =
                        traitManager.player.stats.BaseStats.goldMultiplier;

                    float value =
                        baseValue *
                        (1 +
                        trait.valuePerLevel *
                        level);

                    return $"{value:F1}";
                }

            // 죽음 저항
            case TraitType.DeathResist:
                return $"{level}";

            // 리롤
            case TraitType.Reroll:
                return $"{level}";

            // 대시
            case TraitType.ExtraDash:
                {
                    if (level == 0)
                        return $"{level}";

                    return "1";
                }
        }

        return "";
    }
}
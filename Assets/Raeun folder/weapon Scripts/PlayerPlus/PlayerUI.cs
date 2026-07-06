using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Reference")]
    public Player player;

    [Header("UI")]
    public TMP_Text hpText;
    public Slider hpSlider;

    public TMP_Text goldText;

    private void Start()
    {
        RefreshHP();
        RefreshGold();

        player.OnHpChanged += UpdateHP;

        player.money.OnMoneyChanged += UpdateGold;
    }

    private void OnDestroy()
    {
        player.OnHpChanged -= UpdateHP;

        player.money.OnMoneyChanged -= UpdateGold;
    }

    private void UpdateHP(int hp)
    {
        RefreshHP();
    }

    private void UpdateGold(int gold)
    {
        RefreshGold();
    }

    private void RefreshHP()
    {
        hpText.text =
            $"{player.GetCurrentHp()} / {player.GetMaxHp()}";

        hpSlider.maxValue =
            player.GetMaxHp();

        hpSlider.value =
            player.GetCurrentHp();
    }

    private void RefreshGold()
    {
        goldText.text =
            $"{player.money.CurrentMoney}";
    }
}
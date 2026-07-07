using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Reference")]
    public Player player;

    [Header("UI")]
    public TMP_Text hpText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI flowerText;
    [SerializeField] private TextMeshProUGUI floorText;

    [Header("Boon")]
    [SerializeField] private GameObject attack;
    [SerializeField] private GameObject defense;
    [SerializeField] private GameObject movement;
    [SerializeField] private GameObject debuff;


    private void Start()
    {
        RefreshHP();
        RefreshGold();

        player.OnHpChanged += UpdateHP;

        player.money.OnMoneyChanged += UpdateGold;

        player.stats.OnStatsChanged += RefreshStats;
    }

    private void OnDestroy()
    {
        player.OnHpChanged -= UpdateHP;

        player.money.OnMoneyChanged -= UpdateGold;

        player.stats.OnStatsChanged -= RefreshStats;
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

    public void SetHP(float current, float max)
    {
        hpSlider.maxValue = max;
        hpSlider.value = current;
    }

    public void SetGold(int gold)
    {
        goldText.text = gold.ToString();
    }

    public void SetFlower(int flower)
    {
        flowerText.text = flower.ToString();
    }

    public void SetFloor(int floor)
    {
        floorText.text = "Floor " + floor;
    }

    public void SetBoon(string type, bool active)
    {
        switch (type)
        {
            case "Attack": attack.SetActive(active); break;
            case "Defense": defense.SetActive(active); break;
            case "Movement": movement.SetActive(active); break;
            case "Debuff": debuff.SetActive(active); break;
        }
    }

    private void RefreshStats()
    {
        RefreshHP();
    }
}
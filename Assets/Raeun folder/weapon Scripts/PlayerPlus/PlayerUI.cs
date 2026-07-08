using System.Collections;
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

    [Min(1)]
    [SerializeField] private int playerResolveRetryFrames = 120;

    private bool subscribed;

    private void Start()
    {
        StartCoroutine(BindWhenPlayerIsReady());
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private IEnumerator BindWhenPlayerIsReady()
    {
        for (int i = 0;
             i < playerResolveRetryFrames &&
             player == null;
             i++)
        {
            ResolvePlayer();

            if (player != null)
            {
                break;
            }

            yield return null;
        }

        ResolvePlayer();

        if (player == null)
        {
            Debug.LogError(
                "[PlayerUI] Player reference is missing.",
                this
            );

            yield break;
        }

        RefreshHP();
        RefreshGold();

        Subscribe();
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
        if (player == null ||
            hpText == null ||
            hpSlider == null)
        {
            return;
        }

        hpText.text =
            $"{player.GetCurrentHp()} / {player.GetMaxHp()}";

        hpSlider.maxValue =
            player.GetMaxHp();

        hpSlider.value =
            player.GetCurrentHp();
    }

    private void RefreshGold()
    {
        if (player == null ||
            player.money == null ||
            goldText == null)
        {
            return;
        }

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

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        if (PlayerSceneMover.Instance != null)
        {
            player =
                PlayerSceneMover.Instance.CurrentPlayer;
        }

        if (player == null)
        {
            player =
                FindFirstObjectByType<Player>(
                    FindObjectsInactive.Include
                );
        }
    }

    private void Subscribe()
    {
        if (subscribed ||
            player == null)
        {
            return;
        }

        player.OnHpChanged += UpdateHP;

        if (player.money != null)
        {
            player.money.OnMoneyChanged += UpdateGold;
        }

        if (player.stats != null)
        {
            player.stats.OnStatsChanged += RefreshStats;
        }

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed ||
            player == null)
        {
            subscribed = false;
            return;
        }

        player.OnHpChanged -= UpdateHP;

        if (player.money != null)
        {
            player.money.OnMoneyChanged -= UpdateGold;
        }

        if (player.stats != null)
        {
            player.stats.OnStatsChanged -= RefreshStats;
        }

        subscribed = false;
    }
}

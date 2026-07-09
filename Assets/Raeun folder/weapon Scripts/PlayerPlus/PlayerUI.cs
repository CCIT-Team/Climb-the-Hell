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

    [Header("Boss UI")]
    [SerializeField] private Slider bossHpSlider;
    [SerializeField] private TextMeshProUGUI bossHpText;
    [SerializeField] private TextMeshProUGUI bossNameText;

    [Header("Boon")]
    [SerializeField] private GameObject attack;
    [SerializeField] private GameObject defense;
    [SerializeField] private GameObject movement;
    [SerializeField] private GameObject debuff;

    [Min(1)]
    [SerializeField] private int playerResolveRetryFrames = 120;

    private bool subscribed;
    private int shownFloor = int.MinValue;

    private void Start()
    {
        StartCoroutine(BindWhenPlayerIsReady());
    }

    private void Update()
    {
        RefreshFloor();
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
        RefreshFlower();
        RefreshFloor();
        HideBossHealth();

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

    private void UpdateFlower(int flower)
    {
        RefreshFlower();
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

    private void RefreshFlower()
    {
        if (player == null ||
            flowerText == null)
        {
            return;
        }

        flowerText.text =
            player.FlowerLeaf.ToString();
    }

    private void RefreshFloor()
    {
        if (floorText == null)
        {
            return;
        }

        int floor = 0;

        if (RunFlowManager.Instance != null &&
            RunFlowManager.Instance.IsRunActive)
        {
            floor =
                RunFlowManager.Instance.CurrentFloor;
        }

        if (shownFloor == floor)
        {
            return;
        }

        shownFloor = floor;

        floorText.text =
            floor > 0
                ? "Floor " + floor
                : "Lobby";
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

    public void ShowBossHealth(
        string bossName,
        int current,
        int max
    )
    {
        CacheBossUI();

        if (bossHpSlider == null)
        {
            return;
        }

        int safeMax =
            Mathf.Max(1, max);

        bossHpSlider.gameObject.SetActive(true);
        bossHpSlider.minValue = 0f;
        bossHpSlider.maxValue = safeMax;
        bossHpSlider.value =
            Mathf.Clamp(current, 0, safeMax);

        if (bossHpText != null)
        {
            bossHpText.text =
                $"{Mathf.Clamp(current, 0, safeMax)} / {safeMax}";
        }

        if (bossNameText != null)
        {
            bossNameText.text =
                string.IsNullOrWhiteSpace(bossName)
                    ? "Boss"
                    : bossName;
        }
    }

    public void HideBossHealth()
    {
        CacheBossUI();

        if (bossHpSlider != null)
        {
            bossHpSlider.gameObject.SetActive(false);
        }
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

    private void CacheBossUI()
    {
        if (bossHpSlider == null)
        {
            Slider[] sliders =
                GetComponentsInChildren<Slider>(true);

            for (int i = 0; i < sliders.Length; i++)
            {
                if (sliders[i] != null &&
                    sliders[i].name == "BossHpSlider")
                {
                    bossHpSlider = sliders[i];
                    break;
                }
            }
        }

        TextMeshProUGUI[] texts =
            GetComponentsInChildren<TextMeshProUGUI>(true);

        if (bossHpText == null)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null &&
                    texts[i].name == "BossHpText")
                {
                    bossHpText = texts[i];
                    break;
                }
            }
        }

        if (bossNameText == null)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null &&
                    texts[i].name == "BossNameText")
                {
                    bossNameText = texts[i];
                    break;
                }
            }
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

        player.OnFlowerLeafChanged += UpdateFlower;

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

        player.OnFlowerLeafChanged -= UpdateFlower;

        if (player.stats != null)
        {
            player.stats.OnStatsChanged -= RefreshStats;
        }

        subscribed = false;
    }
}

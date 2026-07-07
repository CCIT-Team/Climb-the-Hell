using TMPro;
using UnityEngine;

public class DebugStatsUI : MonoBehaviour
{
    public Player player;
    public TraitManager traitManager;

    public TMP_Text startGoldText;
    public TMP_Text hpText;
    public TMP_Text attackText;
    public TMP_Text critText;
    public TMP_Text critDamageText;
    public TMP_Text goldMultiplierText;
    public TMP_Text dashText;
    public TMP_Text deathText;
    public TMP_Text rerollText;

    private void Start()
    {
        Refresh();

        traitManager.OnTraitChanged += Refresh;
    }

    private void OnDestroy()
    {
        traitManager.OnTraitChanged -= Refresh;
    }

    public void Refresh()
    {
        startGoldText.text =
            $"StartGold : {traitManager.GetStartGold()}";

        hpText.text =
            $"MaxHP : {player.stats.MaxHp}";

        attackText.text =
            $"Attack : {player.stats.Attack}";

        critText.text =
            $"CritChance : {player.stats.CriticalChance * 100:F1}%";

        critDamageText.text =
            $"CritDamage : {player.stats.CriticalMultiplier:F2}";

        goldMultiplierText.text =
            $"GoldMultiplier : {player.stats.GoldMultiplier:F2}";

        dashText.text =
            $"ExtraDash : {player.stats.MaxDashCount}";

        deathText.text =
            $"DeathResist : {player.stats.DeathResist}";

        rerollText.text =
            $"Reroll : {player.stats.RerollCount}";
    }
}
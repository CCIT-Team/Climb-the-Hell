using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어의 특성 업그레이드를 관리하는 클래스
public class TraitManager : MonoBehaviour
{
    [Header("참조")]
    public Player player;
    public SaveManager saveManager;

    [Header("특성")]
    public List<PlayerTrait> playerTraits =
        new List<PlayerTrait>();

    public List<TraitData> allTraits =
        new List<TraitData>();

    public event Action OnTraitChanged;

    public void EnsureReferences()
    {
        if (playerTraits == null)
        {
            playerTraits =
                new List<PlayerTrait>();
        }

        if (allTraits == null)
        {
            allTraits =
                new List<TraitData>();
        }

        if (saveManager == null &&
            GameManager.Instance != null)
        {
            saveManager =
                GameManager.Instance.saveManager;
        }

        if (saveManager == null &&
            SaveManager.Instance != null)
        {
            saveManager =
                SaveManager.Instance;
        }

        if (saveManager == null)
        {
            saveManager =
                FindObjectOfType<SaveManager>(true);
        }

        if (saveManager != null &&
            saveManager.saveData == null)
        {
            saveManager.saveData =
                new SaveData();
        }

        if (saveManager != null &&
            saveManager.saveData.traits == null)
        {
            saveManager.saveData.traits =
                new List<TraitSaveData>();
        }

        if (player == null &&
            GameManager.Instance != null)
        {
            player =
                GameManager.Instance.CurrentPlayer;
        }

        if (player == null)
        {
            player =
                FindObjectOfType<Player>(true);
        }
    }

    public bool BuyTrait(TraitData trait)
    {
        EnsureReferences();

        if (trait == null)
        {
            Debug.LogWarning(
                "[TraitManager] 구매할 TraitData가 없습니다.",
                this
            );
            return false;
        }

        PlayerTrait playerTrait =
            playerTraits.Find(
                x => x != null &&
                     x.trait == trait
            );

        if (playerTrait == null)
        {
            playerTrait =
                new PlayerTrait();

            playerTrait.trait = trait;
            playerTrait.level = 0;

            playerTraits.Add(playerTrait);
        }

        if (playerTrait.level >= trait.maxLevel)
        {
            Debug.Log("최대 레벨입니다.");
            return false;
        }

        int price =
            GetPrice(
                trait,
                playerTrait.level
            );

        if (GameManager.Instance == null)
        {
            Debug.LogError(
                "[TraitManager] GameManager가 없어 구매할 수 없습니다.",
                this
            );
            return false;
        }

        if (!TrySpendTraitCurrency(price))
        {
            Debug.Log("재화 부족");
            return false;
        }

        playerTrait.level++;

        if (trait.type != TraitType.StartGold)
        {
            ApplyTrait(
                trait,
                playerTrait.level
            );
        }

        GameManager.Instance.SaveGame();

        OnTraitChanged?.Invoke();

        return true;
    }

    public int GetPrice(
        TraitData trait,
        int currentLevel
    )
    {
        if (trait == null)
        {
            return 0;
        }

        if (currentLevel < 0)
        {
            currentLevel = 0;
        }

        try
        {
            return Mathf.RoundToInt(
                trait.levelPrices[currentLevel] *
                700f
            );
        }
        catch
        {
            Debug.LogWarning(
                $"[TraitManager] {trait.type}의 levelPrices가 비었거나 인덱스가 범위를 벗어났습니다. 현재 레벨={currentLevel}",
                this
            );

            return 0;
        }
    }

    public void ApplyTrait(
        TraitData trait,
        int level
    )
    {
        EnsureReferences();

        if (trait == null)
        {
            return;
        }

        if (player == null)
        {
            Debug.LogWarning(
                $"[TraitManager] Player가 없어 {trait.type} 적용을 건너뜁니다.",
                this
            );
            return;
        }

        if (player.stats == null)
        {
            Debug.LogWarning(
                "[TraitManager] Player.stats가 없습니다.",
                player
            );
            return;
        }

        switch (trait.type)
        {
            case TraitType.StartGold:
                break;

            case TraitType.MaxHp:
            {
                int beforeMaxHp =
                    player.stats.MaxHp;

                player.stats.TraitBonusStats.maxHp =
                    Mathf.RoundToInt(
                        player.stats.BaseStats.maxHp *
                        trait.valuePerLevel *
                        level
                    );

                int afterMaxHp =
                    player.stats.MaxHp;

                player.stats.AddCurrentHp(
                    afterMaxHp - beforeMaxHp
                );

                break;
            }

            case TraitType.Attack:
                player.stats.TraitBonusStats.attack =
                    Mathf.RoundToInt(
                        player.stats.BaseStats.attack *
                        trait.valuePerLevel *
                        level
                    );
                break;

            case TraitType.DeathResist:
                player.stats.TraitBonusStats.deathResist =
                    level;
                break;

            case TraitType.CritChance:
                player.stats.TraitBonusStats.criticalChance =
                    player.stats.BaseStats.criticalChance *
                    trait.valuePerLevel *
                    level;
                break;

            case TraitType.CritDamage:
                player.stats.TraitBonusStats.criticalMultiplier =
                    player.stats.BaseStats.criticalMultiplier *
                    trait.valuePerLevel *
                    level;
                break;

            case TraitType.ExtraDash:
                player.stats.TraitBonusStats.extraDashCount =
                    level;
                break;

            case TraitType.GoldMultiplier:
                player.stats.TraitBonusStats.goldMultiplier =
                    player.stats.BaseStats.goldMultiplier *
                    trait.valuePerLevel *
                    level;
                break;

            case TraitType.Reroll:
                player.stats.TraitBonusStats.rerollCount =
                    level;
                break;
        }

        player.RefreshDeathResist();
        player.stats.Init(false);
        player.stats.NotifyChange();

        OnTraitChanged?.Invoke();
    }

    public void ApplyAllTraits()
    {
        EnsureReferences();

        for (int i = 0; i < playerTraits.Count; i++)
        {
            PlayerTrait playerTrait =
                playerTraits[i];

            if (playerTrait == null ||
                playerTrait.trait == null)
            {
                continue;
            }

            if (playerTrait.trait.type ==
                TraitType.StartGold)
            {
                continue;
            }

            ApplyTrait(
                playerTrait.trait,
                playerTrait.level
            );
        }
    }

    public void SaveTraits()
    {
        EnsureReferences();

        if (saveManager == null ||
            saveManager.saveData == null)
        {
            Debug.LogWarning(
                "[TraitManager] SaveManager 또는 SaveData가 없어 저장을 건너뜁니다.",
                this
            );
            return;
        }

        saveManager.saveData.traits.Clear();

        for (int i = 0; i < playerTraits.Count; i++)
        {
            PlayerTrait playerTrait =
                playerTraits[i];

            if (playerTrait == null ||
                playerTrait.trait == null)
            {
                continue;
            }

            TraitSaveData data =
                new TraitSaveData();

            data.type =
                playerTrait.trait.type;

            data.level =
                playerTrait.level;

            saveManager.saveData.traits.Add(data);
        }
    }

    public PlayerTrait GetPlayerTrait(
        TraitData trait
    )
    {
        EnsureReferences();

        if (trait == null)
        {
            return null;
        }

        return playerTraits.Find(
            x => x != null &&
                 x.trait == trait
        );
    }

    public int GetTraitLevel(
        TraitData trait
    )
    {
        PlayerTrait playerTrait =
            GetPlayerTrait(trait);

        return playerTrait == null
            ? 0
            : playerTrait.level;
    }

    public bool CanBuyTrait(
        TraitData trait
    )
    {
        EnsureReferences();

        if (trait == null)
        {
            return false;
        }

        int level =
            GetTraitLevel(trait);

        if (level >= trait.maxLevel)
        {
            return false;
        }

        if (GameManager.Instance == null)
        {
            return false;
        }

        int price =
            GetPrice(
                trait,
                level
            );

        return GetTraitCurrency() >= price;
    }

    public void LoadTraits()
    {
        EnsureReferences();

        playerTraits.Clear();

        if (saveManager == null ||
            saveManager.saveData == null)
        {
            Debug.LogWarning(
                "[TraitManager] SaveData가 없어 특성 불러오기를 건너뜁니다.",
                this
            );
            return;
        }

        if (saveManager.saveData.traits == null)
        {
            saveManager.saveData.traits =
                new List<TraitSaveData>();
        }

        for (int i = 0;
             i < saveManager.saveData.traits.Count;
             i++)
        {
            TraitSaveData data =
                saveManager.saveData.traits[i];

            TraitData trait =
                allTraits.Find(
                    x => x != null &&
                         x.type == data.type
                );

            if (trait == null)
            {
                Debug.LogWarning(
                    $"[TraitManager] 저장된 특성 {data.type}에 맞는 TraitData가 allTraits에 없습니다.",
                    this
                );
                continue;
            }

            PlayerTrait playerTrait =
                new PlayerTrait();

            playerTrait.trait = trait;
            playerTrait.level =
                Mathf.Clamp(
                    data.level,
                    0,
                    trait.maxLevel
                );

            playerTraits.Add(playerTrait);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance
                .permanentMoney
                .SetMoney(
                    saveManager.saveData.permanentMoney
                );
        }

        EnsureReferences();

        if (player != null)
        {
            player.SetFlowerLeaf(
                saveManager.saveData.permanentMoney
            );
        }

        ApplyAllTraits();
    }

    public int GetStartGold()
    {
        EnsureReferences();

        PlayerTrait playerTrait =
            playerTraits.Find(
                x => x != null &&
                     x.trait != null &&
                     x.trait.type ==
                     TraitType.StartGold
            );

        int baseStartGold = 100;

        if (playerTrait == null)
        {
            return baseStartGold;
        }

        float multiplier =
            1f +
            playerTrait.level *
            playerTrait.trait.valuePerLevel;

        return Mathf.RoundToInt(
            baseStartGold * multiplier
        );
    }

    public int GetTraitCurrency()
    {
        EnsureReferences();

        if (player != null)
        {
            return player.FlowerLeaf;
        }

        return GameManager.Instance != null &&
               GameManager.Instance.permanentMoney != null
            ? GameManager.Instance
                .permanentMoney
                .CurrentMoney
            : 0;
    }

    private bool TrySpendTraitCurrency(
        int amount)
    {
        EnsureReferences();

        if (amount <= 0)
        {
            return false;
        }

        if (player != null)
        {
            bool spent =
                player.SpendFlowerLeaf(amount);

            if (spent &&
                GameManager.Instance != null &&
                GameManager.Instance.permanentMoney != null)
            {
                GameManager.Instance
                    .permanentMoney
                    .SetMoney(player.FlowerLeaf);
            }

            return spent;
        }

        return GameManager.Instance != null &&
               GameManager.Instance
                   .TrySpendPermanentMoney(amount);
    }
}

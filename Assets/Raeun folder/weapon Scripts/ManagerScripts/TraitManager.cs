using System.Collections.Generic;
using UnityEngine;

// 플레이어의 특성(업그레이드)을 관리하는 클래스
public class TraitManager : MonoBehaviour
{
    // 특성을 적용할 플레이어
    public Player player;

    // 플레이어가 구매한 특성 목록
    public List<PlayerTrait> playerTraits = new List<PlayerTrait>();

    // 저장 관리
    public SaveManager saveManager;

    // 게임에 존재하는 모든 특성 데이터
    public List<TraitData> allTraits;

    // 특성 구매
    public bool BuyTrait(TraitData trait)
    {
        // 현재 특성이 이미 있는지 검색
        PlayerTrait playerTrait = playerTraits.Find(x => x.trait == trait);

        // 처음 구매하는 특성이면 생성
        if (playerTrait == null)
        {
            playerTrait = new PlayerTrait();
            playerTrait.trait = trait;
            playerTrait.level = 0;

            playerTraits.Add(playerTrait);
        }

        // 최대 레벨인지 확인
        if (playerTrait.level >= trait.maxLevel)
        {
            Debug.Log("최대 레벨입니다.");
            return false;
        }

        // 현재 레벨의 구매 가격
        int price = GetPrice(trait, playerTrait.level);

        // 영구 재화가 부족하면 구매 실패
        if (!GameManager.Instance.TrySpendPermanentMoney(price))
        {
            Debug.Log("재화 부족");
            return false;
        }

        // 레벨 증가
        playerTrait.level++;

        // 시작 골드는 런 시작 시 적용하므로 제외
        if (trait.type != TraitType.StartGold)
        {
            ApplyTrait(trait, playerTrait.level);
        }

        // 변경 내용 저장
        GameManager.Instance.SaveGame();

        // 마지막에 UI 새로고침
        GameManager.Instance.hubManager.traitUI.RefreshUI();

        return true;
    }

    // 현재 레벨의 업그레이드 가격 반환
    public int GetPrice(TraitData trait, int currentLevel)
    {
        return Mathf.RoundToInt(trait.levelPrices[currentLevel]);
    }

    // 특성 효과를 플레이어에게 적용
    public void ApplyTrait(TraitData trait, int level)
    {
        switch (trait.type)
        {
            case TraitType.StartGold:
                // 시작 골드는 런 시작 시 사용
                break;

            case TraitType.MaxHp:
                player.stats.TraitBonusStats.maxHp =
                    Mathf.RoundToInt(trait.valuePerLevel * level);
                break;

            case TraitType.Attack:
                player.stats.TraitBonusStats.attack =
                    Mathf.RoundToInt(trait.valuePerLevel * level);
                break;

            case TraitType.DeathResist:
                player.stats.TraitBonusStats.deathResist = level;
                break;

            case TraitType.CritChance:
                player.stats.TraitBonusStats.criticalChance =
                    trait.valuePerLevel * level;
                break;

            case TraitType.CritDamage:
                player.stats.TraitBonusStats.criticalMultiplier =
                    trait.valuePerLevel * level;
                break;

            case TraitType.ExtraDash:
                player.stats.TraitBonusStats.extraDashCount = level;
                break;

            case TraitType.GoldMultiplier:
                player.stats.TraitBonusStats.goldMultiplier =
                    trait.valuePerLevel * level;
                break;

            case TraitType.Reroll:
                player.stats.TraitBonusStats.rerollCount = level;
                break;
        }

        // 죽음 저항 수치 갱신
        player.RefreshDeathResist();
    }

    // 구매한 모든 특성 적용
    public void ApplyAllTraits()
    {
        foreach (PlayerTrait playerTrait in playerTraits)
        {
            // 시작 골드는 제외
            if (playerTrait.trait.type == TraitType.StartGold)
            {
                continue;
            }

            ApplyTrait(
                playerTrait.trait,
                playerTrait.level);
        }
    }

    // 현재 특성 정보를 저장 데이터에 복사
    public void SaveTraits()
    {
        saveManager.saveData.traits.Clear();

        foreach (PlayerTrait playerTrait in playerTraits)
        {
            TraitSaveData data = new TraitSaveData();

            data.type = playerTrait.trait.type;
            data.level = playerTrait.level;

            saveManager.saveData.traits.Add(data);
        }
    }

    // 특정 특성 정보 가져오기
    public PlayerTrait GetPlayerTrait(TraitData trait)
    {
        return playerTraits.Find(x => x.trait == trait);
    }

    // 현재 특성 레벨 반환
    public int GetTraitLevel(TraitData trait)
    {
        PlayerTrait playerTrait = GetPlayerTrait(trait);

        return playerTrait == null
            ? 0
            : playerTrait.level;
    }

    // 구매 가능한지 확인
    public bool CanBuyTrait(TraitData trait)
    {
        int level = GetTraitLevel(trait);

        // 최대 레벨이면 구매 불가
        if (level >= trait.maxLevel)
        {
            return false;
        }

        // 현재 돈이 가격 이상인지 확인
        return GameManager.Instance.permanentMoney.CurrentMoney >=
            GetPrice(trait, level);
    }

    // 저장된 특성 불러오기
    public void LoadTraits()
    {
        // 기존 데이터 초기화
        playerTraits.Clear();

        // 저장된 특성 복원
        foreach (TraitSaveData data in saveManager.saveData.traits)
        {
            // 같은 타입의 TraitData 찾기
            TraitData trait =
                allTraits.Find(x => x.type == data.type);

            if (trait == null)
            {
                continue;
            }

            PlayerTrait playerTrait = new PlayerTrait();

            playerTrait.trait = trait;
            playerTrait.level = data.level;

            playerTraits.Add(playerTrait);
        }

        // 영구 재화 복원
        GameManager.Instance.permanentMoney.SetMoney(
            saveManager.saveData.permanentMoney);

        // 특성 효과 다시 적용
        ApplyAllTraits();
    }

    // 시작 골드 특성 값 반환
    public int GetStartGold()
    {
        PlayerTrait playerTrait =
            playerTraits.Find(
                x => x.trait.type == TraitType.StartGold);

        // 구매하지 않았다면 0
        if (playerTrait == null)
        {
            return 0;
        }

        // 시작 골드 계산
        return Mathf.RoundToInt(
            playerTrait.trait.valuePerLevel *
            playerTrait.level);
    }
}
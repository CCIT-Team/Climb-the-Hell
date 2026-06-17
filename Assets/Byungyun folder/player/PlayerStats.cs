using UnityEngine;

[System.Serializable]
public class PlayerStats
{
    [Header("기본 스탯")]
    public int playerhp = 100;
    public int playerattack = 10;
    public int playerspeed = 5;
    public int playermana = 50;
    public int playerlv = 1;

    [Header("크리티컬")]
    public float playercritical = 0.1f;
    public float playercriticalPercent = 1.5f;

    [Header("현재 체력")]
    [HideInInspector]
    public int currentHp;

    public void Init() // 현재 체력
    {
        currentHp = playerhp;
    }

    public bool TakeDamage(int damage) // 공격 대미지 받기
    {
        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);
        return currentHp <= 0;
    }

    public void Heal(int amount) // 체력 회복 , 아직 구현 x 
    {
        currentHp = Mathf.Min(currentHp + amount, playerhp);
    }

    public bool IsDead() // 죽음 상태
    {
        return currentHp <= 0;
    }

    public int CalculateDamage() //  크리티컬 , 아직 구현 x 
    {
        bool isCritical = Random.value < playercritical;

        if (isCritical)
            return Mathf.RoundToInt(playerattack * playercriticalPercent);

        return playerattack;
    }
}
using UnityEngine;

[System.Serializable]
public class PlayerStats
{
    public int hp;
    public int attack;
    public int speed;
    public int mana;
    public int lv;
    public float critical;        // 크리티컬 확률 (0~1)
    public float criticalpersent; // 크리티컬 데미지 배율

    public PlayerStats(int hp, int attack, int speed, int mana, int lv, float critical, float criticalpersent)
    {
        this.hp           = hp;
        this.attack       = attack;
        this.speed        = speed;
        this.mana         = mana;
        this.lv           = lv;
        this.critical      = critical;
        this.criticalpersent = criticalpersent;
    }

    /// <summary>
    /// 기본 스탯으로 초기화
    /// </summary>
    public static PlayerStats Default()
    {
        return new PlayerStats(
            hp: 100,
            attack: 10,
            speed: 5,
            mana: 50,
            lv: 1,
            critical: 0.1f,
            criticalpersent: 1.5f
        );
    }
}

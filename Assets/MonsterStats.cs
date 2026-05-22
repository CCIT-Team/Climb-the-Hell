using UnityEngine;

[System.Serializable]
public class MonsterStats : MonoBehaviour
{
    public int mobhp;
    public int mobattack;
    public int mobspeed;
    public int mobmana;
    public int moblv;
    public float mobrange;

    public MonsterStats(int mobhp, int mobattack, int mobspeed, int mobmana, int moblv, float mobrange)
    {
        this.mobhp     = mobhp;
        this.mobattack = mobattack;
        this.mobspeed  = mobspeed;
        this.mobmana   = mobmana;
        this.moblv     = moblv;
        this.mobrange  = mobrange;
    }

    /// <summary>
    /// MobTakeDamage: 데미지를 받아 hp 감소, 사망 여부 반환
    /// </summary>
    public bool MobTakeDamage(int damage)
    {
        mobhp -= damage;
        mobhp = Mathf.Max(mobhp, 0);
        return mobhp <= 0;
    }

    public bool IsDead() => mobhp <= 0;
}

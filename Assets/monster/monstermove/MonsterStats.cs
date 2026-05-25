using UnityEngine;

public abstract class MonsterStats : MonoBehaviour
{
    [Header("기본 스탯")]
    public int monsterhp = 50;
    public int monsterattack = 10;
    public float monsterspeed = 3f;
    public int monstermana = 0;
    public int monsterlv = 1;
    public float monsterrange = 3f;

    [Header("현재 체력")]
    public int currentHp;

    protected virtual void Awake()
    {
        currentHp = monsterhp;
    }

    public virtual bool TakeDamage(int damage)
    {
        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);

        return currentHp <= 0;
    }

    public bool IsDead()
    {
        return currentHp <= 0;
    }
}
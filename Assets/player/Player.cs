using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("player data")]
    public PlayerStats stats = new PlayerStats();

    [Header("player state")]
    public Vector3 playerposition;
    public int exp;
    
    private bool isInvincible = false;

    public System.Action OnDeath;
    public System.Action<int> OnHpChanged;

    private void Awake()
    {
        stats.Init();
    }

    public void Attack()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            1.5f,
            LayerMask.GetMask("Monster")
        );

        foreach (Collider hit in hits)
        {
            MonsterAI monster = hit.GetComponentInParent<MonsterAI>();

            if (monster != null && monster.IsAlive())
            {
                DealDamage(monster);
            }
        }
    }

    private void DealDamage(MonsterAI monster)
    {
        int finalDamage = stats.CalculateDamage();

        monster.TakeDamage(finalDamage);

        Debug.Log($"[Player] {monster.MonsterName}에게 {finalDamage} 데미지");
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;
        if (stats.IsDead()) return;

        bool dead = stats.TakeDamage(damage);

        OnHpChanged?.Invoke(stats.currentHp);

        Debug.Log($"[Player] 피격 — 남은 HP: {stats.currentHp}/{stats.playerhp}");

        if (dead)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibleFrame(0.5f));
        }
    }

    public void HealHp(int amount)
    {
        stats.Heal(amount);

        OnHpChanged?.Invoke(stats.currentHp);

        Debug.Log($"[Player] HP 회복 +{amount} → {stats.currentHp}/{stats.playerhp}");
    }

    public void Reviver()
    {
        stats.Init();
        isInvincible = false;

        OnHpChanged?.Invoke(stats.currentHp);

        Debug.Log("[Player] 부활");
    }

    public void AddExp(int amount)
    {
        exp += amount;

        Debug.Log($"[Player] 경험치 +{amount} (총 {exp})");

        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        int expNeeded = stats.playerlv * 100;

        if (exp >= expNeeded)
        {
            exp -= expNeeded;

            stats.playerlv++;
            stats.playerattack += 2;
            stats.playerhp += 10;

            stats.Init();

            OnHpChanged?.Invoke(stats.currentHp);

            Debug.Log($"[Player] 레벨업! Lv.{stats.playerlv}");
        }
    }

    private void Die()
    {
        Debug.Log("[Player] 사망");

        OnDeath?.Invoke();

        Time.timeScale = 0f;
    }

    private IEnumerator InvincibleFrame(float duration)
    {
        isInvincible = true;

        yield return new WaitForSeconds(duration);

        isInvincible = false;
    }

    public int GetCurrentHp()
    {
        return stats.currentHp;
    }

    public bool IsAlive()
    {
        return !stats.IsDead();
    }
}
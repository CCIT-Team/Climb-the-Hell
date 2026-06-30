using System;
using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("플레이어 스탯")]
    public PlayerStats stats =
        new PlayerStats();

    [Header("돈 데이터")]
    public MoneyData money =
        new MoneyData();

    [Header("플레이어 상태")]
    public Vector3 playerPosition;
    public int exp;

    [Header("피격 무적")]
    [SerializeField]
    private float hitInvincibleDuration = 0.5f;

    private bool isHitInvincible;
    private bool isDashInvincible;

    private Coroutine hitInvincibleCoroutine;

    public event Action OnDeath;
    public event Action<int> OnHpChanged;

    private void Awake()
    {
        stats.Init(true);
    }

    private void Update()
    {
        playerPosition =
            transform.position;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 ||
            stats.IsDead() ||
            IsInvincible())
        {
            return;
        }

        bool dead =
            stats.TakeDamage(damage);

        OnHpChanged?.Invoke(
            stats.CurrentHp
        );

        Debug.Log(
            $"[Player] 피격 {damage} / " +
            $"HP {stats.CurrentHp}/{stats.MaxHp}",
            this
        );

        if (dead)
        {
            Die();
            return;
        }

        StartHitInvincible();
    }

    public void HealHp(int amount)
    {
        if (amount <= 0 ||
            stats.IsDead())
        {
            return;
        }

        stats.Heal(amount);

        OnHpChanged?.Invoke(
            stats.CurrentHp
        );
    }

    public void SetInvincible(bool value)
    {
        isDashInvincible = value;
    }

    public bool IsInvincible()
    {
        return isHitInvincible ||
               isDashInvincible;
    }

    public void AddExp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        exp += amount;
    }

    public void Revive()
    {
        StopHitInvincible();

        isDashInvincible = false;

        stats.Init(true);

        OnHpChanged?.Invoke(
            stats.CurrentHp
        );
    }

    private void StartHitInvincible()
    {
        StopHitInvincible();

        hitInvincibleCoroutine =
            StartCoroutine(
                HitInvincibleRoutine()
            );
    }

    private IEnumerator HitInvincibleRoutine()
    {
        isHitInvincible = true;

        yield return new WaitForSeconds(
            hitInvincibleDuration
        );

        isHitInvincible = false;
        hitInvincibleCoroutine = null;
    }

    private void StopHitInvincible()
    {
        if (hitInvincibleCoroutine != null)
        {
            StopCoroutine(
                hitInvincibleCoroutine
            );

            hitInvincibleCoroutine = null;
        }

        isHitInvincible = false;
    }

    private void Die()
    {
        StopHitInvincible();

        isDashInvincible = false;

        Debug.Log(
            "[Player] 사망",
            this
        );

        OnDeath?.Invoke();
    }

    public int GetCurrentHp()
    {
        return stats.CurrentHp;
    }

    public int GetMaxHp()
    {
        return stats.MaxHp;
    }

    public bool IsAlive()
    {
        return !stats.IsDead();
    }

    private void OnDisable()
    {
        StopHitInvincible();
        isDashInvincible = false;
    }
}
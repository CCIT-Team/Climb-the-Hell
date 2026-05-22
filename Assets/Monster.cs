using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monster: 이동/AI 로직은 기존 MonsterAi에 유지, 전투 관련 메서드만 작성
/// </summary>
public class Monster : MonoBehaviour
{
    // ── 다이어그램 필드 ──────────────────────────────
    public MonsterStats         monsterstats;
    public int                  rewardExp;
    //public List<Item>           rewardItem;
    public int                  rewardGold;
    public float                rewardflowerleaf;
    public string               MonsterName;
    public int                  AttackSpeed;   // 공격 간격 (ms 단위, 다이어그램 기준)
    public float                moverange;
    public float                range;         // 공격 사거리

    // ── 내부 상태 ────────────────────────────────────
    private float  attackTimer = 0f;

    // ── 참조 ─────────────────────────────────────────
    private Player targetPlayer;

    // ────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Start()
    {
        // MonsterStats 미할당 시 기본값으로 자동 초기화
        // Inspector에서 직접 값을 채워넣으면 그 값이 우선 적용됨
        if (monsterstats == null)
        {
            monsterstats = new MonsterStats(
                mobhp: 50, mobattack: 5, mobspeed: 3,
                mobmana: 0, moblv: 1, mobrange: 2f
            );
            Debug.LogWarning($"[Monster:{MonsterName}] monsterstats 미할당 → 기본값으로 초기화");
        }

        targetPlayer = FindObjectOfType<Player>();

        if (targetPlayer == null)
            Debug.LogWarning($"[Monster:{MonsterName}] 씬에서 Player를 찾을 수 없음");
    }

    private void Update()
    {
        if (monsterstats == null) return;
        if (monsterstats.IsDead()) return;
        if (targetPlayer == null || !targetPlayer.IsAlive()) return;

        HandleAttackTimer();
    }

    #endregion

    // ────────────────────────────────────────────────
    #region 전투 메서드

    /// <summary>
    /// 공격 타이머 — AttackSpeed(ms)를 초 단위로 환산
    /// AttackSpeed가 0이면 1초 간격으로 fallback
    /// </summary>
    private void HandleAttackTimer()
    {
        float attackInterval = AttackSpeed > 0 ? AttackSpeed / 1000f : 1f;
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            TryAttackPlayer();
        }
    }

    /// <summary>
    /// 사거리 내에 있으면 플레이어 공격
    /// </summary>
    private void TryAttackPlayer()
    {
        float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (distance <= range)
            Attack();
    }

    /// <summary>
    /// 플레이어에게 데미지 적용
    /// </summary>
    public void Attack()
    {
        if (targetPlayer == null || monsterstats == null) return;

        int damage = monsterstats.mobattack;
        targetPlayer.TakeDamage(damage);
        Debug.Log($"[Monster:{MonsterName}] 플레이어 공격 — {damage} 데미지");
    }

    /// <summary>
    /// 피격 처리
    /// </summary>
    public void OnHit(int damage)
    {
        if (monsterstats == null) return;

        bool isDead = monsterstats.MobTakeDamage(damage);
        Debug.Log($"[Monster:{MonsterName}] 피격 {damage} — 남은 HP: {monsterstats.mobhp}");

        if (isDead)
        {
            DropReward();
            Die();
        }
    }

    /// <summary>
    /// 보상 드롭
    /// </summary>
    public void DropReward()
    {
        Debug.Log($"[Monster:{MonsterName}] 보상 드롭 — Gold:{rewardGold}, Exp:{rewardExp}");
        // MonsterSpawner.killmonster(), Money.AddGold() 등 완성 후 연결
    }

    private void Die()
    {
        Debug.Log($"[Monster:{MonsterName}] 사망");
        Destroy(gameObject, 0.5f);
    }

    #endregion

    // ────────────────────────────────────────────────
    #region 공개 유틸

    public bool IsAlive() => monsterstats != null && !monsterstats.IsDead();

    #endregion
}
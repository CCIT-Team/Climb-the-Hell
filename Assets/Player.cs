using System.Collections;
using UnityEngine;

/// <summary>
/// Player: 이동 로직은 기존 코드 유지, 전투 관련 메서드만 작성
/// </summary>
public class Player : MonoBehaviour
{
    // ── 다이어그램 필드 ──────────────────────────────
    public PlayerStats stats;
    //public Inventory   inventory;        // 추후 연결
    public Vector3     playerposition;
    public int         exp;
    //public Money       money;            // 추후 연결
    //public Dash        dash;             // 추후 연결

    // ── 전투 내부 상태 ────────────────────────────────
    private int  currentHp;
    private bool isInvincible = false;  // Dash 무적과 별개로 피격 무적 관리

    // ── 이벤트 ──────────────────────────────────────
    public System.Action         OnDeath;
    public System.Action<int>    OnHpChanged;   // UI 연동용

    // ────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        // 스탯이 Inspector에서 세팅되지 않은 경우 기본값 사용
        if (stats == null)
            stats = PlayerStats.Default();

        currentHp = stats.hp;
    }

    #endregion

    // ────────────────────────────────────────────────
    #region 다이어그램 메서드

    /// <summary>
    /// 기본 공격 — 범위 내 Monster를 탐색해 데미지 적용
    /// </summary>
    public void Attack()
    {
        // 공격 범위 내 몬스터 감지 (레이어 설정은 프로젝트에 맞게 수정)
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f,
                            LayerMask.GetMask("Monster"));

        foreach (var hit in hits)
        {
            Monster monster = hit.GetComponent<Monster>();
            if (monster != null)
                DealDamage(monster);
        }
    }

    /// <summary>
    /// 스킬 사용 — 마나 소모 후 Skill 실행
    /// </summary>
    // public void UseSkill(Skill skill)
    // {
    //     if (skill == null) return;

    //     if (!skill.SkillCanUse(stats.mana))
    //     {
    //         Debug.Log("마나 부족");
    //         return;
    //     }

    //     skill.Use(stats.mana);  // 마나 소모
    //     skill.SkillUse();       // 스킬 효과 실행
    // }

    /// <summary>
    /// 피격 처리
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        currentHp -= damage;
        currentHp  = Mathf.Max(currentHp, 0);

        OnHpChanged?.Invoke(currentHp);
        Debug.Log($"[Player] 피격 — 남은 HP: {currentHp}/{stats.hp}");

        if (currentHp <= 0)
            Die();
        else
            StartCoroutine(InvincibleFrame(0.5f)); // 피격 후 짧은 무적
    }

    /// <summary>
    /// 부활 (Trait.revive 연동 상정)
    /// </summary>
    public void Reviver()
    {
        currentHp = stats.hp;
        isInvincible = false;
        OnHpChanged?.Invoke(currentHp);
        Debug.Log("[Player] 부활");
    }

    /// <summary>
    /// 경험치 획득 및 레벨업 체크
    /// </summary>
    public void AddExp(int amount)
    {
        exp += amount;
        Debug.Log($"[Player] 경험치 +{amount} (총 {exp})");
        CheckLevelUp();
    }

    #endregion

    // ────────────────────────────────────────────────
    #region 전투 내부 로직

    /// <summary>
    /// 몬스터에게 실제 데미지 계산 후 적용
    /// </summary>
    private void DealDamage(Monster monster)
    {
        int finalDamage = CalculateDamage();
        bool isDead = monster.monsterstats.MobTakeDamage(finalDamage);

        Debug.Log($"[Player] {monster.MonsterName}에게 {finalDamage} 데미지" +
                  (IsCriticalHit() ? " (크리티컬!)" : ""));

        if (isDead)
        {
            monster.DropReward();
            AddExp(monster.rewardExp);
        }
    }

    /// <summary>
    /// 크리티컬 여부 판정 후 최종 데미지 반환
    /// </summary>
    private int CalculateDamage()
    {
        if (IsCriticalHit())
            return Mathf.RoundToInt(stats.attack * stats.criticalpersent);

        return stats.attack;
    }

    private bool IsCriticalHit() => Random.value < stats.critical;

    /// <summary>
    /// 사망 처리
    /// </summary>
    private void Die()
    {
        Debug.Log("[Player] 사망");
        OnDeath?.Invoke();
        // GameManager.Instance.GameOver() 호출은 GameManager 완성 후 연결
    }

    /// <summary>
    /// 피격 무적 프레임
    /// </summary>
    private IEnumerator InvincibleFrame(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
    }

    /// <summary>
    /// 레벨업 조건 체크 (간단한 선형 공식 — 조정 가능)
    /// </summary>
    private void CheckLevelUp()
    {
        int expNeeded = stats.lv * 100;
        if (exp >= expNeeded)
        {
            exp -= expNeeded;
            stats.lv++;
            stats.attack += 2;
            stats.hp     += 10;
            currentHp     = stats.hp; // 레벨업 시 HP 풀 회복
            Debug.Log($"[Player] 레벨업! Lv.{stats.lv}");
        }
    }

    #endregion

    // ────────────────────────────────────────────────
    #region 공개 유틸

    public int  GetCurrentHp()   => currentHp;
    public bool IsAlive()        => currentHp > 0;

    /// <summary>
    /// Potion 사용 시 HP 회복 (Inventory에서 호출 상정)
    /// </summary>
    public void HealHp(int amount)
    {
        currentHp = Mathf.Min(currentHp + amount, stats.hp);
        OnHpChanged?.Invoke(currentHp);
        Debug.Log($"[Player] HP 회복 +{amount} → {currentHp}/{stats.hp}");
    }

    #endregion
}

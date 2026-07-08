using System;
using System.Collections;
using UnityEngine;

// 플레이어의 체력, 경험치, 돈, 피격, 사망 등을 관리하는 클래스
public class Player : MonoBehaviour, IDamageable
{
    [Header("플레이어 스탯")]
    // 플레이어의 기본 스탯(HP, 공격력 등)
    public PlayerStats stats =
        new PlayerStats();

    [Header("돈 데이터")]
    // 플레이어가 가지고 있는 돈
    public MoneyData money =
        new MoneyData();

    [Header("플레이어 상태")]
    // 현재 위치
    public Vector3 playerPosition;

    // 경험치
    public int exp;

    [Header("피격 무적")]
    // 피격 후 무적 시간
    [SerializeField]
    private float hitInvincibleDuration = 0.5f;

    // 피격 무적인지 여부
    private bool isHitInvincible;

    // 대시 등으로 인한 무적인지 여부
    private bool isDashInvincible;

    // 피격 무적 코루틴 저장
    private Coroutine hitInvincibleCoroutine;

    // 플레이어 사망 이벤트
    public event Action OnDeath;

    // HP가 변경될 때 UI 등에 알리는 이벤트
    public event Action<int> OnHpChanged;

    // 남은 부활 횟수(죽음 저항)
    private int remainDeathResist;

    // 득도(수비 계열) 피해 감소율을 읽기 위한 참조
    private BoonInfo boonInfo;

    private void Awake()
    {
        // 플레이어 스탯 초기화
        stats.Init(true);

        // 죽음 저항 횟수 초기화
        remainDeathResist = stats.DeathResist;

        // 득도 피해 감소를 적용하려면 BoonInfo 참조가 필요
        boonInfo = GetComponent<BoonInfo>();

        if (boonInfo == null)
        {
            boonInfo = GetComponentInChildren<BoonInfo>(true);
        }
    }

    private void Update()
    {
        // 현재 위치 저장
        playerPosition = transform.position;
    }

    // 데미지를 받았을 때 호출
    public void TakeDamage(int damage)
    {
        Debug.Log($"🩸 Player Hit: {damage}");

        // 데미지가 없거나, 이미 죽었거나, 무적이면 무시
        if (damage <= 0 ||
            stats.IsDead() ||
            IsInvincible())
        {
            return;
        }

        // 수비 계열 득도의 피해 감소율 적용
        if (boonInfo != null)
        {
            float reductionRate =
                boonInfo.GetTotalDamageReductionRate();

            if (reductionRate > 0f)
            {
                damage =
                    Mathf.RoundToInt(
                        damage * (1f - reductionRate)
                    );
            }
        }

        // 실제 HP 감소
        bool dead =
            stats.TakeDamage(damage);

        // UI 갱신
        OnHpChanged?.Invoke(stats.CurrentHp);

        Debug.Log(
            $"[Player] 피격 {damage} / " +
            $"HP {stats.CurrentHp}/{stats.MaxHp}",
            this
        );

        // 죽었는지 확인
        if (dead)
        {
            // 죽음 저항이 남아있으면 자동 부활
            if (remainDeathResist > 0)
            {
                remainDeathResist--;

                ReviveByDeathResist();

                return;
            }

            // 완전히 사망
            Die();
            return;
        }

        // 피격 후 잠깐 무적
        StartHitInvincible();
    }

    // 죽음 저항으로 부활
    private void ReviveByDeathResist()
    {
        StopHitInvincible();

        isDashInvincible = false;

        // HP 전부 회복
        stats.Init(true);

        OnHpChanged?.Invoke(stats.CurrentHp);

        // 부활 후 잠시 무적
        StartHitInvincible();

        Debug.Log($"죽음 저항 발동! 남은 횟수 : {remainDeathResist}");
    }

    // 체력 회복
    public void HealHp(int amount)
    {
        if (amount <= 0 ||
            stats.IsDead())
        {
            return;
        }

        stats.Heal(amount);

        OnHpChanged?.Invoke(stats.CurrentHp);
    }

    // 대시 무적 설정
    public int ConsumeHp(
        int amount,
        int minimumHp = 1
    )
    {
        if (amount <= 0 ||
            stats.IsDead())
        {
            return 0;
        }

        int safeMinimum =
            Mathf.Clamp(
                minimumHp,
                0,
                stats.CurrentHp
            );

        int consumed =
            Mathf.Min(
                amount,
                Mathf.Max(
                    0,
                    stats.CurrentHp - safeMinimum
                )
            );

        if (consumed <= 0)
        {
            return 0;
        }

        stats.AddCurrentHp(-consumed);

        OnHpChanged?.Invoke(stats.CurrentHp);

        return consumed;
    }

    public void SetInvincible(bool value)
    {
        isDashInvincible = value;
    }

    // 현재 무적인지 확인
    public bool IsInvincible()
    {
        return isHitInvincible ||
               isDashInvincible;
    }

    // 경험치 획득
    public void AddExp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        exp += amount;
    }

    // 외부에서 호출하는 일반 부활
    public void Revive()
    {
        StopHitInvincible();

        isDashInvincible = false;

        stats.Init(true);

        Debug.Log($"남은 부활 횟수 : {remainDeathResist}");

        OnHpChanged?.Invoke(stats.CurrentHp);
    }

    // 피격 무적 시작
    private void StartHitInvincible()
    {
        // 기존 코루틴이 있으면 종료
        StopHitInvincible();

        hitInvincibleCoroutine =
            StartCoroutine(
                HitInvincibleRoutine()
            );
    }

    // 일정 시간 동안 피격 무적 유지
    private IEnumerator HitInvincibleRoutine()
    {
        isHitInvincible = true;

        yield return new WaitForSeconds(
            hitInvincibleDuration
        );

        isHitInvincible = false;
        hitInvincibleCoroutine = null;
    }

    // 피격 무적 종료
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

    // 플레이어 사망
    private void Die()
    {
        StopHitInvincible();

        isDashInvincible = false;

        Debug.Log(
            "[Player] 사망",
            this
        );

        // GameManager 등이 이 이벤트를 받아 게임 오버 처리
        OnDeath?.Invoke();
    }

    // 현재 HP 반환
    public int GetCurrentHp()
    {
        return stats.CurrentHp;
    }

    // 최대 HP 반환
    public int GetMaxHp()
    {
        return stats.MaxHp;
    }

    // 살아있는지 여부
    public bool IsAlive()
    {
        return !stats.IsDead();
    }

    private void OnDisable()
    {
        // 오브젝트 비활성화 시 무적 상태 초기화
        StopHitInvincible();
        isDashInvincible = false;
    }

    // 스탯이 변경됐을 때 죽음 저항 횟수도 다시 적용
    public void RefreshDeathResist()
    {
        remainDeathResist = stats.DeathResist;
    }
}

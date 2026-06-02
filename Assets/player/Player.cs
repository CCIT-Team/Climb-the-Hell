using System.Collections;
using UnityEngine;

// 플레이어의 공격, 체력, 경험치, 사망 처리를 담당하는 스크립트
public class Player : MonoBehaviour
{
    [Header("player data")]
    // 플레이어 능력치 데이터
    public PlayerStats stats = new PlayerStats();

    [Header("player state")]
    // 플레이어 위치 저장용 변수
    public Vector3 playerposition;

    [Header("player control")]
    // 플레이어 컨트롤러 참조
    public PlayerController playerController = new PlayerController();


    // 현재 경험치
    public int exp;
    
    // 피격 후 잠깐 무적 상태인지 확인
    private bool isInvincible = false;

    // 사망 시 실행될 이벤트
    public System.Action OnDeath;

    // HP 변경 시 실행될 이벤트
    public System.Action<int> OnHpChanged;

    private void Awake()
    {
        // 플레이어 스탯 초기화
        stats.Init();
    }

    public void Attack()
    {
        // 플레이어 주변 1.5 범위 안의 Monster 레이어를 가진 콜라이더 탐색
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            1.5f,
            LayerMask.GetMask("Monster")
        );

        foreach (Collider hit in hits)
        {
            // 감지된 콜라이더의 부모에서 MonsterAI 찾기
            MonsterAI monster = hit.GetComponentInParent<MonsterAI>();

            // 몬스터가 있고 살아있으면 공격
            if (monster != null && monster.IsAlive())
            {
                DealDamage(monster);
            }
        }
    }

    private void DealDamage(MonsterAI monster)
    {
        // 최종 데미지 계산 후 몬스터에게 전달 < Calculatedamage 대미지 추가함
        int finalDamage = stats.CalculateDamage();

        monster.TakeDamage(finalDamage);

        Debug.Log($"[Player] {monster.MonsterName}에게 {finalDamage} 데미지");
    }

    public void TakeDamage(int damage) // 대미지 받는거
    {
        // 무적, 죽음 상태면 대미지 무시
        if (isInvincible || stats.IsDead())
        {
            Debug.Log("[Player] 무적/죽음 상태로 피격 무시");
            return;
        }
        else
        {
            // 체력 감소 처리
            bool dead = stats.TakeDamage(damage);

            // HP UI 갱신용 이벤트 호출
            OnHpChanged?.Invoke(stats.currentHp);

            Debug.Log($"[Player] 피격 — 남은 HP: {stats.currentHp}/{stats.playerhp}");
        }
    }

    public void HealHp(int amount)
    {
        // 체력 회복 < 구현 x 
        stats.Heal(amount);

        OnHpChanged?.Invoke(stats.currentHp);

        Debug.Log($"[Player] HP 회복 +{amount} → {stats.currentHp}/{stats.playerhp}");
    }

    public void Reviver()
    {
        // 플레이어 부활 및 스탯 초기화 < 구현 x 
        stats.Init();
        isInvincible = false;

        OnHpChanged?.Invoke(stats.currentHp);

        Debug.Log("[Player] 부활");
    }

    public void AddExp(int amount)
    {
        // 경험치 추가 후 레벨업 확인 < 구현 x 
        exp += amount;

        Debug.Log($"[Player] 경험치 +{amount} (총 {exp})");

        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        // 현재 레벨 기준 필요 경험치 계산
        int expNeeded = stats.playerlv * 100;

        if (exp >= expNeeded)
        {
            exp -= expNeeded;

            // 레벨업 보상
            stats.playerlv++;
            stats.playerattack += 2;
            stats.playerhp += 10;

            // 체력 포함 스탯 재초기화
            stats.Init();

            OnHpChanged?.Invoke(stats.currentHp);

            Debug.Log($"[Player] 레벨업! Lv.{stats.playerlv}");
        }
    }

    private void Die()
    {
        // 사망 이벤트 실행 후 게임 정지
        Debug.Log("[Player] 사망");

        OnDeath?.Invoke();

        Time.timeScale = 0f;
    }

    private IEnumerator InvincibleFrame(float duration)
    {
        // 일정 시간 동안 무적 처리
        isInvincible = true;

        yield return new WaitForSeconds(duration);

        isInvincible = false;
    }

    public int GetCurrentHp()
    {
        // 현재 HP 반환
        return stats.currentHp;
    }

    public bool IsAlive()
    {
        // 생존 여부 반환
        return !stats.IsDead();
    }
}
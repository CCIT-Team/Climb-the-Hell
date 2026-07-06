using System;
using UnityEngine;

// 돈(재화) 데이터를 관리하는 클래스
[Serializable]
public class MoneyData
{
    [SerializeField]
    [Min(0)]
    // 현재 보유한 돈
    private int currentMoney;

    // 현재 돈을 읽기 전용으로 반환
    public int CurrentMoney => currentMoney;

    // 돈이 변경되었을 때 호출되는 이벤트
    public event Action<int> OnMoneyChanged;

    // 현재 돈을 지정한 값으로 설정
    public void SetMoney(int amount)
    {
        // 음수가 되지 않도록 보정
        currentMoney = Mathf.Max(0, amount);

        // UI 등에 변경 사실 알림
        OnMoneyChanged?.Invoke(currentMoney);
    }

    // 돈 추가
    public void AddMoney(int amount)
    {
        // 0 이하의 값은 무시
        if (amount <= 0)
        {
            return;
        }

        // 돈 증가
        currentMoney += amount;

        // UI 갱신 이벤트 호출
        OnMoneyChanged?.Invoke(currentMoney);
    }

    // 돈을 사용할 수 있는지 확인
    public bool CanSpend(int amount)
    {
        return amount >= 0 &&
               currentMoney >= amount;
    }

    // 돈 사용
    public bool TrySpend(int amount)
    {
        // 돈이 부족하면 실패
        if (!CanSpend(amount))
        {
            return false;
        }

        // 돈 차감
        currentMoney -= amount;

        // UI 갱신 이벤트 호출
        OnMoneyChanged?.Invoke(currentMoney);

        return true;
    }
}
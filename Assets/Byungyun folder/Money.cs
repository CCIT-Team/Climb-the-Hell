using System;
using UnityEngine;

[Serializable]
public class MoneyData
{
    [SerializeField]
    [Min(0)]
    private int currentMoney;

    public int CurrentMoney => currentMoney;

    public event Action<int> OnMoneyChanged;

    public void SetMoney(int amount)
    {
        currentMoney =
            Mathf.Max(0, amount);

        OnMoneyChanged?.Invoke(
            currentMoney
        );
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentMoney += amount;

        OnMoneyChanged?.Invoke(
            currentMoney
        );
    }

    public bool CanSpend(int amount)
    {
        return amount >= 0 &&
               currentMoney >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (!CanSpend(amount))
        {
            return false;
        }

        currentMoney -= amount;

        OnMoneyChanged?.Invoke(
            currentMoney
        );

        return true;
    }
}

using UnityEngine;

[System.Serializable]
public class Money
{
    public int   gold;
    public int   flowerleaf;
    public float CollectionGold;

    // ────────────────────────────────────────────────
    #region Gold

    /// <summary>
    /// 골드 추가
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        CollectionGold += amount; // 누적 획득량 기록
        Debug.Log($"[Money] Gold +{amount} → 현재: {gold} (총 획득: {CollectionGold})");
    }

    /// <summary>
    /// 골드 소모 — 성공 여부 반환
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0) return false;
        if (gold < amount)
        {
            Debug.Log($"[Money] Gold 부족 — 필요: {amount}, 보유: {gold}");
            return false;
        }
        gold -= amount;
        Debug.Log($"[Money] Gold -{amount} → 현재: {gold}");
        return true;
    }

    #endregion

    // ────────────────────────────────────────────────
    #region Flowerleaf

    /// <summary>
    /// 꽃잎 추가
    /// </summary>
    public void AddFlowerleaf(int amount)
    {
        if (amount <= 0) return;
        flowerleaf += amount;
        Debug.Log($"[Money] Flowerleaf +{amount} → 현재: {flowerleaf}");
    }

    /// <summary>
    /// 꽃잎 소모 — 성공 여부 반환
    /// </summary>
    public bool SpendFlowerleaf(int amount)
    {
        if (amount <= 0) return false;
        if (flowerleaf < amount)
        {
            Debug.Log($"[Money] Flowerleaf 부족 — 필요: {amount}, 보유: {flowerleaf}");
            return false;
        }
        flowerleaf -= amount;
        Debug.Log($"[Money] Flowerleaf -{amount} → 현재: {flowerleaf}");
        return true;
    }

    #endregion
}

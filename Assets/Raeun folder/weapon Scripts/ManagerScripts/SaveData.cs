using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // 영구 재화
    public int permanentMoney;

    // 특성 레벨
    public List<TraitSaveData> traits = new();
}
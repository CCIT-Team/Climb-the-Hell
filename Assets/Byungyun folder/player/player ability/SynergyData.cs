using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CategoryRequirement
{
    public AbilityCategory category;
    public int requiredCount;
}

[System.Serializable]
public class SynergyEffectData
{
    public SynergyEffectType effectType;
    public float value;
    public float duration;
}

[CreateAssetMenu(
    fileName = "NewSynergy",
    menuName = "Game/Synergy"
)]
public class SynergyData : ScriptableObject
{
    public string synergyId;
    public string synergyName;

    [TextArea]
    public string description;

    [Header("발동 조건")]
    public List<CategoryRequirement> requirements = new();

    [Header("발동 효과")]
    public List<SynergyEffectData> effects = new();
}
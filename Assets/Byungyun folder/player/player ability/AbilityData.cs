using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewAbility",
    menuName = "Game/Ability"
)]
public class AbilityData : ScriptableObject
{
    [Header("기본 정보")]
    public string abilityId;
    public string abilityName;

    [TextArea]
    public string description;

    public Sprite icon;

    [Header("분류")]
    public AbilityCategory category;

    [Header("세부 태그")]
    public List<AbilityTag> tags = new();

    [Header("수치")]
    public float value;
}
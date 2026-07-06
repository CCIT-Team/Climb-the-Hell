using UnityEngine;

// 프로젝트에서 "Create > Trait" 메뉴로 생성 가능한 ScriptableObject
[CreateAssetMenu(menuName = "Trait")]
public class TraitData : ScriptableObject
{
    // 특성 이름
    public string traitName;

    // 특성 종류
    public TraitType type;

    // 특성의 최대 레벨
    public int maxLevel;

    // 레벨당 증가하는 능력치
    public float valuePerLevel;

    // 각 레벨 업그레이드 비용
    public float[] levelPrices;
}
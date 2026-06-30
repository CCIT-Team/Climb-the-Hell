using UnityEngine;

[CreateAssetMenu(menuName = "Trait")]
public class TraitData : ScriptableObject
{
    public TraitType type;
    public int maxLevel;
    public float valuePerLevel;

    // Lv1, Lv2, Lv3...
    public int[] levelPrices;

    
}
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/DoT")]
public class DotEffect : ScriptableObject
{
    [Header("Zone DOT (안에 있을 때)")]
    public int zoneDamage = 5;
    public float zoneTick = 0.5f;

    [Header("Burn DOT (밖으로 나간 후)")]
    public int burnDamage = 2;
    public float burnDuration = 5f;
    public float burnTick = 1f;
}
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/DoT")]
public class DotEffect : ScriptableObject
{
    public float damage;
    public float duration;
    public float tickInterval;
}
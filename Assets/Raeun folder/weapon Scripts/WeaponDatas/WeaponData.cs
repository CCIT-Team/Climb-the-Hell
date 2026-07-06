using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Data", menuName = "Weapon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;   //무기 이름
    public float attack;   //무기 공격
    public float attackSpeed;   //무기 공격 속도
    public float cooldown;      //무기 공격 쿨다운
}
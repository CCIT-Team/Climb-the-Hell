using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Data", menuName = "Weapon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;   //무기 이름
    public int damage;          //무기 공격력
    public float attackSpeed;   //무기 공격 속도
    public float range;         //무기 공격 범위
    public float cooldown;      //무기 공격 쿨다운
}
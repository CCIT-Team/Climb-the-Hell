using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private MeleeWeapon sword;
    [SerializeField] private RangeWeapon gun;

    private void Update()
    {
        HandleAttack();
    }

    private void HandleAttack()
    {
        // 좌클릭
        if (Input.GetMouseButtonDown(0))
        {
            // 근접무기 공격
            if (sword != null && sword.gameObject.activeSelf)
            {
                sword.UseSwing();
            }

            // 원거리무기 공격
            if (gun != null && gun.gameObject.activeSelf)
            {
                gun.Shoot();
            }
        }
    }
}
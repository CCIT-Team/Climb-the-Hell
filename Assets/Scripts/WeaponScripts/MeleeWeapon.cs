using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Weapon
{
    void Start()
    {
        range.enabled = false;
        trailEffect.enabled = false;
    }

    public void UseSwing()
    {
        if (isAttacking == true)
            return;

        StartCoroutine(Swing());
    }

    IEnumerator Swing()
    {
        isAttacking = true;
        yield return new WaitForSeconds(0.1f);
        range.enabled = true;
        trailEffect.enabled = true;
        yield return new WaitForSeconds(0.3f);
        range.enabled = false;
        trailEffect.enabled = false;
        isAttacking = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            //Enemy enemy = other.GetComponent<Enemy>();

            //if (enemy != null)
            //{
            //    enemy.TakeDamage(damage);
            //}

            Debug.Log("Melee Hit Enemy");
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isAttacking = false;
        range.enabled = false;
        trailEffect.enabled = false;
    }
}

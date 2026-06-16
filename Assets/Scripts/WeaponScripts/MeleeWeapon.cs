using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//근거리 무기
public class MeleeWeapon : Weapon
{
    //시작할 때 range와 trailEffect 비활성화
    void Start()
    {
        range.enabled = false;
        trailEffect.enabled = false;
    }

    //공격 중이 아니면 Swing 코루틴 시작
    public void UseSwing()
    {
        if (isAttacking == true)
            return;

        StartCoroutine(Swing());
    }

    //swing 공격 중이면 range와 trailEffect 활성화, 공격 끝나면 비활성화
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

    //충돌 감지 몬스터 태그인 오브젝트와 충돌 시 데미지
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Monster"))
        {
            //Monster monster = other.GetComponent<Monster>();

            //if (monster != null)
            //{
            //    monster.TakeDamage(damage);
            //}

            Debug.Log("Melee Hit Monster");
        }
    }

    //무기가 비활성화될 때 모든 코루틴을 중지하고 공격 상태 초기화
    private void OnDisable()
    {
        StopAllCoroutines();
        isAttacking = false;
        range.enabled = false;
        trailEffect.enabled = false;
    }
}

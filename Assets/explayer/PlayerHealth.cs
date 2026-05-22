using JetBrains.Annotations;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int hp = 100;

    public void TakeDamage(int damage)
    {
        hp -= damage;

        Debug.Log("현재 체력 : " + hp);

        if (hp <= 0)
        {
            Die();
        }
    }

    void Die()
    {

        
        Debug.Log("플레이어 사망");
        Destroy(gameObject);
        
        
            


        
    }
}
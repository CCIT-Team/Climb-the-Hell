using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] protected TrailRenderer trailEffect; //공격 효과
    protected bool canAttack = true;    //공격 가능 여부
    protected bool isAttacking = false;   //공격 중 여부

    public PlayerStats data;

    public virtual void Use()
    {

    }

    //마우스 위치를 월드 좌표로 변환하는 함수
    protected Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }
}

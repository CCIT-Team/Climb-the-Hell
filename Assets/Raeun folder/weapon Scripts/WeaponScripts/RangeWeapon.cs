using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]

//원거리 무기
public class RangeWeapon : Weapon
{
    //[SerializeField] private ParticleSystem ShootingSystem;       //총알 발사 효과
    [SerializeField] private Transform BulletSpawnPoint;            //총알 발사 위치
    //[SerializeField] private ParticleSystem ImpactParticleSystem; //총알이 충돌할 때 효과
    [SerializeField] private TrailRenderer BulletTrail;             //총알 궤적 효과
    [SerializeField] private GameObject BulletTrailPrefab;          //총알 궤적 프리팹
    [SerializeField] private float ShootDelay = 0.5f;               //총알 발사 딜레이
    //[SerializeField] private LayerMask Mask;                      //레이캐스트 충돌 마스크

    private Animator animator;  //봐서 지워야 할 듯
    private float lastShootTime;    //마지막 총알 발사 시간

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Shoot()
    {
        //총알 발사 딜레이 체크
        if (Time.time > lastShootTime + ShootDelay)
        {
            //animator.SetTrigger("Shoot");
            //ShootingSystem.Play();
            Vector3 mousePos = GetMouseWorldPosition();     //마우스 위치를 월드 좌표로 변환

            Vector3 direction = (mousePos - BulletSpawnPoint.position).normalized;  //총알 발사 방향 계산

            //if (Physics.Raycast(BulletSpawnPoint.position, direction, out RaycastHit hit, float.MaxValue, Mask))
            //(임시) 레이캐스트를 사용하여 총알이 충돌한 지점과 충돌한 오브젝트 정보 가져오기
            if (Physics.Raycast(BulletSpawnPoint.position, direction, out RaycastHit hit))
            {
                Debug.Log(hit.collider.name);

                //*****이거 주석 풀어라/////Tag로 바꿔야함******
                //if (hit.collider.TryGetComponent<Monster>(out Monster monster))
                //{
                //    //monster.TakeDamage(damage);

                //    Debug.Log("Range Hit Monster");
                //}

                //총알 궤적 효과 생성
                TrailRenderer trail = Instantiate(BulletTrail, BulletSpawnPoint.position, Quaternion.identity);

                //총알 궤적 효과를 총알이 충돌한 지점까지 이동시키는 코루틴 시작
                StartCoroutine(SpawnTrail(trail, hit));

                lastShootTime = Time.time;
            }
        }
    }

    //총알 궤적 효과를 총알이 충돌한 지점까지 이동시키는 코루틴
    private IEnumerator SpawnTrail(TrailRenderer trail, RaycastHit hit)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;

        while(time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hit.point, time);
            time += Time.deltaTime / trail.time;

            yield return null;
        }

        trail.transform.position = hit.point;
        //Instantiate(ImpactParticleSystem, hit.point, Quaternion.LookRotation(hit.normal));

        Destroy(trail.gameObject, trail.time);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]

public class RangeWeapon : Weapon
{
    //[SerializeField] private ParticleSystem ShootingSystem;
    [SerializeField] private Transform BulletSpawnPoint;
    //[SerializeField] private ParticleSystem ImpactParticleSystem;
    [SerializeField] private TrailRenderer BulletTrail;
    [SerializeField] private GameObject BulletTrailPrefab;
    [SerializeField] private float ShootDelay = 0.5f;
    //[SerializeField] private LayerMask Mask;

    private Animator animator;
    private float lastShootTime;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Shoot()
    {
        if (Time.time > lastShootTime + ShootDelay)
        {
            //animator.SetTrigger("Shoot");
            //ShootingSystem.Play();
            Vector3 mousePos = GetMouseWorldPosition();

            Vector3 direction = (mousePos - BulletSpawnPoint.position).normalized;

            //if (Physics.Raycast(BulletSpawnPoint.position, direction, out RaycastHit hit, float.MaxValue, Mask))
            if (Physics.Raycast(BulletSpawnPoint.position, direction, out RaycastHit hit))
            {
                Debug.Log(hit.collider.name);

                if (hit.collider.TryGetComponent<Enemy>(out Enemy enemy))
                {
                    //enemy.TakeDamage(damage);

                    Debug.Log("Range Hit Enemy");
                }

                TrailRenderer trail = Instantiate(BulletTrail, BulletSpawnPoint.position, Quaternion.identity);

                StartCoroutine(SpawnTrail(trail, hit));

                lastShootTime = Time.time;
            }
        }
    }

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

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RangeWeapon : Weapon
{
    [Header("총 설정")]
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private TrailRenderer bulletTrail;
    [SerializeField] private GameObject muzzleFlash;

    [Header("사격 설정")]
    [SerializeField] private float shootDelay = 0.5f;
    [SerializeField] private float maxDistance = 100f;

    [Header("범위 공격")]
    [SerializeField] private float attackRadius = 3f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("시각 효과")]
    [SerializeField] private Renderer gunRenderer;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color shootColor = Color.yellow;
    [SerializeField] private float flashTime = 0.05f;

    private float lastShootTime;

    public override void Use()
    {
        if (Time.time < lastShootTime + shootDelay)
            return;

        lastShootTime = Time.time;

        Shoot();
        
        StartCoroutine(FlashGun());
        PlayMuzzleFlash();
    }

    #region SINGLE SHOOT

    private void Shoot()
    {
        Vector3 direction = bulletSpawnPoint.forward;

        Vector3 endPoint;

        if (Physics.Raycast(bulletSpawnPoint.position, direction, out RaycastHit hit, maxDistance))
        {
            endPoint = hit.point;

            MonsterAI monster = hit.collider.GetComponentInParent<MonsterAI>();

            if (monster != null)
            {
                monster.TakeDamage(data.playerattack);
                Debug.Log($"Range Hit : {monster.name}");
            }
        }
        else
        {
            endPoint = bulletSpawnPoint.position + direction * maxDistance;
        }

        TrailRenderer trail = Instantiate(
            bulletTrail,
            bulletSpawnPoint.position,
            Quaternion.LookRotation(direction)
        );

        StartCoroutine(SpawnTrail(trail, endPoint));
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 endPoint)
    {
        float time = 0f;
        Vector3 startPosition = trail.transform.position;

        while (time < 1f)
        {
            trail.transform.position = Vector3.Lerp(startPosition, endPoint, time);
            time += Time.deltaTime / trail.time;
            yield return null;
        }

        trail.transform.position = endPoint;
        Destroy(trail.gameObject, trail.time);
    }

    #endregion

    #region VISUAL EFFECTS

    private IEnumerator FlashGun()
    {
        if (gunRenderer == null) yield break;

        gunRenderer.material.color = shootColor;
        yield return new WaitForSeconds(flashTime);
        gunRenderer.material.color = defaultColor;
    }

    private void PlayMuzzleFlash()
    {
        if (muzzleFlash == null) return;

        Instantiate(
            muzzleFlash,
            bulletSpawnPoint.position,
            bulletSpawnPoint.rotation
        );
    }

    #endregion
}
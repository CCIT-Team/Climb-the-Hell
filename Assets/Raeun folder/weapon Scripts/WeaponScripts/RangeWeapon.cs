using System.Collections;
using UnityEngine;

public class RangeWeapon : Weapon
{
    [Header("플레이어 연결")]
    [SerializeField] private Player player;

    [Header("총 설정")]
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private TrailRenderer bulletTrail;
    [SerializeField] private GameObject muzzleFlash;

    [Header("사격 설정")]
    [Min(0f)]
    [SerializeField] private float shootDelay = 0.5f;

    [Min(0.1f)]
    [SerializeField] private float maxDistance = 100f;

    [Tooltip("총알이 충돌할 수 있는 레이어")]
    [SerializeField] private LayerMask hitLayer = ~0;

    [Header("시각 효과")]
    [SerializeField] private Renderer gunRenderer;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color shootColor = Color.yellow;

    [Min(0f)]
    [SerializeField] private float flashTime = 0.05f;

    [Header("총구 화염")]
    [Tooltip("생성된 총구 화염이 삭제되는 시간")]
    [Min(0f)]
    [SerializeField] private float muzzleFlashLifeTime = 0.5f;

    private float nextShootTime;
    private Coroutine flashCoroutine;

    private MaterialPropertyBlock materialPropertyBlock;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private void Awake()
    {
        FindPlayer();
        SetupMaterialPropertyBlock();
        ValidateReferences();
    }

    public override void Use()
    {
        if (Time.time < nextShootTime)
        {
            return;
        }

        if (bulletSpawnPoint == null)
        {
            Debug.LogError(
                "[RangeWeapon] Bullet Spawn Point가 없습니다.",
                this
            );

            return;
        }

        if (player == null ||
            player.stats == null)
        {
            Debug.LogError(
                "[RangeWeapon] PlayerStats를 찾지 못해 사격할 수 없습니다.",
                this
            );

            return;
        }

        nextShootTime =
            Time.time + shootDelay;

        Shoot();

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine =
            StartCoroutine(FlashGun());

        PlayMuzzleFlash();
    }

    private void FindPlayer()
    {
        if (player != null)
        {
            return;
        }

        player =
            GetComponentInParent<Player>();

        if (player == null)
        {
            Debug.LogError(
                "[RangeWeapon] 부모 오브젝트에서 Player를 찾지 못했습니다.",
                this
            );
        }
    }

    private void SetupMaterialPropertyBlock()
    {
        materialPropertyBlock =
            new MaterialPropertyBlock();

        if (gunRenderer != null)
        {
            SetGunColor(defaultColor);
        }
    }

    private void ValidateReferences()
    {
        if (bulletSpawnPoint == null)
        {
            Debug.LogError(
                "[RangeWeapon] Bullet Spawn Point를 연결하세요.",
                this
            );
        }

        if (bulletTrail == null)
        {
            Debug.LogWarning(
                "[RangeWeapon] Bullet Trail이 연결되지 않았습니다.",
                this
            );
        }
    }

    private void Shoot()
    {
        Vector3 origin =
            bulletSpawnPoint.position;

        Vector3 direction =
            bulletSpawnPoint.forward.normalized;

        Vector3 endPoint =
            origin +
            direction * maxDistance;

        bool didHit =
            Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                maxDistance,
                hitLayer,
                QueryTriggerInteraction.Ignore
            );

        if (didHit)
        {
            endPoint =
                hit.point;

            MonsterStats monster =
                hit.collider
                    .GetComponentInParent<MonsterStats>();

            if (monster != null &&
                monster.currentHp > 0)
            {
                /*
                 * PlayerStats의 최종 공격력과
                 * 치명타 확률을 사용해서 피해 계산.
                 */
                int damage =
                    player.stats.CalculateDamage();

                monster.TakeDamage(damage);

                Debug.Log(
                    $"[RangeWeapon] {monster.name}에게 " +
                    $"{damage} 피해",
                    monster
                );
            }
            else
            {
                Debug.Log(
                    $"[RangeWeapon] {hit.collider.name}에 총알 충돌",
                    hit.collider
                );
            }
        }

        SpawnBulletTrail(
            origin,
            direction,
            endPoint
        );
    }

    private void SpawnBulletTrail(
        Vector3 origin,
        Vector3 direction,
        Vector3 endPoint
    )
    {
        if (bulletTrail == null)
        {
            return;
        }

        TrailRenderer trail =
            Instantiate(
                bulletTrail,
                origin,
                Quaternion.LookRotation(
                    direction,
                    Vector3.up
                )
            );

        StartCoroutine(
            MoveTrail(
                trail,
                origin,
                endPoint
            )
        );
    }

    private IEnumerator MoveTrail(
        TrailRenderer trail,
        Vector3 startPosition,
        Vector3 endPoint
    )
    {
        if (trail == null)
        {
            yield break;
        }

        float travelDuration =
            Mathf.Max(
                0.01f,
                trail.time
            );

        float elapsedTime = 0f;

        while (elapsedTime <
               travelDuration)
        {
            if (trail == null)
            {
                yield break;
            }

            elapsedTime +=
                Time.deltaTime;

            float ratio =
                Mathf.Clamp01(
                    elapsedTime /
                    travelDuration
                );

            trail.transform.position =
                Vector3.Lerp(
                    startPosition,
                    endPoint,
                    ratio
                );

            yield return null;
        }

        if (trail == null)
        {
            yield break;
        }

        trail.transform.position =
            endPoint;

        Destroy(
            trail.gameObject,
            Mathf.Max(
                0.01f,
                trail.time
            )
        );
    }

    private IEnumerator FlashGun()
    {
        if (gunRenderer == null)
        {
            flashCoroutine = null;
            yield break;
        }

        SetGunColor(shootColor);

        if (flashTime > 0f)
        {
            yield return new WaitForSeconds(
                flashTime
            );
        }

        SetGunColor(defaultColor);

        flashCoroutine = null;
    }

    private void SetGunColor(Color color)
    {
        if (gunRenderer == null ||
            materialPropertyBlock == null)
        {
            return;
        }

        gunRenderer.GetPropertyBlock(
            materialPropertyBlock
        );

        Material sharedMaterial =
            gunRenderer.sharedMaterial;

        if (sharedMaterial != null &&
            sharedMaterial.HasProperty(
                BaseColorId
            ))
        {
            materialPropertyBlock.SetColor(
                BaseColorId,
                color
            );
        }
        else
        {
            materialPropertyBlock.SetColor(
                ColorId,
                color
            );
        }

        gunRenderer.SetPropertyBlock(
            materialPropertyBlock
        );
    }

    private void PlayMuzzleFlash()
    {
        if (muzzleFlash == null ||
            bulletSpawnPoint == null)
        {
            return;
        }

        GameObject effect =
            Instantiate(
                muzzleFlash,
                bulletSpawnPoint.position,
                bulletSpawnPoint.rotation
            );

        if (muzzleFlashLifeTime > 0f)
        {
            Destroy(
                effect,
                muzzleFlashLifeTime
            );
        }
    }

    private void OnDisable()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(
                flashCoroutine
            );

            flashCoroutine = null;
        }

        SetGunColor(defaultColor);
    }

    private void OnDrawGizmosSelected()
    {
        if (bulletSpawnPoint == null)
        {
            return;
        }

        Gizmos.color =
            Color.red;

        Gizmos.DrawRay(
            bulletSpawnPoint.position,
            bulletSpawnPoint.forward *
            maxDistance
        );
    }
}

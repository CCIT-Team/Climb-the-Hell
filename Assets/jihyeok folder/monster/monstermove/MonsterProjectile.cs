using UnityEngine;

/// <summary>
/// 나찰이 사용하는 원거리 투사체.
/// 빠른 투사체가 플레이어를 통과하지 않도록 SphereCast를 사용한다.
/// </summary>
public class MonsterProjectile : MonoBehaviour
{
    [Header("이동")]
    [Min(0.1f)]
    [SerializeField] private float speed = 12f;

    [Min(0.01f)]
    [SerializeField] private float collisionRadius = 0.2f;

    [Min(0.1f)]
    [SerializeField] private float lifeTime = 5f;

    [Header("충돌")]
    [Tooltip("Player와 Environment 레이어만 포함하고 Monster 레이어는 제외")]
    [SerializeField] private LayerMask collisionLayer = ~0;

    [Header("충돌 효과")]
    [SerializeField] private GameObject impactEffectPrefab;

    private Vector3 moveDirection;
    private Transform owner;
    private int damage;

    private bool isInitialized;

    public void Initialize(
        Vector3 direction,
        int attackDamage,
        Transform projectileOwner
    )
    {
        moveDirection = direction.normalized;
        damage = attackDamage;
        owner = projectileOwner;

        transform.rotation =
            Quaternion.LookRotation(moveDirection);

        isInitialized = true;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        float moveDistance =
            speed * Time.deltaTime;

        if (Physics.SphereCast(
                transform.position,
                collisionRadius,
                moveDirection,
                out RaycastHit hit,
                moveDistance,
                collisionLayer,
                QueryTriggerInteraction.Collide
            ))
        {
            if (owner != null &&
                hit.collider.transform.IsChildOf(owner))
            {
                transform.position +=
                    moveDirection * moveDistance;

                return;
            }

            ProcessHit(hit.collider, hit.point);
            return;
        }

        transform.position +=
            moveDirection * moveDistance;
    }

    private void ProcessHit(
        Collider targetCollider,
        Vector3 hitPoint
    )
    {
        Player targetPlayer =
            targetCollider.GetComponentInParent<Player>();

        if (targetPlayer != null)
        {
            targetPlayer.TakeDamage(damage);
        }

        if (impactEffectPrefab != null)
        {
            Instantiate(
                impactEffectPrefab,
                hitPoint,
                Quaternion.identity
            );
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            collisionRadius
        );
    }
}

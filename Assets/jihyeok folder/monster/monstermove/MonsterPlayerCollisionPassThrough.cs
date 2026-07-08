using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MonsterPlayerCollisionPassThrough : MonoBehaviour
{
    [Min(0.1f)]
    [SerializeField]
    private float refreshInterval = 0.5f;

    private Collider[] monsterColliders;

    private void Awake()
    {
        RefreshMonsterColliders();
    }

    private void OnEnable()
    {
        StartCoroutine(RefreshRoutine());
    }

    private IEnumerator RefreshRoutine()
    {
        while (enabled)
        {
            IgnorePlayerCollisions();

            yield return
                new WaitForSeconds(
                    refreshInterval
                );
        }
    }

    private void RefreshMonsterColliders()
    {
        monsterColliders =
            GetComponentsInChildren<Collider>(
                true
            );
    }

    private void IgnorePlayerCollisions()
    {
        if (monsterColliders == null ||
            monsterColliders.Length == 0)
        {
            RefreshMonsterColliders();
        }

        Player player =
            PlayerSceneMover.Instance != null
                ? PlayerSceneMover.Instance.CurrentPlayer
                : null;

        if (player == null)
        {
            player =
                FindFirstObjectByType<Player>(
                    FindObjectsInactive.Include
                );
        }

        if (player == null)
        {
            return;
        }

        Collider[] playerColliders =
            player.GetComponentsInChildren<Collider>(
                true
            );

        for (int i = 0;
             i < monsterColliders.Length;
             i++)
        {
            Collider monsterCollider =
                monsterColliders[i];

            if (monsterCollider == null ||
                monsterCollider.isTrigger)
            {
                continue;
            }

            for (int j = 0;
                 j < playerColliders.Length;
                 j++)
            {
                Collider playerCollider =
                    playerColliders[j];

                if (playerCollider == null ||
                    playerCollider.isTrigger)
                {
                    continue;
                }

                Physics.IgnoreCollision(
                    monsterCollider,
                    playerCollider,
                    true
                );
            }
        }
    }
}

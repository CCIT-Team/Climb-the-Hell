using System.Collections;
using UnityEngine;

public class Lava : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 10;

    [Header("Flight")]
    public float flightTime = 1f;
    public float damageRadius = 1.5f;

    private Coroutine arcRoutine;

    public void StartArc(Vector3 start, Vector3 end, float arcHeight)
    {
        if (arcRoutine != null)
            StopCoroutine(arcRoutine);

        arcRoutine = StartCoroutine(ArcRoutine(start, end, arcHeight));
    }

    private IEnumerator ArcRoutine(Vector3 start, Vector3 end, float arcHeight)
    {
        float t = 0f;

        while (t < flightTime)
        {
            t += Time.deltaTime;

            float progress = Mathf.Clamp01(t / flightTime);

            Vector3 pos = Vector3.Lerp(start, end, progress);
            pos.y += 4f * arcHeight * progress * (1f - progress);

            transform.position = pos;

            yield return null;
        }

        transform.position = end;

        Explode();
    }

    private void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Player player = hit.GetComponent<Player>();

                if (player != null && player.stats != null)
                {
                    player.stats.TakeDamage(damage);
                    Debug.Log("Player Damage");
                }
            }

            if (hit.CompareTag("Monster"))
            {
                MonsterStats monster = hit.GetComponent<MonsterStats>();

                if (monster != null)
                {
                    monster.TakeDamage(damage);
                    Debug.Log("Monster Damage");
                }
            }
        }

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (arcRoutine != null)
            StopCoroutine(arcRoutine);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
#endif
}
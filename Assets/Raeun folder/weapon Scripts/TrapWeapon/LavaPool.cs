using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LavaPool : MonoBehaviour
{
    public DotEffect effect;

    private HashSet<IDamageable> inside = new();

    private Dictionary<IDamageable, Coroutine> zoneCoroutines = new();

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out var entity))
        {
            inside.Add(entity);

            Debug.Log($"🔥 ENTER: {other.name}");

            if (!zoneCoroutines.ContainsKey(entity))
            {
                zoneCoroutines[entity] = StartCoroutine(ZoneDot(entity));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out var entity))
        {
            inside.Remove(entity);

            Debug.Log($"🚪 EXIT: {other.name}");

            if (zoneCoroutines.TryGetValue(entity, out var co))
            {
                StopCoroutine(co);
                zoneCoroutines.Remove(entity);
            }

            StartCoroutine(BurnDot(entity));
        }
    }
    private IEnumerator ZoneDot(IDamageable entity)
    {
        Debug.Log("▶ ZONE DOT START");

        while (inside.Contains(entity))
        {
            Debug.Log("💥 ZONE DAMAGE");

            entity.TakeDamage(effect.zoneDamage);

            yield return new WaitForSeconds(effect.zoneTick);
        }

        Debug.Log("⛔ ZONE END");
    }
    private IEnumerator BurnDot(IDamageable entity)
    {
        Debug.Log("🔥 BURN START");

        float elapsed = 0f;

        while (elapsed < effect.burnDuration)
        {
            Debug.Log("☠️ BURN DAMAGE");

            entity.TakeDamage(effect.burnDamage);

            yield return new WaitForSeconds(effect.burnTick);

            elapsed += effect.burnTick;
        }

        Debug.Log("✅ BURN END");
    }
}
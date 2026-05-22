using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpawnArea : MonoBehaviour
{
    [Header("Spawn Range")]
    public float minRadius = 2f;
    public float maxRadius = 8f;


    [Header("Sampling")]
    public int sampleCount = 40;
    public float minSlotDistance = 1.5f;

    [Header("Validation")]
    public float monsterRadius = 0.6f;
    public float minDistanceFromPlayer = 5f;
    public LayerMask obstacleLayer;
    public LayerMask monsterLayer;

    [Header("NavMesh")]
    public bool useNavMeshCheck = true;
    public float navMeshSearchDistance = 1.5f;

    private List<Vector3> cachedSlots = new List<Vector3>();

    public List<Vector3> GenerateSpawnSlots(Transform player)
    {
        cachedSlots.Clear();
        
        List<SpawnCandidate> candidates = new List<SpawnCandidate>();

        for (int i = 0; i < sampleCount; i++)
        {
            Vector3 rawPosition = GetDeterministicPoint(i);

            if (useNavMeshCheck)
            {
                if (!TryGetNavMeshPosition(rawPosition, out rawPosition))
                    continue;
            }

            if (!IsValidPosition(rawPosition, player))
                continue;

            float score = CalculateScore(rawPosition, player);

            candidates.Add(new SpawnCandidate(rawPosition, score));
        }

        candidates.Sort((a, b) => b.score.CompareTo(a.score));

        foreach (SpawnCandidate candidate in candidates)
        {
            if (IsFarEnoughFromOtherSlots(candidate.position))
            {
                cachedSlots.Add(candidate.position);
            }
        }

        return cachedSlots;
    }

    Vector3 GetDeterministicPoint(int index)
    {
        float goldenAngle = 137.508f;

        float angle = index * goldenAngle * Mathf.Deg2Rad;

        float t = (float)index / Mathf.Max(1, sampleCount - 1);
        float radius = Mathf.Lerp(minRadius, maxRadius, Mathf.Sqrt(t));

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * radius,
            0f,
            Mathf.Sin(angle) * radius
        );

        return transform.position + offset;
    }

    bool TryGetNavMeshPosition(Vector3 position, out Vector3 result)
    {
        if (NavMesh.SamplePosition(
            position,
            out NavMeshHit hit,
            navMeshSearchDistance,
            NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = position;
        return false;
    }

    bool IsValidPosition(Vector3 position, Transform player)
    {
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(position, player.position);

            if (distanceToPlayer < minDistanceFromPlayer)
                return false;
        }

        bool blockedByObstacle = Physics.CheckSphere(
            position + Vector3.up * 0.5f,
            monsterRadius,
            obstacleLayer
        );

        if (blockedByObstacle)
            return false;

        bool occupiedByMonster = Physics.CheckSphere(
            position + Vector3.up * 0.5f,
            monsterRadius,
            monsterLayer
        );

        if (occupiedByMonster)
            return false;

        return true;
    }

    bool IsFarEnoughFromOtherSlots(Vector3 position)
    {
        foreach (Vector3 slot in cachedSlots)
        {
            float distance = Vector3.Distance(position, slot);

            if (distance < minSlotDistance)
                return false;
        }

        return true;
    }

    float CalculateScore(Vector3 position, Transform player)
    {
        float score = 100f;

        float distanceFromCenter = Vector3.Distance(transform.position, position);
        float idealDistance = (minRadius + maxRadius) * 0.5f;

        score -= Mathf.Abs(distanceFromCenter - idealDistance) * 2f;

        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(position, player.position);

            score += Mathf.Clamp(distanceToPlayer, 0f, 20f) * 0.2f;
        }

        return score;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxRadius);

        Gizmos.color = Color.green;

        foreach (Vector3 slot in cachedSlots)
        {
            Gizmos.DrawSphere(slot + Vector3.up * 0.2f, 0.25f);
        }
    }

    struct SpawnCandidate
    {
        public Vector3 position;
        public float score;

        public SpawnCandidate(Vector3 position, float score)
        {
            this.position = position;
            this.score = score;
        }
    }
}
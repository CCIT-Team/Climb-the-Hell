using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    [SerializeField] private TrailRenderer trailPrefab;
    [SerializeField] private int poolSize = 20;

    private Queue<TrailRenderer> trailPool =
        new Queue<TrailRenderer>();

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < poolSize; i++)
        {
            TrailRenderer trail =
                Instantiate(trailPrefab);

            trail.gameObject.SetActive(false);

            trailPool.Enqueue(trail);
        }
    }

    public TrailRenderer GetTrail()
    {
        if (trailPool.Count > 0)
        {
            TrailRenderer trail = trailPool.Dequeue();

            trail.Clear();
            trail.gameObject.SetActive(true);

            return trail;
        }

        return Instantiate(trailPrefab);
    }

    public void ReturnTrail(TrailRenderer trail)
    {
        trail.Clear();
        trail.gameObject.SetActive(false);

        trailPool.Enqueue(trail);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class LavaCouldron : MonoBehaviour
{
    [Header("Pool")]
    public Lava lavaPrefab;
    public int poolSize = 10;

    [Header("Spawn")]
    public float spawnInterval = 0.5f;

    [Header("Arc")]
    public float arcHeight = 5f;

    private List<Lava> pool = new();

    private Transform footTarget;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
            footTarget = player.transform.Find("FootTarget");

        CreatePool();

        InvokeRepeating(nameof(SpawnLava), 0f, spawnInterval);
    }

    private void CreatePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            Lava lava = Instantiate(lavaPrefab);
            lava.gameObject.SetActive(false);
            pool.Add(lava);
        }
    }

    private Lava GetLava()
    {
        foreach (var lava in pool)
        {
            if (!lava.gameObject.activeSelf)
                return lava;
        }

        return null;
    }

    private void SpawnLava()
    {
        if (footTarget == null)
            return;

        Lava lava = GetLava();

        if (lava == null)
            return;

        lava.transform.position = transform.position;
        lava.gameObject.SetActive(true);

        lava.StartArc(transform.position, footTarget.position, arcHeight);
    }
}
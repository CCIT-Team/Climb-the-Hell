using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PhaseMonsterSpawner : MonoBehaviour
{
    [Header("phase setting")]
    public string phaseName = "Phase 1";

    [Header("monster prefab")]
    public GameObject[] monsterPrefabs;

    [Header("spawn points")]
    public MonsterSpawnPoint[] spawnPoints;

    [Header("spawn count")]
    public int maxMonsterCount = 10;
    public int respawnWhenBelow = 3;
    public int spawnAmount = 5;

    [Header("spawn condition")]
    public Transform player;
    public float minDistanceFromPlayer = 5f;
    public float spawnCheckRadius = 1f;

    [Header("spawn timing")]
    public float checkInterval = 2f;

    private float nextCheckTime;
    private List<GameObject> aliveMonsters = new List<GameObject>();

    private void Start()
    {
        FindPlayer();
        SpawnInitialMonsters();
    }

    private void Update()
    {
        if (Time.time < nextCheckTime) return;

        nextCheckTime = Time.time + checkInterval;

        RemoveDeadMonsters();

        if (aliveMonsters.Count <= respawnWhenBelow)
        {
            SpawnMonsters(spawnAmount);
        }
    }

    private void FindPlayer()
    {
        if (player != null) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
            player = p.transform;
    }

    private void SpawnInitialMonsters()
    {
        SpawnMonsters(maxMonsterCount);
    }

    private void SpawnMonsters(int amount)
    {
        if (monsterPrefabs == null || monsterPrefabs.Length == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        int spawnCount = 0;
        int tryCount = 0;
        int maxTry = amount * 10;

        while (spawnCount < amount && aliveMonsters.Count < maxMonsterCount && tryCount < maxTry)
        {
            tryCount++;

            MonsterSpawnPoint point = GetRandomSpawnPoint();

            if (point == null) continue;
            if (!CanSpawnAt(point.transform.position)) continue;

            GameObject prefab = GetRandomMonsterPrefab();

            GameObject monster = Instantiate(
                prefab,
                point.transform.position,
                point.transform.rotation
            );

            aliveMonsters.Add(monster);
            spawnCount++;
        }

        Debug.Log($"[{phaseName}] 몬스터 {spawnCount}마리 생성 / 현재 {aliveMonsters.Count}마리");
    }

    private MonsterSpawnPoint GetRandomSpawnPoint()
    {
        if (spawnPoints.Length == 0) return null;

        int index = Random.Range(0, spawnPoints.Length);
        return spawnPoints[index];
    }

    private GameObject GetRandomMonsterPrefab()
    {
        int index = Random.Range(0, monsterPrefabs.Length);
        return monsterPrefabs[index];
    }

    private bool CanSpawnAt(Vector3 position)
    {
        if (player != null)
        {
            float distance = Vector3.Distance(position, player.position);

            if (distance < minDistanceFromPlayer)
                return false;
        }

        Collider[] hits = Physics.OverlapSphere(position, spawnCheckRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Monster"))
                return false;
        }

        NavMeshHit navHit;

        if (!NavMesh.SamplePosition(position, out navHit, 1.5f, NavMesh.AllAreas))
            return false;

        return true;
    }

    private void RemoveDeadMonsters()
    {
        for (int i = aliveMonsters.Count - 1; i >= 0; i--)
        {
            if (aliveMonsters[i] == null)
            {
                aliveMonsters.RemoveAt(i);
                continue;
            }

            MonsterAI monsterAI = aliveMonsters[i].GetComponent<MonsterAI>();

            if (monsterAI != null && !monsterAI.IsAlive())
            {
                aliveMonsters.RemoveAt(i);
            }
        }
    }
}
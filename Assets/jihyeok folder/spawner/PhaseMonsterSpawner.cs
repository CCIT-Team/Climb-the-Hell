using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class SpawnPhase
{
    public string phaseName = "Phase";
    public int spawnCount = 5;
    public int nextPhaseWhenDeadCount = 4;
}

public class PhaseMonsterSpawner : MonoBehaviour
{
    [Header("Phase Setting")]
    public SpawnPhase[] phases;

    private int currentPhaseIndex = 0;
    private int deadCountInPhase = 0;
    private bool allPhaseFinished = false;

    public event Action<int> OnPhaseChanged;
    public event Action OnAllPhasesFinished;

    [Header("Monster Prefab")]
    public GameObject[] monsterPrefabs;

    [Header("Spawn Points")]
    public MonsterSpawnPoint[] spawnPoints;

    [Header("Spawn Condition")]
    public Transform player;
    public float minDistanceFromPlayer = 5f;
    public float spawnCheckRadius = 1f;

    private List<GameObject> aliveMonsters = new List<GameObject>();

    private void Start()
    {
        FindPlayer();
        StartPhase(0);
    }

    private void StartPhase(int phaseIndex)
    {
        if (phaseIndex >= phases.Length)
        {
            FinishAllPhases();
            return;
        }

        currentPhaseIndex = phaseIndex;
        deadCountInPhase = 0;

        SpawnPhase phase = phases[currentPhaseIndex];

        Debug.Log($"[{phase.phaseName}] 시작");

        OnPhaseChanged?.Invoke(currentPhaseIndex);

        SpawnMonsters(phase.spawnCount);
    }

    private void StartNextPhase()
    {
        if (allPhaseFinished) return;

        StartPhase(currentPhaseIndex + 1);
    }

    private void FinishAllPhases()
    {
        if (allPhaseFinished) return;

        allPhaseFinished = true;

        Debug.Log("모든 페이즈 종료");

        OnAllPhasesFinished?.Invoke();
    }

    private void SpawnMonsters(int amount)
    {
        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            Debug.LogWarning("몬스터 프리팹이 없음");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("스폰 포인트가 없음");
            return;
        }

        int spawnCount = 0;
        int tryCount = 0;
        int maxTry = amount * 10;

        while (spawnCount < amount && tryCount < maxTry)
        {
            tryCount++;

            MonsterSpawnPoint point = GetRandomSpawnPoint();

            if (point == null) continue;
            if (!CanSpawnAt(point.transform.position)) continue;

            GameObject prefab = GetRandomMonsterPrefab();

            GameObject monsterObj = Instantiate(
                prefab,
                point.transform.position,
                point.transform.rotation
            );

            MonsterAI monsterAI = monsterObj.GetComponent<MonsterAI>();

            if (monsterAI != null)
            {
                monsterAI.OnMonsterDead += HandleMonsterDead;
            }

            aliveMonsters.Add(monsterObj);
            spawnCount++;
        }

        Debug.Log($"몬스터 {spawnCount}마리 생성");
    }

    private void HandleMonsterDead(MonsterAI monster)
    {
        if (monster == null) return;
        if (allPhaseFinished) return;

        monster.OnMonsterDead -= HandleMonsterDead;

        aliveMonsters.Remove(monster.gameObject);

        deadCountInPhase++;

        Debug.Log($"[{phases[currentPhaseIndex].phaseName}] 죽은 몬스터 수: {deadCountInPhase}");

        if (deadCountInPhase >= phases[currentPhaseIndex].nextPhaseWhenDeadCount)
        {
            StartNextPhase();
        }
    }

    private MonsterSpawnPoint GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        int index = UnityEngine.Random.Range(0, spawnPoints.Length);
        return spawnPoints[index];
    }

    private GameObject GetRandomMonsterPrefab()
    {
        int index = UnityEngine.Random.Range(0, monsterPrefabs.Length);
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

    private void FindPlayer()
    {
        if (player != null) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
            player = p.transform;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FixedSpawnPhase
{
    public string phaseName = "Phase";

    // 생성할 몬스터 수
    public int spawnCount = 5;

    // 몇 마리 죽으면 다음 페이즈
    public int nextPhaseWhenDeadCount = 4;
}

public class FixedPhaseMonsterSpawner : MonoBehaviour
{
    [Header("Phase Setting")]
    public FixedSpawnPhase[] phases;

    private int currentPhaseIndex = 0;
    private int deadCountInPhase = 0;
    private bool allPhaseFinished = false;

    public event Action<int> OnPhaseChanged;
    public event Action OnAllPhasesFinished;

    [Header("Monster Prefab")]
    public GameObject[] monsterPrefabs;

    [Header("Player")]
    public Transform player;

    [Header("Spawn Timing")]
    public float firstPhaseDelay = 3f;
    public float spawnInterval = 0.4f;

    [Header("Circular Random Spawn")]
    public float minSpawnDistance = 6f;
    public float maxSpawnDistance = 15f;
    public float spawnCheckRadius = 1.2f;
    public int maxSpawnTry = 50;

    [Header("Empty Space Guided Spawn")]
    public bool useEmptySpaceGuidedSpawn = true;

    // 플레이어 주변을 몇 구역으로 나눌지
    public int sectorCount = 8;

    // 비워둘 구역 번호
    public int emptySectorIndex = 0;

    [Header("Click Effect")]
    public GameObject clickEffectPrefab;
    public float clickEffectDestroyTime = 1f;
    public LayerMask clickLayerMask = ~0;

    private Camera mainCamera;

    private int monsterPrefabIndex = 0;

    // 현재 살아있는 몬스터 목록
    private List<GameObject> aliveMonsters = new List<GameObject>();

    private void Start()
    {
        mainCamera = Camera.main;

        FindPlayer();

        StartCoroutine(StartFirstPhaseAfterDelay());
    }

    private IEnumerator StartFirstPhaseAfterDelay()
    {
        yield return new WaitForSeconds(firstPhaseDelay);

        StartPhase(0);
    }

    private void Update()
    {
        HandleClickEffect();
    }

    // 클릭 이펙트
    private void HandleClickEffect()
    {
        if (clickEffectPrefab == null) return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, clickLayerMask))
            {
                GameObject effect = Instantiate(
                    clickEffectPrefab,
                    hit.point,
                    Quaternion.identity
                );

                Destroy(effect, clickEffectDestroyTime);
            }
        }
    }

    // 페이즈 시작
    private void StartPhase(int phaseIndex)
    {
        if (phaseIndex >= phases.Length)
        {
            FinishAllPhases();
            return;
        }

        currentPhaseIndex = phaseIndex;
        deadCountInPhase = 0;

        // 빈 공간 방향 다시 계산
        UpdateEmptySector();

        FixedSpawnPhase phase = phases[currentPhaseIndex];

        Debug.Log($"[{phase.phaseName}] 시작 / 빈 공간 구역: {emptySectorIndex}");

        OnPhaseChanged?.Invoke(currentPhaseIndex);

        // 순차 생성 시작
        StartCoroutine(SpawnMonstersRoutine(phase.spawnCount));
    }

    // 몬스터 순차 생성
    private IEnumerator SpawnMonstersRoutine(int amount)
    {
        if (player == null)
        {
            FindPlayer();

            if (player == null)
            {
                Debug.LogWarning("Player를 찾을 수 없음");
                yield break;
            }
        }

        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            Debug.LogWarning("몬스터 프리팹이 없음");
            yield break;
        }

        int spawnCount = 0;
        int tryCount = 0;
        int maxTry = amount * maxSpawnTry;

        while (spawnCount < amount && tryCount < maxTry)
        {
            tryCount++;

            Vector3 spawnPos = GetRandomSpawnPosition();

            if (!CanSpawnAt(spawnPos))
                continue;

            GameObject prefab = GetNextMonsterPrefab();

            GameObject monsterObj = Instantiate(
                prefab,
                spawnPos,
                Quaternion.identity
            );

            MonsterAI monsterAI = monsterObj.GetComponent<MonsterAI>();

            if (monsterAI != null)
            {
                monsterAI.OnMonsterDead += HandleMonsterDead;
            }

            aliveMonsters.Add(monsterObj);

            spawnCount++;

            yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log($"[{phases[currentPhaseIndex].phaseName}] 몬스터 {spawnCount}마리 생성");
    }

    // 랜덤 스폰 위치 계산
    private Vector3 GetRandomSpawnPosition()
    {
        int selectedSector = GetRandomSpawnSector();

        float sectorSize = 360f / sectorCount;

        float startAngle = selectedSector * sectorSize;
        float endAngle = startAngle + sectorSize;

        float angle = UnityEngine.Random.Range(startAngle, endAngle);

        float distance = UnityEngine.Random.Range(
            minSpawnDistance,
            maxSpawnDistance
        );

        float rad = angle * Mathf.Deg2Rad;

        Vector3 dir = new Vector3(
            Mathf.Sin(rad),
            0f,
            Mathf.Cos(rad)
        );

        return player.position + dir * distance;
    }

    // 빈 공간 제외하고 랜덤 구역 선택
    private int GetRandomSpawnSector()
    {
        if (!useEmptySpaceGuidedSpawn)
            return UnityEngine.Random.Range(0, sectorCount);

        int sector = UnityEngine.Random.Range(0, sectorCount);

        int safeLoop = 0;

        while (sector == emptySectorIndex && safeLoop < 20)
        {
            sector = UnityEngine.Random.Range(0, sectorCount);
            safeLoop++;
        }

        return sector;
    }

    // 빈 공간 방향 계산
    private void UpdateEmptySector()
    {
        if (sectorCount <= 0)
            sectorCount = 8;

        if (player == null)
        {
            emptySectorIndex = UnityEngine.Random.Range(0, sectorCount);
            return;
        }

        int[] monsterCountBySector = new int[sectorCount];

        foreach (GameObject monster in aliveMonsters)
        {
            if (monster == null) continue;

            int sector = GetSectorIndex(monster.transform.position);

            monsterCountBySector[sector]++;
        }

        int leastMonsterSector = 0;
        int leastCount = int.MaxValue;

        for (int i = 0; i < sectorCount; i++)
        {
            if (monsterCountBySector[i] < leastCount)
            {
                leastCount = monsterCountBySector[i];
                leastMonsterSector = i;
            }
        }

        // 몬스터가 가장 적은 방향을 빈 공간으로 지정
        emptySectorIndex = leastMonsterSector;
    }

    // 위치가 어느 구역인지 계산
    private int GetSectorIndex(Vector3 worldPosition)
    {
        Vector3 dir = worldPosition - player.position;

        dir.y = 0f;

        if (dir == Vector3.zero)
            return 0;

        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        if (angle < 0f)
            angle += 360f;

        float sectorSize = 360f / sectorCount;

        return Mathf.FloorToInt(angle / sectorSize);
    }

    // 생성 가능한 위치인지 검사
    private bool CanSpawnAt(Vector3 position)
    {
        if (player != null)
        {
            float distance = Vector3.Distance(position, player.position);

            if (distance < minSpawnDistance)
                return false;

            if (distance > maxSpawnDistance)
                return false;
        }

        Collider[] hits = Physics.OverlapSphere(
            position,
            spawnCheckRadius
        );

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Monster"))
                return false;
        }

        return true;
    }

    // 몬스터 사망 처리
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

    // 다음 페이즈
    private void StartNextPhase()
    {
        if (allPhaseFinished) return;

        StartPhase(currentPhaseIndex + 1);
    }

    // 전체 종료
    private void FinishAllPhases()
    {
        if (allPhaseFinished) return;

        allPhaseFinished = true;

        Debug.Log("모든 페이즈 종료");

        OnAllPhasesFinished?.Invoke();
    }

    // 몬스터 프리팹 순환 선택
    private GameObject GetNextMonsterPrefab()
    {
        GameObject prefab = monsterPrefabs[monsterPrefabIndex];

        monsterPrefabIndex++;

        if (monsterPrefabIndex >= monsterPrefabs.Length)
            monsterPrefabIndex = 0;

        return prefab;
    }

    // Player 태그 찾기
    private void FindPlayer()
    {
        if (player != null) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
            player = p.transform;
    }

    // 씬 뷰 표시
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, minSpawnDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, maxSpawnDistance);

        if (sectorCount <= 0) return;

        float sectorSize = 360f / sectorCount;

        for (int i = 0; i < sectorCount; i++)
        {
            float angle = (i * sectorSize + sectorSize * 0.5f) * Mathf.Deg2Rad;

            Vector3 dir = new Vector3(
                Mathf.Sin(angle),
                0f,
                Mathf.Cos(angle)
            );

            if (i == emptySectorIndex)
                Gizmos.color = Color.cyan;
            else
                Gizmos.color = Color.gray;

            Gizmos.DrawLine(
                player.position,
                player.position + dir * maxSpawnDistance
            );
        }
    }
}
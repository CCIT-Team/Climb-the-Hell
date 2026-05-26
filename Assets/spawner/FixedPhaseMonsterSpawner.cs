using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 페이즈 하나에 대한 설정 데이터
[System.Serializable]
public class FixedSpawnPhase
{
    // 페이즈 이름
    public string phaseName = "Phase";

    // 이 페이즈에서 생성할 몬스터 수
    public int spawnCount = 5;

    // 몇 마리 죽으면 다음 페이즈로 넘어갈지
    public int nextPhaseWhenDeadCount = 4;
}

// 고정된 스폰 포인트를 순서대로 사용해서 몬스터를 생성하는 스포너
public class FixedPhaseMonsterSpawner : MonoBehaviour
{
    [Header("Phase Setting")]
    // 여러 개의 페이즈 설정
    public FixedSpawnPhase[] phases;

    // 현재 진행 중인 페이즈 번호
    private int currentPhaseIndex = 0;

    // 현재 페이즈에서 죽은 몬스터 수
    private int deadCountInPhase = 0;

    // 모든 페이즈가 끝났는지 확인
    private bool allPhaseFinished = false;

    // 페이즈가 바뀔 때 외부에 알려주는 이벤트
    public event Action<int> OnPhaseChanged;

    // 모든 페이즈가 끝났을 때 외부에 알려주는 이벤트
    public event Action OnAllPhasesFinished;

    [Header("Monster Prefab")]
    // 생성할 몬스터 프리팹 목록
    public GameObject[] monsterPrefabs;

    [Header("Spawn Points - Fixed Order")]
    // 고정 스폰 위치 목록
    public MonsterSpawnPoint[] spawnPoints;

    [Header("Spawn Condition")]
    // 플레이어 위치
    public Transform player;

    // 플레이어와 너무 가까우면 생성하지 않기 위한 최소 거리
    public float minDistanceFromPlayer = 5f;

    // 스폰 위치 주변에 몬스터가 있는지 검사하는 반경
    public float spawnCheckRadius = 1f;

    // 다음에 사용할 스폰 포인트 인덱스
    private int spawnPointIndex = 0;

    // 다음에 사용할 몬스터 프리팹 인덱스
    private int monsterPrefabIndex = 0;

    // 현재 살아있는 몬스터 목록
    private List<GameObject> aliveMonsters = new List<GameObject>();

    private void Start()
    {
        // 플레이어 찾기
        FindPlayer();

        // 0번 페이즈부터 시작
        StartPhase(0);
    }

    // 특정 페이즈 시작
    private void StartPhase(int phaseIndex)
    {
        // 더 이상 시작할 페이즈가 없으면 전체 종료
        if (phaseIndex >= phases.Length)
        {
            FinishAllPhases();
            return;
        }

        currentPhaseIndex = phaseIndex;
        deadCountInPhase = 0;
        spawnPointIndex = 0;

        FixedSpawnPhase phase = phases[currentPhaseIndex];

        Debug.Log($"[{phase.phaseName}] 시작");

        // 페이즈 변경 이벤트 호출
        OnPhaseChanged?.Invoke(currentPhaseIndex);

        // 해당 페이즈의 몬스터 수만큼 생성
        SpawnMonsters(phase.spawnCount);
    }

    // 다음 페이즈 시작
    private void StartNextPhase()
    {
        if (allPhaseFinished) return;

        StartPhase(currentPhaseIndex + 1);
    }

    // 모든 페이즈 종료 처리
    private void FinishAllPhases()
    {
        if (allPhaseFinished) return;

        allPhaseFinished = true;

        Debug.Log("모든 페이즈 종료");

        // 전체 종료 이벤트 호출
        OnAllPhasesFinished?.Invoke();
    }

    // 몬스터 생성 함수
    private void SpawnMonsters(int amount)
    {
        // 몬스터 프리팹이 없으면 생성 불가
        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            Debug.LogWarning("몬스터 프리팹이 없음");
            return;
        }

        // 스폰 포인트가 없으면 생성 불가
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("스폰 포인트가 없음");
            return;
        }

        int spawnCount = 0;
        int tryCount = 0;

        // 무한 반복 방지용 최대 시도 횟수
        int maxTry = amount * 10;

        while (spawnCount < amount && tryCount < maxTry)
        {
            tryCount++;

            // 다음 스폰 포인트 가져오기
            MonsterSpawnPoint point = GetNextSpawnPoint();

            if (point == null) continue;

            // 해당 위치에 생성 가능한지 검사
            if (!CanSpawnAt(point.transform.position)) continue;

            // 다음 몬스터 프리팹 가져오기
            GameObject prefab = GetNextMonsterPrefab();

            // 몬스터 생성
            GameObject monsterObj = Instantiate(
                prefab,
                point.transform.position,
                point.transform.rotation
            );

            // 생성된 몬스터의 MonsterAI 가져오기
            MonsterAI monsterAI = monsterObj.GetComponent<MonsterAI>();

            if (monsterAI != null)
            {
                // 몬스터가 죽었을 때 HandleMonsterDead가 실행되도록 연결
                monsterAI.OnMonsterDead += HandleMonsterDead;
            }

            // 살아있는 몬스터 목록에 추가
            aliveMonsters.Add(monsterObj);

            spawnCount++;
        }

        Debug.Log($"[{phases[currentPhaseIndex].phaseName}] 몬스터 {spawnCount}마리 고정 생성");
    }

    // 몬스터가 죽었을 때 실행되는 함수
    private void HandleMonsterDead(MonsterAI monster)
    {
        if (monster == null) return;
        if (allPhaseFinished) return;

        // 이벤트 중복 호출 방지를 위해 연결 해제
        monster.OnMonsterDead -= HandleMonsterDead;

        // 살아있는 몬스터 목록에서 제거
        aliveMonsters.Remove(monster.gameObject);

        // 현재 페이즈에서 죽은 몬스터 수 증가
        deadCountInPhase++;

        Debug.Log($"[{phases[currentPhaseIndex].phaseName}] 죽은 몬스터 수: {deadCountInPhase}");

        // 일정 수 이상 죽으면 다음 페이즈 시작
        if (deadCountInPhase >= phases[currentPhaseIndex].nextPhaseWhenDeadCount)
        {
            StartNextPhase();
        }
    }

    // 스폰 포인트를 순서대로 가져오는 함수
    private MonsterSpawnPoint GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        MonsterSpawnPoint point = spawnPoints[spawnPointIndex];

        spawnPointIndex++;

        // 마지막 스폰 포인트까지 갔다면 다시 처음으로
        if (spawnPointIndex >= spawnPoints.Length)
            spawnPointIndex = 0;

        return point;
    }

    // 몬스터 프리팹을 순서대로 가져오는 함수
    private GameObject GetNextMonsterPrefab()
    {
        GameObject prefab = monsterPrefabs[monsterPrefabIndex];

        monsterPrefabIndex++;

        // 마지막 프리팹까지 갔다면 다시 처음으로
        if (monsterPrefabIndex >= monsterPrefabs.Length)
            monsterPrefabIndex = 0;

        return prefab;
    }

    // 해당 위치에 몬스터를 생성할 수 있는지 검사
    private bool CanSpawnAt(Vector3 position)
    {
        // 플레이어와 너무 가까우면 생성하지 않음
        if (player != null)
        {
            float distance = Vector3.Distance(position, player.position);

            if (distance < minDistanceFromPlayer)
                return false;
        }

        // 스폰 위치 주변에 이미 몬스터가 있으면 생성하지 않음
        Collider[] hits = Physics.OverlapSphere(position, spawnCheckRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Monster"))
                return false;
        }

        NavMeshHit navHit;

        // 해당 위치가 NavMesh 위 또는 근처인지 검사
        if (!NavMesh.SamplePosition(position, out navHit, 1.5f, NavMesh.AllAreas))
            return false;

        return true;
    }

    // Player 태그를 가진 오브젝트 찾기
    private void FindPlayer()
    {
        if (player != null) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
            player = p.transform;
    }
}
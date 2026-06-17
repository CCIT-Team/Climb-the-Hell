using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FixedSpawnPhase // 페이즈 전용 클래스 (따로 빼도 ㄱㅊ을거 같은데 인스팩터 창에서 관리하는게 편할거 같아서 내뚬)
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

    private int currentPhaseIndex = 0; // 현재 몇 페이즈 인지 저장함
    private int deadCountInPhase = 0; // 현재 죽은 몬스터 수 저장 변수
    private bool allPhaseFinished = false; // 페이즈 끝났는지 확인하는 변수

    public event Action<int> OnPhaseChanged; // 페이즈 바뀌면 다른 코드들에게 신호 보내기
    public event Action OnAllPhasesFinished; // 페이즈 끝났을때 행동되는 변수 > 이걸로 연계 시키셈

    [Header("Monster Prefab")]
    public GameObject[] monsterPrefabs; // 몬스터 프리팹 저장 변수

    [Header("Player")]
    public Transform player;

    [Header("Spawn Timing")]
    public float firstPhaseDelay = 3f;
    public float spawnInterval = 0.4f;

    [Header("Circular Random Spawn")] // 스폰 거리
    public float minSpawnDistance = 6f;
    public float maxSpawnDistance = 15f;
    public float spawnCheckRadius = 1.2f;
    public int maxSpawnTry = 50;

    [Header("Empty Space Guided Spawn")]
    public bool useEmptySpaceGuidedSpawn = true; // 빈 공간 스폰 여부 > 아마 건드릴 일 x

    // 플레이어 주변을 몇 구역으로 나눌지
    public int sectorCount = 8;

    // 비워둘 구역 번호
    public int emptySectorIndex = 0; // 걍 저장용임 건들 ㄴㄴ

    private int monsterPrefabIndex = 0;

    // 현재 살아있는 몬스터 목록
    private List<GameObject> aliveMonsters = new List<GameObject>();

    private void Start()
    {
        FindPlayer();

        StartCoroutine(StartFirstPhaseAfterDelay()); // 코루틴 함수 출력
    }

    private IEnumerator StartFirstPhaseAfterDelay() // 코루틴 함수
    {
        yield return new WaitForSeconds(firstPhaseDelay); // 앞에서 설정했던 fristpahsedelay 변수 만큼 실행 대기

        StartPhase(0); // 0 페이즈 부터 시작
    }

    
    private void StartPhase(int phaseIndex) // 페이즈 시작
    {
        if (phaseIndex >= phases.Length) // 페이즈가 넘었는지 검사하는 if문
        {
            FinishAllPhases(); // 신호 보내는 함수
            return;
        }

        currentPhaseIndex = phaseIndex; // 페이즈 변수 저장
        deadCountInPhase = 0; // 페이즈 비우기

        // 빈 공간 방향 다시 계산
        UpdateEmptySector(); 

        FixedSpawnPhase phase = phases[currentPhaseIndex]; // FixedSpawnPhase 에 있는 phase 함수데이터를 가져와 저장함

        Debug.Log($"[{phase.phaseName}] 시작 / 빈 공간 구역: {emptySectorIndex}"); // 디버그용 출력

        OnPhaseChanged?.Invoke(currentPhaseIndex); // 현재 이벤트가 끝났다는 신호임, ?는 이벤트가 연결되어 있을때만 실행 < 다른 이벤트 연결하면 필요없는 null연산자임

        // 순차 생성 시작
        StartCoroutine(SpawnMonstersRoutine(phase.spawnCount)); // 한번에 소환하면 랙걸리니 순차생성으로 함 + 나중에 순차생성으로 빈공간 유도가 가능하다 생각해서 내뚬
    }

    // 몬스터 순차 생성
    private IEnumerator SpawnMonstersRoutine(int amount) // 코루틴 받기
    {
        if (player == null) // 플레이어 검사, 오류 방지용
        {
            FindPlayer();

            if (player == null)
            {
                Debug.LogWarning("Player를 찾을 수 없음");
                yield break;
            }
        }

        if (monsterPrefabs == null || monsterPrefabs.Length == 0) // 프리팹 검사, 오류 방지용
        {
            Debug.LogWarning("몬스터 프리팹이 없음");
            yield break;
        }

        int spawnCount = 0;
        int tryCount = 0;
        int maxTry = amount * maxSpawnTry;

        while (spawnCount < amount && tryCount < maxTry) // &&는 and연산자임 목표수만큼 남아있거나 시도 횟수 남아있으면 반복
        {
            tryCount++; // 시도횠수 증가

            Vector3 spawnPos = GetRandomSpawnPosition(); // 랜덤 위치 구하는 함수

            if (!CanSpawnAt(spawnPos)) // 그 위치에 생성할 수 없으면 아래 다시 위치 찾음
                continue;

            GameObject prefab = GetNextMonsterPrefab(); //몬스터 프리팹 가져오기



            GameObject monsterObj = Instantiate( //실제 몬스터 생성,노회전으로 소환 > 이거 나중에 플레이어 방향으로 해도 ㄱㅊ을을듯
                prefab,
                spawnPos,
                Quaternion.identity
            );

            MonsterAI monsterAI = monsterObj.GetComponent<MonsterAI>();// 소환한 몬스터 오브젝트의 ai를 가져오는거

            if (monsterAI != null) //  몬스터에 Monsterai가 있으면 
            {
                monsterAI.OnMonsterDead += HandleMonsterDead; // 죽었을때 함수 실행 , +=는 이벤트 함수인데 아까 OnMonsterDead에서 선언한거 발생시 이것도 같이 발생하는 코드임
            }

            aliveMonsters.Add(monsterObj); //살아있는 몬스터를 목록에 추가함

            spawnCount++; // 생성 몬스터 증가

            yield return new WaitForSeconds(spawnInterval); // spawnInterval만큼 기다리기
        }

        Debug.Log($"[{phases[currentPhaseIndex].phaseName}] 몬스터 {spawnCount}마리 생성"); // 스폰 생성
    }

    // 랜덤 스폰 위치 계산 > 
    private Vector3 GetRandomSpawnPosition() // 위에서 spawnpos함수에 들어가는 함수
    {
        int selectedSector = GetRandomSpawnSector(); // 색터 나눈것중 구역 선택함

        float sectorSize = 360f / sectorCount; // 플레이어 주변 360도를 입력한 색터 변수만큼 나눔

        float startAngle = selectedSector * sectorSize; // 선택된 구역의 시작 각도
        float endAngle = startAngle + sectorSize; // 선택된 구역의 끝 각도

        float angle = UnityEngine.Random.Range(startAngle, endAngle); // 그 구역 안에서 랜덤 각도를 선택

        float distance = UnityEngine.Random.Range( //플레이어로부터 떨어질 거리 선택
            minSpawnDistance,
            maxSpawnDistance
        );

        float rad = angle * Mathf.Deg2Rad; // 각도를 라디안으로 변환시킴

        Vector3 dir = new Vector3( // 각도에 따른 방향 백터 만들기 < 이거 솔직히 이해안됌
            Mathf.Sin(rad),
            0f,
            Mathf.Cos(rad)
        );

        return player.position + dir * distance; // 플레이어 위치에서 dir방향으로 거리만큼 떨어진 좌표를 반환시켜 보냄
    }

    // 빈 공간 제외하고 랜덤 구역 선택
    private int GetRandomSpawnSector()
    {
        if (!useEmptySpaceGuidedSpawn) //빈 공간 유도 스폰을 안쓰면 걍 알빠노 선택함
            return UnityEngine.Random.Range(0, sectorCount);

        int sector = UnityEngine.Random.Range(0, sectorCount);

        int safeLoop = 0; // 무한 방지용 변수

        while (sector == emptySectorIndex && safeLoop < 20) //선택된 구역이 비워둘 구역이면 다시 뽑음, 최대 20번
        {
            sector = UnityEngine.Random.Range(0, sectorCount);
            safeLoop++;
        } // 다른 구역을 다시 뽑고 반복 횟수 증가함

        return sector; // 총 정리된 색터 반환
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
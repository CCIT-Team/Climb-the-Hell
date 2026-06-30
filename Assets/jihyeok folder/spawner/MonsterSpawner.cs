using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MonsterSpawner : MonoBehaviour
{
    [Header("페이즈 몬스터 부모")]
    [SerializeField] private Transform phase1Root;
    [SerializeField] private Transform phase2Root;

    [Header("설정")]
    [SerializeField] private float nextPhaseDelay = 1.5f;

    [Tooltip("몬스터 위치 주변 NavMesh 검색 범위")]
    [SerializeField] private float navMeshCheckRadius = 2f;

    [SerializeField] private bool startOnPlay = true;

    public event Action OnAllPhasesCleared;

    public bool IsCleared { get; private set; }

    private MonsterAI[] phase1Monsters;
    private MonsterAI[] phase2Monsters;

    private int aliveMonsterCount;
    private int currentPhase;
    private bool phaseChanging;

    private void Awake()
    {
        /*
         * 부모가 꺼져 있어도 true 옵션 덕분에
         * 비활성화된 자식 몬스터까지 검색 가능
         */
        phase1Monsters = GetMonsters(phase1Root);
        phase2Monsters = GetMonsters(phase2Root);

        // 자식 몬스터 개별 비활성화
        SetMonstersActive(phase1Monsters, false);
        SetMonstersActive(phase2Monsters, false);

        // 페이즈 부모 자체도 비활성화
        SetRootActive(phase1Root, false);
        SetRootActive(phase2Root, false);
    }

    private void Start()
    {
        if (startOnPlay)
        {
            StartBattle();
        }
    }

    public void StartBattle()
    {
        if (currentPhase != 0 || IsCleared)
            return;

        StartPhase(1);
    }

    private MonsterAI[] GetMonsters(Transform root)
    {
        if (root == null)
            return Array.Empty<MonsterAI>();

        return root.GetComponentsInChildren<MonsterAI>(true);
    }

    private void StartPhase(int phaseNumber)
    {
        currentPhase = phaseNumber;
        phaseChanging = false;
        aliveMonsterCount = 0;

        Transform currentRoot =
            phaseNumber == 1
                ? phase1Root
                : phase2Root;

        MonsterAI[] monsters =
            phaseNumber == 1
                ? phase1Monsters
                : phase2Monsters;

        if (currentRoot == null)
        {
            Debug.LogWarning(
                $"[MonsterSpawner] {phaseNumber}페이즈 부모가 없습니다."
            );

            StartCoroutine(FinishCurrentPhase());
            return;
        }

        // 현재 페이즈 부모 활성화
        currentRoot.gameObject.SetActive(true);

        foreach (MonsterAI monster in monsters)
        {
            if (monster == null)
                continue;

            /*
             * 몬스터가 아직 비활성 상태일 때
             * NavMesh 위로 위치만 이동
             */
            if (!MoveToNavMeshPosition(monster))
            {
                Debug.LogWarning(
                    $"[MonsterSpawner] {monster.name} 주변에 NavMesh가 없습니다."
                );

                continue;
            }

            // 활성화 전에 사망 이벤트 등록
            monster.OnMonsterDead += HandleMonsterDead;

            // 몬스터 활성화
            monster.gameObject.SetActive(true);

            aliveMonsterCount++;
        }

        Debug.Log(
            $"[MonsterSpawner] {phaseNumber}페이즈 시작 / " +
            $"몬스터 {aliveMonsterCount}마리"
        );

        if (aliveMonsterCount == 0)
        {
            StartCoroutine(FinishCurrentPhase());
        }
    }

    private bool MoveToNavMeshPosition(MonsterAI monster)
    {
        bool found = NavMesh.SamplePosition(
            monster.transform.position,
            out NavMeshHit hit,
            navMeshCheckRadius,
            NavMesh.AllAreas
        );

        if (!found)
            return false;

        /*
         * 비활성 상태에서는 Agent.Warp 대신
         * Transform 위치를 먼저 NavMesh 위로 이동
         */
        monster.transform.position = hit.position;

        return true;
    }

    private void HandleMonsterDead(MonsterAI monster)
    {
        if (monster != null)
        {
            monster.OnMonsterDead -= HandleMonsterDead;
        }

        aliveMonsterCount =
            Mathf.Max(0, aliveMonsterCount - 1);

        Debug.Log(
            $"[MonsterSpawner] 남은 몬스터: {aliveMonsterCount}"
        );

        if (aliveMonsterCount == 0 && !phaseChanging)
        {
            StartCoroutine(FinishCurrentPhase());
        }
    }

    private IEnumerator FinishCurrentPhase()
    {
        if (phaseChanging)
            yield break;

        phaseChanging = true;

        yield return new WaitForSeconds(nextPhaseDelay);

        // 끝난 페이즈 부모 끄기
        if (currentPhase == 1)
        {
            SetRootActive(phase1Root, false);
            StartPhase(2);
        }
        else
        {
            SetRootActive(phase2Root, false);
            CompleteBattle();
        }
    }

    private void CompleteBattle()
    {
        if (IsCleared)
            return;

        IsCleared = true;

        Debug.Log(
            "[MonsterSpawner] 모든 페이즈 완료"
        );

        OnAllPhasesCleared?.Invoke();
    }

    private void SetMonstersActive(
        MonsterAI[] monsters,
        bool active)
    {
        foreach (MonsterAI monster in monsters)
        {
            if (monster != null)
            {
                monster.gameObject.SetActive(active);
            }
        }
    }

    private void SetRootActive(
        Transform root,
        bool active)
    {
        if (root != null)
        {
            root.gameObject.SetActive(active);
        }
    }

    private void OnDrawGizmosSelected()
    {
        DrawPhaseGizmos(phase1Root);
        DrawPhaseGizmos(phase2Root);
    }

    private void DrawPhaseGizmos(Transform root)
    {
        if (root == null)
            return;

        MonsterAI[] monsters =
            root.GetComponentsInChildren<MonsterAI>(true);

        foreach (MonsterAI monster in monsters)
        {
            if (monster == null)
                continue;

            Gizmos.DrawWireSphere(
                monster.transform.position,
                0.5f
            );
        }
    }
}
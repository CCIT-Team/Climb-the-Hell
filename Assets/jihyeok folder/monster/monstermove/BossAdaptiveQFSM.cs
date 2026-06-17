using System.Collections.Generic;
using UnityEngine;

public class BossAdaptiveQFSM : MonoBehaviour
{
    public Transform player;

    [Header("거리 기준")]
    public float closeDistance = 3f;
    public float midDistance = 7f;

    [Header("Q-Learning 설정")]
    public float alpha = 0.3f;      // 학습률
    public float gamma = 0.8f;      // 할인율
    public float epsilon = 0.2f;    // 랜덤 행동 확률

    [Header("보스 설정")]
    public float moveSpeed = 3f;
    public int attackDamage = 10;

    private BossState currentState;
    private BossAction currentAction;

    private Dictionary<string, float> qTable = new Dictionary<string, float>();

    private PlayerAction lastPlayerAction = PlayerAction.Idle;

    private float lastBossHp = 100f;
    public float bossHp = 100f;

    private float lastPlayerHp = 100f;
    public float playerHp = 100f;

    private void Start()
    {
        currentState = GetCurrentState();
        lastBossHp = bossHp;
        lastPlayerHp = playerHp;
    }

    private void Update()
    {
        // 실제로는 공격 끝, 피격, 회피 성공 같은 이벤트에서 호출하는 게 좋음
        DecideAndAct();
    }

    private void DecideAndAct()
    {
        BossState nextState = GetCurrentState();

        float reward = CalculateReward();

        UpdateQValue(currentState, currentAction, reward, nextState);

        currentState = nextState;

        currentAction = ChooseAction(currentState);

        ExecuteAction(currentAction);
    }

    private BossState GetCurrentState()
    {
        DistanceState distanceState = GetDistanceState();
        return new BossState(distanceState, lastPlayerAction);
    }

    private DistanceState GetDistanceState()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= closeDistance)
            return DistanceState.Close;

        if (distance <= midDistance)
            return DistanceState.Mid;

        return DistanceState.Far;
    }

    private BossAction ChooseAction(BossState state)
    {
        // epsilon 확률로 랜덤 행동
        if (Random.value < epsilon)
        {
            return GetRandomAction();
        }

        BossAction bestAction = BossAction.Attack;
        float bestValue = float.MinValue;

        foreach (BossAction action in System.Enum.GetValues(typeof(BossAction)))
        {
            float qValue = GetQValue(state, action);

            if (qValue > bestValue)
            {
                bestValue = qValue;
                bestAction = action;
            }
        }

        return bestAction;
    }

    private BossAction GetRandomAction()
    {
        BossAction[] actions = (BossAction[])System.Enum.GetValues(typeof(BossAction));
        return actions[Random.Range(0, actions.Length)];
    }

    private void ExecuteAction(BossAction action)
    {
        switch (action)
        {
            case BossAction.Attack:
                Attack();
                break;

            case BossAction.Move:
                MoveToPlayer();
                break;

            case BossAction.Skill:
                UseSkill();
                break;
        }
    }

    private void Attack()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= closeDistance)
        {
            Debug.Log("보스 근접 공격 성공");
            playerHp -= attackDamage;
        }
        else
        {
            Debug.Log("보스 공격 실패 - 거리 부족");
        }
    }

    private void MoveToPlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;

        Debug.Log("보스가 플레이어에게 이동");
    }

    private void UseSkill()
    {
        // 아직 스킬 미구현
        // 나중에 광역 공격, 돌진, 투사체, 장판 등 연결하면 됨

        Debug.Log("스킬 행동 선택됨 - 아직 미구현");
    }

    private float CalculateReward()
    {
        float reward = 0f;

        // 플레이어에게 피해를 줬으면 보상
        if (playerHp < lastPlayerHp)
        {
            reward += 10f;
        }

        // 보스가 피해를 입었으면 패널티
        if (bossHp < lastBossHp)
        {
            reward -= 8f;
        }

        // 공격했는데 너무 멀면 패널티
        if (currentAction == BossAction.Attack)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance > closeDistance)
                reward -= 5f;
        }

        // 멀리 있을 때 이동을 선택하면 보상
        if (currentAction == BossAction.Move && GetDistanceState() == DistanceState.Far)
        {
            reward += 3f;
        }

        lastBossHp = bossHp;
        lastPlayerHp = playerHp;

        return reward;
    }

    private void UpdateQValue(BossState state, BossAction action, float reward, BossState nextState)
    {
        float currentQ = GetQValue(state, action);
        float maxNextQ = GetMaxQValue(nextState);

        float newQ = currentQ + alpha * (reward + gamma * maxNextQ - currentQ);

        string key = GetKey(state, action);
        qTable[key] = newQ;
    }

    private float GetQValue(BossState state, BossAction action)
    {
        string key = GetKey(state, action);

        if (!qTable.ContainsKey(key))
        {
            qTable[key] = 0f;
        }

        return qTable[key];
    }

    private float GetMaxQValue(BossState state)
    {
        float maxValue = float.MinValue;

        foreach (BossAction action in System.Enum.GetValues(typeof(BossAction)))
        {
            float qValue = GetQValue(state, action);

            if (qValue > maxValue)
                maxValue = qValue;
        }

        return maxValue;
    }

    private string GetKey(BossState state, BossAction action)
    {
        return state.distanceState + "_" + state.playerAction + "_" + action;
    }

    // 외부 이벤트에서 호출
    public void OnPlayerAttack()
    {
        lastPlayerAction = PlayerAction.Attack;
    }

    public void OnPlayerDodge()
    {
        lastPlayerAction = PlayerAction.Dodge;
    }

    public void OnPlayerIdle()
    {
        lastPlayerAction = PlayerAction.Idle;
    }

    public void OnBossDamaged(float damage)
    {
        bossHp -= damage;
        lastPlayerAction = PlayerAction.Attack;
    }
}

public enum DistanceState
{
    Close,
    Mid,
    Far
}

public enum PlayerAction
{
    Idle,
    Attack,
    Dodge,
    Run
}

public enum BossAction
{
    Attack,
    Move,
    Skill
}

public struct BossState
{
    public DistanceState distanceState;
    public PlayerAction playerAction;

    public BossState(DistanceState distanceState, PlayerAction playerAction)
    {
        this.distanceState = distanceState;
        this.playerAction = playerAction;
    }
}
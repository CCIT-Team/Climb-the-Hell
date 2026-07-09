using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class DemonAdaptiveBossAI : MonoBehaviour
{
    private enum DistanceBucket
    {
        Close,
        Mid,
        Far
    }

    private enum PlayerMoveBucket
    {
        Still,
        Approaching,
        Retreating,
        Strafing
    }

    private enum BossPhase
    {
        HighHp,
        MidHp,
        LowHp
    }

    private enum BossAction
    {
        Pursue,
        ClawCombo,
        ShadowDash,
        HellfireFan,
        GroundSlam,
        SoulDrain
    }

    [Header("Boss Stats")]
    [SerializeField]
    private int bossMaxHp = 1200;

    [SerializeField]
    private int basicDamage = 14;

    [SerializeField]
    private int specialDamage = 22;

    [SerializeField]
    private float moveSpeed = 3.4f;

    [SerializeField]
    private float turnSpeed = 12f;

    [Header("Ranges")]
    [SerializeField]
    private float meleeRange = 2.4f;

    [SerializeField]
    private float midRange = 7f;

    [SerializeField]
    private float farRange = 13f;

    [Header("Q Learning")]
    [SerializeField]
    private float alpha = 0.28f;

    [SerializeField]
    private float gamma = 0.82f;

    [SerializeField]
    private float epsilon = 0.16f;

    [SerializeField]
    private float decisionCooldown = 0.45f;

    private readonly Dictionary<string, float[]> qTable =
        new Dictionary<string, float[]>();

    private MonsterStats stats;
    private Player player;
    private NavMeshAgent agent;
    private Animator animator;
    private Rigidbody body;
    private Coroutine actionRoutine;
    private Material telegraphMaterial;

    private Vector3 lastPlayerPosition;
    private int lastKnownHp;
    private int damageTakenSinceAction;
    private int damageDealtSinceAction;
    private float nextDecisionTime;
    private bool dead;

    private void Awake()
    {
        stats =
            GetComponent<MonsterStats>();

        if (stats == null)
        {
            stats =
                gameObject.AddComponent<MonsterStats>();
        }

        if (stats.monsterhp < bossMaxHp)
        {
            stats.monsterhp = bossMaxHp;
        }

        stats.monsterattack =
            Mathf.Max(
                stats.monsterattack,
                basicDamage
            );

        stats.monsterspeed =
            Mathf.Max(
                stats.monsterspeed,
                moveSpeed
            );

        stats.monsterrange =
            Mathf.Max(
                stats.monsterrange,
                meleeRange
            );

        stats.ResetStats();
        lastKnownHp = stats.currentHp;

        animator =
            GetComponentInChildren<Animator>(true);

        body =
            GetComponent<Rigidbody>();

        if (body == null)
        {
            body =
                gameObject.AddComponent<Rigidbody>();
        }

        body.useGravity = true;
        body.constraints =
            RigidbodyConstraints.FreezeRotation;

        Collider collider =
            GetComponent<Collider>();

        if (collider == null)
        {
            CapsuleCollider capsule =
                gameObject.AddComponent<CapsuleCollider>();

            capsule.height = 2.5f;
            capsule.radius = 0.65f;
            capsule.center = new Vector3(0f, 1.25f, 0f);
        }

        agent =
            GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            agent =
                gameObject.AddComponent<NavMeshAgent>();
        }

        agent.speed = moveSpeed;
        agent.angularSpeed = 720f;
        agent.acceleration = 18f;
        agent.stoppingDistance = meleeRange * 0.75f;

        MonsterPlayerCollisionPassThrough passThrough =
            GetComponent<MonsterPlayerCollisionPassThrough>();

        if (passThrough == null)
        {
            gameObject.AddComponent<MonsterPlayerCollisionPassThrough>();
        }

        telegraphMaterial =
            new Material(Shader.Find("Sprites/Default"));

        telegraphMaterial.color =
            new Color(1f, 0.05f, 0.02f, 0.32f);
    }

    private void Start()
    {
        ResolvePlayer();

        if (player != null)
        {
            lastPlayerPosition =
                player.transform.position;
        }
    }

    private void Update()
    {
        if (dead ||
            stats == null)
        {
            return;
        }

        TrackDamageTaken();

        if (stats.currentHp <= 0)
        {
            StartCoroutine(DieRoutine());
            return;
        }

        if (player == null ||
            !player.IsAlive())
        {
            ResolvePlayer();
            StopAgent();
            return;
        }

        FacePlayer();

        if (actionRoutine != null ||
            Time.time < nextDecisionTime)
        {
            return;
        }

        string state =
            BuildStateKey();

        BossAction action =
            ChooseAction(state);

        actionRoutine =
            StartCoroutine(
                RunAction(action, state)
            );
    }

    private IEnumerator RunAction(
        BossAction action,
        string state)
    {
        damageDealtSinceAction = 0;
        damageTakenSinceAction = 0;

        switch (action)
        {
            case BossAction.Pursue:
                yield return PursueRoutine(0.8f);
                break;

            case BossAction.ClawCombo:
                yield return ClawComboRoutine();
                break;

            case BossAction.ShadowDash:
                yield return ShadowDashRoutine();
                break;

            case BossAction.HellfireFan:
                yield return HellfireFanRoutine();
                break;

            case BossAction.GroundSlam:
                yield return GroundSlamRoutine();
                break;

            case BossAction.SoulDrain:
                yield return SoulDrainRoutine();
                break;
        }

        string nextState =
            BuildStateKey();

        float reward =
            CalculateReward(action);

        Learn(state, action, reward, nextState);

        nextDecisionTime =
            Time.time + decisionCooldown;

        actionRoutine = null;
    }

    private IEnumerator PursueRoutine(
        float duration)
    {
        float endTime =
            Time.time + duration;

        while (Time.time < endTime &&
               player != null)
        {
            MoveToward(player.transform.position);
            yield return null;
        }

        StopAgent();
    }

    private IEnumerator ClawComboRoutine()
    {
        StopAgent();

        for (int i = 0; i < 2; i++)
        {
            TriggerAnimator("Attack");
            yield return new WaitForSeconds(0.25f);

            TryDamagePlayer(
                basicDamage,
                meleeRange,
                105f
            );

            yield return new WaitForSeconds(0.22f);
        }
    }

    private IEnumerator ShadowDashRoutine()
    {
        StopAgent();

        Vector3 start =
            transform.position;

        Vector3 target =
            player != null
                ? player.transform.position
                : start + transform.forward * 6f;

        Vector3 direction =
            (target - start);

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.1f)
        {
            direction =
                transform.forward;
        }

        direction.Normalize();

        ShowLineTelegraph(
            start,
            start + direction * 8f,
            0.42f
        );

        yield return new WaitForSeconds(0.42f);

        TriggerAnimator("Dash");

        float duration = 0.28f;
        float elapsed = 0f;
        float dashDistance = 8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            transform.position +=
                direction *
                (dashDistance / duration) *
                Time.deltaTime;

            TryDamagePlayer(
                specialDamage,
                1.7f,
                170f
            );

            yield return null;
        }
    }

    private IEnumerator HellfireFanRoutine()
    {
        StopAgent();
        TriggerAnimator("Attack");

        yield return new WaitForSeconds(0.55f);

        int rays = 5;
        float coneAngle = 70f;

        for (int i = 0; i < rays; i++)
        {
            float ratio =
                rays <= 1
                    ? 0.5f
                    : (float)i / (rays - 1);

            float angle =
                Mathf.Lerp(
                    -coneAngle * 0.5f,
                    coneAngle * 0.5f,
                    ratio
                );

            Vector3 direction =
                Quaternion.Euler(0f, angle, 0f) *
                transform.forward;

            ShowLineTelegraph(
                transform.position + Vector3.up * 0.25f,
                transform.position +
                Vector3.up * 0.25f +
                direction * farRange,
                0.18f
            );

            TryDamagePlayerAlongLine(
                direction,
                farRange,
                1.05f,
                specialDamage
            );
        }

        yield return new WaitForSeconds(0.35f);
    }

    private IEnumerator GroundSlamRoutine()
    {
        StopAgent();
        TriggerAnimator("Attack");

        float radius = 4.2f;

        ShowCircleTelegraph(
            transform.position,
            radius,
            0.7f
        );

        yield return new WaitForSeconds(0.7f);

        TryDamagePlayerInRadius(
            specialDamage + 6,
            radius
        );

        yield return new WaitForSeconds(0.35f);
    }

    private IEnumerator SoulDrainRoutine()
    {
        StopAgent();

        if (player != null &&
            Vector3.Distance(
                transform.position,
                player.transform.position
            ) > midRange)
        {
            Vector3 behindPlayer =
                player.transform.position -
                player.transform.forward * 3.2f;

            WarpNear(behindPlayer);
        }

        TriggerAnimator("Attack");

        float duration = 1.4f;
        float tickTimer = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tickTimer += Time.deltaTime;

            FacePlayer();

            if (tickTimer >= 0.35f)
            {
                tickTimer = 0f;

                TryDamagePlayer(
                    Mathf.Max(5, specialDamage / 3),
                    6f,
                    55f
                );

                if (stats != null &&
                    stats.currentHp > 0)
                {
                    stats.Heal(3);
                }
            }

            yield return null;
        }
    }

    private BossAction ChooseAction(
        string state)
    {
        BossAction[] actions =
            (BossAction[])System.Enum.GetValues(
                typeof(BossAction)
            );

        if (Random.value < epsilon)
        {
            return actions[
                Random.Range(0, actions.Length)
            ];
        }

        float[] values =
            GetValues(state);

        int bestIndex = 0;
        float bestValue = values[0];

        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] > bestValue)
            {
                bestValue = values[i];
                bestIndex = i;
            }
        }

        return actions[bestIndex];
    }

    private void Learn(
        string state,
        BossAction action,
        float reward,
        string nextState)
    {
        float[] currentValues =
            GetValues(state);

        float[] nextValues =
            GetValues(nextState);

        float maxNext = nextValues[0];

        for (int i = 1; i < nextValues.Length; i++)
        {
            maxNext =
                Mathf.Max(maxNext, nextValues[i]);
        }

        int actionIndex = (int)action;

        currentValues[actionIndex] =
            currentValues[actionIndex] +
            alpha *
            (reward +
             gamma * maxNext -
             currentValues[actionIndex]);
    }

    private float[] GetValues(
        string state)
    {
        float[] values;

        if (!qTable.TryGetValue(state, out values))
        {
            values =
                new float[
                    System.Enum.GetValues(
                        typeof(BossAction)
                    ).Length
                ];

            qTable[state] = values;
        }

        return values;
    }

    private float CalculateReward(
        BossAction action)
    {
        float reward =
            damageDealtSinceAction * 1.2f -
            damageTakenSinceAction * 0.55f;

        float distance =
            player != null
                ? Vector3.Distance(
                    transform.position,
                    player.transform.position
                )
                : 999f;

        if (action == BossAction.ClawCombo &&
            distance > meleeRange + 0.8f)
        {
            reward -= 5f;
        }

        if (action == BossAction.Pursue &&
            distance > midRange)
        {
            reward += 2f;
        }

        if (action == BossAction.GroundSlam &&
            distance <= 4.5f)
        {
            reward += 3f;
        }

        if (action == BossAction.HellfireFan &&
            distance >= meleeRange)
        {
            reward += 2f;
        }

        if (action == BossAction.SoulDrain &&
            stats.currentHp < stats.monsterhp * 0.45f)
        {
            reward += 2f;
        }

        return reward;
    }

    private string BuildStateKey()
    {
        DistanceBucket distance =
            GetDistanceBucket();

        PlayerMoveBucket movement =
            GetPlayerMoveBucket();

        BossPhase phase =
            GetPhase();

        return distance + "_" + movement + "_" + phase;
    }

    private DistanceBucket GetDistanceBucket()
    {
        if (player == null)
        {
            return DistanceBucket.Far;
        }

        float distance =
            Vector3.Distance(
                transform.position,
                player.transform.position
            );

        if (distance <= meleeRange + 0.5f)
        {
            return DistanceBucket.Close;
        }

        if (distance <= midRange)
        {
            return DistanceBucket.Mid;
        }

        return DistanceBucket.Far;
    }

    private PlayerMoveBucket GetPlayerMoveBucket()
    {
        if (player == null)
        {
            return PlayerMoveBucket.Still;
        }

        Vector3 current =
            player.transform.position;

        Vector3 delta =
            current - lastPlayerPosition;

        lastPlayerPosition = current;
        delta.y = 0f;

        if (delta.sqrMagnitude < 0.0025f)
        {
            return PlayerMoveBucket.Still;
        }

        Vector3 toBoss =
            transform.position - current;

        toBoss.y = 0f;
        toBoss.Normalize();
        delta.Normalize();

        float dot =
            Vector3.Dot(delta, toBoss);

        if (dot > 0.55f)
        {
            return PlayerMoveBucket.Approaching;
        }

        if (dot < -0.55f)
        {
            return PlayerMoveBucket.Retreating;
        }

        return PlayerMoveBucket.Strafing;
    }

    private BossPhase GetPhase()
    {
        if (stats == null ||
            stats.monsterhp <= 0)
        {
            return BossPhase.HighHp;
        }

        float ratio =
            (float)stats.currentHp /
            stats.monsterhp;

        if (ratio <= 0.35f)
        {
            return BossPhase.LowHp;
        }

        if (ratio <= 0.7f)
        {
            return BossPhase.MidHp;
        }

        return BossPhase.HighHp;
    }

    private void TryDamagePlayer(
        int damage,
        float range,
        float angle)
    {
        if (player == null ||
            !player.IsAlive())
        {
            return;
        }

        Vector3 toPlayer =
            player.transform.position -
            transform.position;

        toPlayer.y = 0f;

        if (toPlayer.magnitude > range)
        {
            return;
        }

        if (Vector3.Angle(transform.forward, toPlayer) >
            angle * 0.5f)
        {
            return;
        }

        int before =
            player.GetCurrentHp();

        player.TakeDamage(damage);

        damageDealtSinceAction +=
            Mathf.Max(
                0,
                before - player.GetCurrentHp()
            );
    }

    private void TryDamagePlayerAlongLine(
        Vector3 direction,
        float length,
        float width,
        int damage)
    {
        if (player == null ||
            !player.IsAlive())
        {
            return;
        }

        Vector3 origin =
            transform.position;

        Vector3 toPlayer =
            player.transform.position - origin;

        toPlayer.y = 0f;
        direction.y = 0f;
        direction.Normalize();

        float forwardDistance =
            Vector3.Dot(toPlayer, direction);

        if (forwardDistance < 0f ||
            forwardDistance > length)
        {
            return;
        }

        Vector3 closest =
            origin + direction * forwardDistance;

        float sideDistance =
            Vector3.Distance(
                closest,
                player.transform.position
            );

        if (sideDistance > width)
        {
            return;
        }

        int before =
            player.GetCurrentHp();

        player.TakeDamage(damage);

        damageDealtSinceAction +=
            Mathf.Max(
                0,
                before - player.GetCurrentHp()
            );
    }

    private void TryDamagePlayerInRadius(
        int damage,
        float radius)
    {
        if (player == null ||
            !player.IsAlive())
        {
            return;
        }

        if (Vector3.Distance(
                transform.position,
                player.transform.position
            ) > radius)
        {
            return;
        }

        int before =
            player.GetCurrentHp();

        player.TakeDamage(damage);

        damageDealtSinceAction +=
            Mathf.Max(
                0,
                before - player.GetCurrentHp()
            );
    }

    private void TrackDamageTaken()
    {
        if (stats == null)
        {
            return;
        }

        if (stats.currentHp < lastKnownHp)
        {
            damageTakenSinceAction +=
                lastKnownHp - stats.currentHp;
        }

        lastKnownHp = stats.currentHp;
    }

    private void ResolvePlayer()
    {
        player =
            PlayerSceneMover.Instance != null
                ? PlayerSceneMover.Instance.CurrentPlayer
                : null;

        if (player == null)
        {
            player =
                FindFirstObjectByType<Player>(
                    FindObjectsInactive.Exclude
                );
        }
    }

    private void MoveToward(
        Vector3 target)
    {
        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(target);
            return;
        }

        Vector3 direction =
            target - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
        {
            return;
        }

        transform.position +=
            direction.normalized *
            moveSpeed *
            Time.deltaTime;
    }

    private void StopAgent()
    {
        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void WarpNear(
        Vector3 target)
    {
        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.Warp(target);
        }
        else
        {
            transform.position = target;
        }
    }

    private void FacePlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 direction =
            player.transform.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
    }

    private void TriggerAnimator(
        string triggerName)
    {
        if (animator == null ||
            string.IsNullOrWhiteSpace(triggerName))
        {
            return;
        }

        animator.SetTrigger(triggerName);
    }

    private void ShowCircleTelegraph(
        Vector3 position,
        float radius,
        float duration)
    {
        GameObject circle =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );

        circle.name = "Demon Ground Slam Telegraph";
        circle.transform.position =
            position + Vector3.up * 0.04f;
        circle.transform.localScale =
            new Vector3(radius * 2f, 0.02f, radius * 2f);

        Collider collider =
            circle.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer =
            circle.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = telegraphMaterial;
        }

        Destroy(circle, duration);
    }

    private void ShowLineTelegraph(
        Vector3 from,
        Vector3 to,
        float duration)
    {
        GameObject line =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        line.name = "Demon Line Telegraph";

        Vector3 midpoint =
            (from + to) * 0.5f;

        Vector3 direction =
            to - from;

        float length =
            direction.magnitude;

        line.transform.position =
            midpoint + Vector3.up * 0.08f;
        line.transform.rotation =
            Quaternion.LookRotation(direction);
        line.transform.localScale =
            new Vector3(0.35f, 0.03f, length);

        Collider collider =
            line.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer =
            line.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = telegraphMaterial;
        }

        Destroy(line, duration);
    }

    private IEnumerator DieRoutine()
    {
        if (dead)
        {
            yield break;
        }

        dead = true;
        StopAgent();

        if (actionRoutine != null)
        {
            StopCoroutine(actionRoutine);
            actionRoutine = null;
        }

        TriggerAnimator("Die");

        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        yield return new WaitForSeconds(1.2f);

        Destroy(gameObject);
    }
}

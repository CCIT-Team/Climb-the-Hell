using UnityEngine;
using UnityEngine.AI;

public class RandomMove : MonoBehaviour
{
    public float moveRange = 10f;

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        MoveRandom();
    }

    void Update()
    {
        
        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
        {
            MoveRandom();
        }
    }

    void MoveRandom()
    {
        Vector3 randomPos = Random.insideUnitSphere * moveRange;
        randomPos += transform.position;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomPos, out hit, moveRange, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
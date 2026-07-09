using System.Collections;
using UnityEngine;

public class Spike : MonoBehaviour
{
    [Header("설정")]
    public float activeDelay = 3f;      // 몇 초마다 발동
    public float spikeDuration = 1f;    // 창이 올라와 있는 시간
    public int damage = 30;

    [Header("오브젝트")]
    public Transform spikeModel;        // 창 모델

    private Vector3 downPos;
    private Vector3 upPos;

    private bool isActive = false;
    private bool stopped;

    private void Start()
    {
        downPos = spikeModel.localPosition;
        upPos = downPos + Vector3.up * 1.0f;

        StartCoroutine(SpikeRoutine());
    }

    IEnumerator SpikeRoutine()
    {
        while (true)
        {
            if (stopped)
            {
                yield break;
            }

            yield return new WaitForSeconds(activeDelay);

            if (stopped)
            {
                yield break;
            }

            isActive = true;

            // 창 올라오기
            spikeModel.localPosition = upPos;

            yield return new WaitForSeconds(spikeDuration);

            // 창 내려가기
            spikeModel.localPosition = downPos;

            isActive = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (stopped)
        {
            return;
        }

        if (!isActive) return;

        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerStats>().TakeDamage(damage);
            Debug.Log("Spike Damage to Player: " + damage);
        }

        if (other.CompareTag("Monster"))
        {
            other.GetComponent<MonsterStats>().TakeDamage(damage);
            Debug.Log("Spike Damage to Monster: " + damage);
        }
    }

    public void StopTrap()
    {
        stopped = true;
        isActive = false;
        StopAllCoroutines();

        if (spikeModel != null)
        {
            spikeModel.localPosition = downPos;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TrailRenderer를 재사용하기 위한 오브젝트 풀
public class PoolManager : MonoBehaviour
{
    // 싱글톤
    public static PoolManager Instance;

    // 미리 만들어 둘 Trail 프리팹
    [SerializeField] private TrailRenderer trailPrefab;

    // 풀에 생성할 개수
    [SerializeField] private int poolSize = 20;

    // 사용하지 않는 Trail을 보관하는 큐
    private Queue<TrailRenderer> trailPool =
        new Queue<TrailRenderer>();

    private void Awake()
    {
        // 싱글톤 등록
        Instance = this;

        // 미리 Trail을 생성하여 풀에 저장
        for (int i = 0; i < poolSize; i++)
        {
            TrailRenderer trail =
                Instantiate(trailPrefab);

            // 처음에는 비활성화
            trail.gameObject.SetActive(false);

            // 큐에 보관
            trailPool.Enqueue(trail);
        }
    }

    // Trail 하나 가져오기
    public TrailRenderer GetTrail()
    {
        // 사용 가능한 Trail이 있으면
        if (trailPool.Count > 0)
        {
            // 하나 꺼냄
            TrailRenderer trail = trailPool.Dequeue();

            // 이전 흔적 제거
            trail.Clear();

            // 활성화
            trail.gameObject.SetActive(true);

            return trail;
        }

        // 풀이 비어있으면 새로 생성
        return Instantiate(trailPrefab);
    }

    // 사용이 끝난 Trail 반환
    public void ReturnTrail(TrailRenderer trail)
    {
        // 흔적 제거
        trail.Clear();

        // 비활성화
        trail.gameObject.SetActive(false);

        // 다시 풀에 저장
        trailPool.Enqueue(trail);
    }
}
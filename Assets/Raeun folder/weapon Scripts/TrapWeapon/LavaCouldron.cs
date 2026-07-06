using System.Collections.Generic;
using UnityEngine;

public class LavaCouldron : MonoBehaviour
{
    [Header("Pool")]
    public Lava lavaPrefab;
    public GameObject warningPrefab;   // 🔥 추가
    public int poolSize = 10;

    [Header("Arc")]
    public float arcHeight = 5f;

    [Header("Warning")]
    public float warningTime = 1f;

    private List<Lava> pool = new();
    private Transform footTarget;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
            footTarget = player.transform.Find("FootTarget");

        CreatePool();
        InvokeRepeating(nameof(SpawnLava), 0f, warningTime);
    }

    private void CreatePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            Lava lava = Instantiate(lavaPrefab);
            lava.gameObject.SetActive(false);
            pool.Add(lava);
        }
    }

    private Lava GetLava()
    {
        foreach (var lava in pool)
        {
            if (!lava.gameObject.activeSelf)
                return lava;
        }
        return null;
    }

    private void SpawnLava()
    {
        if (footTarget == null)
            return;

        Lava lava = GetLava();
        if (lava == null)
            return;

        Vector3 targetPos = footTarget.position;

        // 🔥 위치 보정
        Vector3 warnPos = targetPos + Vector3.up * 0.05f;

        // 🔥 Warning 생성 (위치 + 방향 + 스케일)
        GameObject warn = Instantiate(
        warningPrefab,
        warnPos,
        warningPrefab.transform.rotation);

        warn.transform.localScale = Vector3.one * 2f;

        WarningCircle wc = warn.GetComponent<WarningCircle>();
        wc.Init(warningTime);

        // Lava 발사
        lava.transform.position = transform.position;
        lava.gameObject.SetActive(true);
        lava.StartArc(transform.position, targetPos, arcHeight);

        Destroy(warn, warningTime);
    }
}
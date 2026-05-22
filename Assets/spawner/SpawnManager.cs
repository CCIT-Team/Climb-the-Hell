using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Transform player;
        
    public GameObject monsterPrefab;
    public SpawnArea[] spawnAreas;

    public int monsterCountPerArea = 5;
    public float spawnInterval = 0.2f;

    void Start()
    {
        StartCoroutine(SpawnAllAreas());
    }

    IEnumerator SpawnAllAreas()
    {
        foreach (SpawnArea area in spawnAreas)
        {
            List<Vector3> slots = area.GenerateSpawnSlots(player);

            int spawnCount = Mathf.Min(monsterCountPerArea, slots.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                SpawnMonster(slots[i]);
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    void SpawnMonster(Vector3 position)
    {
        Instantiate(monsterPrefab, position, Quaternion.identity);
    }
}

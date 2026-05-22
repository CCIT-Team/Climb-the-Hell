using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawnPool : MonoBehaviour
{
    private Dictionary<GameObject, Queue<GameObject>> poolDict =
        new Dictionary<GameObject, Queue<GameObject>>();

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        if (!poolDict.ContainsKey(prefab))
        {
            poolDict[prefab] = new Queue<GameObject>();
        }

        GameObject obj;

        if (poolDict[prefab].Count > 0)
        {
            obj = poolDict[prefab].Dequeue();

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
        }

        return obj;
    }

    public void Return(GameObject prefab, GameObject obj)
    {
        if (prefab == null || obj == null)
            return;

        if (!poolDict.ContainsKey(prefab))
        {
            poolDict[prefab] = new Queue<GameObject>();
        }

        obj.SetActive(false);
        poolDict[prefab].Enqueue(obj);
    }
}
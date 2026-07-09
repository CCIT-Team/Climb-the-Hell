using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemonBossAutoBinder
{
    private const string BossSceneName = "RealBossRoom";
    private const string BossObjectName = "Demon_APose";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        TryBind(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        TryBind(scene);
    }

    private static void TryBind(
        Scene scene)
    {
        if (!scene.IsValid() ||
            scene.name != BossSceneName)
        {
            return;
        }

        GameObject bossObject =
            FindObjectInScene(
                scene,
                BossObjectName
            );

        if (bossObject == null)
        {
            MonsterAI monster =
                FindMonsterBossCandidate(scene);

            if (monster != null)
            {
                bossObject =
                    monster.gameObject;
            }
        }

        if (bossObject == null)
        {
            Debug.LogWarning(
                "[DemonBossAutoBinder] Could not find a boss object in RealBossRoom."
            );

            return;
        }

        PreparePlacedBoss(
            scene,
            bossObject
        );

        PrepareCombatSetup(bossObject);

        DemonAdaptiveBossAI boss =
            bossObject.GetComponent<DemonAdaptiveBossAI>();

        if (boss == null)
        {
            bossObject.AddComponent<DemonAdaptiveBossAI>();
        }

        MonsterAI normalAi =
            bossObject.GetComponent<MonsterAI>();

        if (normalAi != null)
        {
            normalAi.enabled = false;
        }

        Debug.Log(
            "[DemonBossAutoBinder] Demon_APose is ready as the RealBossRoom boss.",
            bossObject
        );
    }

    private static void PreparePlacedBoss(
        Scene scene,
        GameObject bossObject)
    {
        Transform bossTransform =
            bossObject.transform;

        Transform previousParent =
            bossTransform.parent;

        Vector3 worldPosition =
            bossTransform.position;

        Quaternion worldRotation =
            bossTransform.rotation;

        Vector3 worldScale =
            bossTransform.lossyScale;

        if (previousParent != null)
        {
            HidePointVisuals(previousParent);
            bossTransform.SetParent(null, true);
        }

        if (bossObject.scene != scene)
        {
            SceneManager.MoveGameObjectToScene(
                bossObject,
                scene
            );
        }

        bossTransform.position = worldPosition;
        bossTransform.rotation = worldRotation;
        bossTransform.localScale = worldScale;

        if (!bossObject.activeSelf)
        {
            bossObject.SetActive(true);
        }
    }

    private static void PrepareCombatSetup(
        GameObject bossObject)
    {
        if (bossObject == null)
        {
            return;
        }

        try
        {
            bossObject.tag = "Monster";
        }
        catch
        {
            Debug.LogWarning(
                "[DemonBossAutoBinder] Monster tag is missing from TagManager.",
                bossObject
            );
        }

        int monsterLayer =
            LayerMask.NameToLayer("Monster");

        if (monsterLayer >= 0)
        {
            SetLayerRecursively(
                bossObject.transform,
                monsterLayer
            );
        }

        MonsterDamageNumberWatcher watcher =
            bossObject.GetComponent<MonsterDamageNumberWatcher>();

        if (watcher == null)
        {
            bossObject.AddComponent<MonsterDamageNumberWatcher>();
        }
    }

    private static void SetLayerRecursively(
        Transform target,
        int layer)
    {
        if (target == null)
        {
            return;
        }

        target.gameObject.layer = layer;

        for (int i = 0; i < target.childCount; i++)
        {
            SetLayerRecursively(
                target.GetChild(i),
                layer
            );
        }
    }

    private static void HidePointVisuals(
        Transform pointRoot)
    {
        if (pointRoot == null)
        {
            return;
        }

        Renderer[] renderers =
            pointRoot.GetComponents<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

        Collider[] colliders =
            pointRoot.GetComponents<Collider>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }
    }

    private static GameObject FindObjectInScene(
        Scene scene,
        string targetName)
    {
        GameObject[] roots =
            scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform found =
                FindChildByName(
                    roots[i].transform,
                    targetName
                );

            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static MonsterAI FindMonsterBossCandidate(
        Scene scene)
    {
        GameObject[] roots =
            scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            MonsterAI monster =
                roots[i].GetComponentInChildren<MonsterAI>(
                    true
                );

            if (monster != null)
            {
                return monster;
            }
        }

        return null;
    }

    private static Transform FindChildByName(
        Transform parent,
        string targetName)
    {
        if (parent.name == targetName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found =
                FindChildByName(
                    parent.GetChild(i),
                    targetName
                );

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}

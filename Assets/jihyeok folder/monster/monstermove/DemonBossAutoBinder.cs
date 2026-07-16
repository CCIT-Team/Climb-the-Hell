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
                "[DemonBossAutoBinder] RealBossRoom에서 보스 후보를 찾지 못했습니다."
            );

            return;
        }

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
            "[DemonBossAutoBinder] Demon_APose를 RealBossRoom 보스로 설정했습니다.",
            bossObject
        );
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

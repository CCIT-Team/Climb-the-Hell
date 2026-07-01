using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BoonDatabase",
    menuName = "Game/Boon/Boon Database"
)]
public class BoonDatabase : ScriptableObject
{
    [Header("게임에 등장하는 모든 득도")]

    [SerializeField]
    private List<BoonData> boons =
        new List<BoonData>();

    // 보상 선택기가 이 목록을 읽는다.
    public List<BoonData> Boons
    {
        get
        {
            return boons;
        }
    }

    public int Count
    {
        get
        {
            if (boons == null)
            {
                return 0;
            }

            return boons.Count;
        }
    }

    public BoonData GetBoonById(
        string boonId
    )
    {
        if (boons == null ||
            string.IsNullOrWhiteSpace(boonId))
        {
            return null;
        }

        for (int i = 0;
             i < boons.Count;
             i++)
        {
            BoonData boon =
                boons[i];

            if (boon == null)
            {
                continue;
            }

            if (boon.boonId == boonId)
            {
                return boon;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (boons == null)
        {
            boons =
                new List<BoonData>();

            return;
        }

        HashSet<string> usedIds =
            new HashSet<string>();

        for (int i = 0;
             i < boons.Count;
             i++)
        {
            BoonData boon =
                boons[i];

            if (boon == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    boon.boonId
                ))
            {
                Debug.LogWarning(
                    $"{boon.name}의 Boon ID가 비어 있습니다.",
                    boon
                );

                continue;
            }

            if (!usedIds.Add(boon.boonId))
            {
                Debug.LogError(
                    $"중복된 Boon ID: {boon.boonId}",
                    boon
                );
            }
        }
    }
#endif
}
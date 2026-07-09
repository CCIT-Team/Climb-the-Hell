/// <summary>
/// 문에서 선택한 다음 씬의 Spawn ID를 Loading 씬까지 전달한다.
/// </summary>
public static class PlayerSpawnContext
{
    private const string DefaultSpawnId = "Default";

    private static string nextSpawnId =
        DefaultSpawnId;

    public static void SetSpawnId(
        string spawnId
    )
    {
        nextSpawnId =
            string.IsNullOrWhiteSpace(spawnId)
                ? DefaultSpawnId
                : spawnId;
    }

    public static string ConsumeSpawnId()
    {
        string result =
            string.IsNullOrWhiteSpace(nextSpawnId)
                ? DefaultSpawnId
                : nextSpawnId;

        nextSpawnId =
            DefaultSpawnId;

        return result;
    }

    public static void Clear()
    {
        nextSpawnId =
            DefaultSpawnId;
    }
}

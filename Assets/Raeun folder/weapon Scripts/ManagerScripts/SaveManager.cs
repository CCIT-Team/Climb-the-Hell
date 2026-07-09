using System.IO;
using UnityEngine;

/// <summary>
/// 저장/불러오기 담당.
///
/// 주의:
/// - TraitManager가 SaveManager.Instance를 사용하므로 Instance는 유지한다.
/// - 하지만 여기서 DontDestroyOnLoad는 호출하지 않는다.
/// - SaveManager가 GameManager 자식이면 GameManager가 유지될 때 같이 유지된다.
/// </summary>
[DefaultExecutionOrder(-1100)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("저장 데이터")]
    public SaveData saveData;

    [Header("저장 파일")]
    [SerializeField]
    private string saveFileName = "save.json";

    [Header("디버그")]
    [SerializeField]
    private bool showLogs = true;

    private string SavePath
    {
        get
        {
            return Path.Combine(
                Application.persistentDataPath,
                saveFileName
            );
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        /*
         * 여기서 DontDestroyOnLoad(gameObject)를 호출하면 안 됨.
         * SaveManager가 GameManager 자식이면 Unity에서 아래 경고가 뜬다.
         *
         * DontDestroyOnLoad only works for root GameObjects...
         *
         * 전역 유지는 루트 오브젝트인 GameManager가 담당한다.
         */
        if (saveData == null)
        {
            saveData = new SaveData();
        }
    }

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            saveData = new SaveData();

            if (showLogs)
            {
                Debug.Log(
                    $"[SaveManager] 저장 파일이 없어 새 데이터를 생성합니다. / 경로={SavePath}",
                    this
                );
            }

            return;
        }

        try
        {
            string json =
                File.ReadAllText(SavePath);

            saveData =
                JsonUtility.FromJson<SaveData>(json);

            if (saveData == null)
            {
                saveData = new SaveData();
            }

            if (showLogs)
            {
                Debug.Log(
                    $"[SaveManager] 저장 데이터 로드 완료. / 경로={SavePath}",
                    this
                );
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                $"[SaveManager] 저장 데이터 로드 실패.\n{exception}",
                this
            );

            saveData = new SaveData();
        }
    }

    public void Save()
    {
        if (saveData == null)
        {
            saveData = new SaveData();
        }

        try
        {
            string json =
                JsonUtility.ToJson(
                    saveData,
                    true
                );

            File.WriteAllText(
                SavePath,
                json
            );

            if (showLogs)
            {
                Debug.Log(
                    $"[SaveManager] 저장 완료. / 경로={SavePath}",
                    this
                );
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                $"[SaveManager] 저장 실패.\n{exception}",
                this
            );
        }
    }

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }

            saveData = new SaveData();

            if (showLogs)
            {
                Debug.Log(
                    "[SaveManager] 저장 데이터 삭제 완료.",
                    this
                );
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                $"[SaveManager] 저장 데이터 삭제 실패.\n{exception}",
                this
            );
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
using System.IO;
using UnityEngine;

// 게임 저장 및 불러오기를 담당하는 클래스
public class SaveManager : MonoBehaviour
{
    // 싱글톤
    public static SaveManager Instance;

    // 저장할 데이터
    public SaveData saveData = new SaveData();

    // 저장 파일 경로
    string savePath;

    private void Awake()
    {
        // 싱글톤 생성
        if (Instance == null)
        {
            Instance = this;

            // 씬이 바뀌어도 유지
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 중복 생성 방지
            Destroy(gameObject);
        }

        // 저장 파일 경로 생성
        savePath = Path.Combine(
            Application.persistentDataPath,
            "SaveData.json");
    }

    // 저장
    public void Save()
    {
        // SaveData를 JSON 문자열로 변환
        string json =
            JsonUtility.ToJson(saveData, true);

        // JSON 파일 저장
        File.WriteAllText(savePath, json);

        Debug.Log("저장 완료");
    }

    // 불러오기
    public void Load()
    {
        // 저장 파일이 없으면 종료
        if (!File.Exists(savePath))
            return;

        // JSON 파일 읽기
        string json =
            File.ReadAllText(savePath);

        // JSON → SaveData 객체로 변환
        saveData =
            JsonUtility.FromJson<SaveData>(json);

        Debug.Log("불러오기 완료");
    }
}
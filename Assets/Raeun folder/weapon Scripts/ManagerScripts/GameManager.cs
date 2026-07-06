using UnityEngine;

// 게임 전체를 관리하는 중앙 매니저
// 각종 매니저와 영구 데이터를 보관한다.
public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static GameManager Instance;

    // 저장/불러오기 관리
    public SaveManager saveManager;

    // 특성 관리
    public TraitManager traitManager;

    // 게임(런) 진행 관리
    public RunManager runManager;

    // UI 관리
    public UIManager uiManager;

    // 허브(상점) 관리
    public HubManager hubManager;

    // 결과 UI
    public ResultUI resultUI;

    // 영구 재화(허브에서 사용하는 돈)
    public MoneyData permanentMoney =
        new MoneyData();

    private void Awake()
    {
        // 싱글톤 생성
        if (Instance == null)
        {
            Instance = this;

            // 씬이 바뀌어도 삭제되지 않음
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 이미 GameManager가 존재하면 중복 제거
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 저장 데이터 불러오기
        saveManager.Load();
    }

    // 영구 재화 획득
    public void AddPermanentMoney(int amount)
    {
        // 0 이하이면 무시
        if (amount <= 0)
        {
            return;
        }

        // 영구 재화 추가
        permanentMoney.AddMoney(amount);
    }

    // 영구 재화 사용 시도
    public bool TrySpendPermanentMoney(int amount)
    {
        // 돈이 충분하면 차감 후 true,
        // 부족하면 false 반환
        return permanentMoney.TrySpend(amount);
    }

    // 게임 저장
    public void SaveGame()
    {
        // 현재 특성 레벨 저장
        traitManager.SaveTraits();

        // 영구 재화 저장
        saveManager.saveData.permanentMoney =
            permanentMoney.CurrentMoney;

        // 저장 파일 생성
        saveManager.Save();
    }
}
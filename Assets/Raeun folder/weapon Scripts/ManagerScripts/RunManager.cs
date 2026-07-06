using System;
using UnityEngine;

// 한 판(런)의 시작과 종료를 관리하는 클래스
public class RunManager : MonoBehaviour
{
    // 런 시작 이벤트
    public static event Action OnRunStarted;

    // 런 종료 이벤트
    public static event Action OnRunEnded;

    // 플레이어 참조
    public Player player;

    // 특성 관리
    public TraitManager traitManager;

    // 현재 런 진행 여부
    public bool IsRunning
    {
        get;
        private set;
    }

    // 런 시작
    public void StartRun()
    {
        // 이미 시작되어 있다면 중복 실행 방지
        if (IsRunning)
            return;

        IsRunning = true;

        // 허브에서 구매한 모든 특성 적용
        traitManager.ApplyAllTraits();

        // 플레이어 스탯 초기화
        player.stats.Init();

        // 시작 골드 특성 적용
        player.money.SetMoney(
            traitManager.GetStartGold());

        // 런 시작 이벤트 호출
        OnRunStarted?.Invoke();

        Debug.Log("런 시작");
    }

    // 런 종료
    public void EndRun()
    {
        // 이미 종료된 상태라면 실행하지 않음
        if (!IsRunning)
            return;

        IsRunning = false;

        // 런 종료 이벤트 호출
        OnRunEnded?.Invoke();

        Debug.Log("런 종료");

        // 결과 UI 열기
        GameManager.Instance.uiManager.Open(
            GameManager.Instance.resultUI);
    }
}
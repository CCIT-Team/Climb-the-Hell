using System;
using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static event Action OnRunStarted;
    public static event Action OnRunEnded;

    public Player player;
    public TraitManager traitManager;

    public bool IsRunning { get; private set; }

    public void StartRun()
    {
        if (IsRunning)
        {
            return;
        }

        RefreshReferences();

        if (player == null)
        {
            Debug.LogError("[RunManager] Player가 없습니다.", this);
            return;
        }

        if (player.stats == null)
        {
            Debug.LogError("[RunManager] Player.stats가 없습니다.", player);
            return;
        }

        if (traitManager == null)
        {
            Debug.LogError("[RunManager] TraitManager가 없습니다.", this);
            return;
        }

        IsRunning = true;

        traitManager.ApplyAllTraits();

        player.stats.Init(true);
        player.RefreshDeathResist();

        if (player.money != null)
        {
            player.money.SetMoney(traitManager.GetStartGold());
        }

        OnRunStarted?.Invoke();

        Debug.Log("[RunManager] 런 시작", this);
    }

    public void EndRun()
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;

        OnRunEnded?.Invoke();

        Debug.Log("[RunManager] 런 종료", this);

        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.RefreshSceneReferences();

        if (GameManager.Instance.uiManager == null ||
            GameManager.Instance.resultUI == null)
        {
            Debug.LogWarning("[RunManager] 결과 UI를 열 수 없습니다.", this);
            return;
        }

        GameManager.Instance.uiManager.Open(GameManager.Instance.resultUI);
    }

    private void RefreshReferences()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshSceneReferences();

            if (player == null)
            {
                player = GameManager.Instance.CurrentPlayer;
            }

            if (traitManager == null)
            {
                traitManager = GameManager.Instance.traitManager;
            }
        }

        if (player == null)
        {
            player = FindObjectOfType<Player>(true);
        }

        if (traitManager == null)
        {
            traitManager = FindObjectOfType<TraitManager>(true);
        }

        if (traitManager != null)
        {
            traitManager.player = player;
            traitManager.EnsureReferences();
        }
    }
}
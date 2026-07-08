using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 시작 버튼.
/// Title에서 RunFlowManager를 통해 Loading -> Lobby로 이동한다.
/// </summary>
public class TitleStartButton_RunFlow : MonoBehaviour
{
    [SerializeField]
    private Button startButton;

    private bool clicked;

    private void Awake()
    {
        if (startButton == null)
        {
            startButton = GetComponent<Button>();
        }
    }

    public void StartGame()
    {
        if (clicked)
        {
            return;
        }

        clicked = true;

        if (startButton != null)
        {
            startButton.interactable = false;
        }

        if (RunFlowManager.Instance == null)
        {
            Debug.LogError(
                "[TitleStartButton_RunFlow] RunFlowManager가 없습니다.",
                this
            );
            return;
        }

        RunFlowManager.Instance.GoToLobby();
    }
}

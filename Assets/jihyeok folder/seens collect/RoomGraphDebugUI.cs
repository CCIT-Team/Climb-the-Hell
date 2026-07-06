using TMPro;
using UnityEngine;

/// <summary>
/// 선택 사항.
/// 후보 가중치와 미등장 횟수를 화면에 표시한다.
/// </summary>
public class RoomGraphDebugUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;

    [Min(0.1f)]
    [SerializeField] private float refreshInterval = 0.5f;

    private float nextRefreshTime;

    private void Update()
    {
        if (debugText == null ||
            Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime =
            Time.unscaledTime + refreshInterval;

        debugText.text =
            RunFlowManager.Instance.GetDebugText();
    }
}

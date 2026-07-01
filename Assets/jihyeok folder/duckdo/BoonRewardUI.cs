using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 꺼져 있는 보상 UI를 열고,
/// 3개의 보상 슬롯을 관리한다.
/// </summary>
public class BoonRewardUI : MonoBehaviour
{
    [Header("보상 UI")]

    [Tooltip("처음에 꺼져 있는 보상 Canvas 또는 최상위 패널")]
    [SerializeField]
    private GameObject rewardCanvas;

    [Header("보상 슬롯")]

    [Tooltip("decide1, decide2, decide3 순서로 연결")]
    [SerializeField]
    private BoonRewardSlot[] rewardSlots =
        new BoonRewardSlot[3];

    [Header("선택 연출")]

    [Min(0f)]
    [Tooltip("선택한 슬롯만 남겨둔 뒤 UI가 닫히기까지의 시간")]
    [SerializeField]
    private float selectedHoldDuration = 0.35f;

    [Header("게임 정지")]

    [Tooltip("보상 UI가 열렸을 때 게임 시간을 멈출지 여부")]
    [SerializeField]
    private bool pauseGameWhileOpen = true;

    // 선택 완료 후 실행할 함수
    private Action<BoonData> selectedCallback;

    // 선택을 취소했을 때 실행할 함수
    private Action cancelledCallback;

    private Coroutine selectionRoutine;

    private bool isOpen;
    private bool isSelecting;

    private float previousTimeScale = 1f;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockMode;

    public bool IsOpen
    {
        get
        {
            return isOpen;
        }
    }

    private void Awake()
    {
        ValidateReferences();

        /*
         * 게임 시작 시 보상 UI는 꺼진 상태로 둔다.
         *
         * 주의:
         * 이 스크립트는 rewardCanvas 안이 아니라
         * 항상 켜져 있는 별도 오브젝트에 붙여야 한다.
         */
        if (rewardCanvas != null)
        {
            rewardCanvas.SetActive(false);
        }

        isOpen = false;
        isSelecting = false;
    }

    /// <summary>
    /// E키 상호작용으로 호출한다.
    /// 전달받은 득도 후보를 슬롯에 표시한다.
    /// </summary>
    public bool Open(
        List<BoonData> choices,
        Action<BoonData> onSelected,
        Action onCancelled
    )
    {
        if (isOpen || isSelecting)
        {
            return false;
        }

        if (choices == null ||
            choices.Count == 0)
        {
            Debug.LogWarning(
                "[BoonRewardUI] 표시할 득도 후보가 없습니다.",
                this
            );

            return false;
        }

        if (!ValidateReferences())
        {
            return false;
        }

        selectedCallback = onSelected;
        cancelledCallback = onCancelled;

        /*
         * UI를 열기 전 현재 게임과 커서 상태를 저장한다.
         */
        previousTimeScale =
            Time.timeScale;

        previousCursorVisible =
            Cursor.visible;

        previousCursorLockMode =
            Cursor.lockState;

        /*
         * 슬롯들의 부모가 비활성 상태이므로
         * 슬롯 Setup보다 먼저 전체 UI를 켠다.
         */
        rewardCanvas.SetActive(true);

        int visibleCount =
            Mathf.Min(
                choices.Count,
                rewardSlots.Length
            );

        for (int i = 0;
             i < rewardSlots.Length;
             i++)
        {
            BoonRewardSlot slot =
                rewardSlots[i];

            if (slot == null)
            {
                Debug.LogError(
                    $"[BoonRewardUI] Reward Slots의 Element {i}가 비어 있습니다.",
                    this
                );

                continue;
            }

            if (i < visibleCount)
            {
                /*
                 * 후보 0 → decide1
                 * 후보 1 → decide2
                 * 후보 2 → decide3
                 */
                slot.Setup(
                    choices[i],
                    HandleSlotSelected
                );
            }
            else
            {
                slot.Clear();
            }
        }

        isOpen = true;
        isSelecting = false;

        if (pauseGameWhileOpen)
        {
            Time.timeScale = 0f;
        }

        Cursor.visible = true;
        Cursor.lockState =
            CursorLockMode.None;

        return true;
    }

    /// <summary>
    /// 보상 슬롯을 클릭했을 때
    /// BoonRewardSlot에서 호출된다.
    /// </summary>
    private void HandleSlotSelected(
        BoonRewardSlot selectedSlot,
        BoonData selectedBoon
    )
    {
        if (!isOpen ||
            isSelecting ||
            selectedSlot == null ||
            selectedBoon == null)
        {
            return;
        }

        isSelecting = true;

        /*
         * 중복 클릭이나 중복 코루틴 실행을 방지한다.
         */
        if (selectionRoutine != null)
        {
            StopCoroutine(selectionRoutine);
        }

        selectionRoutine =
            StartCoroutine(
                SelectionRoutine(
                    selectedSlot,
                    selectedBoon
                )
            );
    }

    /// <summary>
    /// 선택되지 않은 슬롯을 즉시 숨기고,
    /// 선택한 슬롯만 잠시 남긴다.
    /// </summary>
    private IEnumerator SelectionRoutine(
        BoonRewardSlot selectedSlot,
        BoonData selectedBoon
    )
    {
        /*
         * 선택하지 않은 두 슬롯은 바로 OFF한다.
         */
        for (int i = 0;
             i < rewardSlots.Length;
             i++)
        {
            BoonRewardSlot slot =
                rewardSlots[i];

            if (slot == null ||
                slot == selectedSlot)
            {
                continue;
            }

            slot.HideImmediately();
        }

        /*
         * 선택한 슬롯은 투명도 반복을 멈추고
         * Alpha 1 상태로 유지한다.
         */
        selectedSlot.ShowSelectedEffect();

        /*
         * Time.timeScale이 0이어도 기다릴 수 있도록
         * WaitForSecondsRealtime을 사용한다.
         */
        if (selectedHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                selectedHoldDuration
            );
        }

        /*
         * CloseImmediately 안에서 콜백이 null로 초기화되므로
         * 닫기 전에 지역 변수로 저장한다.
         */
        Action<BoonData> callback =
            selectedCallback;

        CloseImmediately();

        /*
         * UI를 닫은 뒤 선택한 득도를 적용한다.
         */
        callback?.Invoke(selectedBoon);

        selectionRoutine = null;
    }

    /// <summary>
    /// 닫기 버튼이나 ESC 처리에 연결할 수 있다.
    /// </summary>
    public void Cancel()
    {
        if (!isOpen ||
            isSelecting)
        {
            return;
        }

        Action callback =
            cancelledCallback;

        CloseImmediately();

        callback?.Invoke();
    }

    /// <summary>
    /// 보상 UI를 즉시 닫고
    /// 게임과 커서 상태를 복원한다.
    /// </summary>
    private void CloseImmediately()
    {
        isOpen = false;
        isSelecting = false;

        if (pauseGameWhileOpen)
        {
            Time.timeScale =
                previousTimeScale;
        }

        Cursor.visible =
            previousCursorVisible;

        Cursor.lockState =
            previousCursorLockMode;

        /*
         * 각 슬롯의 이미지와 텍스트를 비운다.
         */
        if (rewardSlots != null)
        {
            for (int i = 0;
                 i < rewardSlots.Length;
                 i++)
            {
                if (rewardSlots[i] != null)
                {
                    rewardSlots[i].Clear();
                }
            }
        }

        if (rewardCanvas != null)
        {
            rewardCanvas.SetActive(false);
        }

        selectedCallback = null;
        cancelledCallback = null;
    }

    /// <summary>
    /// 인스펙터 연결 상태를 검사한다.
    /// </summary>
    private bool ValidateReferences()
    {
        bool valid = true;

        if (rewardCanvas == null)
        {
            Debug.LogError(
                "[BoonRewardUI] Reward Canvas가 연결되지 않았습니다.",
                this
            );

            valid = false;
        }

        if (rewardSlots == null ||
            rewardSlots.Length == 0)
        {
            Debug.LogError(
                "[BoonRewardUI] Reward Slots가 비어 있습니다.",
                this
            );

            return false;
        }

        for (int i = 0;
             i < rewardSlots.Length;
             i++)
        {
            if (rewardSlots[i] != null)
            {
                continue;
            }

            Debug.LogError(
                $"[BoonRewardUI] Reward Slots의 Element {i}가 비어 있습니다.",
                this
            );

            valid = false;
        }

        return valid;
    }

    private void OnDisable()
    {
        /*
         * 관리 오브젝트가 예상치 못하게 꺼져도
         * 게임 시간이 멈춘 상태로 남지 않게 한다.
         */
        if (!isOpen)
        {
            return;
        }

        if (pauseGameWhileOpen)
        {
            Time.timeScale =
                previousTimeScale;
        }

        Cursor.visible =
            previousCursorVisible;

        Cursor.lockState =
            previousCursorLockMode;

        isOpen = false;
        isSelecting = false;
    }

    private void OnDestroy()
    {
        if (selectionRoutine != null)
        {
            StopCoroutine(selectionRoutine);
            selectionRoutine = null;
        }

        if (isOpen &&
            pauseGameWhileOpen)
        {
            Time.timeScale =
                previousTimeScale;
        }
    }
}
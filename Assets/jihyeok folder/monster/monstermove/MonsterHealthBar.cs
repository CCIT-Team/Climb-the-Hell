using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonsterHealthBar : MonoBehaviour
{
    [Header("HP 슬라이더")]
    [SerializeField]
    private Slider hpSlider;

    [Header("숨길 패널")]
    [SerializeField]
    private GameObject monsterPanel;

    [Header("카메라")]
    [SerializeField]
    private Camera targetCamera;

    [Header("표시 시간")]
    [SerializeField]
    private float visibleDuration = 3f;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (monsterPanel != null)
        {
            monsterPanel.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        // HP바가 카메라를 바라보게 함
        transform.forward =
            targetCamera.transform.forward;
    }

    public void SetHealth(
        int currentHp,
        int maxHp
    )
    {
        if (hpSlider == null)
        {
            return;
        }

        hpSlider.maxValue = maxHp;
        hpSlider.value = currentHp;

        if (currentHp <= 0)
        {
            HideHealthBar();
            return;
        }

        ShowHealthBar();
    }

    public void ResetHealthBar(
        int currentHp,
        int maxHp
    )
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }

        HideHealthBar();
    }

    private void ShowHealthBar()
    {
        if (monsterPanel != null)
        {
            monsterPanel.SetActive(true);
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine =
            StartCoroutine(
                HideAfterDelay()
            );
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(
            visibleDuration
        );

        HideHealthBar();
    }

    private void HideHealthBar()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (monsterPanel != null)
        {
            monsterPanel.SetActive(false);
        }
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 몬스터 HP바.
/// 피해를 받으면 표시되고 일정 시간 후 숨겨진다.
/// 몬스터 회전과 관계없이 위치와 방향을 월드 기준으로 유지한다.
/// </summary>
[DefaultExecutionOrder(1000)]
public class MonsterHealthBar : MonoBehaviour
{
    [Header("HP 슬라이더")]
    [SerializeField]
    private Slider hpSlider;

    [Header("숨길 패널")]
    [SerializeField]
    private GameObject monsterPanel;

    [Header("따라갈 몬스터")]
    [Tooltip("몬스터 최상위 오브젝트를 연결하세요.")]
    [SerializeField]
    private Transform followTarget;

    [Header("몬스터 기준 위치")]
    [SerializeField]
    private Vector3 worldOffset =
        new Vector3(0f, 2f, 0f);

    [Header("방향 고정")]
    [SerializeField]
    private Camera targetCamera;

    [Tooltip("게임 시작 시 카메라 방향을 한 번만 가져옵니다.")]
    [SerializeField]
    private bool useCameraRotationAtStart = true;

    [Tooltip("카메라 방향을 사용하지 않을 때 적용할 고정 회전값")]
    [SerializeField]
    private Vector3 fixedWorldEuler =
        Vector3.zero;

    [Tooltip("HP바가 뒤집혀 보이면 체크하세요.")]
    [SerializeField]
    private bool reverseDirection;

    [Tooltip("몬스터의 회전을 상속받지 않도록 부모에서 분리합니다.")]
    [SerializeField]
    private bool detachFromMonster = true;

    [Header("표시 시간")]
    [SerializeField]
    private float visibleDuration = 3f;

    private Coroutine hideCoroutine;
    private Quaternion fixedWorldRotation;

    private void Awake()
    {
        FindFollowTarget();
        SetFixedRotation();

        /*
         * 몬스터 부모의 회전 영향을 받지 않도록 분리한다.
         * 이후 위치는 LateUpdate에서 직접 따라간다.
         */
        if (detachFromMonster)
        {
            transform.SetParent(null, true);
        }

        if (monsterPanel != null)
        {
            monsterPanel.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (followTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        /*
         * 몬스터의 위치만 따라가고,
         * 몬스터의 회전은 따라가지 않는다.
         */
        transform.SetPositionAndRotation(
            followTarget.position + worldOffset,
            fixedWorldRotation
        );
    }

    /// <summary>
    /// 몬스터 체력이 변경될 때 호출한다.
    /// </summary>
    public void SetHealth(
        int currentHp,
        int maxHp
    )
    {
        if (hpSlider == null)
        {
            return;
        }

        int safeMaxHp =
            Mathf.Max(1, maxHp);

        hpSlider.minValue = 0f;
        hpSlider.maxValue = safeMaxHp;

        hpSlider.value =
            Mathf.Clamp(
                currentHp,
                0,
                safeMaxHp
            );

        if (currentHp <= 0)
        {
            HideHealthBar();
            return;
        }

        ShowHealthBar();
    }

    /// <summary>
    /// 몬스터 생성 또는 오브젝트 풀 재사용 시 호출한다.
    /// </summary>
    public void ResetHealthBar(
        int currentHp,
        int maxHp
    )
    {
        if (hpSlider != null)
        {
            int safeMaxHp =
                Mathf.Max(1, maxHp);

            hpSlider.minValue = 0f;
            hpSlider.maxValue = safeMaxHp;

            hpSlider.value =
                Mathf.Clamp(
                    currentHp,
                    0,
                    safeMaxHp
                );
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
            StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(
            visibleDuration
        );

        hideCoroutine = null;

        if (monsterPanel != null)
        {
            monsterPanel.SetActive(false);
        }
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

    private void FindFollowTarget()
    {
        if (followTarget != null)
        {
            return;
        }

        MonsterStats monsterStats =
            GetComponentInParent<MonsterStats>();

        if (monsterStats != null)
        {
            followTarget =
                monsterStats.transform;
        }
    }

    private void SetFixedRotation()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (useCameraRotationAtStart &&
            targetCamera != null)
        {
            fixedWorldRotation =
                targetCamera.transform.rotation;
        }
        else
        {
            fixedWorldRotation =
                Quaternion.Euler(fixedWorldEuler);
        }

        if (reverseDirection)
        {
            fixedWorldRotation *=
                Quaternion.Euler(0f, 180f, 0f);
        }
    }
}
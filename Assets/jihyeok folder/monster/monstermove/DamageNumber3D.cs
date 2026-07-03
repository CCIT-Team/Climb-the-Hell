using TMPro;
using UnityEngine;

/// <summary>
/// 데미지 숫자를 위로 이동시키고
/// 일정 시간이 지나면 제거한다.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class DamageNumber3D : MonoBehaviour
{
    [Header("이동")]
    [Min(0f)]
    [SerializeField]
    private float moveSpeed = 1f;

    [Header("생존 시간")]
    [Min(0.01f)]
    [SerializeField]
    private float lifeTime = 1f;

    [Header("페이드 시작")]
    [Range(0f, 1f)]
    [SerializeField]
    private float fadeStartRatio = 0.5f;

    [Header("카메라")]
    [SerializeField]
    private bool reverseDirection;

    private TextMeshPro damageText;
    private Camera targetCamera;

    private Color originalColor;
    private float elapsedTime;

    private void Awake()
    {
        damageText =
            GetComponent<TextMeshPro>();

        targetCamera =
            Camera.main;
    }

    public void Initialize(
        int damage,
        Color color,
        float scale,
        bool isCritical
    )
    {
        damageText.text =
            damage.ToString();

        damageText.color =
            color;

        damageText.fontStyle =
            isCritical
                ? FontStyles.Bold
                : FontStyles.Normal;

        damageText.alignment =
            TextAlignmentOptions.Center;

        originalColor = color;

        transform.localScale =
            Vector3.one * scale;

        ApplyCameraRotation();
    }

    private void Update()
    {
        elapsedTime +=
            Time.deltaTime;

        transform.position +=
            Vector3.up *
            moveSpeed *
            Time.deltaTime;

        UpdateAlpha();
        ApplyCameraRotation();

        if (elapsedTime >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateAlpha()
    {
        float fadeStartTime =
            lifeTime *
            fadeStartRatio;

        if (elapsedTime <
            fadeStartTime)
        {
            return;
        }

        float fadeDuration =
            lifeTime -
            fadeStartTime;

        float progress =
            fadeDuration > 0f
                ? Mathf.Clamp01(
                    (elapsedTime -
                     fadeStartTime) /
                    fadeDuration
                )
                : 1f;

        Color color =
            originalColor;

        color.a =
            1f - progress;

        damageText.color =
            color;
    }

    private void ApplyCameraRotation()
    {
        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        transform.rotation =
            targetCamera.transform.rotation;

        if (reverseDirection)
        {
            transform.rotation *=
                Quaternion.Euler(
                    0f,
                    180f,
                    0f
                );
        }
    }
}
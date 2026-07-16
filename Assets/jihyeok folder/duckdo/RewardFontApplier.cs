using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기존 보상 UI 코드는 건드리지 않고,
/// 하위 텍스트들의 폰트만 인스펙터에서 지정한 폰트로 바꾼다.
///
/// 사용 위치:
/// - rewardCanvas
/// - 또는 rewardCanvas를 자식으로 가진 항상 켜져 있는 매니저 오브젝트
/// </summary>
public class RewardFontApplier : MonoBehaviour
{
    [Header("적용 대상")]

    [Tooltip("폰트를 적용할 최상위 오브젝트. 비워두면 이 스크립트가 붙은 오브젝트 아래에서 찾는다.")]
    [SerializeField]
    private GameObject targetRoot;

    [Header("TextMeshPro 폰트")]

    [Tooltip("TextMeshPro 텍스트에 TMP Font Asset을 적용할지 여부")]
    [SerializeField]
    private bool applyTmpFont = true;

    [Tooltip("TextMeshPro용 TMP Font Asset. 예: NotoSansKR SDF, Pretendard SDF")]
    [SerializeField]
    private TMP_FontAsset tmpFont;

    [Header("기본 UI Text 폰트")]

    [Tooltip("Unity 기본 UI Text에 Font를 적용할지 여부")]
    [SerializeField]
    private bool applyLegacyFont = true;

    [Tooltip("UnityEngine.UI.Text용 일반 Font. TMP가 아니라 기본 Text일 때 사용")]
    [SerializeField]
    private Font legacyFont;

    [Header("자동 적용")]

    [Tooltip("오브젝트가 켜질 때마다 폰트를 다시 적용한다.")]
    [SerializeField]
    private bool applyOnEnable = true;

    [Tooltip("게임 시작 시 한 번 폰트를 적용한다.")]
    [SerializeField]
    private bool applyOnAwake = true;

    private void Awake()
    {
        if (applyOnAwake)
        {
            ApplyFonts();
        }
    }

    private void OnEnable()
    {
        if (applyOnEnable)
        {
            ApplyFonts();
        }
    }

    /// <summary>
    /// 버튼 OnClick이나 다른 코드에서 직접 호출해도 된다.
    /// </summary>
    public void ApplyFonts()
    {
        GameObject root =
            GetRootObject();

        if (root == null)
        {
            Debug.LogWarning(
                "[RewardFontApplier] 폰트를 적용할 대상이 없습니다.",
                this
            );

            return;
        }

        ApplyTmpFonts(root);
        ApplyLegacyFonts(root);
    }

    private GameObject GetRootObject()
    {
        if (targetRoot != null)
        {
            return targetRoot;
        }

        return gameObject;
    }

    private void ApplyTmpFonts(
        GameObject root
    )
    {
        if (!applyTmpFont)
        {
            return;
        }

        if (tmpFont == null)
        {
            return;
        }

        TMP_Text[] texts =
            root.GetComponentsInChildren<TMP_Text>(
                true
            );

        for (int i = 0;
             i < texts.Length;
             i++)
        {
            TMP_Text text =
                texts[i];

            if (text == null)
            {
                continue;
            }

            text.font =
                tmpFont;

            text.ForceMeshUpdate();
        }
    }

    private void ApplyLegacyFonts(
        GameObject root
    )
    {
        if (!applyLegacyFont)
        {
            return;
        }

        if (legacyFont == null)
        {
            return;
        }

        Text[] texts =
            root.GetComponentsInChildren<Text>(
                true
            );

        for (int i = 0;
             i < texts.Length;
             i++)
        {
            Text text =
                texts[i];

            if (text == null)
            {
                continue;
            }

            text.font =
                legacyFont;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ApplyFonts();
        }
    }
#endif
}